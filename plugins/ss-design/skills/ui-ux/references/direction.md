# Direction: easy to use or easy to learn

Every UI trade-off (density, shortcuts, confirmations, guidance) goes one way for
skilled daily users and the other way for newcomers. Decide the direction first, with
the developer, and design everything else from it.

## Ask the developer

Ask directly, in the conversation's language, before proposing any layout. Do not
guess from the code, the screen type, or the request. A wording to adapt:

> Before designing, which direction should this program follow?
> 1. **Easy to use**: the same trained people use it every day; speed and few steps
>    matter most.
> 2. **Easy to learn**: new or occasional users must succeed without training; clarity
>    and protection against mistakes matter most.
>
> Are there screens whose use differs from the rest (for example a settings screen
> used twice a year)?

If the developer is unsure, explain the consequences in the table below and ask again;
do not decide for them. If the request covers one screen of an existing program, ask
for the program's direction and whether this screen is an exception.

## The two directions

| | Easy to use | Easy to learn |
|---|---|---|
| Typical users | Operators, clerks, engineers who use it daily | Customers, occasional staff, rotating or temporary workers |
| Success measure | Time and steps per task, errors at speed | First-time success without help, errors avoided |
| Training | Expected; the program may assume knowledge | None; the screen must explain itself |
| Typical risk | Slowness, too many clicks, interruptions | Confusion, wrong choices, abandoned tasks |

## What the direction changes

| Topic | Easy to use | Easy to learn |
|---|---|---|
| Navigation | Frequent tasks one step away; everything on one or few screens; tabs or a sidebar always visible | Fewer choices per screen; a guided order (wizard, steps) for multi-step tasks |
| Density | Compact rows and spacing, more columns, more on screen | Comfortable spacing, fewer fields per screen, generous labels |
| Labels and icons | Short labels; icons allowed for well-known actions, with tooltips | Full text labels; icons only next to text |
| Input | Keyboard first: Tab order, Enter to confirm, type-ahead, paste of lists | Pickers, examples, and constrained choices; inline hints |
| Shortcuts | Shortcuts for every frequent action, shown in menus and tooltips | Standard keys only (Enter, Esc, Tab, Ctrl+C/V); shortcuts are optional extras |
| Confirmations | Few; prefer undo; confirm only irreversible, high-cost actions | Confirm actions with consequences, and explain the consequence |
| Defaults | Remember the last values and the user's layout | Safe defaults that work for most people |
| Help | Reference help on demand; no tips in the way | Inline hints, empty states that say what to do, first-run guidance |
| Batch work | Multi-select and bulk actions, keyboard selection | One item at a time, unless the task is truly bulk |
| Customization | Column choice, saved filters, layouts | Little or none; one well-chosen layout |
| Errors | Concise messages; let the user fix and continue fast | Explain what happened, why, and exactly what to do |

Each topic reference describes the rules for both directions; apply the column of the
chosen direction and say so in the design.

## Program direction and screen exceptions

- The program has **one main direction**. It sets navigation, density, and the style
  of messages for the whole application, so the screens feel like one program.
- A screen **may take the other direction** when its users or frequency differ.
  Typical exceptions:
  - In an easy-to-use program: setup, yearly export, first-run configuration, and
    rarely used administration screens are easy to learn.
  - In an easy-to-learn program: an administration screen used daily by one trained
    person may be easy to use.
- An exception changes that screen's density, guidance, and confirmations; it keeps
  the program's navigation, terms, button order, and visual style.

Record the decision at the top of the design:

```markdown
**Direction**: Easy to use (program): order clerks enter 200+ orders a day.
**Exceptions**: Export settings: Easy to learn (used twice a year, by anyone on the team).
```

## Serving the other group without changing direction

- **Easy to use, with newcomers sometimes**: keep the dense screen, and add discoverable
  help that does not slow experts (tooltips with shortcuts, a help panel they can close,
  empty states with one hint).
- **Easy to learn, with some regular users**: keep the guided screen, and add
  accelerators that stay out of the way (shortcuts, remembering the last choice,
  "skip" on a wizard's introduction page).
- **Progressive disclosure** serves both: basic options visible, advanced ones behind
  "More options" or an expander.

## When the direction changes later

If the developer changes the direction, revisit navigation depth, density,
confirmations, labels and icons, and message style on every screen; these are the
decisions the direction drove.
