-- =====================================================
-- CHAT THREAD TABLE
-- =====================================================
-- Per-user conversation threads (sidebar + agent selection).
-- Not workspace reference data or workflow rules.
--
-- Business Rules:
-- - Each chat thread belongs to exactly one user
-- - Apply after dbo.User ([chat] schema must already exist)
-- - All records include audit fields for tracking changes
-- =====================================================
GO

CREATE TABLE [chat].[ChatThread] (
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [userId]                                UNIQUEIDENTIFIER NOT NULL,

    [title]                                 NVARCHAR(200) NOT NULL,
    [chatAgent]                             INT NOT NULL DEFAULT 0,

    [memorySummary]                         NVARCHAR(MAX) NULL,
    [memorySummaryThroughMessageId]         UNIQUEIDENTIFIER NULL,

    [isActive]                               BIT DEFAULT 1,
    [isDeleted]                              BIT DEFAULT 0,

    [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [FK_ChatThread_User] FOREIGN KEY ([userId]) REFERENCES [dbo].[User] ([id])
);
GO

CREATE INDEX [IX_ChatThread_UserId] ON [chat].[ChatThread] ([userId]) WHERE [isDeleted] = 0;
GO

-- No default rows; threads are created in Chat UI.
-- GO
