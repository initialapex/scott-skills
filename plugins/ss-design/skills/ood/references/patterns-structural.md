# Creational and structural patterns

C# 7.3 syntax.

## Factory Method and simple factory

Creation that depends on input or configuration goes in one place, so callers depend
on the abstraction only.

```csharp
public interface IShippingRateFactory
{
    IShippingRate Create(string carrierCode);
}

public sealed class ShippingRateFactory : IShippingRateFactory
{
    public IShippingRate Create(string carrierCode)
    {
        switch (carrierCode)
        {
            case "STD": return new StandardShipping();
            case "EXP": return new ExpressShipping();
            default: throw new ArgumentException($"Unknown carrier '{carrierCode}'.", nameof(carrierCode));
        }
    }
}
```

- This `switch` is the one allowed place for the case list; adding a carrier edits only
  the factory and adds a class.
- With a DI container, the factory can resolve registered implementations instead of
  calling `new`; keep the container out of domain classes.
- The classic Factory Method (a virtual `Create...` method a subclass overrides) is
  useful inside a Template Method; prefer a factory object elsewhere.
- Static factory methods on the type (`Money.Euro(4.90m)`, `Order.CreateFor(customer)`)
  are the right choice when construction needs a name or validation.

## Abstract Factory

Creates families of related objects that must match (for example, document parts for
one output format).

```csharp
public interface IDocumentFactory
{
    IHeading CreateHeading(string text);
    ITable CreateTable(int columns);
}

public sealed class PdfDocumentFactory : IDocumentFactory { /* PDF parts */ }
public sealed class HtmlDocumentFactory : IDocumentFactory { /* HTML parts */ }
```

Use it only when there are several families and mixing them is a bug. One family is
just a set of constructors.

## Builder

Step-by-step construction of an object with many optional parts, validated once at the
end.

```csharp
public sealed class ShipmentBuilder
{
    private readonly List<Parcel> _parcels = new List<Parcel>();
    private Address _destination;
    private bool _insured;
    private string _reference;

    public ShipmentBuilder To(Address destination)
    {
        _destination = destination;
        return this;
    }

    public ShipmentBuilder AddParcel(Parcel parcel)
    {
        _parcels.Add(parcel);
        return this;
    }

    public ShipmentBuilder Insured()
    {
        _insured = true;
        return this;
    }

    public ShipmentBuilder WithReference(string reference)
    {
        _reference = reference;
        return this;
    }

    public Shipment Build()
    {
        if (_destination == null)
        {
            throw new InvalidOperationException("A destination is required.");
        }

        if (_parcels.Count == 0)
        {
            throw new InvalidOperationException("At least one parcel is required.");
        }

        return new Shipment(_destination, _parcels, _insured, _reference);
    }
}
```

- The built object stays immutable and valid; the builder holds the half-finished
  state.
- Test data builders (`AnOrder().WithLines(3).Build()`) are a good use in tests.
- For two or three parameters, use a constructor; optional parameters with defaults
  work for a few flags.

## Adapter

Wraps a class you cannot change (a third-party SDK, a legacy component) so it fits the
interface your code defines.

```csharp
// Your code's interface, in domain terms
public interface IPaymentGateway
{
    PaymentResult Charge(Money amount, string cardToken);
}

// Adapter around a vendor SDK with a different shape
public sealed class AcmePaymentGateway : IPaymentGateway
{
    private readonly AcmeClient _client;

    public AcmePaymentGateway(AcmeClient client)
    {
        _client = client;
    }

    public PaymentResult Charge(Money amount, string cardToken)
    {
        AcmeResponse response = _client.CreateCharge(
            (long)(amount.Amount * 100), amount.Currency, cardToken);

        return response.Ok
            ? PaymentResult.Success(response.TransactionId)
            : PaymentResult.Declined(response.ErrorText);
    }
}
```

- The adapter translates types, units, and errors (vendor exceptions become your
  exceptions or results). No business rules inside.
- Vendor types never leak past the adapter.

## Decorator

Adds behavior around an object with the same interface, without changing it. Good for
cross-cutting concerns: logging, caching, retry, timing, authorization.

```csharp
public sealed class CachingExchangeRates : IExchangeRates
{
    private readonly IExchangeRates _inner;
    private readonly IClock _clock;
    private readonly Dictionary<string, CachedRate> _cache = new Dictionary<string, CachedRate>();

    public CachingExchangeRates(IExchangeRates inner, IClock clock)
    {
        _inner = inner;
        _clock = clock;
    }

    public decimal Rate(string fromCurrency, string toCurrency)
    {
        string key = fromCurrency + "/" + toCurrency;
        CachedRate cached;
        if (_cache.TryGetValue(key, out cached) && cached.ExpiresUtc > _clock.UtcNow)
        {
            return cached.Rate;
        }

        decimal rate = _inner.Rate(fromCurrency, toCurrency);
        _cache[key] = new CachedRate(rate, _clock.UtcNow.AddMinutes(10));
        return rate;
    }
}
```

- Decorators stack (`new Logging(new Caching(new BankExchangeRates()))`); the order
  matters and is decided in the composition root.
- If every method of a large interface must be forwarded just to change one, the
  interface is too large (Interface segregation).

## Facade

A simple entry point to a complicated subsystem, offering the few operations clients
need.

```csharp
public sealed class InvoicingFacade
{
    private readonly PriceCalculator _prices;
    private readonly TaxRules _taxes;
    private readonly InvoiceNumberGenerator _numbers;
    private readonly IInvoiceRenderer _renderer;

    // Constructor takes the four collaborators.

    public Invoice CreateInvoice(Order order)
    {
        Money net = _prices.Total(order);
        Money tax = _taxes.TaxFor(order, net);
        Invoice invoice = new Invoice(_numbers.Next(), order.Id, net, tax);
        invoice.AttachDocument(_renderer.Render(invoice));
        return invoice;
    }
}
```

- A facade coordinates; it does not hold business rules the subsystem classes should
  own.
- A facade that forwards every method one-to-one hides nothing; remove it.

## Composite

Individual items and groups share one interface, so clients treat a tree uniformly.

```csharp
public interface IPriceComponent
{
    Money Price();
}

public sealed class Product : IPriceComponent
{
    private readonly Money _price;

    public Product(Money price)
    {
        _price = price;
    }

    public Money Price() => _price;
}

public sealed class Bundle : IPriceComponent
{
    private readonly List<IPriceComponent> _parts = new List<IPriceComponent>();
    private readonly decimal _discountRate;

    public Bundle(decimal discountRate)
    {
        _discountRate = discountRate;
    }

    public void Add(IPriceComponent part)
    {
        _parts.Add(part);
    }

    public Money Price()
    {
        // A bundle always has at least one part (checked when the catalog builds it).
        Money sum = _parts.Select(p => p.Price()).Aggregate((total, price) => total + price);
        return sum * (1 - _discountRate);
    }
}
```

- Keep child-management members (`Add`, `Remove`) on the group type only, unless
  clients really need to treat them uniformly.
- Guard against cycles if users can build the tree.
