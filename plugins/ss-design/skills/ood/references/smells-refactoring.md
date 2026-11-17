# Code smells and refactorings

A smell is a hint, not a verdict: check whether it actually makes change or testing
harder before refactoring. Refactoring changes structure, never behavior.

## Working method

1. **Protect the behavior first.** If the code has no tests, add characterization
   tests around the part being changed (what it does today, even if odd), or name the
   manual check that will confirm it.
2. **Small steps.** One refactoring at a time; the code builds and the tests pass after
   each. Prefer the IDE's automated refactorings (rename, extract method, move) over
   hand edits.
3. **Separate refactoring from feature work.** Refactor to make the change easy, then
   make the change. Keep them in separate commits when the project's process allows.
4. **Stop when the change is easy.** Do not refactor the whole class because one
   method was touched; list the remaining smells for later instead.

## Smells and their fixes

| Smell | Signs | Refactorings |
|-------|-------|--------------|
| **Large class / God class** | Hundreds of lines, many fields, unrelated method groups, name ends in `Manager` / `Helper` | Extract Class per responsibility (group methods by the fields they use); Extract Interface for clients that use one part |
| **Long method** | Needs scrolling, comments that label sections, deep nesting | Extract Method per section (the comment becomes the name); Replace Nested Conditional with Guard Clauses; Decompose Conditional |
| **Long parameter list** | More than three or four parameters, the same group passed together | Introduce Parameter Object or a value object (`DateRange`, `Address`); pass the object instead of its fields |
| **Feature envy** | A method uses another object's data more than its own | Move Method to that object |
| **Data class / anemic model** | Classes with only getters and setters, rules in "service" classes | Move behavior into the data class; make setters private; Encapsulate Collection |
| **Primitive obsession** | `decimal amount` + `string currency`, `string` ids, magic strings for states | Replace Primitive with Value Object; Replace Type Code with enum, Strategy, or State |
| **Switch on type** | The same `switch` / `if` chain over a type code in several places | Replace Conditional with Polymorphism; one factory keeps the only `switch` |
| **Shotgun surgery** | One change edits many classes | Move Method / Move Field to gather the rule in one class |
| **Divergent change** | One class changes for many unrelated reasons | Extract Class by reason for change |
| **Duplicated code** | Same logic copied with small differences | Extract Method; Pull Up Method; Form Template Method; or a shared value object |
| **Temporal coupling** | Methods must be called in a certain order (`Init` before `Run`) | Require the data in the constructor; return a new object from each step; Builder |
| **Hidden dependencies** | `DateTime.Now`, static singletons, `new` on services inside methods | Inject through the constructor (`IClock`, interfaces) |
| **Inappropriate inheritance** | Subclass overrides to do nothing or throw; base class holds unrelated helpers | Replace Inheritance with Delegation; Extract Superclass only for real shared behavior |
| **Message chains** | `a.B.C.D.Do()` | Hide Delegate (a method on `a`); pass the needed object in |
| **Speculative generality** | Interfaces with one implementation and no test need, unused parameters, abstract base classes "for later" | Inline Class / Inline Interface; Remove Parameter |
| **Comments explaining unclear code** | Comments that say *what* the code does | Rename and extract until the comment is unnecessary; keep comments that say *why* |
| **Boolean parameters** | `Save(order, true, false)` | Split into named methods, or use an enum or options object |

## Example: extract and move

```csharp
// Before: pricing rules live in the screen's handler and need the order's internals
public decimal CalculateTotal(Order order)
{
    decimal total = 0;
    foreach (OrderLine line in order.Lines)
    {
        decimal lineTotal = line.UnitPrice.Amount * line.Quantity;
        if (line.Quantity >= 10)
        {
            lineTotal = lineTotal * 0.95m;   // bulk discount
        }

        total += lineTotal;
    }

    if (order.Customer.IsPremium)   // reaches into another aggregate
    {
        total = total * 0.98m;
    }

    return total;
}

// After: each rule lives with its data; the discount for premium customers is a
// parameter, because the order does not own customer data.
public sealed class OrderLine
{
    private const int BulkQuantity = 10;
    private const decimal BulkDiscount = 0.05m;

    public Money Total()
    {
        Money gross = UnitPrice * Quantity;
        return Quantity >= BulkQuantity ? gross * (1 - BulkDiscount) : gross;
    }
}

public sealed class Order
{
    public Money Total(decimal customerDiscount)
    {
        Money lines = _lines.Select(l => l.Total()).Aggregate((a, b) => a + b);
        return lines * (1 - customerDiscount);
    }
}
```

Report the steps taken: Extract Method (line total), Move Method (to `OrderLine`),
Replace Magic Number with Constant, Introduce Parameter (customer discount). Behavior
is unchanged except where noted.

## Large legacy classes

For a class too big to refactor at once:

- **Sprout**: write new behavior in a new, tested class and call it from the old code.
- **Wrap**: put a new class in front of the old one and move callers over gradually.
- **Extract by seam**: pull out the part with the clearest boundary first (often I/O
  or a calculation), behind an interface, so the rest becomes testable.

Do not rewrite a working legacy class from scratch without the developer's agreement;
propose the incremental path and its first step.
