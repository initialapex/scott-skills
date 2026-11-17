# Field operation

Screens used at a work station on the floor (warehouse, production line, laboratory,
service counter) differ from office screens: users stand, wear gloves, look from a
distance, are interrupted, and a wrong touch can stop work or damage something. Apply
this reference to every screen that runs in such a place, together with the program's
direction.

## Context to find out first

Ask about each of these if the request does not say:

- Input: touch screen, mouse, keyboard, barcode scanner, physical buttons.
- Gloves, wet or dirty hands.
- Viewing distance and angle; standing or sitting; lighting and glare.
- Noise (whether sounds can be heard).
- How long people watch the screen (a quick step, or monitoring for hours).
- Who uses it: trained operators (usually easy to use) or rotating and temporary staff
  (usually easy to learn).
- What a mistake costs (a re-scan, a stopped line, scrapped material, a safety risk).

## Touch targets

- Targets at least about 10 x 10 mm (about 40-48 px at 100% scaling on common
  screens), 15 mm or more for gloves, with at least 2-3 mm between targets.
- Use the full height of a row as its target, not a small icon in it.
- No hover-only information or actions (tooltips, menus that open on hover): a finger
  cannot hover.
- No small controls that need precision: thin scroll bars, splitters, tiny check boxes,
  drag-and-drop as the only way, double-tap.
- Text input by touch is slow; prefer scanning, selection from large buttons, numeric
  keypads, and defaults. When a keyboard is needed, the on-screen keyboard must not
  cover the field.

## Readable at a distance

- Size text for the real viewing distance and check it on the real screen at the real
  distance; body text on field screens is usually much larger than in the office.
- The values the operator watches (current step, count, status) are the largest
  elements on the screen.
- High contrast, no thin fonts, no low-contrast gray text; a light theme often reads
  better in bright halls, a dark theme in dim rooms. Decide with the developer.

## Status at a glance

- The current state of the station is visible from across the room: a large status
  area with color, text, and icon together ("RUNNING", "WAITING FOR MATERIAL",
  "STOPPED - check the door").
- Fixed layout: every piece of information always appears in the same place, so
  position carries meaning and nothing jumps when values change.
- Show what the operator must do next, not only what the system is doing.

## Protection against mis-touch

- Separate frequent actions from consequential ones by distance and by style; never
  place "Stop" or "Reset" next to "Next".
- Actions with serious consequences need a deliberate gesture: a two-step action
  (select, then confirm in a different place), press-and-hold with visible progress, or
  a confirmation that names the consequence.
- Avoid actions on swipe, accidental palm touches near screen edges, and buttons under
  where the hand rests.
- A screen lock or a login for changes to settings and recipes; operation screens stay
  usable without extra steps.
- Safety functions (emergency stop, interlocks) are hardware and follow the machine's
  safety requirements; the screen shows their state but never replaces them.

## Alarms and messages

- Classify alarms by priority (for example critical, warning, information) with a
  consistent color, icon, and sound per class.
- Critical alarms stay on screen until acknowledged; they never vanish on a timer.
  Acknowledging is separate from fixing: the alarm shows "acknowledged, still active"
  until the cause is gone.
- Every alarm says what happened, where, and what to do, in words the operator uses.
- Keep an alarm history with time, acknowledgement, and who acknowledged.
- Avoid alarm floods: group follow-on alarms under their cause.
- Sound only for what needs attention now; also show it visually, for noisy places.

## Interruptions

- Work in progress survives interruptions: a half-finished step stays as it was when
  the operator returns; no timeouts that discard input.
- After a restart or a lost connection, the screen shows what state the work is in and
  how to continue.

## Example

A packing station with a touch screen and a barcode scanner, easy to learn (temporary
staff), viewed from about 1 m:

```
+------------------------------------------------------------------------+
|  PACKING STATION 3                                    Operator: J. Lee |
+------------------------------------------------------------------------+
|                                                                        |
|                 SCAN THE NEXT ITEM                                     |
|                                                                        |
|   Order 10482            Items packed   3 of 5                         |
|                                                                        |
|   [x] Widget A  x2       [x] Cable kit  x1       [ ] Widget B  x2      |
|                                                                        |
+------------------------------------------------------------------------+
|  [  Item missing  ]                    [  Hold to cancel order  ]      |
+------------------------------------------------------------------------+
```

- The instruction is the largest text; scanning advances without touching the screen.
- "Item missing" opens a large-button choice; cancelling the order needs a hold and is
  far from the frequent area.
