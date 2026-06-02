using ErrorOr.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ErrorOr.Analyzers;

/// <summary>
///     Body-source counting (EOE006) and DataAnnotations reflection check (EOE034) for the
///     <see cref="ErrorOrEndpointAnalyzer" />. Body classification routes through
///     <see cref="ErrorOrContext" /> so analyzer and generator share one source of truth
///     (FQN + inheritance, not name-substring match).
/// </summary>
public sealed partial class ErrorOrEndpointAnalyzer
{
    private static int CountBodySources(IMethodSymbol method)
    {
        var bodyCount = 0;
        var hasFromForm = false;
        var hasStream = false;

        foreach (var param in method.Parameters)
        {
            // Check for body-related attributes using HasAttribute
            if (param.HasAttribute(WellKnownTypes.FromBodyAttribute))
            {
                bodyCount++;
                continue;
            }

            if (param.HasAttribute(WellKnownTypes.FromFormAttribute))
            {
                hasFromForm = true;
                continue;
            }

            // Body-related types: route through ErrorOrContext so analyzer + generator
            // share one source of truth (FQN + inheritance, not name substring match).
            if (ErrorOrContext.IsStream(param.Type) || ErrorOrContext.IsPipeReader(param.Type))
                hasStream = true;
            else if (ErrorOrContext.IsFormFile(param.Type) ||
                     ErrorOrContext.IsFormFileCollection(param.Type) ||
                     ErrorOrContext.IsFormCollection(param.Type))
                hasFromForm = true;
        }

        // Multiple [FromBody] is always an error
        if (bodyCount > 1)
            return bodyCount;

        // Otherwise return number of distinct body source buckets used
        return (bodyCount > 0 ? 1 : 0) + (hasFromForm ? 1 : 0) + (hasStream ? 1 : 0);
    }

    private static bool HasAcceptedResponseAttribute(ISymbol method)
    {
        return method.HasAttribute(WellKnownTypes.AcceptedResponseAttribute);
    }

    /// <summary>
    ///     Reports EOE034 for any parameter that would trigger BCL reflection-based validation —
    ///     either directly attributed with a ValidationAttribute, or whose type carries validation
    ///     on a property or constructor parameter. Delegates the predicate to
    ///     <see cref="ErrorOrContext.HasValidationNeeds" /> so analyzer and generator stay aligned
    ///     (the generator dual-reports the same diagnostic for build-output + snapshot coverage).
    /// </summary>
    /// <remarks>
    ///     The shared predicate routes through <see cref="ErrorOrContext.IsOrInheritsFrom" />
    ///     (symbol-side FQN walk) rather than <c>Compilation.GetTypeByMetadataName</c> (Pattern A),
    ///     which returns null on multi-assembly type-forwarding — dotnet/roslyn#52037 and
    ///     Compilation.cs:1221 ("Type forwarders are ignored"). The .NET 10 ref-pack ships
    ///     <c>System.ComponentModel.DataAnnotations.dll</c> as a forwarder facade alongside the
    ///     runtime impl, so Pattern A silently returns null on consumer compilations.
    /// </remarks>
    private static void CheckForValidationAttributes(
        in SymbolAnalysisContext context,
        IMethodSymbol method)
    {
        // EOE034 applies only to the reflection (Validator.TryValidateObject) path. When the consumer
        // references Microsoft.Extensions.Validation, the generator emits source-generated
        // ValidatableTypeInfo.ValidateAsync (no reflection), so the advisory must not fire.
        if (new ErrorOrContext(context.Compilation).HasValidationResolverSupport)
            return;

        foreach (var param in method.Parameters)
        {
            if (!ErrorOrContext.HasValidationNeeds(param))
                continue;

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.ValidationUsesReflection,
                param.Locations.FirstOrDefault() ?? method.Locations.FirstOrDefault(),
                param.Name,
                method.Name));
        }
    }
}
