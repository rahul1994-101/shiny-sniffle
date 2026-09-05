-- =====================================================
-- EMAIL THREAD MEMORY TABLE
-- =====================================================
-- Last mailbox list snapshot per Email chat thread + mailbox alias.
-- Lets follow-ups resolve #N / "that Amazon one" across turns
-- without overwriting another account's list in the same thread.
-- Apply after chat.ChatThread exists.
-- =====================================================
GO

IF OBJECT_ID(N'[chat].[EmailThreadMemory]', N'U') IS NULL
BEGIN
    CREATE TABLE [chat].[EmailThreadMemory] (
        [chatThreadId]      UNIQUEIDENTIFIER NOT NULL,
        [mailboxAlias]      NVARCHAR(64) NOT NULL,
        [userId]            UNIQUEIDENTIFIER NOT NULL,
        [listSnapshotJson]  NVARCHAR(MAX) NOT NULL,
        [updatedAt]         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_EmailThreadMemory] PRIMARY KEY ([chatThreadId], [mailboxAlias]),
        CONSTRAINT [FK_EmailThreadMemory_ChatThread]
            FOREIGN KEY ([chatThreadId]) REFERENCES [chat].[ChatThread] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EmailThreadMemory_User]
            FOREIGN KEY ([userId]) REFERENCES [dbo].[User] ([id])
    );

    CREATE INDEX [IX_EmailThreadMemory_UserId]
        ON [chat].[EmailThreadMemory] ([userId]);
END
GO

IF OBJECT_ID(N'[chat].[EmailThreadMemory]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[chat].[EmailThreadMemory]', N'mailboxAlias') IS NULL
BEGIN
    ALTER TABLE [chat].[EmailThreadMemory]
        ADD [mailboxAlias] NVARCHAR(64) NOT NULL
            CONSTRAINT [DF_EmailThreadMemory_MailboxAlias] DEFAULT (N'');
END
GO

IF OBJECT_ID(N'[chat].[EmailThreadMemory]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[chat].[EmailThreadMemory]', N'mailboxAlias') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.key_constraints kc
        INNER JOIN sys.index_columns ic
            ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
        WHERE kc.parent_object_id = OBJECT_ID(N'[chat].[EmailThreadMemory]')
          AND kc.type = 'PK'
        GROUP BY kc.name
        HAVING COUNT(*) = 1
   )
BEGIN
    UPDATE [chat].[EmailThreadMemory]
    SET [mailboxAlias] = LEFT(LTRIM(RTRIM(JSON_VALUE([listSnapshotJson], '$.mailboxAlias'))), 64)
    WHERE JSON_VALUE([listSnapshotJson], '$.mailboxAlias') IS NOT NULL
      AND LTRIM(RTRIM(JSON_VALUE([listSnapshotJson], '$.mailboxAlias'))) <> N'';

    ALTER TABLE [chat].[EmailThreadMemory] DROP CONSTRAINT [PK_EmailThreadMemory];

    ALTER TABLE [chat].[EmailThreadMemory]
        ADD CONSTRAINT [PK_EmailThreadMemory] PRIMARY KEY ([chatThreadId], [mailboxAlias]);
END
GO
