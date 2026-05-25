using ErrorOrX.Samples.Diagnostics.Domain;

namespace ErrorOrX.Samples.Diagnostics.Services;

public interface IProductService
{
    ErrorOr<List<Product>> Search(SearchFilter filter);
    ErrorOr<Product> GetById(Guid id);
}

public interface INotificationService
{
    ErrorOr<Success> Send(string to, string message);
}
