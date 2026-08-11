-- =====================================================
-- BUCKET MEMBER TABLE
-- =====================================================
-- Links a Bucket to a contact or mailbox (membership target — not the bucket ER itself).
-- Rows removed when the bucket, target, or parent user scope is cleaned up in app.
--
-- Business Rules:
-- - referableKind: membership target only — 0 = Contact, 1 = Mailbox (ReferableKind in app)
-- - Tag and Bucket rows are ERs (tag:{alias}, bucket:{alias}); this table groups contact/mailbox targets
-- - At most one row per (bucketId, referableKind, referableId)
-- - userId matches bucket owner and target owner (denormalized for scoped queries)
-- - Apply after workspace.Bucket and workspace/Tables/Contact.sql
-- =====================================================
GO

CREATE TABLE [workspace].[BucketMember] (
    -- Primary key with auto-generated sequential UUID
    [id]                                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [userId]                                 UNIQUEIDENTIFIER NOT NULL,                 -- Owner (FK to User)

    -- Data fields
    [bucketId]                               UNIQUEIDENTIFIER NOT NULL,                 -- FK to Bucket
    [referableKind]                          TINYINT NOT NULL,                          -- Membership target: Contact = 0, Mailbox = 1
    [referableId]                            UNIQUEIDENTIFIER NOT NULL,                 -- PK of Contact or EmailAccount (target)

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
