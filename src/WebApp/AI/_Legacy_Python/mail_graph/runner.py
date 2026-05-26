from __future__ import annotations

import logging

from mail_graph.graph import get_mail_agent_graph

logger = logging.getLogger(__name__)


def run_mail_agent_sync(message: str, user_email: str | None = None) -> str:
    """
    Run the compiled mail graph synchronously and return assistant text.
    """

    text = (message or "").strip()
    if not text:
        return ""

    app = get_mail_agent_graph()
    payload: dict = {"user_message": text}
    if user_email and str(user_email).strip():
        payload["user_email"] = str(user_email).strip()
    out = app.invoke(payload)
    reply = (out or {}).get("final_reply") or ""
    return str(reply).strip()
