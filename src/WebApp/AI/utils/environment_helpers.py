import os
import logging

from dotenv import load_dotenv

# from utils.cryptography_helpers import Encryption

logger = logging.getLogger(__name__)

# Check if we're in development
if os.getenv("ENVIRONMENT", "development") == "development":
    load_dotenv()


def get_env(key, default=None, encrypted=False):
    value = os.getenv(key, default)

    # If no default is provided and the env var is not set, raise an error
    if value is None:
        logger.error(f"[ENV] Required environment variable '{key}' is not set")
        raise ValueError(f"Environment variable '{key}' not set")

    # Decrypt the value if it's marked as encrypted
    # if encrypted:
    #    # encrypted = Encryption.encrypt(value)
    #    # logger.info(f"[ENV] Encrypted value for '{key}': {encrypted}")
    #    value = Encryption.decrypt(value)

    return value


# =============================================================================
# Application environment registry (read via get_env / helpers below)
# =============================================================================

# Core / hosting
#   ENVIRONMENT                 Optional. development | production. When
#                               development, load_dotenv() runs at import.#


# Streaming
def get_stream_timeout_minutes() -> int:
    return int(get_env("STREAM_TIMEOUT_MINUTES", default="30", encrypted=False))


# Mail (Gmail API)
#   GMAIL_ACCESS_TOKEN          Optional. OAuth access token with  [https://www.googleapis.com/auth/gmail.readonly]
def get_gmail_access_token() -> str:
    return str(get_env("GMAIL_ACCESS_TOKEN", default="", encrypted=False)).strip()


# Azure OpenAI / Azure AI Foundry (LLM)
#   AZURE_OPENAI_ENDPOINT       Optional. e.g. https://<resource>.openai.azure.com/
def get_azure_openai_endpoint() -> str:
    return str(get_env("AZURE_OPENAI_ENDPOINT", default="", encrypted=False)).strip()


#   AZURE_OPENAI_API_KEY        Optional.
def get_azure_openai_api_key() -> str:
    return str(get_env("AZURE_OPENAI_API_KEY", default="", encrypted=False)).strip()


#   AZURE_OPENAI_DEPLOYMENT_NAME Optional. Chat deployment name in Foundry.
def get_azure_openai_deployment_name() -> str:
    return str(
        get_env("AZURE_OPENAI_DEPLOYMENT_NAME", default="", encrypted=False)
    ).strip()


#   AZURE_OPENAI_API_VERSION    Optional. Default 2024-06-01 if unset.
def get_azure_openai_api_version() -> str:
    return str(
        get_env("AZURE_OPENAI_API_VERSION", default="2024-06-01", encrypted=False)
    ).strip()


# Gmail token file (JSON) + OAuth client for refresh
#   GMAIL_TOKENS_FILE_PATH      Optional. Default data/gmail_tokens.json (relative to cwd).
#   GMAIL_OAUTH_CLIENT_ID       Optional. Google OAuth client id (needed to refresh tokens).
#   GMAIL_OAUTH_CLIENT_SECRET   Optional. Google OAuth client secret (needed to refresh).


def get_gmail_tokens_file_path() -> str:
    return str(
        get_env("GMAIL_TOKENS_FILE_PATH", default="data/gmail_tokens.json", encrypted=False)
    ).strip()


def get_gmail_oauth_client_id() -> str:
    return str(get_env("GMAIL_OAUTH_CLIENT_ID", default="", encrypted=False)).strip()


def get_gmail_oauth_client_secret() -> str:
    return str(get_env("GMAIL_OAUTH_CLIENT_SECRET", default="", encrypted=False)).strip()


# =============================================================================