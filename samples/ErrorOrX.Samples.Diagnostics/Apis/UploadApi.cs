using ErrorOrX.Samples.Diagnostics.Domain;

namespace ErrorOrX.Samples.Diagnostics.Apis;

public static class UploadApi
{
    [Post("/api/upload")]
    public static ErrorOr<Created> Upload([FromBody] UploadMetadata meta, Stream body)
        => Result.Created;
}
