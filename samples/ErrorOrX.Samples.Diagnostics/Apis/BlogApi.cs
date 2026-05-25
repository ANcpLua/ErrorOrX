using ErrorOrX.Samples.Diagnostics.Domain;

namespace ErrorOrX.Samples.Diagnostics.Apis;

public static class BlogApi
{
    [Get("/api/posts/{slug}")]
    public static ErrorOr<Post> GetPost(int id) => new Post(id, "stub", "Stub");

    [Get("/api/posts")]
    public static ErrorOr<List<Post>> GetAll() => new List<Post>();
}

public static class BlogSearchApi
{
    [Get("/api/posts")]
    public static ErrorOr<List<Post>> Search([FromQuery] string q) => new List<Post>();
}
