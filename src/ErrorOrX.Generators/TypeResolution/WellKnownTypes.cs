// ReSharper disable InconsistentNaming

namespace ErrorOr.Generators;

/// <summary>
///     Centralized well-known type names for ASP.NET Core and ErrorOr.
///     Top-level constants are STRICT METADATA NAMES for use with GetTypeByMetadataName.
///     Fqn nested class contains global:: names for source code emission.
/// </summary>
internal static class WellKnownTypes
{
    public const string ErrorOrEndpointAttribute = "ErrorOr.ErrorOrEndpointAttribute";
    public const string GetAttribute = "ErrorOr.GetAttribute";
    public const string PostAttribute = "ErrorOr.PostAttribute";
    public const string PutAttribute = "ErrorOr.PutAttribute";
    public const string DeleteAttribute = "ErrorOr.DeleteAttribute";
    public const string PatchAttribute = "ErrorOr.PatchAttribute";
    public const string HeadAttribute = "ErrorOr.HeadAttribute";
    public const string OptionsAttribute = "ErrorOr.OptionsAttribute";
    public const string TraceAttribute = "ErrorOr.TraceAttribute";
    public const string ProducesErrorAttribute = "ErrorOr.ProducesErrorAttribute";
    public const string AcceptedResponseAttribute = "ErrorOr.AcceptedResponseAttribute";
    public const string ReturnsErrorAttribute = "ErrorOr.ReturnsErrorAttribute";

    public const string ErrorOrT = "ErrorOr.ErrorOr`1";
    public const string ErrorType = "ErrorOr.ErrorType";
    public const string Error = "ErrorOr.Error";

    public const string Success = "ErrorOr.Success";
    public const string Created = "ErrorOr.Created";
    public const string Updated = "ErrorOr.Updated";
    public const string Deleted = "ErrorOr.Deleted";

    public const string FromBodyAttribute = "Microsoft.AspNetCore.Mvc.FromBodyAttribute";
    public const string FromFormAttribute = "Microsoft.AspNetCore.Mvc.FromFormAttribute";
    public const string FromHeaderAttribute = "Microsoft.AspNetCore.Mvc.FromHeaderAttribute";
    public const string FromQueryAttribute = "Microsoft.AspNetCore.Mvc.FromQueryAttribute";
    public const string FromRouteAttribute = "Microsoft.AspNetCore.Mvc.FromRouteAttribute";
    public const string FromServicesAttribute = "Microsoft.AspNetCore.Mvc.FromServicesAttribute";
    public const string ProblemDetails = "Microsoft.AspNetCore.Mvc.ProblemDetails";

    public const string AsParametersAttribute = "Microsoft.AspNetCore.Http.AsParametersAttribute";
    public const string HttpContext = "Microsoft.AspNetCore.Http.HttpContext";
    public const string HttpValidationProblemDetails = "Microsoft.AspNetCore.Http.HttpValidationProblemDetails";
    public const string FormCollection = "Microsoft.AspNetCore.Http.IFormCollection";
    public const string FormFile = "Microsoft.AspNetCore.Http.IFormFile";
    public const string FormFileCollection = "Microsoft.AspNetCore.Http.IFormFileCollection";
    public const string BindableFromHttpContext = "Microsoft.AspNetCore.Http.IBindableFromHttpContext`1";
    public const string TypedResults = "Microsoft.AspNetCore.Http.TypedResults";

    public const string FromKeyedServicesAttribute =
        "Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute";

    public const string ValidationAttribute = "System.ComponentModel.DataAnnotations.ValidationAttribute";
    public const string IValidatableObject = "System.ComponentModel.DataAnnotations.IValidatableObject";
    public const string Validator = "System.ComponentModel.DataAnnotations.Validator";
    public const string ValidationContext = "System.ComponentModel.DataAnnotations.ValidationContext";
    public const string ValidationResult = "System.ComponentModel.DataAnnotations.ValidationResult";

    public const string CancellationToken = "System.Threading.CancellationToken";
    public const string JsonSerializableAttribute = "System.Text.Json.Serialization.JsonSerializableAttribute";

    public const string JsonSourceGenerationOptionsAttribute =
        "System.Text.Json.Serialization.JsonSourceGenerationOptionsAttribute";

    public const string JsonSerializerContext = "System.Text.Json.Serialization.JsonSerializerContext";
    public const string ParameterInfo = "System.Reflection.ParameterInfo";

    public const string TaskT = "System.Threading.Tasks.Task`1";
    public const string ValueTaskT = "System.Threading.Tasks.ValueTask`1";
    public const string ListT = "System.Collections.Generic.List`1";
    public const string IListT = "System.Collections.Generic.IList`1";
    public const string IEnumerableT = "System.Collections.Generic.IEnumerable`1";
    public const string IAsyncEnumerableT = "System.Collections.Generic.IAsyncEnumerable`1";
    public const string IReadOnlyListT = "System.Collections.Generic.IReadOnlyList`1";
    public const string ICollectionT = "System.Collections.Generic.ICollection`1";
    public const string HashSetT = "System.Collections.Generic.HashSet`1";
    public const string ReadOnlySpanT = "System.ReadOnlySpan`1";
    public const string IFormatProvider = "System.IFormatProvider";
    public const string Guid = "System.Guid";
    public const string DateTime = "System.DateTime";
    public const string DateTimeOffset = "System.DateTimeOffset";
    public const string DateOnly = "System.DateOnly";
    public const string TimeOnly = "System.TimeOnly";
    public const string TimeSpan = "System.TimeSpan";

    public const string Stream = "System.IO.Stream";
    public const string PipeReader = "System.IO.Pipelines.PipeReader";

    public const string SseItemT = "System.Net.ServerSentEvents.SseItem`1";

    public const string AuthorizeAttribute = "Microsoft.AspNetCore.Authorization.AuthorizeAttribute";
    public const string AllowAnonymousAttribute = "Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute";

    public const string EnableRateLimitingAttribute =
        "Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute";

    public const string DisableRateLimitingAttribute =
        "Microsoft.AspNetCore.RateLimiting.DisableRateLimitingAttribute";

    public const string OutputCacheAttribute = "Microsoft.AspNetCore.OutputCaching.OutputCacheAttribute";

    public const string EnableCorsAttribute = "Microsoft.AspNetCore.Cors.EnableCorsAttribute";
    public const string DisableCorsAttribute = "Microsoft.AspNetCore.Cors.DisableCorsAttribute";

    public const string ApiVersionAttribute = "Asp.Versioning.ApiVersionAttribute";
    public const string ApiVersionNeutralAttribute = "Asp.Versioning.ApiVersionNeutralAttribute";
    public const string MapToApiVersionAttribute = "Asp.Versioning.MapToApiVersionAttribute";
    public const string ApiVersion = "Asp.Versioning.ApiVersion";

    public const string RouteGroupAttribute = "ErrorOr.RouteGroupAttribute";
    public const string AllowEmptyBodyAttribute = "ErrorOr.AllowEmptyBodyAttribute";
    public const string EndpointMetadataAttribute = "ErrorOr.EndpointMetadataAttribute";

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
        public const string Trace = "TRACE";

        /// <summary>
        ///     Returns true if the HTTP method typically has no request body.
        /// </summary>
        public static bool IsBodyless(string method)
        {
            return method.ToUpperInvariant() is Get or Head or Options or Delete or Trace;
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
        // Frequently used Fqn constants emitted in generated code
        public const string ErrorType = "global::ErrorOr.ErrorType";
        public const string Error = "global::ErrorOr.Error";
        public const string ProducesErrorAttribute = "global::ErrorOr.ProducesErrorAttribute";
        public const string AcceptedResponseAttribute = "global::ErrorOr.AcceptedResponseAttribute";

        public const string ProblemDetails = "global::Microsoft.AspNetCore.Mvc.ProblemDetails";

        public const string ProducesResponseTypeMetadata =
            "global::Microsoft.AspNetCore.Http.ProducesResponseTypeMetadata";

        public const string HttpValidationProblemDetails =
            "global::Microsoft.AspNetCore.Http.HttpValidationProblemDetails";

        public const string Result = "global::Microsoft.AspNetCore.Http.IResult";

        public const string Validator = "global::System.ComponentModel.DataAnnotations.Validator";
        public const string ValidationContext = "global::System.ComponentModel.DataAnnotations.ValidationContext";
        public const string ValidationResult = "global::System.ComponentModel.DataAnnotations.ValidationResult";

        public const string JsonException = "global::System.Text.Json.JsonException";

        public const string List = "global::System.Collections.Generic.List";
        public const string Dictionary = "global::System.Collections.Generic.Dictionary";
        public const string ReadOnlyList = "global::System.Collections.Generic.IReadOnlyList";

        public const string ApiVersion = "global::Asp.Versioning.ApiVersion";

        /// <summary>
        ///     TypedResults factory method prefixes for generated code.
        ///     Usage: $"{TypedResults.Ok}(value)" emits "global::...TypedResults.Ok(value)"
        /// </summary>
        public static class TypedResults
        {
            private const string T = "global::Microsoft.AspNetCore.Http.TypedResults";

            public const string Ok = $"{T}.Ok";
            public const string Created = $"{T}.Created";
            public const string Accepted = $"{T}.Accepted";
            public const string NoContent = $"{T}.NoContent";
            public const string BadRequest = $"{T}.BadRequest";
            public const string Unauthorized = $"{T}.Unauthorized";
            public const string Forbid = $"{T}.Forbid";
            public const string NotFound = $"{T}.NotFound";
            public const string Conflict = $"{T}.Conflict";
            public const string UnprocessableEntity = $"{T}.UnprocessableEntity";
            public const string InternalServerError = $"{T}.InternalServerError";
            public const string ValidationProblem = $"{T}.ValidationProblem";
            public const string Problem = $"{T}.Problem";
            public const string StatusCode = $"{T}.StatusCode";
            public const string ServerSentEvents = $"{T}.ServerSentEvents";
        }

        /// <summary>
        ///     HttpResults type names for Results union declarations.
        ///     Usage: $"{HttpResults.Ok}&lt;T&gt;" emits "global::...HttpResults.Ok&lt;T&gt;"
        /// </summary>
        public static class HttpResults
        {
            private const string H = "global::Microsoft.AspNetCore.Http.HttpResults";

            public const string Ok = $"{H}.Ok";
            public const string Created = $"{H}.Created";
            public const string Accepted = $"{H}.Accepted";
            public const string NoContent = $"{H}.NoContent";
            public const string BadRequest = $"{H}.BadRequest";
            public const string UnauthorizedHttpResult = $"{H}.UnauthorizedHttpResult";
            public const string ForbidHttpResult = $"{H}.ForbidHttpResult";
            public const string NotFound = $"{H}.NotFound";
            public const string Conflict = $"{H}.Conflict";
            public const string UnprocessableEntity = $"{H}.UnprocessableEntity";
            public const string InternalServerError = $"{H}.InternalServerError";
            public const string ValidationProblem = $"{H}.ValidationProblem";
            public const string ProblemHttpResult = $"{H}.ProblemHttpResult";
            public const string StatusCodeHttpResult = $"{H}.StatusCodeHttpResult";

            /// <summary>
            ///     Results union type for typed endpoint returns.
            ///     Usage: $"{HttpResults.Results}&lt;T1, T2&gt;" emits "global::...HttpResults.Results&lt;T1, T2&gt;"
            /// </summary>
            public const string Results = $"{H}.Results";
        }
    }

    /// <summary>
    ///     Content type and URL constants for generated code.
    /// </summary>
    public static class Constants
    {
        public const string ContentTypeJson = "application/json";
        public const string ContentTypeProblemJson = "application/problem+json";
        public const string ContentTypeFormData = "multipart/form-data";
        public const string HttpStatusesBaseUrl = "https://httpstatuses.io/";
    }
}
