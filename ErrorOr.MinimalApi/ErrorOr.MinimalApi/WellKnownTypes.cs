namespace ErrorOr.MinimalApi;

/// <summary>
///     Centralized well-known type names for ASP.NET Core and ErrorOr.Http.
/// </summary>
internal static class WellKnownTypes
{
    // ErrorOr.Http attributes
    public const string ErrorOrEndpointAttribute = "ErrorOr.Http.ErrorOrEndpointAttribute";
    public const string GetAttribute = "ErrorOr.Http.GetAttribute";
    public const string PostAttribute = "ErrorOr.Http.PostAttribute";
    public const string PutAttribute = "ErrorOr.Http.PutAttribute";
    public const string DeleteAttribute = "ErrorOr.Http.DeleteAttribute";
    public const string PatchAttribute = "ErrorOr.Http.PatchAttribute";
    public const string ProducesErrorAttribute = "ErrorOr.Http.ProducesErrorAttribute";
    public const string AcceptedResponseAttribute = "ErrorOr.Http.AcceptedResponseAttribute";

    // ErrorOr
    public const string ErrorOrT = "ErrorOr.ErrorOr<TValue>";

    // ASP.NET Core MVC
    public const string FromBodyAttribute = "Microsoft.AspNetCore.Mvc.FromBodyAttribute";
    public const string FromFormAttribute = "Microsoft.AspNetCore.Mvc.FromFormAttribute";
    public const string FromHeaderAttribute = "Microsoft.AspNetCore.Mvc.FromHeaderAttribute";
    public const string FromQueryAttribute = "Microsoft.AspNetCore.Mvc.FromQueryAttribute";
    public const string FromRouteAttribute = "Microsoft.AspNetCore.Mvc.FromRouteAttribute";
    public const string FromServicesAttribute = "Microsoft.AspNetCore.Mvc.FromServicesAttribute";
    public const string ProblemDetails = "Microsoft.AspNetCore.Mvc.ProblemDetails";

    // ASP.NET Core Http
    public const string AsParametersAttribute = "Microsoft.AspNetCore.Http.AsParametersAttribute";
    public const string HttpContext = "Microsoft.AspNetCore.Http.HttpContext";
    public const string HttpValidationProblemDetails = "Microsoft.AspNetCore.Http.HttpValidationProblemDetails";
    public const string IFormCollection = "Microsoft.AspNetCore.Http.IFormCollection";
    public const string IFormFile = "Microsoft.AspNetCore.Http.IFormFile";
    public const string IFormFileCollection = "Microsoft.AspNetCore.Http.IFormFileCollection";
    public const string IBindableFromHttpContext = "Microsoft.AspNetCore.Http.IBindableFromHttpContext`1";

    // DI
    public const string FromKeyedServicesAttribute =
        "Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute";

    // System
    public const string CancellationToken = "System.Threading.CancellationToken";
    public const string ObsoleteAttribute = "System.ObsoleteAttribute";
    public const string JsonSerializableAttribute = "System.Text.Json.Serialization.JsonSerializableAttribute";
    public const string JsonSerializerContext = "System.Text.Json.Serialization.JsonSerializerContext";
    public const string ParameterInfo = "System.Reflection.ParameterInfo";

    public const string TaskT = "System.Threading.Tasks.Task<TResult>";
    public const string ValueTaskT = "System.Threading.Tasks.ValueTask<TResult>";

    public const string Stream = "System.IO.Stream";
    public const string PipeReader = "System.IO.Pipelines.PipeReader";

    public const string SseItemT = "System.Net.ServerSentEvents.SseItem<T>";
    public const string IAsyncEnumerableT = "System.Collections.Generic.IAsyncEnumerable<T>";

    /// <summary>
    ///     HTTP method constants to replace magic strings throughout the generator.
    /// </summary>
    public static class HttpMethod
    {
        public const string Get = "GET";
        public const string Post = "POST";
        public const string Put = "PUT";
        public const string Delete = "DELETE";
        public const string Patch = "PATCH";
        public const string Head = "HEAD";
        public const string Options = "OPTIONS";

        /// <summary>
        ///     Returns true if the HTTP method typically has no request body.
        /// </summary>
        public static bool IsBodyless(string method)
        {
            return method.ToUpperInvariant() is Get or Head or Options or Delete;
        }
    }

    /// <summary>
    ///     Fully-qualified names for generated code output (with global:: prefix).
    ///     These intentionally shadow outer-class constants which use metadata names.
    ///     Outer: for Roslyn symbol lookup | Fqn: for emitted source code.
    /// </summary>
    // ReSharper disable MemberHidesStaticFromOuterClass - Intentional: metadata vs codegen naming
    public static class Fqn
    {
        public const string CancellationToken = "global::System.Threading.CancellationToken";
        public const string HttpContext = "global::Microsoft.AspNetCore.Http.HttpContext";
        public const string IFormCollection = "global::Microsoft.AspNetCore.Http.IFormCollection";
        public const string IFormFile = "global::Microsoft.AspNetCore.Http.IFormFile";
        public const string IFormFileCollection = "global::Microsoft.AspNetCore.Http.IFormFileCollection";
        public const string ProblemDetails = "global::Microsoft.AspNetCore.Mvc.ProblemDetails";

        public const string HttpValidationProblemDetails =
            "global::Microsoft.AspNetCore.Http.HttpValidationProblemDetails";

        public const string String = "global::System.String";
        public const string Guid = "global::System.Guid";
        public const string DateTime = "global::System.DateTime";
        public const string DateTimeOffset = "global::System.DateTimeOffset";
        public const string DateOnly = "global::System.DateOnly";
        public const string TimeOnly = "global::System.TimeOnly";
        public const string TimeSpan = "global::System.TimeSpan";

        public const string Stream = "global::System.IO.Stream";
        public const string PipeReader = "global::System.IO.Pipelines.PipeReader";
    }
}
