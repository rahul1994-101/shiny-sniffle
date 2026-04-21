import asyncio

from mail_graph.runner import run_mail_agent_sync
from utils.behaviour_pipelines import service_behavior
from utils.gmail_token_store import save_user_tokens
from utils.streaming_helpers import ProgressFn


# -----------------------------------------------
# Wrappers over Service Methods for Streaming
# -----------------------------------------------
async def stream_normal_endpoint(name: str, progress: ProgressFn):
    await asyncio.sleep(0)
    response = await asyncio.to_thread(get_normal_endpoint, name)

    return response


async def stream_mail_agent_chat(payload: dict, progress: ProgressFn):
    message = (payload or {}).get("message") or ""
    user_email = (payload or {}).get("user_email")
    await asyncio.sleep(0)

    progress("Resolving the summary day from your message…")
    progress("Fetching Gmail metadata for that UTC day…")
    progress("Summarizing with the configured Azure model (if available)…")

    response = await asyncio.to_thread(get_mail_agent_chat, message, user_email)

    return response


# -------------------------------
# Service Methods
# -------------------------------
@service_behavior
def get_normal_endpoint(name: str):

    return f"Hi {name}, this is a normal endpoint response from the service layer!"


@service_behavior
def get_mail_agent_chat(message: str, user_email: str | None = None):

    return run_mail_agent_sync(message, user_email)


@service_behavior
def store_gmail_tokens(
    email: str,
    refresh_token: str | None = None,
    access_token: str | None = None,
    expires_in_seconds: int | None = None,
):

    return save_user_tokens(
        email,
        refresh_token=refresh_token,
        access_token=access_token,
        expires_in_seconds=expires_in_seconds,
    )


# -------------------------------
# Private Helper Functions
# -------------------------------
