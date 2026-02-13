namespace ErrorOrX.Validation.Showcase;

public interface IOrderService
{
    Order Create(CreateOrderRequest request);
    Order? GetById(Guid id);
    List<Order> GetAll();
    Order? Update(Guid id, UpdateOrderRequest request);
    bool Delete(Guid id);
}

public sealed class OrderService : IOrderService
{
    private readonly List<Order> _orders = [];
    private readonly TimeProvider _timeProvider;

    public OrderService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Order Create(CreateOrderRequest request)
    {
        var order = new Order(
            Guid.NewGuid(),
            request.CustomerName,
            request.Email,
            request.Items,
            request.Items.Sum(static i => i.Quantity * i.UnitPrice),
            _timeProvider.GetUtcNow());
        _orders.Add(order);
        return order;
    }

    public Order? GetById(Guid id)
    {
        return _orders.Find(o => o.Id == id);
    }

    public List<Order> GetAll()
    {
        return [.. _orders];
    }

    public Order? Update(Guid id, UpdateOrderRequest request)
    {
        var index = _orders.FindIndex(o => o.Id == id);
        if (index < 0)
        {
            return null;
        }

        var existing = _orders[index];
        var updated = existing with
        {
            CustomerName = request.CustomerName ?? existing.CustomerName,
            Email = request.Email ?? existing.Email
        };
        _orders[index] = updated;
        return updated;
    }

    public bool Delete(Guid id)
    {
        return _orders.RemoveAll(o => o.Id == id) > 0;
    }
}
