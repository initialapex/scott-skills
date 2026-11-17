---
name: ui-ux
description: 'Design and review user interfaces and user experience, mainly for desktop applications - first asking the developer whether the program must be easy to use (efficiency for frequent, skilled users) or easy to learn (new or occasional users), then information architecture, screens and navigation, task flows, empty, loading, and error states, layout, spacing, density, visual hierarchy, color and contrast, forms and input validation messages, data tables, confirmations and undo, progress and error feedback, UI wording, keyboard operation and focus order, accessibility, DPI scaling, localization, and touch screens for field operators - with Mermaid flows and ASCII wireframes. Use this whenever the user designs a new screen, window, dialog, wizard, or workflow, asks how a screen should be laid out or behave, asks whether a UI is easy to use, wants a usability or UX review of a screen, screenshot, or mockup, wants to simplify a cluttered screen, or writes UI messages and labels, in WPF or any other UI technology. Not for XAML styling or MaterialDesignThemes implementation, not for ViewModel or class design, and not for logo, brand, or marketing graphics.'
---

# UI and UX design

Design screens around the tasks people do with them, in the direction the program
needs, and review existing screens against the same rules. The principles are
technology-independent; examples are Windows desktop business and tool applications
(WPF), with notes where the web differs.

## Step 1: Decide the direction first

Before any layout, ask the developer which direction the program follows, and wait for
the answer. Do not infer it from the code or the request.

- **Easy to use**: frequent, trained users; the goal is speed and few steps (dense
  screens, shortcuts, few confirmations, batch actions).
- **Easy to learn**: new or occasional users; the goal is success without training
  (clear labels, guidance, one thing at a time, protection against mistakes).

The program has one main direction; a screen may take the other one when its use
differs, and the design says so. How to ask, what each direction changes, and how to
record exceptions: [references/direction.md](references/direction.md).

## Step 2: Understand the context

1. Who uses each screen, for which task, how often, and where (office desk with mouse
   and keyboard, or a field station with a touch screen).
2. Read the existing screens and their conventions (navigation, button order, terms,
   message style); follow them unless they are the problem.
3. Find the UI technology and its controls, the smallest screen and the display scaling
   to support, and the UI language. UI text defaults to English and may be translated
   later; the design document itself is written in the conversation's language.
4. List what is still unclear (rules, edge cases, who may do what) and ask about each;
   do not invent business rules.

## Step 3: Pick the mode

- **Design**: a new screen, dialog, wizard, or flow. Produce the output of
  [references/design-output.md](references/design-output.md): direction, flow, ASCII
  wireframes, component and state table, UI text, open points.
- **Improve**: an existing screen is cluttered, slow, or confusing. List the problems
  with the checklist ids, then propose the redesign in the same output format.
- **Review**: the developer asks whether a screen, screenshot, mockup, or XAML is good.
  Go through [references/review-checklist.md](references/review-checklist.md) and report
  each finding with its location, the rule, and the fix.

## Core rules

1. **The direction decides the trade-offs.** State it at the top of every design and
   review; when two rules conflict, the direction wins. It never overrides
   accessibility (rules 8 and 9, the *must* items of UX-K) or protection against data
   loss and unsafe actions; the direction only changes how they are met.
2. **Design for tasks, not for tables.** Screens follow what users do and in which
   order, not the shape of the database or the class model.
3. **Every screen has all its states.** Empty, loading, normal, partly loaded, error,
   read-only, and no permission are designed, not left to chance.
4. **One primary action per screen or area.** Visual weight follows importance; the
   primary action is easy to find, a destructive one is hard to hit by accident.
5. **Same thing, same name, same place.** Follow the platform's conventions and the
   application's own; a term means one thing everywhere.
6. **Prevent errors before reporting them.** Constrain input, offer defaults, and make
   messages say what happened and what to do next.
7. **Answer every action.** Visible feedback within 0.1 s, a busy state after 1 s, and
   progress with a way to cancel after about 10 s.
8. **Everything works from the keyboard**, focus order follows reading order, and focus
   is always visible.
9. **Never rely on color alone**, and keep text contrast at 4.5:1 or more.
10. **Text will grow.** Leave room for translation, never build sentences from
    fragments, and never put text in images.

## Topic references

| Topic | Reference |
|-------|-----------|
| Easy to use versus easy to learn: asking, consequences per topic, screen exceptions | [references/direction.md](references/direction.md) |
| Information architecture: tasks, screens, navigation, windows and dialogs, flows, states | [references/information-architecture.md](references/information-architecture.md) |
| Layout and visual hierarchy: alignment, spacing, density, typography levels, color, window size | [references/layout.md](references/layout.md) |
| Forms, data tables, feedback, confirmations and undo, UI wording | [references/forms-tables-feedback.md](references/forms-tables-feedback.md) |
| Usability and accessibility: keyboard, focus, contrast, DPI scaling, text length, localization, testing | [references/usability-accessibility.md](references/usability-accessibility.md) |
| Field operation: touch screens, viewing distance, status at a glance, protection against mis-touch, alarms | [references/field-operation.md](references/field-operation.md) |
| Design output: flow, ASCII wireframes, component and state table, UI text, open points | [references/design-output.md](references/design-output.md) |
| Review checklist | [references/review-checklist.md](references/review-checklist.md) |
| Sources: Nielsen's heuristics, ISO 9241-110, WCAG, Windows guidelines, and the laws behind the rules, mapped to the checklist ids | [references/sources.md](references/sources.md) |

## Boundaries

- This skill produces designs and review findings, not XAML. In WPF, styles, themes,
  and controls follow `ss-wpf:material-design`, ViewModels, commands, and bindings
  follow `ss-wpf:mvvm`, and the classes behind them follow `ss-design:ood`, when those
  skills are installed.
- Logo, brand identity, illustration, and marketing graphics are out of scope.
- Safety functions of machines (emergency stop, interlocks) follow the machine's safety
  requirements; a screen never replaces them.
- Comments in any generated code are in English, whatever language the conversation
  uses.
