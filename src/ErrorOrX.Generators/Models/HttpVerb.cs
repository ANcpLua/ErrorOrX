using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

/// <summary>
///     HTTP verb enum matching ASP.NET Core MapGet/MapPost/etc. methods.
///     Exhaustive switch expressions — all members handled explicitly, discard arms are defensive only.
/// </summary>
internal enum HttpVerb : byte
{
    Get = 0,
    Post = 1,
    Put = 2,
    Delete = 3,
    Patch = 4,
    Head = 5,
    Options = 6,
    Trace = 7
}

internal static class HttpVerbExtensions
{
    /// <summary>
    ///     Maps enum to uppercase HTTP method string for emission into generated code.
    /// </summary>
    public static string ToHttpString(this HttpVerb verb)
    {
        return verb switch
        {
            HttpVerb.Get => "GET",
            HttpVerb.Post => "POST",
            HttpVerb.Put => "PUT",
            HttpVerb.Delete => "DELETE",
            HttpVerb.Patch => "PATCH",
            HttpVerb.Head => "HEAD",
            HttpVerb.Options => "OPTIONS",
            HttpVerb.Trace => "TRACE",
            _ => throw new ArgumentOutOfRangeException(nameof(verb), verb, null)
        };
    }

    /// <summary>
    ///     Maps enum to ASP.NET Core's Map* method name.
    /// </summary>
    public static string ToMapMethod(this HttpVerb verb)
    {
        return verb switch
        {
            HttpVerb.Get => "MapGet",
            HttpVerb.Post => "MapPost",
            HttpVerb.Put => "MapPut",
            HttpVerb.Delete => "MapDelete",
            HttpVerb.Patch => "MapPatch",
            HttpVerb.Head => "MapMethods",
            HttpVerb.Options => "MapMethods",
            HttpVerb.Trace => "MapMethods",
            _ => throw new ArgumentOutOfRangeException(nameof(verb), verb, null)
        };
    }

    /// <summary>
    ///     Returns true if the verb typically has no request body (GET, HEAD, OPTIONS, DELETE, TRACE).
    /// </summary>
    public static bool IsBodyless(this HttpVerb verb)
    {
        return verb is HttpVerb.Get or HttpVerb.Head or HttpVerb.Options or HttpVerb.Delete or HttpVerb.Trace;
    }

    /// <summary>
    ///     Parses an attribute name to HttpVerb, returning null for unrecognized attributes.
    /// </summary>
    public static HttpVerb? TryParseFromAttribute(string attrName, ImmutableArray<TypedConstant> args)
    {
        return attrName switch
        {
            "GetAttribute" or "Get" => HttpVerb.Get,
            "PostAttribute" or "Post" => HttpVerb.Post,
            "PutAttribute" or "Put" => HttpVerb.Put,
            "DeleteAttribute" or "Delete" => HttpVerb.Delete,
            "PatchAttribute" or "Patch" => HttpVerb.Patch,
            "ErrorOrEndpointAttribute" or "ErrorOrEndpoint" when args is [{ Value: string m }, ..] =>
                ParseMethodString(m),
            _ => null
        };
    }

    internal static HttpVerb? ParseMethodString(string method)
    {
        return method.ToUpperInvariant() switch
        {
            "GET" => HttpVerb.Get,
            "POST" => HttpVerb.Post,
            "PUT" => HttpVerb.Put,
            "DELETE" => HttpVerb.Delete,
            "PATCH" => HttpVerb.Patch,
            "HEAD" => HttpVerb.Head,
            "OPTIONS" => HttpVerb.Options,
            "TRACE" => HttpVerb.Trace,
            _ => null
        };
    }
}
