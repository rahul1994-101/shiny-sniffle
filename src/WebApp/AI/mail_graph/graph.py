from __future__ import annotations

import logging
from typing import Any, TypedDict

from langgraph.graph import END, StateGraph
from langchain_core.messages import HumanMessage, SystemMessage

from mail_graph.azure_llm import get_azure_chat
from mail_graph.gmail_access import fetch_mail_for_day
from utils.mail_dates import resolve_mail_summary_date

logger = logging.getLogger(__name__)


class MailAgentState(TypedDict, total=False):
    user_message: str
    user_email: str | None
    target_date_iso: str
    mail_items: list[dict[str, Any]]
    fetch_error: str | None
    final_reply: str


def _node_resolve_date(state: MailAgentState) -> dict[str, Any]:
    text = state.get("user_message") or ""
    day = resolve_mail_summary_date(text)
    return {"target_date_iso": day.isoformat()}


def _node_fetch_mail(state: MailAgentState) -> dict[str, Any]:
    target = state.get("target_date_iso")
    if not target:
        return {"mail_items": [], "fetch_error": "missing_target_date"}

    try:
        who = state.get("user_email")
        items = fetch_mail_for_day(target, user_email=who)
        return {"mail_items": items, "fetch_error": None}
    except Exception as ex:
        logger.exception("fetch_mail failed")
        return {"mail_items": [], "fetch_error": f"{type(ex).__name__}: {ex}"}


def _format_mail_context(items: list[dict[str, Any]]) -> str:
    lines: list[str] = []
    for i, m in enumerate(items, start=1):
        subj = m.get("subject") or "(no subject)"
        sender = m.get("from") or ""
        snip = (m.get("snippet") or "").replace("\n", " ").strip()
        lines.append(f"{i}. Subject: {subj}\n   From: {sender}\n   Snippet: {snip}")
    return "\n".join(lines) if lines else "(no messages in window)"


def _node_summarize(state: MailAgentState) -> dict[str, Any]:
    user_message = state.get("user_message") or ""
    target = state.get("target_date_iso") or ""
    items = state.get("mail_items") or []
    fetch_error = state.get("fetch_error")

    mail_block = _format_mail_context(items)
    llm = get_azure_chat()

    if fetch_error:
        mail_block = (
            f"(Mail fetch failed: {fetch_error})\n\n"
            f"Partial or empty context follows:\n{mail_block}"
        )

    if llm is None:
        reply = (
            f"**Mail window:** {target} (UTC day)\n\n"
            f"**Inbox snippets (metadata only):**\n{mail_block}\n\n"
            "_Azure OpenAI is not configured (set AZURE_OPENAI_ENDPOINT, "
            "AZURE_OPENAI_API_KEY, AZURE_OPENAI_DEPLOYMENT_NAME). "
            "This is a deterministic preview of fetched mail only._"
        )
        return {"final_reply": reply.strip()}

    system = SystemMessage(
        content=(
            "You are an expert email triage assistant. Input: the user's question plus "
            "Gmail metadata (subject, from, snippet) for one UTC calendar day—snippets only, "
            "not full bodies.\n\n"
            "Write a clear, scannable answer in GitHub-flavored Markdown:\n"
            "- Start with a short **Executive summary** (2–4 sentences): who matters, "
            "what changed, any deadlines implied by subjects/snippets.\n"
            "- Then use `##` section headings only (e.g. `## Needs attention`, "
            "`## FYI / low priority`, `## Newsletters & automated`, `## Threads`). "
            "Avoid `####` and deeper; use **bold** labels and bullet lists instead.\n"
            "- Under each section use `-` bullets. Per important message include "
            "**Subject**, **From**, and a one-line **Why it matters** tied to the snippet.\n"
            "- Explicitly separate **Likely human** vs **Likely automated / marketing** "
            "when you can tell from From/subject/snippet.\n"
            "- Call out **Action items** and **Time-sensitive** items if subjects/snippets "
            "suggest replies, meetings, payments, or deadlines.\n"
            "- If the user asks for full bodies, attachments, or sending mail, say you "
            "only have snippets for that day and what they should do next.\n"
            "- If the list is empty or fetch failed, say so plainly and avoid inventing mail.\n"
            "Keep total length proportional to inbox volume; prefer clarity over exhaustiveness."
        )
    )
    n = len(items)
    human = HumanMessage(
        content=(
            f"User request:\n{user_message}\n\n"
            f"Target day (ISO date, UTC window): {target}\n"
            f"Message count in window: {n}\n\n"
            f"Messages:\n{mail_block}"
        )
    )

    resp = llm.invoke([system, human])
    content = getattr(resp, "content", None) or str(resp)
    return {"final_reply": str(content).strip()}


_compiled = None


def get_mail_agent_graph():
    global _compiled
    if _compiled is not None:
        return _compiled

    graph = StateGraph(MailAgentState)
    graph.add_node("resolve_date", _node_resolve_date)
    graph.add_node("fetch_mail", _node_fetch_mail)
    graph.add_node("summarize", _node_summarize)
    graph.set_entry_point("resolve_date")
    graph.add_edge("resolve_date", "fetch_mail")
    graph.add_edge("fetch_mail", "summarize")
    graph.add_edge("summarize", END)
    _compiled = graph.compile()
    return _compiled
