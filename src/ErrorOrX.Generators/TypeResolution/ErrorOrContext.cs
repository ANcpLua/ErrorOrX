using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using ANcpLua.Roslyn.Utilities.Contexts;
using Microsoft.CodeAnalysis;

namespace ErrorOr.Generators;

internal sealed class ErrorOrContext
{
    public ErrorOrContext(Compilation compilation)
    {
        FromBodyAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromBodyAttribute);
        FromServicesAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromServicesAttribute);
        FromRouteAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromRouteAttribute);
        FromQueryAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromQueryAttribute);
        FromHeaderAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromHeaderAttribute);
        FromFormAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromFormAttribute);

        ProducesErrorAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.ProducesErrorAttribute);
        AcceptedResponseAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.AcceptedResponseAttribute);
        ReturnsErrorAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.ReturnsErrorAttribute);
        ErrorOrOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.ErrorOrT)?.ConstructedFrom;
        Error = compilation.GetBestTypeByMetadataName(WellKnownTypes.ErrorStruct);

        FromKeyedServicesAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.FromKeyedServicesAttribute);
        AsParametersAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.AsParametersAttribute);
        FormFileCollection = compilation.GetBestTypeByMetadataName(WellKnownTypes.FormFileCollection);
        FormCollection = compilation.GetBestTypeByMetadataName(WellKnownTypes.FormCollection);
        FormFile = compilation.GetBestTypeByMetadataName(WellKnownTypes.FormFile);
        HttpContext = compilation.GetBestTypeByMetadataName(WellKnownTypes.HttpContext);
        BindableFromHttpContext = compilation.GetBestTypeByMetadataName(WellKnownTypes.BindableFromHttpContext);
        ParameterInfo = compilation.GetBestTypeByMetadataName(WellKnownTypes.ParameterInfo);
        SseItemOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.SseItemT)?.ConstructedFrom;

        CancellationToken = compilation.GetBestTypeByMetadataName(WellKnownTypes.CancellationToken);

        SuccessMarker = compilation.GetBestTypeByMetadataName(WellKnownTypes.Success);
        CreatedMarker = compilation.GetBestTypeByMetadataName(WellKnownTypes.CreatedMarker);
        UpdatedMarker = compilation.GetBestTypeByMetadataName(WellKnownTypes.Updated);
        DeletedMarker = compilation.GetBestTypeByMetadataName(WellKnownTypes.Deleted);

        Awaitable = new AwaitableContext(compilation);

        ListOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.ListT)?.ConstructedFrom;
        IListOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.IListT)?.ConstructedFrom;
        IEnumerableOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.IEnumerableT)?.ConstructedFrom;
        IAsyncEnumerableOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.IAsyncEnumerableT)
            ?.ConstructedFrom;
        IReadOnlyListOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.IReadOnlyListT)
            ?.ConstructedFrom;
        ICollectionOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.ICollectionT)?.ConstructedFrom;
        HashSetOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.HashSetT)?.ConstructedFrom;

        Guid = compilation.GetBestTypeByMetadataName(WellKnownTypes.Guid);
        DateTime = compilation.GetBestTypeByMetadataName(WellKnownTypes.DateTime);
        DateTimeOffset = compilation.GetBestTypeByMetadataName(WellKnownTypes.DateTimeOffset);
        DateOnly = compilation.GetBestTypeByMetadataName(WellKnownTypes.DateOnly);
        TimeOnly = compilation.GetBestTypeByMetadataName(WellKnownTypes.TimeOnly);
        TimeSpan = compilation.GetBestTypeByMetadataName(WellKnownTypes.TimeSpan);

        ReadOnlySpanOfT = compilation.GetBestTypeByMetadataName(WellKnownTypes.ReadOnlySpanT)?.ConstructedFrom;
        IFormatProvider = compilation.GetBestTypeByMetadataName(WellKnownTypes.IFormatProvider);
        Stream = compilation.GetBestTypeByMetadataName(WellKnownTypes.Stream);
        PipeReader = compilation.GetBestTypeByMetadataName(WellKnownTypes.PipeReader);

        AuthorizeAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.AuthorizeAttribute);
        AllowAnonymousAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.AllowAnonymousAttribute);
        EnableRateLimitingAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.EnableRateLimitingAttribute);
        DisableRateLimitingAttribute =
            compilation.GetBestTypeByMetadataName(WellKnownTypes.DisableRateLimitingAttribute);
        OutputCacheAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.OutputCacheAttribute);
        EnableCorsAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.EnableCorsAttribute);
        DisableCorsAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.DisableCorsAttribute);

        ValidationAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.ValidationAttribute);
        IValidatableObject = compilation.GetBestTypeByMetadataName(WellKnownTypes.IValidatableObject);

        IValidatableInfoResolverSymbol =
            compilation.GetBestTypeByMetadataName(WellKnownTypes.IValidatableInfoResolver);

        ApiVersionAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.ApiVersionAttribute);
        ApiVersionNeutralAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.ApiVersionNeutralAttribute);
        MapToApiVersionAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.MapToApiVersionAttribute);

        RouteGroupAttribute = compilation.GetBestTypeByMetadataName(WellKnownTypes.RouteGroupAttribute);
    }

    private INamedTypeSymbol? FromBodyAttribute { get; }
    private INamedTypeSymbol? FromServicesAttribute { get; }
    private INamedTypeSymbol? FromRouteAttribute { get; }
    private INamedTypeSymbol? FromQueryAttribute { get; }
    private INamedTypeSymbol? FromHeaderAttribute { get; }
    private INamedTypeSymbol? FromFormAttribute { get; }

    public INamedTypeSymbol? SuccessMarker { get; }
    public INamedTypeSymbol? CreatedMarker { get; }
    public INamedTypeSymbol? UpdatedMarker { get; }
    public INamedTypeSymbol? DeletedMarker { get; }

    public AwaitableContext Awaitable { get; }

    public INamedTypeSymbol? ListOfT { get; }
    public INamedTypeSymbol? IListOfT { get; }
    public INamedTypeSymbol? IEnumerableOfT { get; }
    public INamedTypeSymbol? IAsyncEnumerableOfT { get; }
    public INamedTypeSymbol? IReadOnlyListOfT { get; }
    public INamedTypeSymbol? ICollectionOfT { get; }
    public INamedTypeSymbol? HashSetOfT { get; }
    public INamedTypeSymbol? SseItemOfT { get; }
    public INamedTypeSymbol? ErrorOrOfT { get; }
    public INamedTypeSymbol? Error { get; }

    public INamedTypeSymbol? Guid { get; }
    public INamedTypeSymbol? DateTime { get; }
    public INamedTypeSymbol? DateTimeOffset { get; }
    public INamedTypeSymbol? DateOnly { get; }
    public INamedTypeSymbol? TimeOnly { get; }
    public INamedTypeSymbol? TimeSpan { get; }

    public INamedTypeSymbol? ReadOnlySpanOfT { get; }
    public INamedTypeSymbol? IFormatProvider { get; }
    private INamedTypeSymbol? Stream { get; }
    private INamedTypeSymbol? PipeReader { get; }

    public INamedTypeSymbol? ProducesErrorAttribute { get; }
    public INamedTypeSymbol? AcceptedResponseAttribute { get; }
    public INamedTypeSymbol? ReturnsErrorAttribute { get; }

    private INamedTypeSymbol? FromKeyedServicesAttribute { get; }
    private INamedTypeSymbol? AsParametersAttribute { get; }
    private INamedTypeSymbol? FormFileCollection { get; }
    private INamedTypeSymbol? FormCollection { get; }
    private INamedTypeSymbol? FormFile { get; }
    private INamedTypeSymbol? HttpContext { get; }
    public INamedTypeSymbol? BindableFromHttpContext { get; }
    private INamedTypeSymbol? ParameterInfo { get; }

    private INamedTypeSymbol? CancellationToken { get; }

    public INamedTypeSymbol? AuthorizeAttribute { get; }
    public INamedTypeSymbol? AllowAnonymousAttribute { get; }
    public INamedTypeSymbol? EnableRateLimitingAttribute { get; }
    public INamedTypeSymbol? DisableRateLimitingAttribute { get; }
    public INamedTypeSymbol? OutputCacheAttribute { get; }
    public INamedTypeSymbol? EnableCorsAttribute { get; }
    public INamedTypeSymbol? DisableCorsAttribute { get; }

    private INamedTypeSymbol? ValidationAttribute { get; }
    private INamedTypeSymbol? IValidatableObject { get; }
    private INamedTypeSymbol? IValidatableInfoResolverSymbol { get; }

    /// <summary>
    ///     Returns true if the Microsoft.Extensions.Validation package is referenced,
    ///     meaning the consumer supports .NET 10 validation infrastructure.
    /// </summary>
    public bool HasValidationResolverSupport => IValidatableInfoResolverSymbol is not null;

    public INamedTypeSymbol? ApiVersionAttribute { get; }
    public INamedTypeSymbol? ApiVersionNeutralAttribute { get; }
    public INamedTypeSymbol? MapToApiVersionAttribute { get; }

    public INamedTypeSymbol? RouteGroupAttribute { get; }

    /// <summary>
    ///     Returns true if the Asp.Versioning.Http package is referenced.
    /// </summary>
    public bool HasApiVersioningSupport => ApiVersionAttribute is not null;

    public INamedTypeSymbol? FromBody => FromBodyAttribute;
    public INamedTypeSymbol? FromServices => FromServicesAttribute;
    public INamedTypeSymbol? FromRoute => FromRouteAttribute;
    public INamedTypeSymbol? FromQuery => FromQueryAttribute;
    public INamedTypeSymbol? FromHeader => FromHeaderAttribute;
    public INamedTypeSymbol? FromForm => FromFormAttribute;
    public INamedTypeSymbol? FromKeyedServices => FromKeyedServicesAttribute;
    public INamedTypeSymbol? AsParameters => AsParametersAttribute;

    /// <summary>
    ///     Creates an incremental provider that resolves ErrorOrContext once per compilation.
    ///     This avoids the N+1 performance issue where ErrorOrContext was created N times
    ///     (once per endpoint), causing 90+ symbol lookups per endpoint.
    /// </summary>
    public static IncrementalValueProvider<ErrorOrContext> CreateProvider(
        IncrementalGeneratorInitializationContext context)
    {
        return context.CompilationProvider
            .Select(static (compilation, _) => new ErrorOrContext(compilation));
    }

    /// <summary>
    ///     Determines if a type requires BCL validation.
    ///     Returns true if the type:
    ///     1. Has any property (including inherited) with an attribute deriving from ValidationAttribute, OR
    ///     2. Has a constructor parameter with a matching property and a ValidationAttribute (records), OR
    ///     3. Implements IValidatableObject
    /// </summary>
    public bool RequiresValidation(ITypeSymbol? type)
    {
        if (type is null || ValidationAttribute is null)
        {
            return false;
        }

        if (type.SpecialType is not SpecialType.None ||
            type.TypeKind is TypeKind.Enum or TypeKind.Interface)
        {
            return false;
        }

        if (IValidatableObject is not null &&
            type.AllInterfaces.Any(i => i.IsEqualTo(IValidatableObject)))
        {
            return true;
        }

        var current = type;
        while (current is INamedTypeSymbol namedType)
        {
            foreach (var member in namedType.GetMembers())
            {
                if (member is not IPropertySymbol property)
                {
                    continue;
                }

                if (property.HasAttribute(ValidationAttribute))
                {
                    return true;
                }
            }

            // In modern .NET, ValidationAttribute targets Parameter, so for records
            // [Required] stays on the constructor parameter, not the synthesized property.
            if (HasValidationAttributeOnConstructorParam(namedType))
            {
                return true;
            }

            current = namedType.BaseType;
        }

        return false;
    }

    /// <summary>
    ///     Checks if any constructor parameter that corresponds to a property has a validation attribute.
    ///     Handles records where attributes like [Required] target the parameter, not the property.
    /// </summary>
    private bool HasValidationAttributeOnConstructorParam(INamedTypeSymbol namedType)
    {
        foreach (var ctor in namedType.InstanceConstructors)
        {
            foreach (var param in ctor.Parameters)
            {
                if (!param.HasAttribute(ValidationAttribute))
                {
                    continue;
                }

                // Only count if there's a matching property (record positional parameter pattern)
                foreach (var member in namedType.GetMembers(param.Name))
                {
                    if (member is IPropertySymbol)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     Collects validatable property descriptors for a type.
    ///     For each property, checks both property-level and constructor parameter-level attributes
    ///     (records place validation attributes on the constructor parameter, not the property).
    /// </summary>
    public EquatableArray<ValidatablePropertyDescriptor> CollectValidatableProperties(ITypeSymbol? type)
    {
        if (type is null || ValidationAttribute is null)
        {
            return default;
        }

        var properties = ImmutableArray.CreateBuilder<ValidatablePropertyDescriptor>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var current = type;
        while (current is INamedTypeSymbol namedType)
        {
            foreach (var member in namedType.GetMembers())
            {
                if (member is not IPropertySymbol property)
                {
                    continue;
                }

                // Skip properties already collected from derived type (handles overrides/hides)
                if (!seen.Add(property.Name))
                {
                    continue;
                }

                var propertyAttrs = CollectValidationAttributes(property);
                var ctorParam = FindMatchingConstructorParam(namedType, property.Name);
                var paramAttrs = ctorParam is not null
                    ? CollectValidationAttributes(ctorParam)
                    : default;

                var attrs = MergeValidationAttributes(propertyAttrs, paramAttrs);
                if (attrs.IsDefaultOrEmpty)
                {
                    continue;
                }

                properties.Add(new ValidatablePropertyDescriptor(
                    property.Name,
                    property.Type.GetFullyQualifiedName(),
                    property.Name,
                    attrs));
            }

            current = namedType.BaseType;
        }

        return properties.Count > 0
            ? new EquatableArray<ValidatablePropertyDescriptor>(properties.ToImmutable())
            : default;
    }

    private static IParameterSymbol? FindMatchingConstructorParam(INamedTypeSymbol type, string propertyName)
    {
        foreach (var ctor in type.InstanceConstructors)
        {
            foreach (var param in ctor.Parameters)
            {
                if (string.Equals(param.Name, propertyName, StringComparison.Ordinal))
                {
                    return param;
                }
            }
        }

        return null;
    }

    private static EquatableArray<ValidatableAttributeInfo> MergeValidationAttributes(
        EquatableArray<ValidatableAttributeInfo> a, EquatableArray<ValidatableAttributeInfo> b)
    {
        if (a.IsDefaultOrEmpty) return b;
        if (b.IsDefaultOrEmpty) return a;
        return new EquatableArray<ValidatableAttributeInfo>([.. a.AsImmutableArray(), .. b.AsImmutableArray()]);
    }

    private EquatableArray<ValidatableAttributeInfo> CollectValidationAttributes(ISymbol property)
    {
        if (ValidationAttribute is null)
        {
            return default;
        }

        var attrs = ImmutableArray.CreateBuilder<ValidatableAttributeInfo>();
        foreach (var attrData in property.GetAttributes())
        {
            if (attrData.AttributeClass is null)
            {
                continue;
            }

            if (!attrData.AttributeClass.IsOrInheritsFrom(ValidationAttribute))
            {
                continue;
            }

            var ctorArgs = ImmutableArray.CreateBuilder<string>();
            foreach (var arg in attrData.ConstructorArguments)
            {
                ctorArgs.Add(TypedConstantToLiteral(arg));
            }

            var namedArgs = ImmutableArray.CreateBuilder<NamedArgLiteral>();
            foreach (var namedArg in attrData.NamedArguments)
            {
                namedArgs.Add(new NamedArgLiteral(namedArg.Key, TypedConstantToLiteral(namedArg.Value)));
            }

            attrs.Add(new ValidatableAttributeInfo(
                attrData.AttributeClass.GetFullyQualifiedName(),
                new EquatableArray<string>(ctorArgs.ToImmutable()),
                new EquatableArray<NamedArgLiteral>(namedArgs.ToImmutable())));
        }

        return attrs.Count > 0
            ? new EquatableArray<ValidatableAttributeInfo>(attrs.ToImmutable())
            : default;
    }

    private static string TypedConstantToLiteral(TypedConstant constant)
    {
        if (constant.Kind == TypedConstantKind.Array)
        {
            var elements = constant.Values;
            return $"new[] {{ {string.Join(", ", elements.Select(TypedConstantToLiteral))} }}";
        }

        if (constant.Value is null)
        {
            return "null";
        }

        return constant switch
        {
            { Kind: TypedConstantKind.Type, Value: ITypeSymbol typeSymbol } =>
                $"typeof({typeSymbol.GetFullyQualifiedName()})",
            { Kind: TypedConstantKind.Enum, Type: not null } =>
                $"({constant.Type.GetFullyQualifiedName()}){constant.Value}",
            _ => constant.Value switch
            {
                string s => $"\"{EscapeStringLiteral(s)}\"",
                bool b => b ? "true" : "false",
                char c => $"'{EscapeCharLiteral(c)}'",
                float f => f.ToString(CultureInfo.InvariantCulture) + "f",
                double d => d.ToString(CultureInfo.InvariantCulture) + "d",
                decimal m => m.ToString(CultureInfo.InvariantCulture) + "m",
                long l => $"{l}L",
                ulong ul => $"{ul}UL",
                uint ui => $"{ui}U",
                _ => Convert.ToString(constant.Value, CultureInfo.InvariantCulture) ?? "null"
            }
        };
    }

    private static string EscapeStringLiteral(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            switch (c)
            {
                case '\\': sb.Append(@"\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append(@"\n"); break;
                case '\r': sb.Append(@"\r"); break;
                case '\t': sb.Append(@"\t"); break;
                case '\0': sb.Append(@"\0"); break;
                case '\a': sb.Append(@"\a"); break;
                case '\b': sb.Append(@"\b"); break;
                case '\f': sb.Append(@"\f"); break;
                case '\v': sb.Append(@"\v"); break;
                default:
                    if (char.IsControl(c) || c > 127)
                    {
                        sb.Append($"\\u{(int)c:X4}");
                    }
                    else
                    {
                        sb.Append(c);
                    }

                    break;
            }
        }

        return sb.ToString();
    }

    private static string EscapeCharLiteral(char c)
    {
        return c switch
        {
            '\\' => @"\\",
            '\'' => @"\'",
            '\n' => @"\n",
            '\r' => @"\r",
            '\t' => @"\t",
            '\0' => @"\0",
            '\a' => @"\a",
            '\b' => @"\b",
            '\f' => @"\f",
            '\v' => @"\v",
            _ when char.IsControl(c) || c > 127 => $"\\u{(int)c:X4}",
            _ => c.ToString()
        };
    }

    /// <summary>Checks if the type implements IFormFile.</summary>
    public bool IsFormFile(ITypeSymbol? type)
    {
        type = type?.UnwrapNullable();
        if (type is null)
        {
            return false;
        }

        if (FormFile is not null && type.IsOrImplements(FormFile))
        {
            return true;
        }

        return type.Name == "IFormFile" &&
               type.ContainingNamespace.ToDisplayString() == "Microsoft.AspNetCore.Http";
    }

    /// <summary>Checks if the type is IFormFileCollection or IReadOnlyList&lt;IFormFile&gt;.</summary>
    public bool IsFormFileCollection(ITypeSymbol? type)
    {
        type = type?.UnwrapNullable();
        if (type is null)
        {
            return false;
        }

        if (FormFileCollection is not null &&
            type.IsEqualTo(FormFileCollection))
        {
            return true;
        }

        if (IReadOnlyListOfT is not null &&
            type is INamedTypeSymbol { IsGenericType: true } named &&
            named.ConstructedFrom.IsEqualTo(IReadOnlyListOfT))
        {
            if (IsFormFile(named.TypeArguments[0]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Checks if the type implements IFormCollection.</summary>
    public bool IsFormCollection(ITypeSymbol? type)
    {
        type = type?.UnwrapNullable();
        if (type is null)
        {
            return false;
        }

        return FormCollection is not null && type.IsOrImplements(FormCollection);
    }

    /// <summary>Checks if the type is or inherits from HttpContext.</summary>
    public bool IsHttpContext(ITypeSymbol? type)
    {
        type = type?.UnwrapNullable();
        if (type is null)
        {
            return false;
        }

        if (HttpContext is not null && type.IsOrInheritsFrom(HttpContext))
        {
            return true;
        }

        return type.Name == "HttpContext" &&
               type.ContainingNamespace.ToDisplayString() == "Microsoft.AspNetCore.Http";
    }

    /// <summary>Checks if the type is or inherits from System.IO.Stream.</summary>
    public bool IsStream(ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        type = type.UnwrapNullable();
        if (Stream is not null)
        {
            return type.IsOrInheritsFrom(Stream);
        }

        return type.Name == "Stream" &&
               type.ContainingNamespace.ToDisplayString() == "System.IO";
    }

    /// <summary>Checks if the type is or inherits from System.IO.Pipelines.PipeReader.</summary>
    public bool IsPipeReader(ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        type = type.UnwrapNullable();
        if (PipeReader is not null)
        {
            return type.IsOrInheritsFrom(PipeReader);
        }

        return type.Name == "PipeReader" &&
               type.ContainingNamespace.ToDisplayString() == "System.IO.Pipelines";
    }

    /// <summary>Checks if the type is System.Threading.CancellationToken.</summary>
    public bool IsCancellationToken(ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        type = type.UnwrapNullable();
        if (CancellationToken is not null)
        {
            return type.IsEqualTo(CancellationToken);
        }

        return type.Name == "CancellationToken" &&
               type.ContainingNamespace.ToDisplayString() == "System.Threading";
    }

    /// <summary>Checks if the type is System.Reflection.ParameterInfo.</summary>
    public bool IsParameterInfo(ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        type = type.UnwrapNullable();
        if (ParameterInfo is not null)
        {
            return type.IsEqualTo(ParameterInfo);
        }

        return type.Name == "ParameterInfo" &&
               type.ContainingNamespace.ToDisplayString() == "System.Reflection";
    }
}
