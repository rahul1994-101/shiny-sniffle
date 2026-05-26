from __future__ import annotations

import calendar
import json
import logging
import os
import tempfile
import threading
import time
from datetime import datetime, timezone
from typing import Any

from google.auth.transport.requests import Request
from google.oauth2.credentials import Credentials

from utils.environment_helpers import (
    get_gmail_oauth_client_id,
    get_gmail_oauth_client_secret,
    get_gmail_tokens_file_path,
)

logger = logging.getLogger(__name__)

_lock = threading.Lock()

GMAIL_READONLY = "https://www.googleapis.com/auth/gmail.readonly"
TOKEN_URI = "https://oauth2.googleapis.com/token"


def _normalize_email(email: str) -> str:
    return (email or "").strip().lower()


def resolved_tokens_file_path() -> str:
    """Absolute path to the Gmail credentials JSON file (creates parent dirs on write only)."""

    path = get_gmail_tokens_file_path()
    if os.path.isabs(path):
        return os.path.normpath(path)
    return os.path.normpath(os.path.join(os.getcwd(), path))


def _resolved_path() -> str:
    return resolved_tokens_file_path()


def _read_all_unlocked() -> dict[str, Any]:
    path = _resolved_path()
    if not os.path.isfile(path):
        return {}
    try:
        with open(path, encoding="utf-8") as f:
            data = json.load(f)
        if isinstance(data, dict):
            return data
    except (json.JSONDecodeError, OSError) as ex:
        logger.warning("Could not read Gmail token file %s: %s", path, ex)
    return {}


def _write_all_unlocked(blob: dict[str, Any]) -> None:
    path = _resolved_path()
    directory = os.path.dirname(path)
    if directory:
        os.makedirs(directory, exist_ok=True)

    raw = json.dumps(blob, indent=2, ensure_ascii=False)
    fd, tmp = tempfile.mkstemp(
        prefix="gmail_tokens_", suffix=".tmp", dir=directory or None, text=True
    )
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            f.write(raw)
            f.flush()
            os.fsync(f.fileno())
        os.replace(tmp, path)
    except Exception:
        try:
            os.unlink(tmp)
        except OSError:
            pass
        raise


def read_all_tokens() -> dict[str, Any]:
    with _lock:
        return dict(_read_all_unlocked())


def _credentials_from_record(
    rec: dict[str, Any],
) -> Credentials | None:
    client_id = get_gmail_oauth_client_id()
    client_secret = get_gmail_oauth_client_secret()
    refresh = (rec.get("refresh_token") or "").strip() or None
    access = (rec.get("access_token") or "").strip() or None
    expires_at = rec.get("expires_at")

    expiry = None
    if expires_at is not None:
        try:
            aware = datetime.fromtimestamp(float(expires_at), tz=timezone.utc)
            # google.auth uses naive UTC for skew comparisons
            expiry = aware.replace(tzinfo=None)
        except (TypeError, ValueError, OSError):
            expiry = None

    if not access and not refresh:
        return None

    return Credentials(
        token=access,
        refresh_token=refresh,
        token_uri=TOKEN_URI,
        client_id=client_id or None,
        client_secret=client_secret or None,
        scopes=[GMAIL_READONLY],
        expiry=expiry,
    )


def get_valid_access_token(user_email: str) -> str:
    """
    Return a usable OAuth access token for Gmail API for this mailbox.
    Refreshes using refresh_token when possible and writes updates back to disk.
    """

    key = _normalize_email(user_email)
    if not key:
        return ""

    with _lock:
        all_data = _read_all_unlocked()
        rec = dict(all_data.get(key) or {})
        if not rec:
            return ""

        creds = _credentials_from_record(rec)
        if creds is None:
            return ""

        try:
            if creds.expired and creds.refresh_token:
                if not (creds.client_id and creds.client_secret):
                    logger.warning(
                        "Gmail token expired for %s; set GMAIL_OAUTH_CLIENT_ID and "
                        "GMAIL_OAUTH_CLIENT_SECRET to enable refresh.",
                        key,
                    )
                    return (creds.token or "").strip()
                creds.refresh(Request())
                rec["access_token"] = creds.token
                if creds.expiry:
                    exp = creds.expiry
                    if exp.tzinfo is None:
                        rec["expires_at"] = float(calendar.timegm(exp.timetuple()))
                    else:
                        rec["expires_at"] = exp.astimezone(timezone.utc).timestamp()
                all_data[key] = rec
                _write_all_unlocked(all_data)
            elif creds.expired and not creds.refresh_token:
                logger.warning(
                    "Gmail access token expired for %s and no refresh_token", key
                )
                return (creds.token or "").strip()

            return (creds.token or "").strip()
        except Exception as ex:
            logger.exception("Failed to refresh Gmail token for %s: %s", key, ex)
            return (rec.get("access_token") or "").strip()
