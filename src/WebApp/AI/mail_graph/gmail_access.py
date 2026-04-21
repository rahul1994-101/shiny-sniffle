from __future__ import annotations

import logging
from datetime import date, datetime, timedelta, timezone
from typing import Any

from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build
from googleapiclient.errors import HttpError

from utils.environment_helpers import get_gmail_access_token
from utils.gmail_token_store import get_valid_access_token

logger = logging.getLogger(__name__)


def _day_bounds_utc(d: date) -> tuple[datetime, datetime]:
    start = datetime(d.year, d.month, d.day, tzinfo=timezone.utc)
    end = start + timedelta(days=1)
    return start, end


def _gmail_query_for_day(d: date) -> str:
    start, end = _day_bounds_utc(d)
    after = start.strftime("%Y/%m/%d")
    before = end.strftime("%Y/%m/%d")
    return f"after:{after} before:{before}"


def _stub_mail_for_day(d: date) -> list[dict[str, Any]]:
    return [
        {
            "id": "stub-1",
            "threadId": "stub-thread",
            "subject": "(Stub) Project sync",
            "from": "alice@example.com",
            "snippet": f"No Gmail token configured - sample mail for {d.isoformat()}.",
            "internalDate": str(int(datetime.now(timezone.utc).timestamp() * 1000)),
        },
        {
            "id": "stub-2",
            "threadId": "stub-thread-2",
            "subject": "(Stub) Shipping notice",
            "from": "orders@example.com",
            "snippet": "Set tokens via POST /gmail/store_tokens (user_email on chat) or GMAIL_ACCESS_TOKEN.",
            "internalDate": str(int(datetime.now(timezone.utc).timestamp() * 1000)),
        },
    ]


def _headers_to_map(headers: list[dict[str, str]] | None) -> dict[str, str]:
    out: dict[str, str] = {}
    if not headers:
        return out
    for h in headers:
        name = (h.get("name") or "").lower()
        value = h.get("value") or ""
        if name:
            out[name] = value
    return out


def _resolve_access_token(user_email: str | None) -> str:
    if user_email and str(user_email).strip():
        t = get_valid_access_token(str(user_email).strip())
        if t:
            return t
    return get_gmail_access_token()


def fetch_mail_for_day(target_iso: str, user_email: str | None = None) -> list[dict[str, Any]]:
    """
    List Gmail messages for the given calendar day (UTC window).
    Token order: file entry for user_email (see /gmail/store_tokens), else
    GMAIL_ACCESS_TOKEN env. When missing, returns stub items.
    """

    token = _resolve_access_token(user_email)
    try:
        day = date.fromisoformat(target_iso)
    except ValueError:
        day = datetime.now(timezone.utc).date()

    if not token:
        logger.info("No Gmail token (file/env); returning stub inbox items")
        return _stub_mail_for_day(day)

    creds = Credentials(token=token)
    service = build("gmail", "v1", credentials=creds, cache_discovery=False)
    query = _gmail_query_for_day(day)

    try:
        listed = (
            service.users()
            .messages()
            .list(userId="me", q=query, maxResults=40)
            .execute()
        )
    except HttpError as ex:
        logger.error("Gmail list failed: %s", ex)
        raise

    ids = [m.get("id") for m in listed.get("messages", []) if m.get("id")]
    items: list[dict[str, Any]] = []

    for msg_id in ids:
        try:
            detail = (
                service.users()
                .messages()
                .get(
                    userId="me",
                    id=msg_id,
                    format="metadata",
                    metadataHeaders=["Subject", "From", "Date"],
                )
                .execute()
            )
        except HttpError as ex:
            logger.warning("Gmail get failed for %s: %s", msg_id, ex)
            continue

        hdrs = _headers_to_map(detail.get("payload", {}).get("headers"))
        items.append(
            {
                "id": detail.get("id"),
                "threadId": detail.get("threadId"),
                "subject": hdrs.get("subject", "(no subject)"),
                "from": hdrs.get("from", "(unknown sender)"),
                "snippet": detail.get("snippet") or "",
                "internalDate": detail.get("internalDate"),
            }
        )

    return items
