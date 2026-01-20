namespace ErrorOrX.Tests.Errors;

public sealed class ErrorEqualityTests
{
    public static readonly TheoryData<string, string, int, Dictionary<string, object>?> ValidData =
        new()
        {
            {
                "CodeA", "DescriptionA", 1, null
            },
            {
                "CodeB", "DescriptionB", 3215, new Dictionary<string, object>
                {
                    {
                        "foo", "bar"
                    },
                    {
                        "baz", "qux"
                    }
                }
            }
        };

    public static readonly TheoryData<SerializableError, SerializableError> DifferentInstances =
        new()
        {
            {
                Error.Failure(), Error.Forbidden()
            },
            {
                Error.NotFound(), Error.NotFound(metadata: new Dictionary<string, object>
                {
                    ["Foo"] = "Bar"
                })
            },
            {
                Error.Unexpected(metadata: new Dictionary<string, object>
                {
                    ["baz"] = "qux"
                }),
                Error.Unexpected()
            },
            {
                Error.Failure(metadata: new Dictionary<string, object>
                {
                    ["baz"] = "qux"
                }),
                Error.Failure(metadata: new Dictionary<string, object>
                {
                    ["Foo"] = "Bar", ["baz"] = "qux"
                })
            },
            {
                Error.Failure(metadata: new Dictionary<string, object>
                {
                    ["baz"] = "qux"
                }),
                Error.Failure(metadata: new Dictionary<string, object>
                {
                    ["baz"] = "gorge"
                })
            }
        };

    [Theory]
    [MemberData(nameof(ValidData))]
    public void Equals_WhenTwoInstancesHaveTheSameValues_ShouldReturnTrue(
        string code,
        string description,
        int numericType,
        Dictionary<string, object>? metadata)
    {
        var error1 = Error.Custom(numericType, code, description, metadata);
        var clonedDictionary = metadata is null ? null : new Dictionary<string, object>(metadata);
        var error2 = Error.Custom(numericType, code, description, clonedDictionary);

        var result = error1.Equals(error2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WhenTwoInstancesHaveTheSameMetadataInstanceAndPropertyValues_ShouldReturnTrue()
    {
        var metadata = new Dictionary<string, object>
        {
            {
                "foo", "bar"
            }
        };
        var error1 = Error.Custom(1, "Code", "Description", metadata);
        var error2 = Error.Custom(1, "Code", "Description", metadata);

        var result = error1.Equals(error2);

        result.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(DifferentInstances))]
    public void Equals_WhenTwoInstancesHaveDifferentValues_ShouldReturnFalse(SerializableError error1, SerializableError error2)
    {
        var result = ((Error)error1).Equals(error2);

        result.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(ValidData))]
    public void GetHashCode_WhenTwoInstancesHaveSameValues_ShouldReturnSameHashCode(
        string code,
        string description,
        int numericType,
        Dictionary<string, object>? metadata)
    {
        var error1 = Error.Custom(numericType, code, description, metadata);
        var clonedDictionary = metadata is null ? null : new Dictionary<string, object>(metadata);
        var error2 = Error.Custom(numericType, code, description, clonedDictionary);

        var hashCode1 = error1.GetHashCode();
        var hashCode2 = error2.GetHashCode();

        hashCode1.Should().Be(hashCode2);
    }

    [Theory]
    [MemberData(nameof(DifferentInstances))]
    public void GetHashCode_WhenTwoInstancesHaveDifferentValues_ShouldReturnDifferentHashCodes(
        SerializableError error1,
        SerializableError error2)
    {
        var hashCode1 = ((Error)error1).GetHashCode();
        var hashCode2 = ((Error)error2).GetHashCode();

        hashCode1.Should().NotBe(hashCode2);
    }
}
