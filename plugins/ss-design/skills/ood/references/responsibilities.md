# Responsibilities, coupling, SOLID

Examples use one small domain throughout the ood references: orders, pricing, and
shipping. C# 7.3 syntax only.

## Assigning responsibilities

Ask, for each piece of behavior: *which object has the information to do this?* Give
the behavior to that object (information expert). Then check the result against
cohesion and coupling.

| Question | Good sign | Warning sign |
|----------|-----------|--------------|
| Can the class be described in one sentence without "and"? | "Calculates the price of an order" | "Loads orders and calculates prices and sends e-mails" |
| Do its methods use the same fields? | Most methods touch most fields (high cohesion) | Groups of methods that each use their own fields: two classes in one |
| How many other classes does it know? | A few, mostly abstractions or value types | Many concrete classes, statics, or the whole object graph |
| Who creates its collaborators? | Someone else passes them in | It calls `new` on services, or reaches a static/global |
| Where does a change in rule X land? | One class | Several classes edited together (shotgun surgery) |

**Tell, don't ask.** Ask an object to do something instead of pulling its data out and
deciding for it:

```csharp
// Ask: the caller decides with the order's data
if (order.Status == OrderStatus.Open && order.Lines.Count > 0)
{
    order.Status = OrderStatus.Submitted;
}

// Tell: the order owns the rule
order.Submit();   // throws if the order is not open or has no lines
```

**Law of Demeter.** A method talks to its own fields, its parameters, and objects it
creates; not to the friends of friends (`order.Customer.Address.Country.TaxRate`).
Long chains mean the caller knows too much of the structure; add a method closer to the
data (`order.ShippingCountry`), or pass in what is needed.

## SOLID, applied

**S: Single responsibility.** One reason to change. Split by who asks for changes, not
by "one method per class".

```csharp
// Before: pricing rules, persistence, and notification change for different reasons
public class OrderService
{
    public void Checkout(Order order) { /* compute total, save, send e-mail */ }
}

// After: each class changes for one reason
public sealed class PriceCalculator { public Money Total(Order order) { /* rules */ } }
public sealed class CheckoutService
{
    private readonly PriceCalculator _prices;
    private readonly IOrderRepository _orders;
    private readonly IOrderNotifier _notifier;

    public CheckoutService(PriceCalculator prices, IOrderRepository orders, IOrderNotifier notifier)
    {
        _prices = prices;
        _orders = orders;
        _notifier = notifier;
    }

    public void Checkout(Order order)
    {
        order.Submit(_prices.Total(order));
        _orders.Save(order);
        _notifier.OrderSubmitted(order);
    }
}
```

**O: Open/closed.** Adding a new case should mean adding a class, not editing a
`switch` in five places. Apply it where variation is real and recurring (shipping
carriers, discount rules), using Strategy or polymorphism. A single `switch` in a
factory is fine.

**L: Liskov substitution.** A subtype must work wherever the base type is expected:
no stronger preconditions, no weaker guarantees, no `NotSupportedException` for
inherited members. If a subclass has to disable base behavior, the hierarchy is wrong;
use composition or separate interfaces.

**I: Interface segregation.** Clients depend only on the members they use. Split
"fat" interfaces by client role (`IOrderReader` for queries, `IOrderWriter` for
changes) when different clients use different halves. Do not split interfaces that
always travel together.

**D: Dependency inversion.** Policy (business rules) does not depend on details
(database, file system, devices, clock, network). The policy layer defines the
interface it needs; the detail implements it.

```csharp
// Defined next to the policy that needs it, in terms of the domain
public interface IExchangeRates
{
    decimal Rate(string fromCurrency, string toCurrency);
}

// Implemented in the infrastructure layer
public sealed class BankExchangeRates : IExchangeRates { /* HTTP call */ }
```

## Composition over inheritance

Use inheritance only when all of these hold:

- the subtype *is a* base type for every client (Liskov holds),
- the base class's invariants hold for the subtype too,
- the variation is fixed at construction and not combined with other variations.

Otherwise compose: hold the varying part as a field behind an interface and delegate
to it. Composition lets variations combine (a carrier *and* a discount rule) without a
subclass per combination, and lets tests replace one part.

Prefer `sealed` classes by default in C#; open a class for inheritance deliberately,
with `protected` members designed for it (Template Method), or not at all.

## Interfaces and testability

- Introduce an interface for: I/O (files, database, network, devices), time
  (`IClock`), randomness, external services, and real variation points.
- Do not introduce an interface for: value objects, entities, pure calculation classes
  with no dependencies, or a class that has one implementation and no test need. Tests
  can use the real class.
- Name interfaces by role from the client's point of view (`IOrderRepository`,
  `IClock`), not after the implementation (`ISqlHelper`).
- Keep interfaces small and stable; a frequently changing interface is a sign that the
  abstraction is at the wrong level.
- A class is testable when every dependency comes through the constructor and every
  side effect goes through one of those dependencies. Static calls to
  `DateTime.Now`, `File`, or singletons are hidden dependencies; wrap them.

```csharp
public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
```

## Coupling between modules

- Dependencies between namespaces or projects form a directed graph without cycles.
  A cycle means two modules are really one, or a shared abstraction is missing.
- Higher-level modules (use cases, domain) do not reference lower-level ones
  (database, UI, devices); see [domain-model.md](domain-model.md#layers-and-dependency-direction).
- Shared "Common" or "Utilities" projects stay small and free of business rules; a
  growing common project is a smell (everything depends on it, it changes for every
  reason).
