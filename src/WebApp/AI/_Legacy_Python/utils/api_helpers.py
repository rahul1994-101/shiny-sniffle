import logging
from starlette.requests import Request
from starlette.middleware.base import BaseHTTPMiddleware
from fastapi.responses import JSONResponse
from fastapi.exceptions import RequestValidationError
from starlette.status import (
    HTTP_500_INTERNAL_SERVER_ERROR,
    HTTP_422_UNPROCESSABLE_ENTITY,
)
from typing import Dict, Any, List, Tuple

logger = logging.getLogger(__name__)


class RequestLoggerMiddleware(BaseHTTPMiddleware):
    async def dispatch(self, request: Request, call_next):
        response = await call_next(request)
        return response


async def global_exception_handler(request: Request, exc: Exception):
    error_msg = f"{type(exc).__name__}: {str(exc)}"
    logger.error(
        f"[EXCEPTION] {request.method} {request.url.path} - {error_msg}", exc_info=True
    )
    return JSONResponse(
        status_code=HTTP_500_INTERNAL_SERVER_ERROR,
        content={
            "hasError": True,
            "errors": error_msg,  # "Something went wrong"}
            "payload": None,
        },
    )


async def validation_exception_handler(request: Request, exc: RequestValidationError):
    error_msg = exc.errors()
    logger.warning(
        f"[VALIDATION] {request.method} {request.url.path} - "
        f"Validation errors: {error_msg}"
    )
    return JSONResponse(
        status_code=HTTP_422_UNPROCESSABLE_ENTITY,
        content={"hasError": True, "errors": error_msg, "payload": None},  # ...
    )
