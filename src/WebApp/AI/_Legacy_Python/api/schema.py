from pydantic import BaseModel, ConfigDict, Field, field_validator


class MailChatRequest(BaseModel):
    model_config = ConfigDict(str_strip_whitespace=True)

    message: str = Field(..., min_length=1, max_length=20000)
    user_email: str | None = Field(default=None, max_length=320)

    @field_validator("user_email", mode="before")
    @classmethod
    def user_email_blank_as_none(cls, v):
        if v is None:
            return None
        if isinstance(v, str) and not v.strip():
            return None
        return v.strip() if isinstance(v, str) else v
