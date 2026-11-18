# Layout and visual hierarchy

Arrange each screen so that the eye finds the main content and the primary action
first, related things look related, and the screen still works when the window is
resized or scaled.

## Reading order and placement

- Users scan from the top left. Put the screen title and the main content there;
  put the primary action where the task ends.
- Windows dialog buttons sit at the bottom right, in the order **primary, then Cancel**
  (OK left of Cancel). Follow the platform; on the web, follow the product's existing
  convention.
- Actions on a whole screen go in a toolbar or command bar at the top; actions on one
  item go next to the item or in its context menu, and also in the toolbar for keyboard
  users.
- Keep the position of things stable between screens of the same kind (title, toolbar,
  status bar, OK and Cancel), so users stop searching.

## Alignment and grid

- Align controls to a few vertical lines. Each extra alignment line makes the screen
  look busier.
- **Labels beside fields** (left-aligned labels, fields in one column) suit dense
  desktop forms and easy to use. **Labels above fields** read fastest for long or
  unfamiliar forms and suit easy to learn and narrow windows.
- Numbers align right (and to the decimal point when possible); text aligns left; do
  not center text in tables or forms.
- Fields have widths that hint at the expected content (a postal code field is short).

## Spacing

Use a small spacing scale based on 4 px and apply it consistently:

| Step | Use for |
|------|---------|
| 4 px | Inside a control group (icon to its text) |
| 8 px | Between related controls (label to field, fields in one group) |
| 16 px | Between groups of controls |
| 24-32 px | Between sections of a screen, around the window content |

- Use spacing to group before using lines or boxes; a border around every group adds
  noise.
- Related things are closer to each other than to unrelated things (proximity).

### Consistent margin and padding

The same kind of element gets the same margin and padding on every screen. Users read
unequal gaps as meaning something, and unequal gaps between screens of one program
look careless.

- **One scale, defined once.** Write the scale down for the program (the values above,
  or the ones its existing screens use) and keep each value in one place: a style, a
  resource, or a theme token. A screen refers to it by name and does not type its own
  number.
- **Per kind of element, one value.** Decide it once for: the margin around window and
  dialog content, label to field, field to field in a group, group to group, section
  to section, the padding inside buttons and list items, and the row of dialog buttons.
  Every screen uses these values for these elements.
- **No value off the scale.** A gap that looks wrong is fixed with a scale value, not
  with a one-off number. A value that is really needed is added to the scale first, and
  then used everywhere it applies.
- **Compare with the neighbor.** When a screen is added or changed, put it next to a
  similar existing screen: the same kind of element has the same gap. Where they
  differ, one of them is wrong; fix the odd one, and name which it is.
- Equal controls have equal sizes (buttons of one row, fields of one column), and
  labels and fields share their edges.

## Density

| | Easy to use | Easy to learn | Field operation |
|---|---|---|---|
| Table row height | Compact (about 24-32 px) | Comfortable (about 36-48 px) | Large (see field-operation.md) |
| Fields per screen | Everything the task needs, in groups | One group or one step at a time | Few, large |
| Padding and spacing | Lower end of the scale | Upper end of the scale | Large |
| Secondary information | Visible (more columns, details pane) | Behind an expander or a details page | Hidden unless needed |

Density is a choice of the direction, not of taste; state it in the design. Offer a
density switch only when both kinds of users really share the program.

## Visual hierarchy

- Make importance visible with, in order of strength: position, size, weight, color,
  and decoration. Use as few as needed.
- **Typography levels**: about four are enough (screen title, section title, body,
  secondary or caption). Every text on the screen belongs to one of them; do not invent
  sizes per screen.
- **One primary button style** per screen or dialog. Other actions are secondary
  (outlined or text buttons); destructive actions are styled as destructive and set
  apart from the primary one.
- Status and key values (totals, the current state of a process) are larger or bolder
  than labels.
- Remove before adding: every element that does not serve the task makes the others
  harder to find.

## Visual guidance

Guide the eye and the next step, so that nobody has to hunt for what to do:

- **One primary action** per screen, styled differently from the others (filled or
  accented against plain), placed where the task ends. A destructive action is never
  the primary one; set it apart and confirm it.
- **Reading order is task order.** Start top left, end at the primary action; related
  controls sit together under a heading. The Tab order is the reading order, and the
  first focus is on the first field to fill in, or on the primary action when there is
  nothing to fill in.
- **Hierarchy has few levels** (title, section heading, body, hint), made with size,
  weight, color, and space. Everything of the same level looks the same.
- **State is visible**: enabled and disabled, selected, focused, busy, empty, and
  error each look different, and an error sits next to its cause and says what to do.
  A state is never shown by color alone.
- **Say what comes next**: an empty list says how to fill it, a disabled primary
  action says what is missing (a hint beside it), and after an action its result is
  visible on the screen.

## Color

- Use color with roles, defined once for the application:
  - **brand or accent**: primary actions, selection, focus (one accent color)
  - **semantic**: error, warning, success, information (always with text or an icon)
  - **neutral**: text, borders, backgrounds
- Never rely on color alone: pair it with text, an icon, or a shape (a red "Failed"
  label with an error icon, not only a red row).
- Contrast: body text at least 4.5:1 against its background, large text (about 18 pt,
  or 14 pt bold) and meaningful graphics or control borders at least 3:1 (WCAG 2.x).
- Support the application's light and dark themes and the system's high-contrast mode
  if the platform offers them; check every color role in each.
- The actual colors and how to define them in WPF belong to `ss-wpf:material-design`.

## Window size and resizing

- Find the smallest screen and the display scaling to support (for example
  1366 x 768 at 100%, or 1920 x 1080 at 150%, which leaves 1280 x 720 effective
  pixels). Design for that size first, then check the largest common size.
- Decide which areas grow when the window grows (usually the list or the document),
  and which keep their size (forms, side panels). Set a minimum window size below
  which the layout would break.
- Long forms scroll vertically; never scroll horizontally except in wide data tables.
- Splitters between panes, with remembered positions, serve easy-to-use screens well.

## Example wireframe

An easy-to-use order entry screen at 1280 x 720 effective pixels:

```
+--------------------------------------------------------------------------+
| Orders > New order                                   [Save  Ctrl+S] [x]  |
+--------------------------------------------------------------------------+
| Customer [Contoso Ltd.________v]   Delivery [2026-11-20__]  Ref [______] |
|                                                                          |
| Product              Qty   Unit price      Total     Stock               |
| [Widget A_______v]   [10]     12.50       118.75     OK                  |
| [Widget B_______v]   [ 2]     40.00        80.00     2 left              |
| [______________v]    [  ]                                                |
|                                                                          |
|                                                        Total   198.75    |
+--------------------------------------------------------------------------+
| Enter: new line    Ctrl+D: duplicate line    F4: product list            |
+--------------------------------------------------------------------------+
```
