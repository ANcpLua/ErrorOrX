namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Coverage tests for the HTTP-verb attribute matrix — proves every supported verb
///     is wired end-to-end (runtime attribute emitted, parsed, provider registered, emit
///     produces a callable map call). HEAD/OPTIONS/TRACE share one shape because ASP.NET
///     Core has no dedicated MapHead/MapOptions/MapTrace and the generator routes them
///     through MapMethods(route, new[] { "VERB" }, ...).
///     <para>
///         GET/POST/PUT/DELETE/PATCH are covered transitively by the rest of the suite;
///         this file exists specifically to guard the three verbs that took a different
///         emit path and were missed in the original wire-up.
///     </para>
/// </summary>
public class HttpVerbCoverageTests : GeneratorTestBase
{
    [Fact]
    public Task Head_Attribute_Emits_MapMethods_With_HEAD()
    {
        const string Source = """
                              using ErrorOr;

                              public static class HealthApi
                              {
                                  [Head("/health")]
                                  public static ErrorOr<Success> Ping() => Result.Success;
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task Options_Attribute_Emits_MapMethods_With_OPTIONS()
    {
        const string Source = """
                              using ErrorOr;

                              public static class CorsApi
                              {
                                  [Options("/widgets")]
                                  public static ErrorOr<Success> Preflight() => Result.Success;
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task Trace_Attribute_Emits_MapMethods_With_TRACE()
    {
        const string Source = """
                              using ErrorOr;

                              public static class DiagnosticsApi
                              {
                                  [Trace("/echo")]
                                  public static ErrorOr<Success> Echo() => Result.Success;
                              }
                              """;

        return VerifyAsync(Source);
    }
}
