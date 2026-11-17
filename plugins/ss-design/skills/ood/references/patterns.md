# Choosing a pattern

Start from the problem in the code, find it in the table, and check the "not when"
column before using the pattern. A pattern is justified by a variation or a constraint
that exists now; name it in the design ("Strategy, because carriers differ in rate
rules and two more are planned").

## Selection table

| Problem | Pattern | Not when | Reference |
|---------|---------|----------|-----------|
| One algorithm or rule varies by case, chosen at run time | Strategy | Only one variant exists, or a plain `Func<>` parameter is enough | [behavioral](patterns-behavioral.md#strategy) |
| Behavior depends on the object's state, with many `if (state == ...)` in several methods | State | Two or three states with simple transitions; an enum and a guard per method read better | [behavioral](patterns-behavioral.md#state) |
| Several classes share the steps of an algorithm but differ in some steps | Template Method | The variations combine or change at run time (use Strategy) | [behavioral](patterns-behavioral.md#template-method) |
| Other objects must react to something without the source knowing them | Observer (C# events) | There is exactly one listener that is known; call it directly | [behavioral](patterns-behavioral.md#observer) |
| Operations must be queued, logged, undone, or retried as objects | Command | A direct method call with no undo or queue | [behavioral](patterns-behavioral.md#command) |
| Callers check for `null` before every use of an optional collaborator | Null Object | "Missing" must be handled differently by each caller | [behavioral](patterns-behavioral.md#null-object) |
| Which concrete class to create depends on input or configuration | Factory Method / simple factory | The caller always knows the concrete class | [structural](patterns-structural.md#factory-method-and-simple-factory) |
| Families of related objects must be created consistently | Abstract Factory | Only one family exists | [structural](patterns-structural.md#abstract-factory) |
| An object needs many optional parameters or step-by-step construction with validation | Builder | Two or three parameters; a constructor is clearer | [structural](patterns-structural.md#builder) |
| A class you cannot change has the wrong interface for your code | Adapter | You own both sides; change one of them | [structural](patterns-structural.md#adapter) |
| Add behavior (logging, caching, retry) around an object without changing it | Decorator | The behavior belongs in the class itself, or is needed only once | [structural](patterns-structural.md#decorator) |
| A subsystem is complicated to use and clients need a few simple operations | Facade | It hides nothing; it only forwards one-to-one | [structural](patterns-structural.md#facade) |
| Tree structures where a group and a single item are treated the same | Composite | The hierarchy is flat or the item types are treated differently | [structural](patterns-structural.md#composite) |
| Domain code needs to load and save aggregates without knowing the storage | Repository | A thin wrapper that only re-exposes an ORM's generic API | [application](patterns-application.md#repository) |
| A business rule for selecting or checking objects is repeated or combined | Specification | A single query used in one place | [application](patterns-application.md#specification) |
| Several changes must be committed or rolled back together | Unit of Work | One aggregate per transaction (the common case) | [application](patterns-application.md#unit-of-work) |
| An expected failure must be returned to the caller with a reason | Result | The failure is a programming error or truly exceptional (use exceptions) | [application](patterns-application.md#result) |

## Rules

- **No pattern without a second case or a real constraint.** Write the simple version
  first; refactor to the pattern when the second case arrives (the refactoring is
  usually small).
- **Use the language feature when it is the pattern.** C# events are Observer;
  delegates (`Func<>`, `Action<>`) are a one-method Strategy or Command; `IEnumerable`
  and `yield` are Iterator; `IDisposable` with `using` is scoped cleanup.
- **Name classes by their domain role**, and mention the pattern in a comment or the
  design, not in every class name (`StandardShipping`, not `ShippingStrategyImplA`).
- **One pattern per problem.** Stacking patterns (a factory of builders of strategies)
  is a smell unless each solves a named problem.
- **Patterns from the project win.** If the code base already solves the problem one
  way (its own Result type, its own repository base), follow it instead of introducing
  a second way.
