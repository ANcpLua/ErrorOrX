// Shared model types used across diagnostic demos
// Centralized here to avoid duplicate definitions

namespace DiagnosticsDemos;

// Common response types
public record SharedTodoItem(int Id, string Title);

public record SharedUserItem(int Id, string Name);

public record SharedProductItem(int Id, string Name, decimal Price);

public record SharedPagedResponse<T>(List<T> Items, int TotalCount, int Page, int PageSize);
