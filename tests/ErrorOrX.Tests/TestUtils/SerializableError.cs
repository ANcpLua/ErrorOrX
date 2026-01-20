using Xunit.Sdk;

namespace ErrorOrX.Tests.TestUtils;

public class SerializableError : IXunitSerializable
{
    public SerializableError() { }

    public SerializableError(Error error) => Value = error;
    public Error Value { get; private set; }

    public void Deserialize(IXunitSerializationInfo info)
    {
        var code = info.GetValue<string>("Code") ?? string.Empty;
        var description = info.GetValue<string>("Description") ?? string.Empty;
        var type = (ErrorType)info.GetValue<int>("Type");

        var metadataCount = info.GetValue<int>("MetadataCount");
        Dictionary<string, object>? metadata = null;

        if (metadataCount >= 0)
        {
            metadata = new Dictionary<string, object>();
            for (var i = 0; i < metadataCount; i++)
            {
                var key = info.GetValue<string>($"MetadataKey_{i}") ?? string.Empty;
                var value = info.GetValue<string>($"MetadataValue_{i}") ?? string.Empty;
                metadata[key] = value;
            }
        }

        Value = Error.Custom((int)type, code, description, metadata);
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue("Code", Value.Code);
        info.AddValue("Description", Value.Description);
        info.AddValue("Type", (int)Value.Type);

        if (Value.Metadata is null)
        {
            info.AddValue("MetadataCount", -1);
        }
        else
        {
            info.AddValue("MetadataCount", Value.Metadata.Count);
            var i = 0;
            foreach (var kvp in Value.Metadata)
            {
                info.AddValue($"MetadataKey_{i}", kvp.Key);
                // Assumption: Test metadata values are simple strings as observed in the codebase
                info.AddValue($"MetadataValue_{i}", kvp.Value?.ToString() ?? string.Empty);
                i++;
            }
        }
    }

    public static implicit operator Error(SerializableError s) => s.Value;
    public static implicit operator SerializableError(Error e) => new(e);

    public override string ToString() => $"Error({Value.Code}, {Value.Type})";
}
