using ErrorOr.Core.Errors;

namespace ErrorOr.Core.ErrorOr;

internal static class EmptyErrors
{
    public static IReadOnlyList<Error> Instance { get; } = Array.Empty<Error>();
}
