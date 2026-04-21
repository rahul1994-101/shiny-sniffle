import logging
import os
from fastapi import FastAPI
from fastapi.exceptions import RequestValidationError


# Api Endpoints
from api.endpoints import router as endpoints_router


# Utilities
from utils.api_helpers import (
    RequestLoggerMiddleware,
    global_exception_handler,
    validation_exception_handler,
)


# Logger
logging.basicConfig(
    level=logging.INFO,
    # format="%(asctime)s | %(levelname)-5s | %(filename)-15s | %(message)s",
    format="%(asctime)s | %(levelname)-7s | %(message)s",
    datefmt="%Y-%m-%d %H:%M:%S",
)
logging.getLogger("uvicorn.error").disabled = True
logging.getLogger("uvicorn.access").disabled = True


# Get the root path from environment variable (set by Azure App Service when behind Application Gateway)
root_path = os.getenv("ROOT_PATH", "")

app = FastAPI(
    title="Agentic API",
    version="1.0.0",
    root_path=root_path,
    # Explicitly set the OpenAPI URL paths
    openapi_url="/openapi.json",
    docs_url="/docs",
    redoc_url="/redoc",
)


# Register middlewares
app.add_middleware(RequestLoggerMiddleware)


# Register exception handlers
app.exception_handler(Exception)(global_exception_handler)
app.exception_handler(RequestValidationError)(validation_exception_handler)


# Register routers
app.include_router(endpoints_router)
