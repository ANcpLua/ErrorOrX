using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace ErrorOrX.Generators.Tests;

public sealed partial class DiagnosticsAlignmentTests
{
    [Fact]
    public void Diagnostics_AreAligned_With_Docs_And_AnalyzerReleases()
    {
        var repoRoot = RepoRootLocator.Find();

        var descriptors = LoadDescriptors();
        var descriptorIds = descriptors.Select(static d => d.Id).ToArray();
        descriptorIds.Should().OnlyHaveUniqueItems("descriptor IDs must be unique");

        var docsPath = Path.Combine(repoRoot, "docs", "diagnostics.md");
        var docs = DiagnosticsDocsParser.Parse(docsPath);
        docs.Keys.Should().OnlyHaveUniqueItems("docs must list each diagnostic once");
        docs.Keys.Should().BeEquivalentTo(descriptorIds, "docs must cover all diagnostics");

        foreach (var descriptor in descriptors)
        {
            docs.TryGetValue(descriptor.Id, out var doc).Should().BeTrue($"docs must include {descriptor.Id}");
            doc!.Title.Should().Be(descriptor.Title, $"{descriptor.Id} title must match descriptor");
            doc.Severity.Should().Be(descriptor.Severity, $"{descriptor.Id} severity must match descriptor");
        }

        var releaseData = AnalyzerReleasesParser.Parse(repoRoot);
        releaseData.RemovedRuleIds.Intersect(descriptorIds)
            .Should().BeEmpty("removed rules should not exist in descriptors");

        var releaseIds = releaseData.NewRules.Keys
            .Concat(releaseData.ChangedRules.Keys)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        releaseIds.Should().BeEquivalentTo(descriptorIds, "release tables must cover all diagnostics");

        foreach (var descriptor in descriptors)
        {
            var release = releaseData.GetRule(descriptor.Id);
            release.Should().NotBeNull($"release notes must include {descriptor.Id}");
            release.Category.Should().Be(descriptor.Category, $"{descriptor.Id} category must match descriptor");
            release.Severity.Should().Be(descriptor.Severity, $"{descriptor.Id} severity must match descriptor");
            if (release.TitleIsAuthoritative)
                release.Title.Should().Be(descriptor.Title, $"{descriptor.Id} title must match descriptor");
        }
    }

    private static List<DescriptorInfo> LoadDescriptors() =>
        typeof(Descriptors).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static f => f.FieldType == typeof(DiagnosticDescriptor))
            .Select(static f => (DiagnosticDescriptor)f.GetValue(null)!)
            .Select(static d => new DescriptorInfo(
                d.Id,
                d.Title.ToString(),
                d.DefaultSeverity,
                d.Category))
            .OrderBy(static d => d.Id, StringComparer.Ordinal)
            .ToList();

    private sealed record DescriptorInfo(
        string Id,
        string Title,
        DiagnosticSeverity Severity,
        string Category);

    private sealed record DocInfo(string Title, DiagnosticSeverity Severity);

    private sealed record ReleaseRule(string Category,
        DiagnosticSeverity Severity,
        string Title,
        bool TitleIsAuthoritative);

    private sealed record ReleaseData(
        Dictionary<string, ReleaseRule> NewRules,
        Dictionary<string, ReleaseRule> ChangedRules,
        HashSet<string> RemovedRuleIds)
    {
        public ReleaseRule? GetRule(string id) => ChangedRules.TryGetValue(id, out var changed) ? changed : NewRules.GetValueOrDefault(id);
    }

    private static partial class DiagnosticsDocsParser
    {
        private static readonly Regex HeadingRegex = MyRegex();

        private static readonly Regex SeverityRegex = MyRegex1();

        public static Dictionary<string, DocInfo> Parse(string path)
        {
            var content = File.ReadAllText(path);
            var matches = HeadingRegex.Matches(content);
            var result = new Dictionary<string, DocInfo>(StringComparer.Ordinal);

            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var id = match.Groups["id"].Value.Trim();
                var title = match.Groups["title"].Value.Trim();
                var start = match.Index;
                var end = i + 1 < matches.Count ? matches[i + 1].Index : content.Length;
                var block = content.Substring(start, end - start);

                var severityMatch = SeverityRegex.Match(block);
                if (!severityMatch.Success)
                    throw new InvalidOperationException($"Missing severity for {id} in {path}");

                var severity = SeverityParser.Parse(severityMatch.Groups["severity"].Value);
                result.Add(id, new DocInfo(title, severity));
            }

            return result;
        }

        [GeneratedRegex(@"^###\s+(?<id>[A-Z]+\d+)\s+-\s+(?<title>.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
        private static partial Regex MyRegex();

        [GeneratedRegex(@"^\*\*Severity:\*\*\s+(?<severity>[A-Za-z]+)\s*$", RegexOptions.Multiline | RegexOptions.Compiled)]
        private static partial Regex MyRegex1();
    }

    private static class AnalyzerReleasesParser
    {
        public static ReleaseData Parse(string repoRoot)
        {
            var shipped = ParseFile(Path.Combine(repoRoot, "src", "ErrorOrX.Generators", "AnalyzerReleases.Shipped.md"));
            var unshipped = ParseFile(Path.Combine(repoRoot, "src", "ErrorOrX.Generators", "AnalyzerReleases.Unshipped.md"));

            Merge(shipped, unshipped);
            return shipped;
        }

        private static ReleaseData ParseFile(string path)
        {
            var newRules = new Dictionary<string, ReleaseRule>(StringComparer.Ordinal);
            var changedRules = new Dictionary<string, ReleaseRule>(StringComparer.Ordinal);
            var removedRuleIds = new HashSet<string>(StringComparer.Ordinal);

            var section = ReleaseSection.None;
            var table = TableType.None;

            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    table = TableType.None;
                    continue;
                }

                if (line.StartsWith("### ", StringComparison.Ordinal))
                {
                    section = line switch
                    {
                        "### New Rules" => ReleaseSection.NewRules,
                        "### Removed Rules" => ReleaseSection.RemovedRules,
                        "### Changed Rules" => ReleaseSection.ChangedRules,
                        _ => ReleaseSection.None
                    };
                    table = TableType.None;
                    continue;
                }

                if (line.StartsWith("Rule ID | Category | Severity | Notes", StringComparison.Ordinal))
                {
                    table = TableType.Standard;
                    continue;
                }

                if (line.StartsWith("Rule ID | New Category | New Severity | Old Category | Old Severity | Notes",
                        StringComparison.Ordinal))
                {
                    table = TableType.Changed;
                    continue;
                }

                if (line.StartsWith("--------|", StringComparison.Ordinal) || table == TableType.None)
                    continue;

                if (!line.Contains('|', StringComparison.Ordinal))
                {
                    table = TableType.None;
                    continue;
                }

                if (table == TableType.Standard)
                {
                    var cells = SplitRow(line, 4, path);
                    var id = cells[0];
                    if (!RuleIdParser.IsMatch(id))
                        continue;

                    if (section == ReleaseSection.NewRules)
                    {
                        var category = cells[1];
                        var severity = SeverityParser.Parse(cells[2]);
                        var notes = cells[3];
                        AddUnique(newRules, id, new ReleaseRule(category, severity, notes, true), path);
                    }
                    else if (section == ReleaseSection.RemovedRules)
                    {
                        removedRuleIds.Add(id);
                    }

                    continue;
                }

                if (table == TableType.Changed)
                {
                    var cells = SplitRow(line, 6, path);
                    var id = cells[0];
                    if (!RuleIdParser.IsMatch(id))
                        continue;

                    if (section == ReleaseSection.ChangedRules)
                    {
                        var category = cells[1];
                        var severity = SeverityParser.Parse(cells[2]);
                        var notes = cells[5];
                        AddUnique(changedRules, id, new ReleaseRule(category, severity, notes, false), path);
                    }
                }
            }

            return new ReleaseData(newRules, changedRules, removedRuleIds);
        }

        private static void Merge(ReleaseData target, ReleaseData source)
        {
            foreach (var rule in source.NewRules)
                AddUnique(target.NewRules, rule.Key, rule.Value, "merged release data");

            foreach (var rule in source.ChangedRules)
                AddUnique(target.ChangedRules, rule.Key, rule.Value, "merged release data");

            target.RemovedRuleIds.UnionWith(source.RemovedRuleIds);
        }

        private static void AddUnique(Dictionary<string, ReleaseRule> target, string id, ReleaseRule rule, string path)
        {
            if (!target.TryAdd(id, rule))
                throw new InvalidOperationException($"Duplicate rule ID '{id}' in {path}");
        }

        private static string[] SplitRow(string line, int expectedColumns, string path)
        {
            var cells = line.Split('|')
                .Select(static cell => cell.Trim())
                .ToArray();

            if (cells.Length < expectedColumns)
                throw new InvalidOperationException($"Invalid table row in {path}: '{line}'");

            return cells;
        }

        private enum ReleaseSection
        {
            None,
            NewRules,
            RemovedRules,
            ChangedRules
        }

        private enum TableType
        {
            None,
            Standard,
            Changed
        }
    }

    private static class SeverityParser
    {
        public static DiagnosticSeverity Parse(string value)
        {
            if (value.Equals("Error", StringComparison.OrdinalIgnoreCase))
                return DiagnosticSeverity.Error;
            if (value.Equals("Warning", StringComparison.OrdinalIgnoreCase))
                return DiagnosticSeverity.Warning;
            if (value.Equals("Info", StringComparison.OrdinalIgnoreCase))
                return DiagnosticSeverity.Info;
            if (value.Equals("Hidden", StringComparison.OrdinalIgnoreCase))
                return DiagnosticSeverity.Hidden;
            return value.Equals("Suggestion", StringComparison.OrdinalIgnoreCase) ? DiagnosticSeverity.Info : throw new InvalidOperationException($"Unknown severity '{value}'");
        }
    }

    private static partial class RuleIdParser
    {
        private static readonly Regex RuleIdRegex = MyRegex();

        public static bool IsMatch(string value) => RuleIdRegex.IsMatch(value);

        [GeneratedRegex(@"^[A-Z]+\d+$", RegexOptions.Compiled)]
        private static partial Regex MyRegex();
    }

    private static class RepoRootLocator
    {
        public static string Find()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "ErrorOrX.slnx")))
                    return dir.FullName;
                dir = dir.Parent;
            }

            throw new InvalidOperationException("Repo root not found (expected ErrorOrX.slnx).");
        }
    }
}
