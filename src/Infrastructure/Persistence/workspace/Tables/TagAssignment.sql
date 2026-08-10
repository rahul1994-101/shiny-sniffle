-- =====================================================
-- TAG ASSIGNMENT TABLE
-- =====================================================
-- Links a Tag to a referable workspace object (contact or mailbox).
-- Rows removed when the tag, referable object, or parent user scope is cleaned up in app.
--
-- Business Rules:
-- - referableKind: 0 = Contact, 1 = Mailbox (ReferableKind in app)
-- - At most one row per (tagId, referableKind, referableId)
-- - userId matches tag owner and referable owner (denormalized for scoped queries)
-- - Apply after workspace.Tag
-- =====================================================
GO

CREATE TABLE [workspace].[TagAssignment] (
    -- Primary key with auto-generated sequential UUID
    [id]                                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [userId]                                 UNIQUEIDENTIFIER NOT NULL,                 -- Owner (FK to User)

    -- Data fields
    [tagId]                                  UNIQUEIDENTIFIER NOT NULL,                 -- FK to Tag
    [referableKind]                          TINYINT NOT NULL,                          -- ReferableKind: Contact = 0, Mailbox = 1
    [referableId]                            UNIQUEIDENTIFIER NOT NULL,                 -- PK of Contact or EmailAccount

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
