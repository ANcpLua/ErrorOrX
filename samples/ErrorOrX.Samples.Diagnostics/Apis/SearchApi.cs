using ErrorOrX.Samples.Diagnostics.Domain;
using ErrorOrX.Samples.Diagnostics.Services;

namespace ErrorOrX.Samples.Diagnostics.Apis;

public static class SearchApi
{
    [Get("/api/search")]
    public static ErrorOr<List<Product>> Search(SearchFilter filter)
        => new List<Product>();

    [Get("/api/products/{id:int}")]
    public static ErrorOr<Product> GetById(Guid id, IProductService svc)
        => svc.GetById(id);

    [Delete("/api/products/{id:guid}")]
    public static ErrorOr<Deleted> Delete(Guid id, DeleteOptions options)
        => Result.Deleted;
}
