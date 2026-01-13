using System.Runtime.CompilerServices;

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
///     JSON converter for EquatableArray that serializes as a simple array.
/// </summary>
file sealed class EquatableArrayJsonConverter : WriteOnlyJsonConverter
{
    public override void Write(VerifyJsonWriter writer, object value)
    {
        // Use reflection to get the Items property and serialize as array
        var type = value.GetType();
        if (!type.IsGenericType || type.GetGenericTypeDefinition().Name != "EquatableArray`1")
        {
            writer.Serialize(value);
            return;
        }

        // Get IsEmpty and Items through the interface
        var isEmptyProperty = type.GetProperty("IsEmpty");
        var isEmpty = isEmptyProperty?.GetValue(value) is true;

        if (isEmpty)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
            return;
        }

        // Get Items (returns ImmutableArray<T>)
        var itemsProperty = type.GetProperty("Items");
        var items = itemsProperty?.GetValue(value);

        if (items is null)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
            return;
        }

        // Convert to array and serialize
        var immutableArrayType = items.GetType();
        var toArrayMethod = immutableArrayType.GetMethod("ToArray");
        var array = toArrayMethod?.Invoke(items, null);

        writer.Serialize(array ?? Array.Empty<object>());
    }

    public override bool CanConvert(Type type) =>
        type.IsGenericType &&
        type.GetGenericTypeDefinition().Name == "EquatableArray`1";
}