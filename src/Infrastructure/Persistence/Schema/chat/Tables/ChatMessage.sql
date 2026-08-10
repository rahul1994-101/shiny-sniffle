-- =====================================================
-- CHAT MESSAGE TABLE
-- =====================================================
-- Messages within a chat thread (MAF roles: user, assistant, system, tool).
-- Apply after chat/Tables/ChatThread.sql
-- =====================================================
GO

CREATE TABLE [chat].[ChatMessage] (
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [chatThreadId]                          UNIQUEIDENTIFIER NOT NULL,

    [role]                                  NVARCHAR(20) NOT NULL DEFAULT N'user',
    [content]                               NVARCHAR(MAX) NOT NULL,

    [isActive]                               BIT DEFAULT 1,
    [isDeleted]                              BIT DEFAULT 0,

    [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [FK_ChatMessage_ChatThread] FOREIGN KEY ([chatThreadId]) REFERENCES [chat].[ChatThread] ([id])
);
GO

CREATE INDEX [IX_ChatMessage_ChatThreadId] ON [chat].[ChatMessage] ([chatThreadId]) WHERE [isDeleted] = 0;
GO

-- No default rows; messages are created per thread.
-- GO
