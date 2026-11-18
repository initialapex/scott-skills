# Design output

A UI design proposal has these parts, in this order. Keep it as short as the change
allows: a small dialog may need only the direction, one wireframe, and its UI text.

1. **Direction and users**: the program's direction, this screen's exception if any,
   who uses it, for which task, how often, and where.
2. **Flow** (Mermaid flowchart), when the task crosses screens or branches.
3. **Wireframes** (ASCII), one per screen or dialog, in its main state.
4. **Components and states**: a table of the areas and controls, their behavior, and
   every screen state.
5. **UI text**: the labels, buttons, and messages, in the UI language.
6. **Decisions and open points**: trade-offs made because of the direction, rejected
   alternatives, and every question still open for the developer.

## Languages

- The design document (explanations, tables, decisions) is written in the language of
  the conversation.
- UI text (in wireframes and the UI text table) is in the program's UI language:
  English by default, or the language of the project's existing screens. Write it as
  final text, ready for resources, not as placeholders.
- Chinese, Japanese, and Korean characters are about two columns wide in monospaced
  fonts and break the alignment of ASCII wireframes. Keep text inside wireframes in
  English (or another Latin-script UI language) and put notes in the document's language
  outside the box, with numbered markers.

## Flow

See information-architecture.md for the flowchart conventions. Mark the frequent path
and the number of steps on it.

## Wireframes

Draw with plain ASCII so that the wireframe survives every editor, terminal, and diff:

| Element | Notation |
|---------|----------|
| Window or area border | `+----+` and `\|`; a horizontal line `+----+` splits areas |
| Button | `[ Save ]`; the primary action marked `[*Save*]` when it is not obvious |
| Text field | `[__________]`, with a value `[Contoso Ltd.__]` |
| Combo box | `[Select a carrier_v]` |
| Check box / radio button | `[x] Label` `[ ] Label` / `(o) Label` `( ) Label` |
| Table | A header row of column names, then two or three sample rows |
| Tabs | `\| Orders \| *Customers* \| Reports \|`, the active tab between asterisks |
| Icon | A short word in angle brackets: `<search>`, `<warning>` |
| Note marker | `(1)`, `(2)` in the wireframe, explained below it |
| Scroll area | `:` in the right margin, or a note |

- Use realistic sample data (names, amounts, long and short values), not "Lorem ipsum"
  or "xxx"; it shows whether columns and labels fit.
- Keep wireframes at most about 100 characters wide; draw the smallest supported window
  size, and note what grows when the window grows.
- Draw other states (empty, error) as separate small wireframes only when the layout
  changes; otherwise describe them in the states table.

```
+--------------------------------------------------------------------------+
| Orders                                   [ New order  Ctrl+N ] <search>  |
+--------------------------------------------------------------------------+
| Status [All_______v]  Date [Today____v]                       (1)        |
|                                                                          |
| Order    Customer          Delivery     Total     Status                 |
| 10482    Contoso Ltd.      2026-11-20   198.75    Open                   |
| 10481    Fabrikam GmbH     2026-11-19    42.00    <warning> On hold      |
| 10480    Tailspin Toys     2026-11-18   980.10    Shipped                |
|                                                                        : |
+--------------------------------------------------------------------------+
| 3 orders                                                                 |
+--------------------------------------------------------------------------+
```

(1) Filters are remembered per user; Ctrl+F moves focus to the search box.

## Components and states

| Area | Element | Behavior | Keyboard |
|------|---------|----------|----------|
| Toolbar | New order | Opens order entry for a new order | Ctrl+N |
| Toolbar | Search | Filters by order number or customer name while typing | Ctrl+F |
| List | Order rows | Double-click or Enter opens the order; multi-select for bulk actions | Arrows, Shift, Ctrl+A |
| List | Status column | Text plus an icon for "On hold" | |

| State | What the user sees |
|-------|--------------------|
| Empty (no orders today) | "No orders for today." with the New order button highlighted |
| No results | "No orders match "Fab"." with a Clear filters link |
| Loading | Rows keep their layout; a busy indicator in the list area after 1 s |
| Error (cannot load) | A message bar: "Orders could not be loaded. Check the network connection, then press F5." |

## UI text

List every new or changed text so it can go into resources and be reviewed in one
place:

| Key | Text | Where |
|-----|------|-------|
| `Orders.Empty` | No orders for today. | Empty list |
| `Orders.NoResults` | No orders match "{0}". | List after search |
| `Orders.LoadFailed` | Orders could not be loaded. Check the network connection, then press F5. | Message bar |
| `Orders.Count` | {0} orders | Status bar (needs plural forms) |

Follow the wording rules in forms-tables-feedback.md.

## Decisions and open points

```markdown
### Decisions
- Easy to use: the list and the filters share one screen; new orders open with Ctrl+N
  without leaving the list.
- Deleting an order is not on the toolbar: orders are cancelled, not deleted (keeps the
  history), through the order's details.

### Open points
1. May an order on hold still be edited, or only released?
2. Which columns does the team lead need that clerks do not?
```

Do not resolve open points by assumption; ask the developer and update the design when
they answer.

## Operation manual

The design is the plan; the operation manual is what a user reads once the screen
exists. When the program has a manual, a design that adds, changes, or removes a view,
a control, a shortcut, or a message lists the manual among the files to update, and the
control names, states, and texts of the manual are the ones of the Components and
states table and the UI text above. Format, structure, and the rules for the control
table: operation-manual.md.
