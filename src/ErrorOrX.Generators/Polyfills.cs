// Minimal polyfills for netstandard2.0 - replaces PolySharp to avoid type conflicts
// when test projects reference this assembly with ReferenceOutputAssembly="true"

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    ///     Specifies that the method or property will ensure that the listed field and property members have not-null
    ///     values when returning with the specified return value condition.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    internal sealed class MemberNotNullWhenAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the specified return value condition and a field or property member.</summary>
        public MemberNotNullWhenAttribute(bool returnValue, string member)
        {
            ReturnValue = returnValue;
            Members = [member];
        }

        /// <summary>Initializes the attribute with the specified return value condition and list of field and property members.</summary>
        public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
        {
            ReturnValue = returnValue;
            Members = members;
        }

        /// <summary>Gets the return value condition.</summary>
        public bool ReturnValue { get; }

        /// <summary>Gets field or property member names.</summary>
        public string[] Members { get; }
    }

    /// <summary>
    ///     Specifies that when a method returns ReturnValue, the parameter will not be null even if the corresponding
    ///     type allows it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class NotNullWhenAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the specified return value condition.</summary>
        public NotNullWhenAttribute(bool returnValue)
        {
            ReturnValue = returnValue;
        }

        /// <summary>Gets the return value condition.</summary>
        public bool ReturnValue { get; }
    }
}

namespace System.Runtime.CompilerServices
{
    /// <summary>Used to indicate to the compiler that the .locals init flag should not be set in method headers.</summary>
    [AttributeUsage(
        AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface |
        AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event,
        Inherited = false)]
    internal sealed class SkipLocalsInitAttribute : Attribute;

    /// <summary>Used to indicate to the compiler that a method should be called in its containing module's initializer.</summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class ModuleInitializerAttribute : Attribute;

    /// <summary>
    ///     Reserved to be used by the compiler for tracking metadata. This class should not be used by developers in
    ///     source code.
    /// </summary>
    internal sealed class IsExternalInit
    {
    }

    /// <summary>Specifies that a type has required members or that a member is required.</summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property,
        Inherited = false)]
    internal sealed class RequiredMemberAttribute : Attribute;

    /// <summary>
    ///     Indicates that compiler support for a particular feature is required for the location where this attribute is
    ///     applied.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        /// <summary>The RefStructs feature.</summary>
        public const string RefStructs = nameof(RefStructs);

        /// <summary>The RequiredMembers feature.</summary>
        public const string RequiredMembers = nameof(RequiredMembers);

        /// <summary>Initializes a new instance of the CompilerFeatureRequiredAttribute class for the specified compiler feature.</summary>
        public CompilerFeatureRequiredAttribute(string featureName)
        {
            FeatureName = featureName;
        }

        /// <summary>Gets the name of the compiler feature.</summary>
        public string FeatureName { get; }

        /// <summary>
        ///     Gets or sets a value indicating whether the compiler can choose to allow access to the location where this
        ///     attribute is applied if it does not understand FeatureName.
        /// </summary>
        public bool IsOptional { get; set; }
    }

    /// <summary>
    ///     Indicates the type of the async method builder that should be used by a language compiler to build the
    ///     attributed async method or to build the attributed type when used as the return type of an async method.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Delegate |
        AttributeTargets.Enum | AttributeTargets.Method, Inherited = false)]
    internal sealed class AsyncMethodBuilderAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the AsyncMethodBuilderAttribute class.</summary>
        public AsyncMethodBuilderAttribute(Type builderType)
        {
            BuilderType = builderType;
        }

        /// <summary>Gets the Type of the associated builder.</summary>
        public Type BuilderType { get; }
    }
}

namespace System
{
    /// <summary>Represent a range has start and end indexes.</summary>
    internal readonly struct Range : IEquatable<Range>
    {
        /// <summary>Represents the inclusive start index of the Range.</summary>
        public Index Start { get; }

        /// <summary>Represents the exclusive end index of the Range.</summary>
        public Index End { get; }

        /// <summary>Constructs a Range object using the start and end indexes.</summary>
        public Range(Index start, Index end)
        {
            Start = start;
            End = end;
        }

        /// <inheritdoc />
        public override bool Equals(object? value)
        {
            return value is Range r && r.Start.Equals(Start) && r.End.Equals(End);
        }

        /// <inheritdoc />
        public bool Equals(Range other)
        {
            return other.Start.Equals(Start) && other.End.Equals(End);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Start.GetHashCode() * 31 + End.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Start + ".." + End;
        }

        /// <summary>Creates a Range object starting from start index to the end of the collection.</summary>
        public static Range StartAt(Index start)
        {
            return new Range(start, Index.End);
        }

        /// <summary>Creates a Range object starting from first element in the collection to the end Index.</summary>
        public static Range EndAt(Index end)
        {
            return new Range(Index.Start, end);
        }

        /// <summary>Creates a Range object starting from first element to the end.</summary>
        public static Range All => new(Index.Start, Index.End);

        /// <summary>Calculates the start offset and length of the range object using a collection length.</summary>
        public (int Offset, int Length) GetOffsetAndLength(int length)
        {
            var start = Start.GetOffset(length);
            var end = End.GetOffset(length);
            if ((uint)end > (uint)length || (uint)start > (uint)end)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return (start, end - start);
        }
    }

    /// <summary>Represents a type that can be used to index a collection either from the start or the end.</summary>
    internal readonly struct Index : IEquatable<Index>
    {
        private readonly int _value;

        /// <summary>Constructs an Index using a value and indicating if the index is from the start or from the end.</summary>
        public Index(int value, bool fromEnd = false)
        {
            _ = Guard.NotNegative(value);
            _value = fromEnd ? ~value : value;
        }

        private Index(int value)
        {
            _value = value;
        }

        /// <summary>Creates an Index pointing at first element.</summary>
        public static Index Start => new(0);

        /// <summary>Creates an Index pointing at beyond last element.</summary>
        public static Index End => new(~0);

        /// <summary>Creates an Index from the start at the position indicated by the value.</summary>
        public static Index FromStart(int value)
        {
            _ = Guard.NotNegative(value);
            return new Index(value);
        }

        /// <summary>Creates an Index from the end at the position indicated by the value.</summary>
        public static Index FromEnd(int value)
        {
            _ = Guard.NotNegative(value);
            return new Index(~value);
        }

        /// <summary>Gets the index value.</summary>
        private int Value => _value < 0 ? ~_value : _value;

        /// <summary>Indicates whether the index is from the start or the end.</summary>
        private bool IsFromEnd => _value < 0;

        /// <summary>Calculates the offset from the start using the given collection length.</summary>
        public int GetOffset(int length)
        {
            return IsFromEnd ? length - ~_value : _value;
        }

        /// <inheritdoc />
        public override bool Equals(object? value)
        {
            return value is Index index && _value == index._value;
        }

        /// <inheritdoc />
        public bool Equals(Index other)
        {
            return _value == other._value;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _value;
        }

        /// <summary>Converts integer number to an Index.</summary>
        public static implicit operator Index(int value)
        {
            return FromStart(value);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return IsFromEnd ? "^" + (uint)Value : ((uint)Value).ToString();
        }
    }
}
