using Microsoft.CodeAnalysis.Testing;

namespace ErrorOr.Endpoints.Tests;

public class XUnitV3Verifier : IVerifier
{
    public void Empty<T>(string collectionName, IEnumerable<T> collection) => Assert.Empty(collection);
    public void Equal<T>(T expected, T actual, string? message = null) => Assert.Equal(expected, actual);
    public void False(bool assert, string? message = null) => Assert.False(assert, message);
    public void NotEmpty<T>(string collectionName, IEnumerable<T> collection) => Assert.NotEmpty(collection);

    public void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual,
        IEqualityComparer<T>? equalityComparer = null, string? message = null) => Assert.Equal(expected, actual);

    public void True(bool assert, string? message = null) => Assert.True(assert, message);

    [DoesNotReturn]
    public void Fail(string? message = null) => Assert.Fail(message ?? "Assertion failed");

    public void LanguageIsSupported(string language)
    {
        if (language != "C#" && language != "Visual Basic")
            Fail($"Language {language} is not supported");
    }

    public IVerifier PushContext(string context) => this;
}
