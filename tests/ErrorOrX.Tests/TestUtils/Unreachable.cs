using System.Runtime.CompilerServices;

namespace ErrorOrX.Tests.TestUtils;

internal static class Unreachable
{
    [DoesNotReturn]
    public static void Throw(
        string? message = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        => Microsoft.Shared.Diagnostics.Throw.UnreachableException(message, memberName, filePath, lineNumber);

    [DoesNotReturn]
    public static T Throw<T>(
        string? message = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        => Microsoft.Shared.Diagnostics.Throw.UnreachableException<T>(message, memberName, filePath, lineNumber);

    [DoesNotReturn]
    public static void Throw(
        bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))] string conditionExpression = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        => Microsoft.Shared.Diagnostics.Throw.UnreachableException(
            condition, message, conditionExpression, memberName, filePath, lineNumber);

    [DoesNotReturn]
    public static T Throw<T>(
        [DoesNotReturnIf(true)] bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))] string conditionExpression = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        => Microsoft.Shared.Diagnostics.Throw.UnreachableException<T>(
            condition, message, conditionExpression, memberName, filePath, lineNumber);

    public static void ThrowIf(
        [DoesNotReturnIf(true)] bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))] string conditionExpression = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        => Microsoft.Shared.Diagnostics.Throw.UnreachableExceptionIf(
            condition, message, conditionExpression, memberName, filePath, lineNumber);

    [DoesNotReturn]
    public static T ThrowIf<T>(
        [DoesNotReturnIf(true)] bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))] string conditionExpression = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        => Microsoft.Shared.Diagnostics.Throw.UnreachableException<T>(
            condition, message, conditionExpression, memberName, filePath, lineNumber);
}
