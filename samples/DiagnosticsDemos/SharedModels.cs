// Shared model types used across diagnostic demos
// Centralized here to avoid duplicate definitions

namespace DiagnosticsDemos;

// Common response types
public sealed record SharedTodoItem(int Id, string Title);

public sealed record SharedUserItem(int Id, string Name);

public sealed record SharedProductItem(int Id, string Name, decimal Price);

public sealed record SharedPagedResponse<T>(List<T> Items, int TotalCount, int Page, int PageSize);
