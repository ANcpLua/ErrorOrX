// Test fixture for snapshot-content integrity. Legitimately needs Directory/File access to
// scan Snapshots/ — RS1035's "no IO in analyzers" rule doesn't apply to xunit test fixtures.
#pragma warning disable RS1035

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ANcpLua.Roslyn.Utilities;

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

    [Fact]
    public void EOE_Snapshots_Contain_Their_Own_Diagnostic_Id()
    {
        var snapshotsDir = Path.Combine(Path.GetDirectoryName(GetCallerFilePath())!, "Snapshots");
        Assert.True(Directory.Exists(snapshotsDir),
            $"Snapshots directory not found at '{snapshotsDir}'.");

        var failures = Directory.EnumerateFiles(snapshotsDir, "*.verified.txt")
            .Select(static path => (Filename: Path.GetFileName(path), Path: path))
            .Where(static f => !IsNegativeCase(f.Filename))
            .Select(static f => (f.Filename, Match: s_eoeIdInFilename.Match(f.Filename), f.Path))
            .Where(static f => f.Match.Success)
            .Select(static f => (f.Filename, PromisedId: $"EOE{f.Match.Groups["id"].Value}", Content: File.ReadAllText(f.Path)))
            .Where(static f => !f.Content.ContainsOrdinal($"Id: {f.PromisedId}"))
            .Select(static f => $"  {f.Filename} -> promises {f.PromisedId} but Diagnostics array does not contain it")
            .ToList();

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

    private static bool IsNegativeCase(string filename) =>
        s_negativeCaseMarkers.Any(marker => filename.ContainsOrdinal(marker));

    private static string GetCallerFilePath([CallerFilePath] string path = "") => path;
}
