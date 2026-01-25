using System.Runtime.CompilerServices;
using VerifyTests;

namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Configures Verify serialization settings for source generator tests.
/// </summary>
public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Initialize Verify.SourceGenerators - handles GeneratorDriverRunResult serialization
        // This properly handles ImmutableArray<T> and other Roslyn types
        VerifySourceGenerators.Initialize();

        // Configure Verify to properly serialize EquatableArray<T> as arrays
        // This prevents the ImmutableArray<T> default instance error during serialization
        VerifierSettings.AddExtraSettings(static settings =>
        {
            settings.Converters.Add(new EquatableArrayJsonConverter());
        });
    }
}

/// <summary>
///     JSON converter for EquatableArray that serializes as a simple array without reflection.
/// </summary>
file sealed class EquatableArrayJsonConverter : WriteOnlyJsonConverter
{
    public override void Write(VerifyJsonWriter writer, object value)
    {
        // EquatableArray<T> implements IEnumerable<T>, so we can serialize it directly
        // Verify will handle the enumeration without needing reflection
        writer.Serialize(value);
    }

    public override bool CanConvert(Type type) =>
        type.IsGenericType &&
        type.GetGenericTypeDefinition().Name == "EquatableArray`1";
}
