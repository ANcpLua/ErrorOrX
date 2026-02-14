namespace ErrorOr.Generators;

/// <summary>
///     Type-safe enum representing a parameter binding source.
///     Inspired by ASP.NET Core's <c>BindingSource</c>.
/// </summary>
internal enum ParameterSource
{
    Route,
    Body,
    Query,
    Header,
    Form,
    FormFile,
    FormFiles,
    FormCollection,
    Stream,
    PipeReader,
    Service,
    KeyedService,
    HttpContext,
    CancellationToken,
    AsParameters
}

internal static class ParameterSourceExtensions
{
    /// <summary>Gets whether this source binds from form-related data.</summary>
    public static bool IsFormRelated(this ParameterSource source) =>
        source is ParameterSource.Form or ParameterSource.FormFile
            or ParameterSource.FormFiles or ParameterSource.FormCollection;
}
