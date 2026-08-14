-- =====================================================
-- TAG ASSIGNMENT TABLE
-- =====================================================
-- Links a Tag to a contact or mailbox (assignment target — not the tag ER itself).
-- Rows removed when the tag, target, or parent user scope is cleaned up in app.
--
-- Business Rules:
-- - referableKind: assignment target only — 0 = Contact, 1 = Mailbox (ReferableKind in app)
-- - Tag and Bucket rows are ERs (tag:{alias}, bucket:{alias}); this table tags contact/mailbox targets
-- - At most one row per (tagId, referableKind, referableId)
-- - userId matches tag owner and target owner (denormalized for scoped queries)
-- - Apply after workspace.Tag and workspace/Tables/Contact.sql
-- =====================================================
GO

CREATE TABLE [workspace].[TagAssignment] (
    -- Primary key with auto-generated sequential UUID
    [id]                                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [userId]                                 UNIQUEIDENTIFIER NOT NULL,                 -- Owner (FK to User)

    -- Data fields
    [tagId]                                  UNIQUEIDENTIFIER NOT NULL,                 -- FK to Tag
    [referableKind]                          TINYINT NOT NULL,                          -- Assignment target: Contact = 0, Mailbox = 1
    [referableId]                            UNIQUEIDENTIFIER NOT NULL,                 -- PK of Contact or EmailAccount (target)

    -- Foreign keys
    CONSTRAINT [FK_TagAssignment_User] FOREIGN KEY ([userId]) REFERENCES [dbo].[User] ([id]),
    CONSTRAINT [FK_TagAssignment_Tag] FOREIGN KEY ([tagId]) REFERENCES [workspace].[Tag] ([id])
);
GO

-- =====================================================
-- INDEXES FOR TAG ASSIGNMENT TABLE
-- =====================================================

-- One tag assignment per referable target
CREATE UNIQUE INDEX [IX_TagAssignment_TagId_ReferableKind_ReferableId]
    ON [workspace].[TagAssignment] ([tagId], [referableKind], [referableId]);
GO

-- Load all tags for a referable object
CREATE INDEX [IX_TagAssignment_UserId_ReferableKind_ReferableId]
    ON [workspace].[TagAssignment] ([userId], [referableKind], [referableId]);
GO

-- =====================================================
-- SEED DATA (optional — uncomment on new databases)
-- =====================================================
-- No default rows; assignments are created when editing contacts or mailboxes.
-- GO
