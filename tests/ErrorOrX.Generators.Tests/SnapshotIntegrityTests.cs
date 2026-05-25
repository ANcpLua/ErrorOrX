// Test fixture for snapshot-content integrity. Legitimately needs Directory/File access to
// scan Snapshots/ — RS1035's "no IO in analyzers" rule doesn't apply to xunit test fixtures.
#pragma warning disable RS1035

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Guards against the silent-pass failure mode where a snapshot file named for a specific
///     EOE diagnostic verifies <c>Diagnostics: []</c> (or a different diagnostic) instead of
///     the one its filename promises.
/// </summary>
/// <remarks>
///     <para>
///         Rule: for every <c>*.verified.txt</c> whose filename matches <c>*.EOE\d{3}_*</c>,
///         the file content must contain <c>Id: EOE&lt;that-number&gt;</c> inside its
///         <c>Diagnostics:</c> array — OR the filename must contain one of the project's
///         negative-case markers (<c>_No_Diagnostic</c>, <c>_No_Diagnostics</c>, <c>_Is_Valid</c>,
///         <c>_Do_Not_Report</c>, <c>_Allowed</c>) to opt out as an intentional absence test.
///     </para>
///     <para>
///         Snapshots tracked under <see cref="s_pendingDecisions" /> are temporarily skipped
///         pending a design decision; that list should empty out as those decisions land.
///     </para>
/// </remarks>
public class SnapshotIntegrityTests
{
    private static readonly TimeSpan s_regexTimeout = TimeSpan.FromSeconds(1);

    private static readonly Regex s_eoeIdInFilename = new(
        @"\.EOE(?<id>\d{3})_",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        s_regexTimeout);

    private static readonly string[] s_negativeCaseMarkers =
    [
        "_No_Diagnostic",
        "_No_Diagnostics",
        "_Is_Valid",
        "_Do_Not_Report",
        "_Allowed"
    ];

    // Snapshots pending a design decision before they can either fire their named diagnostic
    // OR be renamed to the negative-case convention. Keep this list small and time-bounded —
    // every entry is a known-broken test silenced for review, not a long-term suppression.
    private static readonly HashSet<string> s_pendingDecisions = new(StringComparer.Ordinal)
    {
        // EOE039 (×2): analyzer-only diagnostic that the snapshot-test framework cannot exercise.
        // Test<TGenerator>.Run only invokes the source generator; the standalone
        // ErrorOrEndpointAnalyzer (which solely reports EOE039) is never wired in. The analyzer's
        // Pattern C fix at ErrorOrEndpointAnalyzer.BodyAndValidation.cs is correct and fires in
        // real consumer builds. Resolving these snapshots requires either dual-reporting EOE039
        // from the generator pipeline OR migrating these tests to AnalyzerTest<TAnalyzer>.
        "JsonAotValidationTests.EOE039_Validation_Attribute_On_Parameter.verified.txt",
        "JsonAotValidationTests.EOE039_Multiple_Validation_Attributes.verified.txt"
    };

    [Fact]
    public void EOE_Snapshots_Contain_Their_Own_Diagnostic_Id()
    {
        var snapshotsDir = Path.Combine(Path.GetDirectoryName(GetCallerFilePath())!, "Snapshots");
        Assert.True(Directory.Exists(snapshotsDir),
            $"Snapshots directory not found at '{snapshotsDir}'.");

        var failures = new List<string>();

        foreach (var path in Directory.EnumerateFiles(snapshotsDir, "*.verified.txt"))
        {
            var filename = Path.GetFileName(path);

            if (s_pendingDecisions.Contains(filename))
                continue;

            if (IsNegativeCase(filename))
                continue;

            var match = s_eoeIdInFilename.Match(filename);
            if (!match.Success)
                continue;

            var promisedId = $"EOE{match.Groups["id"].Value}";
            var content = File.ReadAllText(path);

            if (!content.Contains($"Id: {promisedId}", StringComparison.Ordinal))
                failures.Add($"  {filename} -> promises {promisedId} but Diagnostics array does not contain it");
        }

        if (failures.Count > 0)
        {
            var message =
                $"Snapshot integrity check failed: {failures.Count} snapshot(s) verify the wrong content.\n" +
                "Every *.verified.txt filename matching `*.EOE<NNN>_*` must contain `Id: EOE<NNN>` " +
                "in its Diagnostics array, or be renamed to match the project's negative-case convention " +
                "(`_No_Diagnostic`, `_No_Diagnostics`, `_Is_Valid`, `_Do_Not_Report`, `_Allowed`).\n\n" +
                "Failing snapshots:\n" +
                string.Join("\n", failures);

            Assert.Fail(message);
        }
    }

    private static bool IsNegativeCase(string filename)
    {
        foreach (var marker in s_negativeCaseMarkers)
        {
            if (filename.Contains(marker, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static string GetCallerFilePath([CallerFilePath] string path = "") => path;
}
