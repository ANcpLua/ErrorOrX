namespace ErrorOrX.Samples.Diagnostics.Domain;

public sealed record Product(Guid Id, string Name, decimal Price);

public sealed record Post(int Id, string Slug, string Title);

public sealed record Order(Guid Id, decimal Total);

public sealed record NewOrder(decimal Total, string CustomerEmail);

public sealed record UploadMetadata(string FileName, string ContentType);

// Classes (not records) so IsComplexType classifies them as DTOs that need explicit binding on
// bodyless verbs — records are skipped by the generator's complex-type heuristic.
public sealed class SearchFilter
{
    public string? Query { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class DeleteOptions
{
    public bool Force { get; set; }
    public bool Cascade { get; set; }
}
