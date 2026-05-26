import logging

from fastapi import APIRouter, Body, Request

from api.schema import MailChatRequest
from api.service import (
    get_mail_agent_chat,
    get_normal_endpoint,
    stream_mail_agent_chat,
    stream_normal_endpoint,
)
from utils.streaming_helpers import run_with_progress

router = APIRouter()
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


# --------------------------------
# Normal Endpoints
# --------------------------------
@router.post("/get_normal_endpoint")
async def normal_endpoint(name: str):

    return get_normal_endpoint(name)


@router.post("/mail_agent_chat")
async def mail_agent_chat(body: MailChatRequest):

    return get_mail_agent_chat(body.message, body.user_email)


# --------------------------------
# Streaming Endpoints
# --------------------------------
@router.post("/get_normal_endpoint_as_stream", tags=["Health"])
async def normal_endpoint_as_stream(request: Request, name: str):

    return await run_with_progress(request, stream_normal_endpoint, name)


@router.post("/mail_agent_chat_as_stream", tags=["Health"])
async def mail_agent_chat_as_stream(request: Request, body: MailChatRequest):

    return await run_with_progress(
        request,
        stream_mail_agent_chat,
        {"message": body.message, "user_email": body.user_email},
    )


# --------------------------------
# Testing Endpoint
# --------------------------------
@router.get("/health", tags=["Health"])
async def health_check():

    return "Ok"
