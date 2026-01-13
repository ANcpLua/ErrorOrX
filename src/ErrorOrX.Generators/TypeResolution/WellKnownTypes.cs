// ReSharper disable InconsistentNaming

namespace ErrorOr.Generators;

/// <summary>
///     Centralized well-known type names for ASP.NET Core and ErrorOr.
///     Top-level constants are STRICT METADATA NAMES for use with GetTypeByMetadataName.
///     Fqn nested class contains global:: names for source code emission.
/// </summary>
internal static class WellKnownTypes
{
    // ErrorOr attributes (moved from ErrorOr.Endpoints to ErrorOr namespace)
    public const string ErrorOrEndpointAttribute = "ErrorOr.ErrorOrEndpointAttribute";
    public const string GetAttribute = "ErrorOr.GetAttribute";
    public const string PostAttribute = "ErrorOr.PostAttribute";
    public const string PutAttribute = "ErrorOr.PutAttribute";
    public const string DeleteAttribute = "ErrorOr.DeleteAttribute";
    public const string PatchAttribute = "ErrorOr.PatchAttribute";
    public const string ProducesErrorAttribute = "ErrorOr.ProducesErrorAttribute";
    public const string AcceptedResponseAttribute = "ErrorOr.AcceptedResponseAttribute";
    public const string ReturnsErrorAttribute = "ErrorOr.ReturnsErrorAttribute";

    // ErrorOr core types
    public const string ErrorOrT = "ErrorOr.ErrorOr`1";
    public const string ErrorType = "ErrorOr.ErrorType";
    public const string Error = "ErrorOr.Error";

    // ErrorOr result markers
    public const string Success = "ErrorOr.Success";
    public const string Created = "ErrorOr.Created";
    public const string Updated = "ErrorOr.Updated";
    public const string Deleted = "ErrorOr.Deleted";

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
    public const string FormCollection = "Microsoft.AspNetCore.Http.IFormCollection";
    public const string FormFile = "Microsoft.AspNetCore.Http.IFormFile";
    public const string FormFileCollection = "Microsoft.AspNetCore.Http.IFormFileCollection";
    public const string BindableFromHttpContext = "Microsoft.AspNetCore.Http.IBindableFromHttpContext`1";
    public const string TypedResults = "Microsoft.AspNetCore.Http.TypedResults";

    // DI
    public const string FromKeyedServicesAttribute =
        "Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute";

    // BCL Validation (base types for automatic detection)
    public const string ValidationAttribute = "System.ComponentModel.DataAnnotations.ValidationAttribute";
    public const string IValidatableObject = "System.ComponentModel.DataAnnotations.IValidatableObject";
    public const string Validator = "System.ComponentModel.DataAnnotations.Validator";
    public const string ValidationContext = "System.ComponentModel.DataAnnotations.ValidationContext";
    public const string ValidationResult = "System.ComponentModel.DataAnnotations.ValidationResult";

    // System
    public const string CancellationToken = "System.Threading.CancellationToken";
    public const string JsonSerializableAttribute = "System.Text.Json.Serialization.JsonSerializableAttribute";
    public const string JsonSourceGenerationOptionsAttribute = "System.Text.Json.Serialization.JsonSourceGenerationOptionsAttribute";
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
    public const string NullableT = "System.Nullable`1";
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

    // Authorization attributes
    public const string AuthorizeAttribute = "Microsoft.AspNetCore.Authorization.AuthorizeAttribute";
    public const string AllowAnonymousAttribute = "Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute";

    // Rate limiting attributes
    public const string EnableRateLimitingAttribute =
        "Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute";

    public const string DisableRateLimitingAttribute =
        "Microsoft.AspNetCore.RateLimiting.DisableRateLimitingAttribute";

    // Output caching attributes
    public const string OutputCacheAttribute = "Microsoft.AspNetCore.OutputCaching.OutputCacheAttribute";

    // CORS attributes
    public const string EnableCorsAttribute = "Microsoft.AspNetCore.Cors.EnableCorsAttribute";
    public const string DisableCorsAttribute = "Microsoft.AspNetCore.Cors.DisableCorsAttribute";

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
        // ErrorOr attributes
        public const string ErrorOrEndpointAttribute = "global::ErrorOr.ErrorOrEndpointAttribute";
        public const string GetAttribute = "global::ErrorOr.GetAttribute";
        public const string PostAttribute = "global::ErrorOr.PostAttribute";
        public const string PutAttribute = "global::ErrorOr.PutAttribute";
        public const string DeleteAttribute = "global::ErrorOr.DeleteAttribute";
        public const string PatchAttribute = "global::ErrorOr.PatchAttribute";
        public const string ProducesErrorAttribute = "global::ErrorOr.ProducesErrorAttribute";
        public const string AcceptedResponseAttribute = "global::ErrorOr.AcceptedResponseAttribute";

        // ErrorOr core types
        public const string ErrorOr = "global::ErrorOr.ErrorOr";
        public const string ErrorType = "global::ErrorOr.ErrorType";
        public const string Error = "global::ErrorOr.Error";
        public const string ResultSuccess = "global::ErrorOr.Success";
        public const string ResultCreated = "global::ErrorOr.Created";
        public const string ResultUpdated = "global::ErrorOr.Updated";
        public const string ResultDeleted = "global::ErrorOr.Deleted";

        // ASP.NET Core MVC
        public const string FromBodyAttribute = "global::Microsoft.AspNetCore.Mvc.FromBodyAttribute";
        public const string FromFormAttribute = "global::Microsoft.AspNetCore.Mvc.FromFormAttribute";
        public const string FromHeaderAttribute = "global::Microsoft.AspNetCore.Mvc.FromHeaderAttribute";
        public const string FromQueryAttribute = "global::Microsoft.AspNetCore.Mvc.FromQueryAttribute";
        public const string FromRouteAttribute = "global::Microsoft.AspNetCore.Mvc.FromRouteAttribute";
        public const string FromServicesAttribute = "global::Microsoft.AspNetCore.Mvc.FromServicesAttribute";
        public const string ProblemDetails = "global::Microsoft.AspNetCore.Mvc.ProblemDetails";

        public const string ProducesResponseTypeAttribute =
            "global::Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute";

        // ASP.NET Core Http
        public const string AsParametersAttribute = "global::Microsoft.AspNetCore.Http.AsParametersAttribute";
        public const string HttpContext = "global::Microsoft.AspNetCore.Http.HttpContext";
        public const string Result = "global::Microsoft.AspNetCore.Http.IResult";

        public const string HttpValidationProblemDetails =
            "global::Microsoft.AspNetCore.Http.HttpValidationProblemDetails";

        public const string FormCollection = "global::Microsoft.AspNetCore.Http.IFormCollection";
        public const string FormFile = "global::Microsoft.AspNetCore.Http.IFormFile";
        public const string FormFileCollection = "global::Microsoft.AspNetCore.Http.IFormFileCollection";

        // DI
        public const string FromKeyedServicesAttribute =
            "global::Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute";

        // BCL Validation
        public const string Validator = "global::System.ComponentModel.DataAnnotations.Validator";
        public const string ValidationContext = "global::System.ComponentModel.DataAnnotations.ValidationContext";
        public const string ValidationResult = "global::System.ComponentModel.DataAnnotations.ValidationResult";

        // System
        public const string CancellationToken = "global::System.Threading.CancellationToken";

        public const string JsonSerializableAttribute =
            "global::System.Text.Json.Serialization.JsonSerializableAttribute";

        public const string JsonSerializerContext = "global::System.Text.Json.Serialization.JsonSerializerContext";
        public const string JsonException = "global::System.Text.Json.JsonException";
        public const string ParameterInfo = "global::System.Reflection.ParameterInfo";

        public const string Task = "global::System.Threading.Tasks.Task";
        public const string ValueTask = "global::System.Threading.Tasks.ValueTask";

        public const string Stream = "global::System.IO.Stream";
        public const string PipeReader = "global::System.IO.Pipelines.PipeReader";

        public const string List = "global::System.Collections.Generic.List";
        public const string Dictionary = "global::System.Collections.Generic.Dictionary";
        public const string ReadOnlyList = "global::System.Collections.Generic.IReadOnlyList";

        public const string String = "global::System.String";
        public const string Guid = "global::System.Guid";
        public const string DateTime = "global::System.DateTime";
        public const string DateTimeOffset = "global::System.DateTimeOffset";
        public const string DateOnly = "global::System.DateOnly";
        public const string TimeOnly = "global::System.TimeOnly";
        public const string TimeSpan = "global::System.TimeSpan";

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
        public const string ContentTypeFormData = "multipart/form-data";
        public const string HttpStatusesBaseUrl = "https://httpstatuses.io/";
    }
}