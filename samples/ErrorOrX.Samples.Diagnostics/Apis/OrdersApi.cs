using ErrorOrX.Samples.Diagnostics.Domain;

namespace ErrorOrX.Samples.Diagnostics.Apis;

public static class OrdersApi
{
    [Post("/api/orders")]
    public static ErrorOr<Order> Place(NewOrder order) => new Order(Guid.NewGuid(), order.Total);
}
