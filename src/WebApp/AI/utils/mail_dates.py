from __future__ import annotations

import logging
import re
from datetime import date, datetime, timedelta, timezone

from dateutil import parser as date_parser

logger = logging.getLogger(__name__)


def resolve_mail_summary_date(user_text: str, today: date | None = None) -> date:
    """
    Pick the calendar day the user wants summarized from free text.
    Defaults to today (UTC) when nothing specific is found.
    """

    text = (user_text or "").strip().lower()
    anchor = today or datetime.now(timezone.utc).date()

    if not text:
        return anchor

    if "yesterday" in text:
        return anchor - timedelta(days=1)

    if "today" in text:
        return anchor

    if "tomorrow" in text:
        return anchor + timedelta(days=1)

    iso_match = re.search(
        r"\b(20\d{2}-\d{2}-\d{2}|20\d{2}/\d{2}/\d{2}|\d{1,2}/\d{1,2}/20\d{2})\b", text
    )
    if iso_match:
        raw = iso_match.group(1)
        try:
            parsed = date_parser.parse(raw, fuzzy=False).date()
            return parsed
        except (ValueError, TypeError, OverflowError):
            logger.info("Could not parse date token %r; falling back to today", raw)

    month_names = (
        "january",
        "february",
        "march",
        "april",
        "may",
        "june",
        "july",
        "august",
        "september",
        "october",
        "november",
        "december",
    )
    for i, name in enumerate(month_names, start=1):
        if name in text:
            m = re.search(rf"{name}\s+(\d{{1,2}})(?:,?\s+(20\d{{2}}))?", text)
            if m:
                day = int(m.group(1))
                year = int(m.group(2)) if m.group(2) else anchor.year
                try:
                    return date(year, i, day)
                except ValueError:
                    pass

    return anchor
