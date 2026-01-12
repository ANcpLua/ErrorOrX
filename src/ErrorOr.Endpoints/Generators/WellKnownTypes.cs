// ReSharper disable InconsistentNaming
namespace ErrorOr.Endpoints.Generators;

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
    public const string ErrorOrT = "ErrorOr.Core.ErrorOr.ErrorOr`1";
    public const string ErrorType = "ErrorOr.Core.Errors.ErrorType";
    public const string Error = "ErrorOr.Core.Errors.Error";

    // ErrorOr result markers
    public const string Success = "ErrorOr.Core.Results.Success";
    public const string Created = "ErrorOr.Core.Results.Created";
    public const string Updated = "ErrorOr.Core.Results.Updated";
    public const string Deleted = "ErrorOr.Core.Results.Deleted";

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
    public const string ObsoleteAttribute = "System.ObsoleteAttribute";
    public const string JsonSerializableAttribute = "System.Text.Json.Serialization.JsonSerializableAttribute";
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
    public const string AsyncEnumerableT = "System.Collections.Generic.IAsyncEnumerable`1";

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
        public const string ErrorOr = "global::ErrorOr.Core.ErrorOr.ErrorOr";
        public const string ErrorType = "global::ErrorOr.Core.Errors.ErrorType";
        public const string Error = "global::ErrorOr.Core.Errors.Error";
        public const string ResultSuccess = "global::ErrorOr.Core.Results.Success";
        public const string ResultCreated = "global::ErrorOr.Core.Results.Created";
        public const string ResultUpdated = "global::ErrorOr.Core.Results.Updated";
        public const string ResultDeleted = "global::ErrorOr.Core.Results.Deleted";

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
        public const string ObsoleteAttribute = "global::System.ObsoleteAttribute";

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
    }
}