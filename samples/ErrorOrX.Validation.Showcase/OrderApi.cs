namespace ErrorOrX.Validation.Showcase;

public static class OrderApi
{
    [Get("/api/orders")]
    public static ErrorOr<List<Order>> GetAll(IOrderService svc)
    {
        return svc.GetAll();
    }

    [Get("/api/orders/{id:guid}")]
    public static ErrorOr<Order> GetById(Guid id, IOrderService svc)
    {
        var order = svc.GetById(id);
        return order is not null
            ? order
            : Error.NotFound("Order.NotFound", $"Order with ID {id} was not found.");
    }

    [Post("/api/orders")]
    public static ErrorOr<Order> Create(CreateOrderRequest request, IOrderService svc)
    {
        return svc.Create(request);
    }

    [Put("/api/orders/{id:guid}")]
    public static ErrorOr<Order> Update(Guid id, UpdateOrderRequest request, IOrderService svc)
    {
        var order = svc.Update(id, request);
        return order is not null
            ? order
            : Error.NotFound("Order.NotFound", $"Order with ID {id} was not found.");
    }

    [Delete("/api/orders/{id:guid}")]
    public static ErrorOr<Deleted> Delete(Guid id, IOrderService svc)
    {
        return svc.Delete(id)
            ? Result.Deleted
            : Error.NotFound("Order.NotFound", $"Order with ID {id} was not found.");
    }
}
