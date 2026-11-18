# Operation manual

An operation manual tells a person how to operate the program: what each view is for,
and how each control behaves. It is the form of the design that a user reads. The
design (see design-output.md) is written before the screen exists; the manual is kept
true after it exists.

Use it for a program with a GUI. A program without one has nothing to put in it.

## Format

- **One HTML file**, with its style inline and nothing loaded from the network, so that
  it opens from a file, an attachment, or a machine without internet.
- **Always written in English**, whatever language the conversation or the UI uses.
  Quote UI text exactly as the user sees it, in the UI's own language.
- Start from [templates/operation-manual.html](../templates/operation-manual.html). It
  has the structure below, a light and a dark color scheme, and real table headers.
- Where it lives: next to the program's spec or in the project's documentation folder,
  at the path that the project's rules name. When the rules name none, ask the
  developer; do not invent a place.

## Structure

| Section | Content |
|---------|---------|
| Overview | Who uses the program, for which task, and the main flow through its views. |
| Views and controls | One block per view (window, dialog, page, wizard step): its purpose, how the user gets there, an optional screenshot or sketch, and a table with one row per control. |
| Keyboard shortcuts | Keys, the view they work in, and the action. |
| Messages | The text of each message, its kind (information, warning, error), when it appears, and what the user can do. |

### The control table

| Column | What it says |
|--------|--------------|
| Control | The label as the user sees it; for an unlabeled control, the name of its tooltip or accessible name |
| Type | Button, text box, list, check box, menu entry, and so on |
| Shown / enabled when | The state in which it is visible and enabled, and the state in which it is not |
| User action | Click, type, select, key, drag |
| Behavior | What happens, in order, and the state of the view afterwards |
| Errors and feedback | The validation rule, the message text, and where it is shown |

- List the controls in the order in which a user meets them: top to bottom, left to
  right, the same order as the Tab order.
- A control has **one row for every state and every action that changes what happens**
  (a button that is disabled until a row is selected, a field that validates on leaving
  it, a menu entry with a different result for a multiple selection). Say it in the
  table, not in a paragraph below it.
- Use the user's words: labels, terms, and messages exactly as on the screen. Do not
  write class, ViewModel, command, or file names; say what the user sees and what
  happens, not how it is built.
- State the result, including the state afterwards: "The list is filtered to the
  typed text, and the status bar shows the number of matches", not "Filters the list".
- Every shortcut that the table mentions is also in the shortcuts table, and every
  message that it mentions is also in the messages table, with the same text.

## How it relates to the design

- The **Components and states** table and the **UI text** of the design are the source:
  the control names, the states, and the texts are the same in both. The manual adds the
  views that were not part of this design, and the final text as it was built.
- When the built screen differs from the design, the manual follows what was built, and
  the difference goes back to the developer as an open point.

## Keeping it true

- A change that adds, changes, or removes a view, a control, a shortcut, or a message
  updates the manual in the same change. Update only the rows of the views that the
  change touches.
- A review reads the diff of the manual next to the diff of the screens (UX-M1 to UX-M4
  in the review checklist).
- The first change that adds a view fills in the overview; until then the template's
  placeholder text stays, so that nobody mistakes an empty manual for a finished one.
