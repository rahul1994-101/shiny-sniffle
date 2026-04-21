from __future__ import annotations

import logging
from typing import Optional

from langchain_openai import AzureChatOpenAI

from utils.environment_helpers import (
    get_azure_openai_api_key,
    get_azure_openai_api_version,
    get_azure_openai_deployment_name,
    get_azure_openai_endpoint,
)

logger = logging.getLogger(__name__)


def get_azure_chat() -> Optional[AzureChatOpenAI]:
    """
    Azure OpenAI / Azure AI Foundry chat client.
    Values come from utils.environment_helpers (see registry there).
    """

    endpoint = get_azure_openai_endpoint()
    key = get_azure_openai_api_key()
    deployment = get_azure_openai_deployment_name()
    if not (endpoint and key and deployment):
        logger.info("Azure OpenAI env not fully set; LLM summarization disabled")
        return None

    api_version = get_azure_openai_api_version()
    return AzureChatOpenAI(
        azure_endpoint=endpoint,
        azure_deployment=deployment,
        api_key=key,
        api_version=api_version,
        temperature=0.2,
    )
