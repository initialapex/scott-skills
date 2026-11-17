# Forms, tables, feedback, and wording

## Forms

### Structure

- Order fields the way the task runs: the order of the paper form, the phone call, or
  the physical process the user follows, not the order of the database columns.
- Group related fields under a short group title; one group per logical unit
  (customer, delivery, payment).
- Ask only for what is needed now. Optional and rarely used fields go behind
  "More options" (easy to learn) or at the end of the form (easy to use).
- Every field has a visible label. A placeholder is an example, never the label: it
  disappears when the user types.
- Mark the minority: if most fields are required, mark the optional ones, and the
  other way round. Say what the mark means once at the top.

### Input

- Constrain instead of validating when the set of values is known: a combo box,
  radio buttons (2-5 visible choices), a date picker, a numeric field with limits.
- Easy to use: allow typing in every constrained control (type-ahead in combo boxes,
  typed dates), accept common formats, and paste of lists where users have them.
- Easy to learn: show the expected format next to the field ("Example: 2026-11-20"),
  and offer pickers.
- Defaults: easy to use remembers the last value per user; easy to learn uses the safe
  value that most people need. Never default a choice with serious consequences.
- Units are part of the label or shown after the field ("Weight [____] kg").
- Recognition rather than recall: users pick from what they see (lists, recent values,
  names next to codes) instead of remembering codes or copying values from another
  screen; the context they need for the decision (the selected order, the customer)
  stays visible while they work.

### Validation and errors

- **When**: check a field when the user leaves it, not while they are still typing
  the first value; re-check on every change after a field has shown an error; check
  the whole form on submit.
- **Where**: the message sits next to the field it is about, and the field is marked
  (border plus icon, not color alone). On submit with several errors, show a summary
  at the top that links to the fields, and move focus to the first error.
- **What**: say what is wrong and how to fix it, in the user's terms:
  "Delivery date must be after today." not "Invalid date." or "Error 1042".
- Keep everything the user entered after an error, including on server errors.
- **Submit button**: easy to learn keeps it enabled and explains the missing fields on
  click, because a disabled button does not say why. Easy to use may disable it when
  the reason is obvious on screen (an empty required field next to it); never disable
  without a visible reason.
- Validation rules that belong to the domain are defined once and shared with the
  model; this skill decides how they are presented (see `ss-design:ood` for where the
  rules live).

## Data tables and lists

- Choose columns for the task: the values users scan, compare, or decide on. Put the
  identifying column first; easy to use may offer column choice and reordering.
- Numbers align right, with the same number of decimals per column; units go in the
  header ("Weight (kg)"), not in every cell. Dates use one format, from the user's
  culture.
- Status columns use text plus an icon or a colored badge, never color alone.
- Sorting on every column where order is meaningful; show the current sort. Filtering
  or search when the list can exceed about a screenful (20-50 rows).
- Large data: page or virtualize, show the count, and keep the scroll position and the
  selection after a refresh.
- **Selection and bulk actions** (easy to use): multi-select with Shift and Ctrl and
  Ctrl+A, actions on the selection in the toolbar and the context menu, and the count
  of selected items visible.
- **Row actions**: the primary row action is a double-click or Enter (open or edit) and
  also a visible command; destructive row actions are not one click away from it.
- **Editing**: inline editing in the grid suits easy to use with simple values; a
  details form suits easy to learn, many fields, or values with rules.
- Empty table and no-results states follow information-architecture.md.

## Feedback and response times

| Time since the action | Feedback |
|-----------------------|----------|
| Up to 0.1 s | The control reacts (pressed state, the value appears); nothing else is needed |
| 0.1-1 s | No extra indicator; avoid a flashing spinner |
| 1-10 s | A busy indicator in the area that works (or the cursor), the trigger disabled so it is not clicked twice |
| Over about 10 s | A progress bar with percentage or steps when the length is known, a Cancel button, and the rest of the application usable if possible |

- Long work runs in the background; the screen stays responsive. Tell the user when it
  finishes if they may have moved on (a notification or a status bar message).
- After a successful action, show the result in place (the new row appears, selected)
  rather than a "Saved successfully" dialog to click away.

### Choosing the channel

| Situation | Channel |
|-----------|---------|
| Result of the user's action, nothing to decide | Inline change, status bar text, or a short snackbar/toast |
| Error in the user's input | Next to the field (see Validation) |
| Problem that blocks the current task | A message area at the top of the screen, with what to do |
| A decision is needed before continuing | Modal dialog |
| Something happened elsewhere (background job done, new data) | Non-modal notification; critical ones stay until dismissed |

Dialogs that only inform ("Saved.") interrupt without giving anything; avoid them.

### Confirmations and undo

- Prefer **undo** for reversible actions (delete to a recycle bin, revert a change):
  it is faster than confirming and protects better, because users stop reading
  confirmations they see every time.
- **Confirm** irreversible or costly actions, and actions that affect other people.
  The dialog names the object and the consequence; the buttons are verbs:
  "Delete 3 orders?  Deleted orders cannot be restored.  [Delete orders] [Cancel]",
  not "Are you sure?  [Yes] [No]".
- Easy to use: confirm only irreversible, high-cost actions; offer undo for the rest.
  Easy to learn: confirm actions with consequences and explain the consequence.
- The safe button is the default (Enter) for destructive confirmations.

## UI wording

UI text is in English by default; write it so that it can be translated later.

- **Buttons and commands are verbs** that say what happens: "Save order", "Export",
  "Delete". Dialog buttons repeat the verb of the question instead of Yes and No.
- **Sentence-style capitalization** for labels, buttons, titles, and messages
  ("Delivery date", "Save order"), the current Windows and Microsoft style; follow the
  project's existing style if it differs.
- **One term per concept** across the application; keep a glossary in the design when
  the domain has synonyms (order, job, ticket: pick one).
- **Users' words, not the system's**: "Customer not found" rather than
  "Null reference in customer lookup"; error codes, if needed for support, come after
  the human message.
- **Messages**: what happened, why if it helps, what to do: "The printer is offline.
  Check that it is on, then select Print again."
- No blame, no exclamation marks, no humor in error messages.
- Labels end without a colon unless the platform convention uses one; keep it
  consistent.
- Ellipsis (...) on a command means it asks for more input before acting
  ("Export..."); no ellipsis means it acts at once.
- Localization rules (text growth, no concatenation, formats) are in
  usability-accessibility.md.
