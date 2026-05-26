import asyncio
import logging
import json
from functools import wraps


def worker_behavior(func):
    name = f"{func.__module__}.{func.__name__}"
    is_coro = asyncio.iscoroutinefunction(func)

    if is_coro:

        @wraps(func)
        async def awrapper(*args, **kwargs):
            logging.info(f"# {name}")
            try:
                result = await func(*args, **kwargs)
                return {"hasError": False, "errors": None, "payload": result}
            except Exception as e:
                error_msg = f"{type(e).__name__}: {str(e)}"
                logging.error(f"# Error: {error_msg}")
                return {"hasError": True, "errors": error_msg, "payload": None}

        return awrapper
    else:

        @wraps(func)
        def swrapper(*args, **kwargs):
            logging.info(f"# {name}")
            try:
                result = func(*args, **kwargs)
                return {"hasError": False, "errors": None, "payload": result}
            except Exception as e:
                error_msg = f"{type(e).__name__}: {str(e)}"
                logging.error(f"# Error: {error_msg}")
                return {"hasError": True, "errors": error_msg, "payload": None}

        return swrapper


def service_behavior(func):

    @wraps(func)
    def wrapper(*args, **kwargs):
        name = f"{func.__module__}.{func.__name__}"

        logging.info(f"# {name}")
        try:
            result = func(*args, **kwargs)
            return {"hasError": False, "errors": None, "payload": result}
        except Exception as e:
            error_msg = f"{type(e).__name__}: {str(e)}"
            logging.error(f"# Error: {error_msg}")
            return {"hasError": True, "errors": error_msg, "payload": None}

    return wrapper


def repository_behavior(func):

    @wraps(func)
    def wrapper(*args, **kwargs):
        name = f"{func.__module__}.{func.__name__}"

        logging.info(f"# {name}")

        result = func(*args, **kwargs)
        return result

    return wrapper


def _safe_serialize(data):
    try:
        return json.dumps(data, default=str)
    except Exception as e:
        return f"Unserializable: {e}"
