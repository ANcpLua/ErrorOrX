using Xunit.Sdk;

namespace ErrorOrX.Tests.TestUtils;

/// <summary>
///     Serializable wrapper for Dictionary&lt;string, object&gt; to enable xUnit test discovery.
/// </summary>
public sealed class SerializableMetadata : IXunitSerializable
{
    public SerializableMetadata() => Value = null;

    public SerializableMetadata(Dictionary<string, object>? metadata) => Value = metadata;

    public Dictionary<string, object>? Value { get; private set; }

    public void Deserialize(IXunitSerializationInfo info)
    {
        var count = info.GetValue<int>("Count");
        if (count < 0)
        {
            Value = null;
            return;
        }

        Value = new Dictionary<string, object>();
        for (var i = 0; i < count; i++)
        {
            var key = info.GetValue<string>($"Key_{i}") ?? string.Empty;
            var value = info.GetValue<string>($"Value_{i}") ?? string.Empty;
            Value[key] = value;
        }
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        if (Value is null)
        {
            info.AddValue("Count", -1);
            return;
        }

        info.AddValue("Count", Value.Count);
        var i = 0;
        foreach (var kvp in Value)
        {
            info.AddValue($"Key_{i}", kvp.Key);
            info.AddValue($"Value_{i}", kvp.Value.ToString() ?? string.Empty);
            i++;
        }
    }

    public static implicit operator Dictionary<string, object>?(SerializableMetadata s) => s.Value;
    public static implicit operator SerializableMetadata(Dictionary<string, object>? d) => new(d);

    public override string ToString() => Value is null ? "null" : $"[{Value.Count} items]";
}
