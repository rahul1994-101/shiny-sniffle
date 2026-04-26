# Task, Subtask, Issue, and Backlog Schema Design Notes

## Overview

This document explains the design decisions for the Task, Subtask, Issue, Backlog, and TaskIssueLink tables.

## Design Decisions

### 1. Task Table

**Key Features:**
- Belongs to exactly one Project (via `projectId`)
- Rich workflow status: Todo (0), In Progress (1), Review (2), Done (3)
- User assignment fields: `requestedById` (who requested/created) and `assignedToId` (who is assigned to work on it)
- Date fields: `startDate`, `dueDate`, `completedDate` (all optional)
- Optional priority field for future prioritization features
- Standard audit fields and soft delete pattern

**Status Enum Values:**
- `0` = Todo
- `1` = In Progress
- `2` = Review
- `3` = Done

**User Assignment Fields:**
- `requestedById` - User who requested/created the task (optional, FK to User)
- `assignedToId` - User assigned to work on the task (optional, FK to User)

**Date Fields:**
- `startDate` - When work on the task should/may begin (optional)
- `dueDate` - When the task is due (optional)
- `completedDate` - When the task was completed (optional)

### 2. Subtask Table

**Design Decision: Separate Table vs. Embedded**

We chose a **separate table** for subtasks for the following reasons:

**Pros of Separate Table:**
- ✅ Flexibility: Can track individual subtask completion, ordering, and future metadata
- ✅ Scalability: Can easily add fields later (assignee, due date, etc.) without schema changes
- ✅ Query Performance: Can efficiently query subtasks independently
- ✅ Data Integrity: Foreign key constraints ensure referential integrity
- ✅ Future-Proof: Easy to evolve if subtasks need more features later

**Cons:**
- ❌ Slightly more complex than a JSON/text field
- ❌ Requires a join to get all subtasks for a task

**Alternative Approach (Not Implemented):**
If subtasks truly remain "lightweight checklist items" forever, you could store them as:
- JSON field in Task table: `subtasks NVARCHAR(MAX)` containing JSON array
- Simple text field with delimiters (not recommended)

**Current Implementation:**
- Simple structure: `title`, `isCompleted`, `orderIndex`
- Belongs to exactly one Task (via `taskId`)
- Can be easily extended later if needed

### 3. Issue Table

**Key Features:**
- Belongs to exactly one Project (via `projectId`)
- Lightweight workflow status: Open (0), Investigating (1), Resolved (2), Closed (3)
- User assignment fields: `requestedById` (who reported/requested) and `assignedToId` (who is assigned to fix it)
- Date fields: `startDate`, `dueDate`, `completedDate` (all optional) - identical to Task table
- Optional priority field
- Standard audit fields and soft delete pattern

**Status Enum Values:**
- `0` = Open
- `1` = Investigating
- `2` = Resolved
- `3` = Closed

**User Assignment Fields:**
- `requestedById` - User who requested/reported the issue (optional, FK to User)
- `assignedToId` - User assigned to work on the issue (optional, FK to User)

**Date Fields:**
- `startDate` - When work on the issue should/may begin (optional)
- `dueDate` - When the issue is due (optional)
- `completedDate` - When the issue was completed (set when status = Closed) (optional)

**Note:** The Issue table uses the same date structure as Task for consistency. The `completedDate` field represents final completion (when status = Closed). The distinction between "resolved" and "closed" is tracked via the `status` field.

### 4. Backlog Table

**Key Features:**
- Belongs to exactly one Project (via `projectId`)
- Staging area for unprocessed work items before categorization
- Minimal fields - items are moved to Task or Issue for full workflow
- No status field (implicitly "pending" until moved)
- No assignment field (items not assigned until moved to Task/Issue)
- No date fields (dates set when moved to Task/Issue)
- Standard audit fields and soft delete pattern

**Purpose:**
- Quick capture of work items without full details
- Allows users to "dump" ideas/requests before proper categorization
- Items can be moved to either Task (planned work) or Issue (reactive work)
- Simplifies initial data entry - details added during move operation

**Fields Included:**
- `projectId` - Which project the backlog item belongs to
- `title` - Brief description of the work item
- `description` - Optional detailed description
- `priority` - Optional priority level (0-5 scale)
- `requestedById` - User who added the item to backlog

**Fields Excluded (compared to Task/Issue):**
- ❌ `assignedToId` - Not assigned until moved to Task/Issue
- ❌ `status` - No workflow status (implicitly pending)
- ❌ `startDate` - Set when moved to Task/Issue
- ❌ `dueDate` - Set when moved to Task/Issue
- ❌ `completedDate` - Set when moved to Task/Issue and completed

**Workflow:**
1. User creates backlog item with minimal info (title, description, priority)
2. Backlog item can be reviewed/prioritized
3. Item is moved to either Task or Issue table with additional details:
   - Assignment (`assignedToId`)
   - Dates (`startDate`, `dueDate`)
   - Status (appropriate for Task or Issue workflow)
4. Original backlog item is soft-deleted or marked inactive

### 5. TaskIssueLink Table

**Key Features:**
- Many-to-many relationship between Tasks and Issues
- One Task can link to many Issues (optional)
- One Issue can link to one Task (optional)
- Unique constraint prevents duplicate links
- Standard audit fields and soft delete pattern

**Use Cases:**
- Link a bug (Issue) to the feature task (Task) it affects
- Link multiple related issues to a single task
- Track relationships between planned work (Tasks) and reactive work (Issues)

## Relationships Summary

```
Project (1) ──→ (Many) Task
Project (1) ──→ (Many) Issue
Project (1) ──→ (Many) Backlog
Task (1) ──→ (Many) Subtask
Task (Many) ──→ (Many) Issue [via TaskIssueLink]
Backlog ──→ (Moved to) Task or Issue
```

## Schema Consistency

**Task and Issue tables are designed to be near-identical for maintainability:**

### Common Structure:
- **User Assignments:** Both use `requestedById` and `assignedToId` with consistent naming
- **Dates:** Both use `startDate`, `dueDate`, and `completedDate` (same field names and types)
- **Core Fields:** Both have `projectId`, `title`, `description`, `status`, `priority`
- **Lifecycle:** Both use `isActive` and `isDeleted` for soft delete pattern
- **Audit Fields:** Both have `createdBy`, `createdAt`, `updatedBy`, `updatedAt`

### Benefits:
- ✅ Consistent query patterns across both tables
- ✅ Easier code generation and maintenance
- ✅ Predictable field names for developers
- ✅ Simplified date management logic

## Indexing Strategy

All tables include comprehensive indexes (commented out, ready to enable when needed):

### Task and Issue Tables - Complete Index Set:

**Foreign Key Indexes:**
- Project ID index (filtered by active/non-deleted)
- Requester ID index (filtered by active/non-deleted)
- Assignee ID index (filtered by active/non-deleted)

**Status & Priority Indexes:**
- Status index (filtered by active/non-deleted)
- Composite project + status index
- Priority index (filtered by active/non-deleted)

**Date Indexes:**
- Start date index (filtered by active/non-deleted)
- Due date index (filtered by active/non-deleted)
- Completed date index (filtered by active/non-deleted)

**Lifecycle Indexes:**
- Active status index
- Deleted status index
- Composite active + deleted index

**Audit Indexes:**
- Created at index (DESC for recent-first queries)
- Created by index
- Updated by index
- Updated at index (DESC for recent-first queries)

**Index Features:**
- Filtered indexes exclude NULL values and soft-deleted records where appropriate
- Composite indexes support common query patterns
- All indexes are commented out for optional deployment

## Future Considerations

### Subtasks
If subtasks need more features later, you can easily add:
- `assigneeId` - assign subtasks to different users
- `dueDate` - individual subtask deadlines
- `estimatedHours` - time tracking
- `actualHours` - actual time spent

### Tasks & Issues
Both tables are designed to be extensible and maintain consistency:
- Priority field is already included (can be used for 0-5 scale or similar)
- Date fields (`startDate`, `dueDate`, `completedDate`) support scheduling and reporting
- Status enums can be extended if needed (though this requires migration)
- User assignment fields (`requestedById`, `assignedToId`) support delegation scenarios
- All indexes are defined but commented out for performance tuning as needed

### Buckets (Future)
When implementing UserBucket and ProjectBucket:
- Create `BucketItem` junction table linking buckets to Tasks/Issues
- Use a discriminator field or separate tables for Task vs Issue items

## Backlog Table Design Rationale

**Why a Separate Backlog Table?**

The Backlog table serves as a lightweight staging area with several benefits:

1. **Quick Capture:** Users can quickly add items without filling out all required fields
2. **Flexibility:** Items can be moved to either Task or Issue based on later categorization
3. **Simplified UI:** Backlog view can be simpler - just title, description, priority
4. **Workflow Separation:** Clear distinction between "pending review" (Backlog) and "in workflow" (Task/Issue)
5. **Data Integrity:** Prevents incomplete Task/Issue records

**Moving Items from Backlog:**

When moving a backlog item to Task or Issue:
- Copy core fields: `title`, `description`, `priority`, `requestedById`, `projectId`
- Add workflow-specific fields: `assignedToId`, `startDate`, `dueDate`, `status`
- Optionally preserve backlog `id` in a reference field (future enhancement)
- Soft-delete or mark inactive the original backlog item

## Migration Notes

When deploying these schemas:
1. Create tables in order: Backlog → Task → Subtask, Issue → TaskIssueLink
2. Foreign key constraints will enforce referential integrity
3. Indexes are created after table creation
4. Consider adding seed data for status enums if needed
5. Backlog table can be created independently as it has no dependencies on Task/Issue

