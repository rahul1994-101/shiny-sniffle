-- =====================================================
-- BUCKET MEMBER TABLE
-- =====================================================
-- Links a Bucket to a referable workspace object (contact or mailbox).
-- Rows removed when the bucket, referable object, or parent user scope is cleaned up in app.
--
-- Business Rules:
-- - referableKind: 0 = Contact, 1 = Mailbox (ReferableKind in app)
-- - At most one row per (bucketId, referableKind, referableId)
-- - userId matches bucket owner and referable owner (denormalized for scoped queries)
-- - Apply after workspace.Bucket
-- =====================================================
GO

CREATE TABLE [workspace].[BucketMember] (
    -- Primary key with auto-generated sequential UUID
    [id]                                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [userId]                                 UNIQUEIDENTIFIER NOT NULL,                 -- Owner (FK to User)

    -- Data fields
    [bucketId]                               UNIQUEIDENTIFIER NOT NULL,                 -- FK to Bucket
    [referableKind]                          TINYINT NOT NULL,                          -- ReferableKind: Contact = 0, Mailbox = 1
    [referableId]                            UNIQUEIDENTIFIER NOT NULL,                 -- PK of Contact or EmailAccount

    -- Foreign keys
    CONSTRAINT [FK_BucketMember_User] FOREIGN KEY ([userId]) REFERENCES [dbo].[User] ([id]),
    CONSTRAINT [FK_BucketMember_Bucket] FOREIGN KEY ([bucketId]) REFERENCES [workspace].[Bucket] ([id])
);
GO

-- =====================================================
-- INDEXES FOR BUCKET MEMBER TABLE
-- =====================================================

-- One bucket membership per referable target
CREATE UNIQUE INDEX [IX_BucketMember_BucketId_ReferableKind_ReferableId]
    ON [workspace].[BucketMember] ([bucketId], [referableKind], [referableId]);
GO

-- Load all buckets for a referable object
CREATE INDEX [IX_BucketMember_UserId_ReferableKind_ReferableId]
    ON [workspace].[BucketMember] ([userId], [referableKind], [referableId]);
GO

-- =====================================================
-- SEED DATA (optional — uncomment on new databases)
-- =====================================================
-- No default rows; memberships are created when editing contacts or mailboxes.
-- GO
