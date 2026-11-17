# Usability and accessibility

A screen is usable when its users can finish their tasks with any input device, at any
display scaling, in any language the program ships in, and when someone has watched
them do it.

## Keyboard

- **Every function works from the keyboard**: every control is reachable with Tab, and
  every command has a keyboard path (a key, a shortcut, or a menu).
- **Tab order follows reading order**: left to right, top to bottom, group by group.
  Check it after every layout change; it is the most common keyboard defect.
- **Initial focus** goes to where the user starts typing (the first field of a form,
  the search box of a list); a dialog focuses its first input or its default button.
- **Default and cancel buttons**: Enter triggers the default button, Esc cancels or
  closes. For destructive confirmations, the safe button is the default.
- **Access keys** (Alt plus an underlined letter) on labels and buttons of forms and
  dialogs, unique per screen.
- **Focus is always visible**: a clear focus indicator on every control, also in custom
  styles; never remove it for looks.
- Lists and grids: arrow keys move, Space selects, Enter opens, Home and End jump,
  type-ahead finds an item by its first letters.
- Do not trap focus: the user can always Tab out of a control and Esc out of a popup.

### Shortcuts by direction

| | Easy to use | Easy to learn |
|---|---|---|
| Standard shortcuts (Ctrl+S, Ctrl+N, Ctrl+F, Ctrl+Z, F5, Delete) | Yes | Yes |
| Shortcuts for every frequent action | Yes, shown in menus, tooltips, and a status bar hint | Optional, never the only way |
| Function keys and single-key shortcuts | Allowed for very frequent actions, where they do not collide with typing | Avoid |
| A shortcut reference (F1 or a cheat sheet) | Yes | Not needed |

Shortcuts follow the platform's standard meanings; never reassign Ctrl+C, Ctrl+V,
Ctrl+Z, or similar keys to other actions.

## Color and contrast

- Text contrast at least 4.5:1, large text and meaningful graphics at least 3:1 (WCAG
  2.x); check placeholder text, disabled-looking text that is still meant to be read,
  and text on colored backgrounds.
- Information carried by color is also carried by text, icon, or shape, for users with
  color vision deficiencies and for monochrome printouts. Red and green together are the
  most common problem.
- Respect the system's high-contrast mode where the platform supports it: no
  hard-coded colors that ignore it, and icons that stay visible.

## Display scaling (DPI)

- Check every screen at 100%, 125%, 150%, and 200% scaling, and when the window moves
  between monitors with different scaling.
- Sizes are in device-independent units; icons are vector or provided in several
  sizes; nothing is positioned with absolute pixel coordinates that break when text
  grows.
- Text never gets cut off: containers size to their content or wrap, with a minimum
  window size that still fits the longest expected text.
- In WPF, the application must be declared DPI-aware (per-monitor where possible); the
  implementation belongs to `ss-wpf:material-design` and the project setup.

## Text length and localization

UI text is English by default and may be translated later. Design for it now:

- **Room to grow**: translations are often longer than English. Plan for about 30-50%
  more for sentences and up to 100-200% for very short labels (one or two words). Let
  buttons and labels size to their content instead of fixed widths.
- **No concatenated sentences**: "Delete " + count + " orders" cannot be translated;
  use whole sentences with placeholders ("Delete {0} orders?") and plural forms.
- **All text in resources**, never in code or images; the same word in two places may
  need two resource entries if the context differs.
- **Formats from the user's culture**: dates, times, numbers, decimal separators,
  currency. Store and exchange data in an invariant format, display it in the user's.
- **Sorting and searching** follow the culture's rules.
- **Fonts**: the chosen font must contain the target languages' characters (many
  Latin fonts have no Chinese, Japanese, or Korean glyphs); the font choice belongs to
  the styling skill.
- Right-to-left languages are out of scope unless the developer says otherwise.

## Assistive technology

- Every control has an accessible name: the visible label associated with the field,
  or an explicit name for icon-only buttons (which also need a tooltip).
- Images that carry information have a text alternative; decorative ones are hidden
  from screen readers.
- Status changes that matter (a background job failed) are also announced, not only
  shown with color or animation.
- Do not convey information only through timing (messages that vanish before they can
  be read); critical messages stay until dismissed.
- Click targets are large enough for users with limited dexterity: about 24 x 24 px
  at least on desktop, larger for touch (see field-operation.md).

## Checking usability

Rules catch common defects; watching users catches the rest. Propose the lightest
check that fits:

- **Easy to learn**: give a person who has never seen the screen a task without
  explanation, and watch silently. Note where they hesitate, misread, or ask. Three to
  five people find most problems.
- **Easy to use**: count the steps, keystrokes, and pointer travel for the top tasks;
  time a trained user on the frequent path; look for repeated work that a default,
  a remembered value, or a bulk action would remove.
- **Both**: walk through each task with the design, step by step, asking at each step
  whether the user knows what to do, sees how to do it, and understands the feedback.
- Record findings with the review checklist ids, and fix the ones on the frequent
  path first.
