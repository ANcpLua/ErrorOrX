using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ErrorOrX.Tests.TestUtils;

/// <summary>
/// Test helper for marking unreachable code paths.
/// Used in Match/Switch tests to verify correct branch execution.
/// </summary>
/// <remarks>
/// This is a simplified xUnit v2 compatible version of the SDK's Throw.UnreachableException.
/// Required because the linked tests from ErrorOrX.Tests reference this helper.
/// </remarks>
internal static class Unreachable
{
    [DoesNotReturn]
    public static void Throw(
        string? message = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        throw new UnreachableException(FormatMessage(message, memberName, filePath, lineNumber));
    }

    [DoesNotReturn]
    public static T Throw<T>(
        string? message = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        throw new UnreachableException(FormatMessage(message, memberName, filePath, lineNumber));
    }

    [DoesNotReturn]
    public static void Throw(
        bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))] string conditionExpression = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        _ = condition; // Suppress unused parameter warning
        throw new UnreachableException(FormatMessage(message, memberName, filePath, lineNumber, conditionExpression));
    }

    [DoesNotReturn]
    public static T Throw<T>(
        [DoesNotReturnIf(true)] bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))] string conditionExpression = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        _ = condition;
        throw new UnreachableException(FormatMessage(message, memberName, filePath, lineNumber, conditionExpression));
    }

    public static void ThrowIf(
        [DoesNotReturnIf(true)] bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))] string conditionExpression = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (condition)
        {
            throw new UnreachableException(FormatMessage(message, memberName, filePath, lineNumber, conditionExpression));
        }
    }

    [DoesNotReturn]
    public static T ThrowIf<T>(
        [DoesNotReturnIf(true)] bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))] string conditionExpression = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        _ = condition;
        throw new UnreachableException(FormatMessage(message, memberName, filePath, lineNumber, conditionExpression));
    }

    private static string FormatMessage(
        string? message,
        string memberName,
        string filePath,
        int lineNumber,
        string? conditionExpression = null)
    {
        var location = $"{filePath}:{lineNumber} in {memberName}";
        var condition = conditionExpression is not null ? $" Condition: {conditionExpression}." : "";
        var detail = message is not null ? $" {message}" : "";

        return $"Unreachable code reached at {location}.{condition}{detail}";
    }
}
