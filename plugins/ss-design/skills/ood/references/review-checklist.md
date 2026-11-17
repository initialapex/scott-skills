# Review checklist

Review the changed classes (or the files or design the developer named) against these
items. Report each finding as:

```
<file>:<line>  [<item id>] <what is wrong>  →  <the fix>
```

For a design document without code, use the class name instead of `file:line`.

Group findings by severity: **must fix** (broken invariants, wrong dependency
direction, untestable core logic, swallowed errors), **should fix** (rule violations
that will make the next change harder), **consider** (naming and polish). Say which
language version you reviewed against. Code that was not changed in this request is
reported only when it blocks the change; list other smells separately as "later".

The ids start with `OOD-` so they do not collide with other checklists (`ss-wpf:mvvm`,
`ss-wpf:material-design`) used in the same review.

## OOD-R: Responsibilities and coupling

- OOD-R1 Each class has one reason to change; its responsibility fits one sentence.
  *(should)*
- OOD-R2 Behavior sits with the data it uses; no feature envy, no anemic data classes
  driven by "manager" or "helper" classes. *(should)*
- OOD-R3 Dependencies come through the constructor; no `new` on services, static
  singletons, service locators, or hidden `DateTime.Now` / file / device access in
  business logic. *(must)*
- OOD-R4 No circular dependencies between classes, namespaces, or projects. *(must)*
- OOD-R5 Interfaces exist at boundaries and real variation points, are named by role,
  and are small; no interface per class without a reason. *(should)*
- OOD-R6 No long chains through other objects' structure (Law of Demeter). *(consider)*

## OOD-S: SOLID and inheritance

- OOD-S1 Adding a new case of a known variation adds a class, not edits to several
  `switch` statements. *(should)*
- OOD-S2 Subclasses honor the base contract: no overrides that throw
  `NotSupportedException` or do nothing to disable behavior. *(must)*
- OOD-S3 Inheritance is used only for true "is a" relationships; otherwise composition.
  Classes are `sealed` unless designed for inheritance. *(should)*
- OOD-S4 Clients depend only on the members they use (no fat interfaces forcing empty
  implementations). *(should)*
- OOD-S5 Business rules do not depend on UI, database, or framework types. *(must)*

## OOD-P: Patterns

- OOD-P1 Each pattern solves a problem that exists now; the design names it. *(should)*
- OOD-P2 No speculative abstractions: interfaces with one implementation and no test
  need, unused extension points, factories for one class. *(consider)*
- OOD-P3 The project's existing solution to the same problem is reused instead of a
  second one (its result type, repository base, event mechanism). *(should)*
- OOD-P4 Event subscribers that live shorter than the source unsubscribe. *(must)*
- OOD-P5 Repositories are per aggregate and do not leak storage types (`IQueryable`,
  ORM entities, SQL). *(should)*

## OOD-D: Domain model

- OOD-D1 Objects are valid after construction and after every public method; state
  changes go through methods that check the rules. *(must)*
- OOD-D2 Collections are not exposed for outside modification. *(must)*
- OOD-D3 Concepts with rules are value objects, not primitives (money with currency,
  ranges, typed ids). *(should)*
- OOD-D4 Parts of an aggregate are changed only through the root; other aggregates are
  referenced by id. *(should)*
- OOD-D5 Dependencies point inward: domain and application layers do not reference
  infrastructure or presentation. *(must)*
- OOD-D6 Names use the domain's language; no `Manager`, `Helper`, `Util`, `Data`,
  `Info` classes. *(consider)*

## OOD-E: Errors

- OOD-E1 Public methods validate arguments with guard clauses and clear messages.
  *(should)*
- OOD-E2 No swallowed exceptions; no `catch (Exception)` except in last-resort handlers
  or with a stated reason. *(must)*
- OOD-E3 `throw;` keeps the stack trace; translated exceptions keep the inner
  exception. *(must)*
- OOD-E4 Expected business outcomes are return values (result, `Try...`), not
  exceptions; programming errors are exceptions. *(should)*
- OOD-E5 Methods do not return `null` collections; nullable returns are documented.
  *(should)*
- OOD-E6 Resources are released on every path (`using`, `finally`). *(must)*

## OOD-C: Code shape

- OOD-C1 Methods do one thing and read top to bottom; deep nesting is replaced by guard
  clauses or extracted methods. *(should)*
- OOD-C2 Parameter lists are short; groups that travel together are objects. *(consider)*
- OOD-C3 No duplicated logic with small variations. *(should)*
- OOD-C4 No boolean flag parameters that switch behavior. *(consider)*
- OOD-C5 The code compiles in the project's language version (C# 7.3 projects: no
  records, `init`, switch expressions, nullable reference types, or default interface
  members). *(must)*
- OOD-C6 Comments explain why, not what; code comments are in English. *(consider)*
