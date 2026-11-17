# Error handling

Decide for each operation how it fails, before writing it. The decision is part of the
class's contract and belongs in the design.

## Choose the mechanism

| Situation | Mechanism | Example |
|-----------|-----------|---------|
| Programming error: a caller broke the contract | Exception (`ArgumentException`, `ArgumentNullException`, `ArgumentOutOfRangeException`, `InvalidOperationException`) | Negative quantity passed to `AddLine`; `Submit` on a shipped order |
| Infrastructure failure the code cannot prevent | Exception (let the framework's `IOException`, `TimeoutException`, `SqlException` through, or wrap it in your own) | Disk full, network down, device not responding |
| Expected business outcome the caller must handle | Result value, a `bool Try...` method, or a status in the return type | Card declined, input not valid, item out of stock |
| Absence is normal | `null` / `Try...` pattern / empty collection, documented | `Find(id)` with an unknown id typed by the user |
| A state the object can legitimately be in | Model it (a state, a status property) | An order that is "on hold" |

A simple test: *would a correct program ever hit this?* If no, it is an exception. If
yes, it is part of the return value.

## Exceptions

- **Validate arguments at public boundaries** with guard clauses at the start of the
  method; fail fast with a message that names the value and the rule.

```csharp
public void Reserve(ProductId product, int quantity)
{
    if (product == null) throw new ArgumentNullException(nameof(product));
    if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be positive.");

    // ...
}
```

- **Catch only what you can handle**, at the level that can do something useful
  (retry, fall back, tell the user). Do not catch `Exception` in the middle of the
  code just to log and rethrow, and never swallow it silently.
- **Rethrow with `throw;`**, not `throw ex;`, to keep the stack trace. When translating,
  pass the original as the inner exception.
- **Translate at boundaries.** An adapter turns vendor or infrastructure exceptions into
  your own (`PaymentGatewayUnavailableException`) so the domain does not depend on the
  vendor's types.
- **Custom exception types** only when a caller catches them specifically. Derive from
  `Exception` (not `ApplicationException`), keep them few, and include the data the
  handler needs as properties.
- **One last-resort handler per application** (UI thread, background tasks, service
  entry point) logs the exception and informs the user; individual classes do not.
- **Do not use exceptions for control flow** in normal paths (parsing user input,
  checking existence); use `TryParse`, `TryGetValue`, or a result.
- **Clean up with `using` / `finally`** so resources are released on every path.

## Result values and Try methods

```csharp
// Try pattern: a bool plus an out value, for lookups and parsing
public bool TryFind(OrderId id, out Order order) { /* ... */ }

// Result: when the caller needs the reason (see patterns-application.md#result)
public Result<Shipment> CreateShipment(Order order)
{
    if (order.Status != OrderStatus.Submitted)
    {
        return Result<Shipment>.Failure("Only submitted orders can be shipped.");
    }

    // ...
}
```

- Results must be checked; name the method so that ignoring the result is obviously
  wrong (`TryReserve`, `Validate`), and make the value inaccessible on failure.
- Do not mix: a method returns a result *or* throws for business outcomes, not both
  for the same kind of failure.

## Validation

- **Input validation** (format, required fields, ranges a user can get wrong) happens at
  the edge (UI, API) and is reported to the user, all errors at once.
- **Invariants** are enforced again inside the domain objects, with exceptions, because
  the domain cannot trust that every caller validated.
- Validation rules that belong to the domain (a valid quantity, a valid date range) live
  in one place, typically the value object's constructor or a static `Validate` method
  that both the UI and the domain call.

## Null

- Treat `null` as "absent" only where the contract says so, and document it on the
  member ("returns null when not found").
- Constructors reject `null` for required dependencies and values.
- Return empty collections, never `null` collections.
- Newer C# (8+) has nullable reference types (`string?`) to state this in the type
  system; use them when the project enables them. In C# 7.3, document it and guard.
