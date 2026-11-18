# Review checklist

Review the screens the developer named (running application, screenshot, mockup,
design document, or XAML) against these items. Start by stating the direction you
reviewed against; if it is not known, ask for it first (see direction.md). Report each
finding as:

```
<location>  [<item id>] <what is wrong>  →  <the fix>
```

The location is `file:line` for XAML or code, and the screen plus the area for a
screenshot or design ("Order entry, toolbar").

Group findings by severity: **must fix** (a task cannot be finished, data can be lost,
a control cannot be reached or read, a destructive action is unprotected), **should
fix** (slows users down or causes mistakes on a frequent path), **consider** (polish
and consistency). Screens not part of this request are reported only when they block
it; list other problems separately as "later".

Items marked *[use]* apply only to easy-to-use screens, items marked *[learn]* only to
easy-to-learn screens; unmarked items apply to both. Items under UX-O apply only to
field operation screens, and items under UX-M only to a program that has an operation
manual.

The ids start with `UX-` so they do not collide with other checklists
(`ss-wpf:mvvm`, `ss-wpf:material-design`, `ss-design:ood`) used in the same review.

The direction never lowers a *must* item of UX-K or a protection against data loss
(UX-F5, UX-B3); it only changes how the item is met. When the developer asks where a
rule comes from, cite the source from [sources.md](sources.md).

## UX-D: Direction

- UX-D1 The program's direction is stated, and the screen follows it or is a recorded
  exception with a reason. *(should)*
- UX-D2 Exception screens keep the program's navigation, terms, button order, and
  visual style. *(should)*

## UX-F: Flows and structure

- UX-F1 Each screen serves one main task; its layout puts that task first. *(should)*
- UX-F2 Frequent tasks are one step from the main window, without dialog chains.
  *(should)* *[use]*
- UX-F3 Multi-step tasks done rarely are guided (steps or a wizard with a summary
  before finishing). *(should)* *[learn]*
- UX-F4 Every state is designed: empty, loading, no results, error, read-only, no
  permission. *(must)*
- UX-F5 Every screen has a way back or out; unsaved changes are not lost without
  asking. *(must)*
- UX-F6 Modal dialogs are only for decisions and short input; no stacked modals.
  *(should)*
- UX-F7 Windows remember size, position, and layout, and open on a visible monitor.
  *(consider)*

## UX-L: Layout and visual hierarchy

- UX-L1 Controls align to a few vertical lines; numbers align right, text left.
  *(consider)*
- UX-L2 Spacing follows one scale; groups are separated by spacing rather than boxes
  everywhere. *(consider)*
- UX-L3 One primary action per screen or area; destructive actions are styled and
  placed apart from it. *(should)*
- UX-L4 Text uses the application's few typography levels; key values and status
  stand out. *(consider)*
- UX-L5 Density matches the direction (compact for easy to use, comfortable for easy to
  learn). *(should)*
- UX-L6 The layout works at the smallest supported window size; the right areas grow;
  a minimum window size prevents breaking. *(should)*
- UX-L7 Dialog buttons follow the platform order and are verbs. *(should)*
- UX-L8 The same kind of element has the same margin and padding on every screen
  (window margin, label to field, field to field, group to group, button padding,
  dialog button row); the screen matches a similar neighbor. *(should)*
- UX-L9 Every margin and padding is a value of the program's spacing scale, taken from
  its style or resource, not a one-off number. *(should)*
- UX-L10 Visual guidance: the primary action is set apart, the reading order and the
  Tab order follow the task, the first focus is right, each state is visible, and the
  user is told the next step (empty and disabled states explain themselves).
  *(should)*

## UX-I: Forms and input

- UX-I1 Every field has a visible label; placeholders are not used as labels. *(must)*
- UX-I2 Fields follow the order of the task and are grouped. *(should)*
- UX-I3 Known value sets use constrained controls; typing still works in them.
  *(should)*
- UX-I4 Validation runs on leaving the field and on submit, not on the first
  keystroke; messages sit next to the field, say how to fix it, and keep the input.
  *(must)*
- UX-I5 A disabled submit button always has a visible reason; easy-to-learn screens
  keep it enabled and explain on click. *(should)*
- UX-I6 Defaults remember the last values *[use]* or are the safe common choice
  *[learn]*; no default for choices with serious consequences. *(should)*
- UX-I7 Users pick from visible choices instead of recalling codes or values from
  another screen; the context needed for the decision stays visible. *(should)*

## UX-T: Tables and lists

- UX-T1 Columns serve the task; the identifying column is first; units are in headers.
  *(should)*
- UX-T2 Lists longer than a screenful can be sorted and filtered or searched; the
  current sort and filter are visible. *(should)*
- UX-T3 Empty and no-results states are distinct and say what to do. *(should)*
- UX-T4 Selection and scroll position survive a refresh. *(should)*
- UX-T5 Multi-select and bulk actions exist for tasks done on many items. *(should)*
  *[use]*

## UX-B: Feedback and confirmations

- UX-B1 Actions taking over 1 s show a busy state, and the trigger cannot be pressed
  twice. *(should)*
- UX-B2 Actions taking over about 10 s show progress and can be cancelled; the
  application stays responsive. *(must)*
- UX-B3 Irreversible or costly actions are confirmed, or reversible with undo.
  *(must)*
- UX-B4 Confirmations name the object and the consequence, with verb buttons; the safe
  button is the default. *(should)*
- UX-B5 Confirmations are not shown for routine, reversible actions. *(should)* *[use]*
- UX-B6 No dialogs that only inform; results show in place or in a non-modal message.
  *(consider)*
- UX-B7 Error messages say what happened and what to do, in the user's words.
  *(should)*

## UX-W: Wording and localization

- UX-W1 One term per concept across the application. *(should)*
- UX-W2 Buttons and commands are verbs; capitalization is consistent (sentence style by
  default). *(consider)*
- UX-W3 All UI text is in resources; no sentences built from fragments; plural forms
  are handled. *(should)*
- UX-W4 Controls size to their text and leave room for longer translations; nothing is
  cut off. *(should)*
- UX-W5 Dates, numbers, and currency use the user's culture. *(should)*
- UX-W6 Help fits the direction: reference help on demand (F1, a help link) with no
  tips in the way *[use]*; inline hints, examples, and first-run guidance *[learn]*.
  *(consider)*

## UX-K: Keyboard and accessibility

- UX-K1 Every control and command is reachable from the keyboard; no focus traps.
  *(must)*
- UX-K2 Tab order follows the reading order; initial focus is where the user starts.
  *(should)*
- UX-K3 Enter and Esc trigger the default and cancel buttons. *(should)*
- UX-K4 The focus indicator is visible on every control, including custom styles.
  *(must)*
- UX-K5 Frequent actions have shortcuts, shown in menus or tooltips; standard
  shortcuts keep their standard meaning. *(should)* *[use]*
- UX-K6 Text contrast is at least 4.5:1 (3:1 for large text and meaningful graphics).
  *(must)*
- UX-K7 No information is carried by color alone. *(must)*
- UX-K8 The screen works at 100-200% display scaling and across monitors with different
  scaling. *(should)*
- UX-K9 Icon-only buttons have an accessible name and a tooltip. *(should)*

## UX-O: Field operation

- UX-O1 Touch targets are at least about 10 mm (15 mm with gloves) with space between
  them; no hover-only or precision-only interaction. *(must)*
- UX-O2 Text and key values are readable at the real viewing distance and lighting.
  *(must)*
- UX-O3 The station's state and the next step are visible at a glance, with color, text,
  and icon, in a fixed position. *(should)*
- UX-O4 Consequential actions are far from frequent ones and need a deliberate gesture.
  *(must)*
- UX-O5 Critical alarms stay until acknowledged, say what to do, and are logged.
  *(must)*
- UX-O6 Work in progress survives interruptions and restarts; no timeouts discard input.
  *(should)*

## UX-M: Operation manual

- UX-M1 Every view, control, shortcut, and message that the change adds, changes, or
  removes is reflected in the manual, in the same change. *(should)*
- UX-M2 Each row says what the user sees and what happens, with the label and message
  text exactly as on the screen, and no class, ViewModel, or command names. *(should)*
- UX-M3 The behavior in the manual is what the screen does: states in which a control is
  disabled, validation rules, and the state after the action. *(must)*
- UX-M4 The manual is one English HTML file with inline style and nothing loaded from
  the network, in the structure of the template. *(consider)*
