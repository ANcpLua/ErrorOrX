namespace ErrorOr.Analyzers;

/// <summary>
///     Diagnostic descriptors for ErrorOr.Endpoints. Split into cohesive partials by concern:
///     <list type="bullet">
///         <item><c>Descriptors.Handlers.cs</c> — handler method shape (EOE001, EOE002, EOE033).</item>
///         <item><c>Descriptors.Routing.cs</c> — route templates and constraints (EOE003, EOE004, EOE005, EOE020, EOE032).</item>
///         <item><c>Descriptors.HttpSemantics.cs</c> — body sources, read-only-method body rules (EOE006, EOE008, EOE009).</item>
///         <item>
///             <c>Descriptors.ParameterBinding.cs</c> — binding-attribute type validation and ambiguity (EOE010–EOE014,
///             EOE016, EOE017, EOE021).
///         </item>
///         <item>
///             <c>Descriptors.Types.cs</c> — type-system constraints on parameter and return types (EOE015, EOE018,
///             EOE019).
///         </item>
///         <item>
///             <c>Descriptors.Results.cs</c> — Results&lt;...&gt; union surface + error documentation (EOE022, EOE023,
///             EOE024).
///         </item>
///         <item>
///             <c>Descriptors.JsonSerialization.cs</c> — AOT serialization story (EOE007, EOE025, EOE026, EOE034,
///             EOE035, EOE036).
///         </item>
///         <item><c>Descriptors.ApiVersioning.cs</c> — API versioning attributes (EOE027–EOE031).</item>
///     </list>
/// </summary>
/// <remarks>
///     The active ID surface is contiguous <c>EOE001–EOE036</c>. Prior versions reserved
///     EOE034–EOE038 for AOT-hostile call site checks (Activator.CreateInstance, Type.GetType,
///     Expression.Compile, dynamic) which were removed in favour of ANcpLua.Analyzers'
///     AL0094/AL0095/AL0101/AL0102. v4.0.0 reclaims those IDs for the JSON-context diagnostics.
/// </remarks>
public static partial class Descriptors
{
    private const string Category = "ErrorOr.Endpoints";

    private const string TrimWarningsUrl =
        "https://learn.microsoft.com/dotnet/core/deploying/trimming/fixing-warnings";

    private const string AotRdgUrl =
        "https://learn.microsoft.com/aspnet/core/fundamentals/aot/request-delegate-generator/rdg";
}
