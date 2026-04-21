from __future__ import annotations
import asyncio
import json
import logging
from typing import Any, Callable, Awaitable
from fastapi import Request
from fastapi.responses import StreamingResponse
from fastapi.encoders import jsonable_encoder

from utils.environment_helpers import get_stream_timeout_minutes

logger = logging.getLogger(__name__)

# Types for your workers
ProgressFn = Callable[[Any], None]
WorkerFn = Callable[[Any, ProgressFn], Awaitable[Any]]


def _nd(obj: Any) -> str:
    # """Encode one NDJSON line."""
    # return json.dumps(obj, default=str) + "\n"

    # Make it JSON-safe the same way FastAPI would
    safe = jsonable_encoder(obj)
    return json.dumps(safe, ensure_ascii=False) + "\n"


async def run_with_progress(
    request: Request, worker: WorkerFn, payload: Any, heartbeat: int = 60
) -> StreamingResponse:
    """
    Run `worker(payload, progress)` and stream NDJSON to the client:
      - {"type":"start"}
      - progress events pushed by the worker via `progress(...)`
      - heartbeat {"type":"progress","msg":"tick"} every `heartbeat` seconds when idle
      - {"type":"final","result": ...} when done
      - {"type":"error","msg": "..."} if something blows up
    """

    async def gen():
        # Get timeout from environment variable
        timeout_minutes = get_stream_timeout_minutes()
        timeout_seconds = timeout_minutes * 60

        # queue for progress events emitted by the worker
        q: asyncio.Queue[dict] = asyncio.Queue()

        # Progress function passed to the worker
        def progress(evt: Any) -> None:
            """Worker calls this to report progress; accepts dict or string."""
            if isinstance(evt, str):
                evt = {"type": "progress", "msg": evt}
            elif isinstance(evt, dict) and "type" not in evt:
                evt = {"type": "progress", **evt}
            try:
                q.put_nowait(evt)
            except asyncio.QueueFull:
                pass

        # Start the worker
        task = asyncio.create_task(worker(payload, progress))
        yield _nd({"type": "progress", "msg": "Starting.."})

        # Create timeout task with environment-based timeout
        timeout_task = asyncio.create_task(asyncio.sleep(timeout_seconds))

        # Main loop with heartbeats, q.get(), etc.
        try:
            while True:
                # Stop if client disconnected
                if await request.is_disconnected():
                    logger.info("client disconnected; canceling worker")
                    task.cancel()
                    timeout_task.cancel()
                    try:
                        yield _nd({"type": "error", "msg": "Client disconnected"})
                    except GeneratorExit:
                        pass
                    break

                # Wait for either: task completion, a progress item, timeout, or heartbeat timeout
                wait_for = {task, asyncio.create_task(q.get()), timeout_task}

                done, _ = await asyncio.wait(
                    wait_for, timeout=heartbeat, return_when=asyncio.FIRST_COMPLETED
                )

                if task in done:
                    try:
                        result = task.result()
                    except Exception as ex:
                        # Worker blew up AND wasn't wrapped by behavior.
                        envelope = {
                            "hasError": True,
                            "errors": f"{type(ex).__name__}: {ex}",
                            "payload": None,
                        }
                        yield _nd({"type": "final", "result": envelope})
                        break

                    # If worker is decorated, it already has the envelope-pass through.
                    if isinstance(result, dict) and {
                        "hasError",
                        "errors",
                        "payload",
                    } <= set(result.keys()):
                        envelope = result
                    else:
                        # Back-compat: wrap undecorated worker results.
                        envelope = {
                            "hasError": False,
                            "errors": None,
                            "payload": result,
                        }

                    # yield _nd({"type": "final", **envelope})
                    yield _nd({"type": "final", "result": envelope})
                    break

                # Check if timeout occurred
                if timeout_task in done:
                    logger.warning(
                        f"Stream timeout after {timeout_minutes} minutes; canceling worker"
                    )
                    task.cancel()
                    # Use existing error response format for timeout
                    envelope = {
                        "hasError": True,
                        "errors": f"Stream timed out after {timeout_minutes} minutes",
                        "payload": None,
                    }
                    yield _nd({"type": "final", "result": envelope})
                    break

                progress_tasks = [d for d in done if d is not task]
                try:
                    if progress_tasks:
                        evt = progress_tasks[0].result()
                        yield _nd(evt)
                    else:
                        yield _nd({"type": "progress", "msg": "tick"})
                except GeneratorExit:
                    raise
                except BaseException:
                    try:
                        yield _nd({"type": "error", "msg": "Stream interrupted"})
                    except GeneratorExit:
                        pass
                    raise

                await asyncio.sleep(0)

        # Client likely disconnected; this is transport-level, no final possible.
        except asyncio.CancelledError:
            task.cancel()
            timeout_task.cancel()
            try:
                yield _nd({"type": "error", "msg": "Client disconnected"})
            except GeneratorExit:
                pass
            raise

        # Transport/runtime failure: emit error (not final) because we don't control state.
        except Exception as ex:
            logger.exception("streaming failed: %s", ex)
            timeout_task.cancel()
            yield _nd({"type": "error", "msg": str(ex)})

    # return StreamingResponse(gen(), media_type="application/x-ndjson")
    return StreamingResponse(
        gen(),
        media_type="application/x-ndjson",
        headers={
            "Cache-Control": "no-transform",
            "Connection": "keep-alive",
            "Content-Encoding": "identity",
            "Transfer-Encoding": "chunked",
        },
    )
