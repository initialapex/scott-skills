# Behavioral patterns

C# 7.3 syntax. Each section: when to use it, a minimal example, and the traps.

## Strategy

A family of interchangeable algorithms behind one interface; the client picks one at
run time.

```csharp
public interface IShippingRate
{
    string Carrier { get; }
    Money Quote(Parcel parcel, Address destination);
}

public sealed class StandardShipping : IShippingRate
{
    public string Carrier => "Standard";

    public Money Quote(Parcel parcel, Address destination)
    {
        return Money.Euro(4.90m + 0.50m * parcel.WeightKg);
    }
}

public sealed class ExpressShipping : IShippingRate
{
    public string Carrier => "Express";

    public Money Quote(Parcel parcel, Address destination)
    {
        return Money.Euro(destination.IsDomestic ? 12m : 29m);
    }
}

public sealed class ShippingQuoteService
{
    private readonly IReadOnlyList<IShippingRate> _rates;

    public ShippingQuoteService(IEnumerable<IShippingRate> rates)
    {
        _rates = rates.ToList();
    }

    public IEnumerable<ShippingQuote> QuoteAll(Parcel parcel, Address destination)
    {
        return _rates.Select(r => new ShippingQuote(r.Carrier, r.Quote(parcel, destination)));
    }
}
```

- A strategy with one method and no state can be a `Func<...>`; use an interface when
  it has a name in the domain, several members, or its own dependencies.
- Adding a carrier adds a class and a registration; no existing class changes.

## State

The object's behavior changes with its state; each state is a class that handles the
operations and decides the next state.

```csharp
public sealed class Order
{
    private OrderState _state = OrderState.Open;

    public string StatusName => _state.Name;

    public void Submit()  { _state = _state.Submit(this); }
    public void Ship()    { _state = _state.Ship(this); }
    public void Cancel()  { _state = _state.Cancel(this); }

    private readonly List<OrderLine> _lines = new List<OrderLine>();

    internal bool HasLines => _lines.Count > 0;

    public void AddLine(OrderLine line)
    {
        _state.EnsureEditable();
        _lines.Add(line);
    }
}

internal abstract class OrderState
{
    public static readonly OrderState Open = new OpenState();
    public static readonly OrderState Submitted = new SubmittedState();
    public static readonly OrderState Shipped = new ShippedState();
    public static readonly OrderState Cancelled = new CancelledState();

    public abstract string Name { get; }

    public virtual OrderState Submit(Order order) { throw Invalid("submit"); }
    public virtual OrderState Ship(Order order)   { throw Invalid("ship"); }
    public virtual OrderState Cancel(Order order) { throw Invalid("cancel"); }
    public virtual void EnsureEditable()          { throw Invalid("edit"); }

    private InvalidOperationException Invalid(string action)
    {
        return new InvalidOperationException($"Cannot {action} an order that is {Name}.");
    }
}

internal sealed class OpenState : OrderState
{
    public override string Name => "Open";

    public override OrderState Submit(Order order)
    {
        if (!order.HasLines)
        {
            throw new InvalidOperationException("Cannot submit an empty order.");
        }

        return Submitted;
    }

    public override OrderState Cancel(Order order) { return Cancelled; }
    public override void EnsureEditable() { }
}

// SubmittedState allows Ship and Cancel; ShippedState and CancelledState allow nothing.
```

- Use it when the same state checks repeat across many methods. For three states and
  two transitions, an enum with a guard in each method is simpler; say so.
- States without data are shared static instances; states with data are created per
  transition.
- Persist the state as its name or an enum, not as the class.

## Template Method

A base class fixes the steps of an algorithm and lets subclasses fill in some steps.

```csharp
public abstract class ReportExporter
{
    // The fixed algorithm; not virtual.
    public void Export(Report report, Stream output)
    {
        WriteHeader(report, output);
        foreach (ReportRow row in report.Rows)
        {
            WriteRow(row, output);
        }

        WriteFooter(report, output);
    }

    protected abstract void WriteHeader(Report report, Stream output);
    protected abstract void WriteRow(ReportRow row, Stream output);
    protected virtual void WriteFooter(Report report, Stream output) { }   // Optional step
}

public sealed class CsvReportExporter : ReportExporter { /* overrides */ }
```

- Keep the template method non-virtual and the steps `protected`.
- If subclasses multiply to combine variations (CSV with totals, CSV without, Excel
  with...), switch to Strategy for the varying steps.

## Observer

Interested objects subscribe to notifications from a source that does not know them.
In C#, use events.

```csharp
public sealed class StockLevel
{
    private int _quantity;

    public event EventHandler<StockChangedEventArgs> Changed;

    public void Remove(int count)
    {
        if (count > _quantity)
        {
            throw new InvalidOperationException("Not enough stock.");
        }

        int old = _quantity;
        _quantity -= count;
        OnChanged(new StockChangedEventArgs(old, _quantity));
    }

    private void OnChanged(StockChangedEventArgs e)
    {
        Changed?.Invoke(this, e);
    }
}
```

- Subscribers that live shorter than the source must unsubscribe (`-=`), or the source
  keeps them alive. Make the subscriber `IDisposable` and unsubscribe in `Dispose`.
- Raise events after the state change is complete, so handlers see consistent state.
- Handlers should not throw back into the source; an exception in one handler stops
  the others.
- For notifications across modules or threads, a message bus or `IObservable<T>`
  (Reactive Extensions) may fit better; do not build one without a need.

## Command

An operation as an object, so it can be queued, logged, retried, or undone.

```csharp
public interface IEditCommand
{
    string Description { get; }
    void Execute();
    void Undo();
}

public sealed class RenameItemCommand : IEditCommand
{
    private readonly CatalogItem _item;
    private readonly string _newName;
    private string _oldName;

    public RenameItemCommand(CatalogItem item, string newName)
    {
        _item = item;
        _newName = newName;
    }

    public string Description => $"Rename to {_newName}";

    public void Execute()
    {
        _oldName = _item.Name;
        _item.Rename(_newName);
    }

    public void Undo()
    {
        _item.Rename(_oldName);
    }
}

public sealed class UndoStack
{
    private readonly Stack<IEditCommand> _done = new Stack<IEditCommand>();

    public void Run(IEditCommand command)
    {
        command.Execute();
        _done.Push(command);
    }

    public void Undo()
    {
        if (_done.Count > 0)
        {
            _done.Pop().Undo();
        }
    }
}
```

- UI commands (`ICommand` in WPF) are a different concept: they bind a button to a
  method. This pattern is for operations that need history, queuing, or replay.
- A command captures everything it needs at creation, so it can run later.

## Null Object

A do-nothing implementation that replaces `null` for an optional collaborator.

```csharp
public interface IAuditLog
{
    void Record(string action, string detail);
}

public sealed class NullAuditLog : IAuditLog
{
    public static readonly NullAuditLog Instance = new NullAuditLog();

    private NullAuditLog() { }

    public void Record(string action, string detail) { }
}
```

- Use it when "missing" means "do nothing" for every caller. If callers must react
  differently to "missing", keep `null` (or an explicit optional) and handle it.
- Never use a Null Object to hide a required dependency that failed to load.
