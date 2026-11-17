# Sources

The rules of this skill condense established usability sources. No single source is
the top principle: the program's direction decides trade-offs between rules (see
direction.md), and accessibility and protection against data loss are never traded
away. Use this page to cite where a rule comes from, or to cross-check a design against
a well-known list.

## Precedence

1. Safety requirements of the machine, and laws or contracts the program must meet
   (for example an accessibility requirement in a customer contract).
2. Accessibility (WCAG contrast, keyboard access, no color alone) and protection
   against data loss: met in both directions.
3. The direction: decides every other trade-off.
4. The core rules and the topic references.
5. Platform conventions and the application's own conventions, unless they are the
   problem being fixed.

## Nielsen's 10 usability heuristics

Jakob Nielsen's heuristics (1994) are a general list for heuristic evaluation. They
do not resolve trade-offs; several of them pull in opposite directions (7 against 8),
which the direction decides.

| # | Heuristic | Where this skill covers it |
|---|-----------|----------------------------|
| 1 | Visibility of system status | Core rule 7; UX-B1, UX-B2, UX-O3 |
| 2 | Match between the system and the real world | Core rule 2; UX-B7, UX-W1, UX-T1 |
| 3 | User control and freedom | UX-F5, UX-B2 (cancel), UX-B3 (undo) |
| 4 | Consistency and standards | Core rule 5; UX-D2, UX-L7, UX-W1, UX-W2 |
| 5 | Error prevention | Core rule 6; UX-I3, UX-I6, UX-B3, UX-O4 |
| 6 | Recognition rather than recall | UX-I1, UX-I7, UX-K5, UX-T2 |
| 7 | Flexibility and efficiency of use | The easy-to-use direction; UX-F2, UX-K5, UX-T5, UX-I6 |
| 8 | Aesthetic and minimalist design | Core rule 4; UX-F1, UX-L2, UX-L4, UX-B6 |
| 9 | Help users recognize, diagnose, and recover from errors | UX-I4, UX-B7, UX-O5 |
| 10 | Help and documentation | direction.md (Help row); UX-W6, UX-T3 |

## ISO 9241-110 interaction principles

| Principle | Where this skill covers it |
|-----------|----------------------------|
| Suitability for the user's tasks | Core rule 2; UX-F1, UX-I2 |
| Self-descriptiveness | The easy-to-learn direction; UX-I1, UX-B7 |
| Conformity with user expectations | Core rule 5; UX-L7, UX-K3, UX-W1 |
| Learnability | The easy-to-learn direction; UX-F3, UX-W6 |
| Controllability | UX-F5, UX-B2, UX-K1 |
| Use error robustness (error tolerance) | Core rule 6; UX-I4, UX-B3 |
| User engagement | Core rule 7; UX-B6 |

ISO 9241-11 defines usability as effectiveness, efficiency, and satisfaction in a
context of use; the two directions weigh efficiency (easy to use) and first-time
effectiveness (easy to learn).

## Accessibility and platform

| Source | Used for |
|--------|----------|
| WCAG 2.x | Contrast 4.5:1 and 3:1, no color alone, keyboard access, visible focus, target size (UX-K1, UX-K4, UX-K6, UX-K7) |
| Windows app design guidelines (Microsoft) | Dialog button order and placement, sentence-style capitalization, standard shortcuts, DPI scaling (UX-L7, UX-W2, UX-K5, UX-K8) |

## Laws and findings behind specific rules

| Finding | Rule |
|---------|------|
| Response time limits 0.1 s / 1 s / 10 s (Miller 1968; Card, Robertson, and Mackinlay 1991; Nielsen 1993) | Core rule 7; feedback table in forms-tables-feedback.md |
| Fitts's law: time to hit a target grows with distance and shrinks with size | Touch target sizes, destructive actions placed apart (UX-L3, UX-O1, UX-O4) |
| Hick's law: decision time grows with the number of choices | Fewer choices per screen and progressive disclosure for easy to learn (direction.md) |
| Gestalt proximity and similarity | Grouping by spacing, consistent styling of the same kind of element (UX-L2, UX-L4) |
| Norman's gulfs of execution and evaluation | The walkthrough questions in usability-accessibility.md (does the user know what to do, see how, understand the feedback) |
| Shneiderman's eight golden rules | Shortcuts for frequent users, informative feedback, easy reversal, reduced memory load |
| Nielsen and Landauer (1993): a few test users find most problems | Three to five users per test round (usability-accessibility.md) |
