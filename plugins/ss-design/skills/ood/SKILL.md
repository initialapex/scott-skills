---
name: ood
description: 'Design, refactor, and review object-oriented code and class structures - responsibilities, cohesion and coupling, SOLID, composition versus inheritance, interfaces and testability, design patterns (Strategy, State, Observer, Command, Factory, Builder, Adapter, Decorator, Facade, Composite, Null Object, Repository, Specification, Unit of Work, Result), domain models (entities, value objects, aggregates, invariants, layering), code smells, refactoring, and error handling - with Mermaid class and sequence diagrams. Use this whenever the user designs classes or modules for a feature, asks which pattern fits, wants a class diagram, asks whether a design or class is good, wants to split a large class or untangle dependencies, refactors smelly code (including a single method with too many parameters, boolean flag arguments, deep nesting, or duplicated logic), or asks for a design review, in C# or any other object-oriented language. Not for UI layout or UX, not for framework-specific MVVM wiring, and not for database schema or infrastructure design.'
---

# Object-oriented design

Design and review classes so that each has one clear job, dependencies point toward
stable policy, and change stays local. The principles are language-independent;
examples are C# limited to C# 7.3 syntax so they compile in .NET Framework projects.
Translate them to the project's language and version when it differs.

## Step 1: Understand before designing

1. Read the existing code around the change: the classes it touches, their callers,
   and the project's conventions (naming, folders, error handling, test style). Follow
   the existing design unless it is the problem being solved.
2. List what the design depends on and is not yet clear (rules, edge cases, what may
   change later, performance limits). Ask the developer about each before designing;
   do not invent business rules.
3. Find the language version (for C#, `<LangVersion>` or the target framework) so the
   proposed code compiles.

## Step 2: Pick the mode

- **Design**: a new feature, module, or class structure. Produce the design in the
  format of [references/diagrams.md](references/diagrams.md): responsibilities table,
  Mermaid class diagram, sequence diagram when the interaction has more than a few
  steps, and the open points.
- **Refactor**: existing code is hard to change or test. Name the smells
  ([references/smells-refactoring.md](references/smells-refactoring.md)), propose
  small behavior-preserving steps, and keep the code working after each step.
- **Review**: the developer asks whether a design or code is good, or a change needs a
  design review. Go through [references/review-checklist.md](references/review-checklist.md)
  and report each finding with `file:line`, the rule it breaks, and the fix.
- **Which pattern?**: start from the problem, not the pattern; use the selection table
  in [references/patterns.md](references/patterns.md).

## Core rules

1. **One reason to change per class.** If describing a class needs "and", or two
   different people would ask for its changes, split it.
2. **Put behavior with the data it uses.** A method that mostly reads another object's
   fields belongs on that object. Avoid anemic data bags plus "manager" classes that do
   all the work.
3. **Depend on abstractions at boundaries, on concrete classes inside.** Interfaces
   go where something varies or must be replaced in tests (I/O, devices, time, external
   services); not on every class.
4. **Prefer composition over inheritance.** Inherit only for a true "is a" with shared
   invariants; otherwise compose and delegate.
5. **Protect invariants.** Objects are valid after construction and after every public
   method; state changes go through methods that check the rules, not public setters.
6. **Make dependencies explicit.** Constructor parameters, no hidden statics or service
   locators, no circular dependencies between classes or layers.
7. **Simplest design that meets today's known needs.** Add a pattern or an abstraction
   when a second case or a real variation exists, not in advance. Say what would
   justify it later.
8. **Errors are part of the design.** Decide per operation whether failure is an
   exception, a result value, or a valid state ([references/error-handling.md](references/error-handling.md)).
9. **Names say what things are in the domain's language.** `RecipeScheduler`, not
   `RecipeHelper`, `Manager`, `Util`, or `Data`.

## Topic references

| Topic | Reference |
|-------|-----------|
| Responsibilities, cohesion and coupling, SOLID, composition, interfaces and testability | [references/responsibilities.md](references/responsibilities.md) |
| Pattern selection table and when not to use a pattern | [references/patterns.md](references/patterns.md) |
| Behavioral patterns: Strategy, State, Template Method, Observer, Command, Null Object | [references/patterns-behavioral.md](references/patterns-behavioral.md) |
| Creational and structural patterns: Factory Method, Abstract Factory, Builder, Adapter, Decorator, Facade, Composite | [references/patterns-structural.md](references/patterns-structural.md) |
| Application patterns: Repository, Specification, Unit of Work, Result | [references/patterns-application.md](references/patterns-application.md) |
| Domain model: entities, value objects, aggregates, invariants, layers and dependency direction | [references/domain-model.md](references/domain-model.md) |
| Code smells and the refactorings that fix them | [references/smells-refactoring.md](references/smells-refactoring.md) |
| Errors: exceptions, result values, validation, guard clauses | [references/error-handling.md](references/error-handling.md) |
| Design output: responsibilities table, Mermaid class and sequence diagrams | [references/diagrams.md](references/diagrams.md) |
| Review checklist | [references/review-checklist.md](references/review-checklist.md) |

## Boundaries

- WPF ViewModels, commands, bindings, and DI wiring follow `ss-wpf:mvvm` when it is
  installed; this skill covers the Model and service classes behind them and general
  class design.
- Screen layout, flows, and usability belong to `ss-design:ui-ux` when it is installed.
- Database schemas, deployment, and infrastructure are out of scope; mention the
  dependency and leave the decision to the developer.
- Comments in generated code are in English, whatever language the conversation uses.
