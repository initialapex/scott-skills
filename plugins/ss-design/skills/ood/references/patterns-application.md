# Application patterns

Patterns from enterprise application design that are not in the GoF catalog but come
up in most business code. C# 7.3 syntax.

## Repository

A collection-like interface for loading and saving aggregates, defined in the domain's
terms; the implementation hides the storage (database, files, a web service).

```csharp
// Domain layer: what the use cases need, nothing more
public interface IOrderRepository
{
    Order Find(OrderId id);                          // null when not found
    IReadOnlyList<Order> FindOpenFor(CustomerId customer);
    void Add(Order order);
    void Save(Order order);
}

// Infrastructure layer
public sealed class SqlOrderRepository : IOrderRepository { /* SQL or ORM */ }
```

- One repository per aggregate root (not per table). Child entities are loaded and
  saved through their root.
- Methods are named for the domain question (`FindOpenFor`), not generic query
  builders. Do not expose `IQueryable` or ORM types through the interface.
- A generic `IRepository<T>` with `GetAll` / `Update` / `Delete` for every entity is
  usually a thin, leaky layer over the ORM. Prefer specific interfaces; if the project
  already has a generic base, follow it.
- Read-only screens and reports may use a separate query service that returns flat
  data, instead of loading aggregates.

## Specification

A business rule for selecting or checking objects, as an object that can be named,
tested, and combined.

```csharp
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T candidate);
}

public sealed class OverdueInvoice : ISpecification<Invoice>
{
    private readonly DateTime _today;

    public OverdueInvoice(DateTime today)
    {
        _today = today;
    }

    public bool IsSatisfiedBy(Invoice invoice)
    {
        return !invoice.IsPaid && invoice.DueDate < _today;
    }
}

public static class SpecificationExtensions
{
    public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right)
    {
        return new AndSpecification<T>(left, right);
    }
}

// Usage
ISpecification<Invoice> remind = new OverdueInvoice(clock.UtcNow.Date).And(new LargeAmount(1000m));
IEnumerable<Invoice> toRemind = invoices.Where(remind.IsSatisfiedBy);
```

- Use it when the rule is used in several places or combined in different ways. A rule
  used once is a method on the entity (`invoice.IsOverdue(today)`).
- Translating specifications to database queries (expression trees) adds complexity;
  do it only when filtering in memory is too slow and the project already supports it.

## Unit of Work

Tracks the changes made during one business operation and commits them together.

```csharp
public interface IUnitOfWork : IDisposable
{
    IOrderRepository Orders { get; }
    IStockRepository Stock { get; }
    void Commit();   // One transaction; nothing is saved without it
}

public sealed class PlaceOrderHandler
{
    private readonly Func<IUnitOfWork> _unitOfWorkFactory;

    public PlaceOrderHandler(Func<IUnitOfWork> unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public void Handle(PlaceOrder command)
    {
        using (IUnitOfWork work = _unitOfWorkFactory())
        {
            Order order = work.Orders.Find(command.OrderId);
            order.Submit();
            work.Stock.Reserve(order.Lines);
            work.Commit();
        }
    }
}
```

- Most ORMs already are a unit of work (Entity Framework's `DbContext`); do not wrap
  one in another layer just to rename `SaveChanges`.
- Prefer designs where one operation changes one aggregate; needing a unit of work
  across many aggregates often means the aggregate boundaries are wrong, or that the
  other changes should happen after the commit (an event, a follow-up job).
- The unit of work lives for one operation, never for the whole application.

## Result

Returns an expected failure as a value with a reason, instead of throwing. See
[error-handling.md](error-handling.md) for when to choose it over exceptions.

```csharp
public sealed class Result
{
    private Result(bool succeeded, string error)
    {
        Succeeded = succeeded;
        Error = error;
    }

    public bool Succeeded { get; }
    public string Error { get; }   // null when Succeeded

    public static Result Success() { return new Result(true, null); }
    public static Result Failure(string error) { return new Result(false, error); }
}

public sealed class Result<T>
{
    private readonly T _value;

    private Result(bool succeeded, T value, string error)
    {
        Succeeded = succeeded;
        _value = value;
        Error = error;
    }

    public bool Succeeded { get; }
    public string Error { get; }

    public T Value
    {
        get
        {
            if (!Succeeded)
            {
                throw new InvalidOperationException("No value: " + Error);
            }

            return _value;
        }
    }

    public static Result<T> Success(T value) { return new Result<T>(true, value, null); }
    public static Result<T> Failure(string error) { return new Result<T>(false, default(T), error); }
}

// Usage
Result<Order> placed = ordering.Place(cart);
if (!placed.Succeeded)
{
    ShowMessage(placed.Error);
    return;
}

Ship(placed.Value);
```

- Use it for failures the caller is expected to handle: validation, business rule
  refusals, "not found" in a lookup the user typed.
- A string error is enough for messages shown to people. When callers must branch on
  the reason, use an error enum or code next to the message.
- Do not return `Result` from every method; programming errors and infrastructure
  failures stay exceptions.
- If the project already has a result type (or uses a library for it), use that one.
- Newer C# (9+) can write these types more briefly with records; keep the class form
  in projects limited to C# 7.3.
