using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ErrorOrX.Tools.Analyzers;

/// <summary>
///     Validates diagnostics documentation and release notes against Descriptors.cs.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DiagnosticsAlignmentAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor DiagnosticsAlignmentMismatch = new(
        "EOE900",
        "Diagnostics alignment mismatch",
        "{0}",
        "ErrorOr.Internal",
        DiagnosticSeverity.Error,
        true,
        customTags:
        [
            WellKnownDiagnosticTags.CompilationEnd
        ]);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticsAlignmentMismatch];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeAlignment);
    }

    private static void AnalyzeAlignment(CompilationAnalysisContext context)
    {
        var docsFile = FindAdditionalFile(context.Options.AdditionalFiles, "diagnostics.md");
        var shippedFile = FindAdditionalFile(context.Options.AdditionalFiles, "AnalyzerReleases.Shipped.md");
        var unshippedFile = FindAdditionalFile(context.Options.AdditionalFiles, "AnalyzerReleases.Unshipped.md");
        var descriptorsFile = FindAdditionalFile(context.Options.AdditionalFiles, "Descriptors.cs");

        if (docsFile is null || shippedFile is null || unshippedFile is null || descriptorsFile is null)
            return;

        var issues = new List<string>();

        var descriptors = DescriptorParser.Parse(descriptorsFile, context.CancellationToken, issues);
        CompareDocs(descriptors, DiagnosticsDocsParser.Parse(docsFile, context.CancellationToken, issues), issues);
        CompareReleaseNotes(descriptors,
            AnalyzerReleasesParser.Parse(shippedFile, unshippedFile, context.CancellationToken, issues), issues);

        if (issues.Count is 0)
            return;

        foreach (var issue in issues)
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticsAlignmentMismatch,
                Location.None,
                issue));
    }

    private static AdditionalText? FindAdditionalFile(ImmutableArray<AdditionalText> files, string fileName)
    {
        foreach (var file in files)
            if (string.Equals(Path.GetFileName(file.Path), fileName, StringComparison.Ordinal))
                return file;
        return null;
    }

    private static void CompareDocs(
        IReadOnlyDictionary<string, DescriptorInfo> descriptors,
        IReadOnlyDictionary<string, DocInfo> docs,
        List<string> issues)
    {
        foreach (var descriptor in descriptors.Values)
        {
            if (!docs.TryGetValue(descriptor.Id, out var doc))
            {
                issues.Add($"Docs missing entry for {descriptor.Id}.");
                continue;
            }

            if (!string.Equals(doc.Title, descriptor.Title, StringComparison.Ordinal))
                issues.Add(
                    $"Docs title mismatch for {descriptor.Id}. Descriptor='{descriptor.Title}' Docs='{doc.Title}'.");

            if (doc.Severity != descriptor.Severity)
                issues.Add(
                    $"Docs severity mismatch for {descriptor.Id}. Descriptor='{descriptor.Severity}' Docs='{doc.Severity}'.");
        }

        foreach (var docId in docs.Keys)
            if (!descriptors.ContainsKey(docId))
                issues.Add($"Docs includes unknown diagnostic {docId}.");
    }

    private static void CompareReleaseNotes(
        IReadOnlyDictionary<string, DescriptorInfo> descriptors,
        ReleaseData releases,
        List<string> issues)
    {
        foreach (var descriptor in descriptors.Values)
        {
            var rule = releases.GetRule(descriptor.Id);
            if (rule is null)
            {
                issues.Add($"Release notes missing entry for {descriptor.Id}.");
                continue;
            }

            if (!string.Equals(rule.Category, descriptor.Category, StringComparison.Ordinal))
                issues.Add(
                    $"Release category mismatch for {descriptor.Id}. Descriptor='{descriptor.Category}' Release='{rule.Category}'.");

            if (rule.Severity != descriptor.Severity)
                issues.Add(
                    $"Release severity mismatch for {descriptor.Id}. Descriptor='{descriptor.Severity}' Release='{rule.Severity}'.");

            if (rule.TitleIsAuthoritative &&
                !string.Equals(rule.Title, descriptor.Title, StringComparison.Ordinal))
                issues.Add(
                    $"Release title mismatch for {descriptor.Id}. Descriptor='{descriptor.Title}' Release='{rule.Title}'.");
        }

        foreach (var ruleId in releases.AllRuleIds)
            if (!descriptors.ContainsKey(ruleId))
                issues.Add($"Release notes include unknown diagnostic {ruleId}.");

        foreach (var removed in releases.RemovedRuleIds)
            if (descriptors.ContainsKey(removed))
                issues.Add($"Release notes mark {removed} as removed but it still exists in descriptors.");
    }

    private sealed class DescriptorInfo
    {
        public DescriptorInfo(string id, string title, DiagnosticSeverity severity, string category)
        {
            Id = id;
            Title = title;
            Severity = severity;
            Category = category;
        }

        public string Id { get; }
        public string Title { get; }
        public DiagnosticSeverity Severity { get; }
        public string Category { get; }
    }

    private sealed class DocInfo
    {
        public DocInfo(string title, DiagnosticSeverity severity)
        {
            Title = title;
            Severity = severity;
        }

        public string Title { get; }
        public DiagnosticSeverity Severity { get; }
    }

    private sealed class ReleaseRule
    {
        public ReleaseRule(string category, DiagnosticSeverity severity, string title, bool titleIsAuthoritative)
        {
            Category = category;
            Severity = severity;
            Title = title;
            TitleIsAuthoritative = titleIsAuthoritative;
        }

        public string Category { get; }
        public DiagnosticSeverity Severity { get; }
        public string Title { get; }
        public bool TitleIsAuthoritative { get; }
    }

    private sealed class ReleaseData
    {
        public ReleaseData(
            Dictionary<string, ReleaseRule> newRules,
            Dictionary<string, ReleaseRule> changedRules,
            HashSet<string> removedRuleIds)
        {
            NewRules = newRules;
            ChangedRules = changedRules;
            RemovedRuleIds = removedRuleIds;
        }

        public Dictionary<string, ReleaseRule> NewRules { get; }
        public Dictionary<string, ReleaseRule> ChangedRules { get; }
        public HashSet<string> RemovedRuleIds { get; }

        public IEnumerable<string> AllRuleIds => NewRules.Keys.Concat(ChangedRules.Keys);

        public ReleaseRule? GetRule(string id)
        {
            if (ChangedRules.TryGetValue(id, out var changed))
                return changed;
            return NewRules.TryGetValue(id, out var added) ? added : null;
        }
    }

    private static class DescriptorParser
    {
        public static IReadOnlyDictionary<string, DescriptorInfo> Parse(
            AdditionalText file,
            CancellationToken ct,
            List<string> issues)
        {
            var text = file.GetText(ct);
            if (text is null)
            {
                issues.Add($"Descriptors file unreadable: {file.Path}");
                return new Dictionary<string, DescriptorInfo>(StringComparer.Ordinal);
            }

            var tree = CSharpSyntaxTree.ParseText(text, path: file.Path);
            var root = tree.GetRoot(ct);
            var constants = ExtractStringConstants(root);

            var result = new Dictionary<string, DescriptorInfo>(StringComparer.Ordinal);
            foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
            {
                if (!IsDiagnosticDescriptorType(field.Declaration.Type))
                    continue;

                foreach (var variable in field.Declaration.Variables)
                {
                    var argumentList = GetArgumentList(variable.Initializer?.Value);
                    if (argumentList is null)
                        continue;

                    if (argumentList.Arguments.Count < 5)
                    {
                        issues.Add("Descriptor declaration missing required arguments.");
                        continue;
                    }

                    var args = argumentList.Arguments;
                    var id = GetStringLiteral(args[0].Expression);
                    var title = GetStringLiteral(args[1].Expression);
                    var category = ResolveCategory(args[3].Expression, constants);
                    var severity = ResolveSeverity(args[4].Expression);

                    if (id is null || title is null || category is null || severity is null)
                    {
                        issues.Add($"Descriptor declaration could not be parsed in {file.Path}.");
                        continue;
                    }

                    if (result.ContainsKey(id))
                    {
                        issues.Add($"Descriptors.cs has duplicate entry for {id}.");
                        continue;
                    }

                    result.Add(id, new DescriptorInfo(id, title, severity.Value, category));
                }
            }

            return result;
        }

        private static Dictionary<string, string> ExtractStringConstants(SyntaxNode root)
        {
            var constants = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
            {
                if (!field.Modifiers.Any(SyntaxKind.ConstKeyword))
                    continue;

                if (!IsStringType(field.Declaration.Type))
                    continue;

                foreach (var variable in field.Declaration.Variables)
                    if (variable.Initializer?.Value is LiteralExpressionSyntax literal &&
                        literal.IsKind(SyntaxKind.StringLiteralExpression) &&
                        variable.Identifier.ValueText is { Length: > 0 } name)
                        constants[name] = literal.Token.ValueText;
            }

            return constants;
        }

        private static bool IsStringType(TypeSyntax type)
        {
            return type is PredefinedTypeSyntax { Keyword.ValueText: "string" } ||
                   string.Equals(type.ToString(), "string", StringComparison.Ordinal);
        }

        private static bool IsDiagnosticDescriptorType(TypeSyntax type)
        {
            return type is IdentifierNameSyntax { Identifier.ValueText: "DiagnosticDescriptor" } or QualifiedNameSyntax
            {
                Right.Identifier.ValueText: "DiagnosticDescriptor"
            };
        }

        private static ArgumentListSyntax? GetArgumentList(ExpressionSyntax? expression)
        {
            if (expression is ObjectCreationExpressionSyntax creation)
                return creation.ArgumentList;

            if (expression is ImplicitObjectCreationExpressionSyntax implicitCreation)
                return implicitCreation.ArgumentList;

            return null;
        }

        private static string? GetStringLiteral(ExpressionSyntax expression)
        {
            if (expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
                return literal.Token.ValueText;

            return null;
        }

        private static string? ResolveCategory(ExpressionSyntax expression, Dictionary<string, string> constants)
        {
            if (expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
                return literal.Token.ValueText;

            if (expression is IdentifierNameSyntax identifier &&
                constants.TryGetValue(identifier.Identifier.ValueText, out var value))
                return value;

            if (expression is MemberAccessExpressionSyntax member &&
                constants.TryGetValue(member.Name.Identifier.ValueText, out var memberValue))
                return memberValue;

            return null;
        }

        private static DiagnosticSeverity? ResolveSeverity(ExpressionSyntax expression)
        {
            string? name = null;
            if (expression is MemberAccessExpressionSyntax member)
                name = member.Name.Identifier.ValueText;
            else if (expression is IdentifierNameSyntax identifier)
                name = identifier.Identifier.ValueText;

            if (name is null)
                return null;

            if (string.Equals(name, "Error", StringComparison.Ordinal))
                return DiagnosticSeverity.Error;
            if (string.Equals(name, "Warning", StringComparison.Ordinal))
                return DiagnosticSeverity.Warning;
            if (string.Equals(name, "Info", StringComparison.Ordinal))
                return DiagnosticSeverity.Info;
            if (string.Equals(name, "Hidden", StringComparison.Ordinal))
                return DiagnosticSeverity.Hidden;

            return null;
        }
    }

    private static class DiagnosticsDocsParser
    {
        private static readonly Regex HeadingRegex = new(
            @"^###\s+(?<id>[A-Z]+\d+)\s+-\s+(?<title>.+)$",
            RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex SeverityRegex = new(
            @"^\*\*Severity:\*\*\s+(?<severity>[A-Za-z]+)\s*$",
            RegexOptions.Multiline | RegexOptions.Compiled);

        public static IReadOnlyDictionary<string, DocInfo> Parse(
            AdditionalText file,
            CancellationToken ct,
            List<string> issues)
        {
            var text = file.GetText(ct);
            if (text is null)
            {
                issues.Add($"Docs file unreadable: {file.Path}");
                return new Dictionary<string, DocInfo>(StringComparer.Ordinal);
            }

            var content = text.ToString();
            var matches = HeadingRegex.Matches(content);
            var result = new Dictionary<string, DocInfo>(StringComparer.Ordinal);

            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var id = match.Groups["id"].Value.Trim();
                var title = match.Groups["title"].Value.Trim();
                if (!RuleIdParser.IsMatch(id))
                    continue;

                var start = match.Index;
                var end = i + 1 < matches.Count ? matches[i + 1].Index : content.Length;
                var block = content.Substring(start, end - start);
                var severityMatch = SeverityRegex.Match(block);
                if (!severityMatch.Success)
                {
                    issues.Add($"Docs missing severity for {id}.");
                    continue;
                }

                if (!SeverityParser.TryParse(severityMatch.Groups["severity"].Value, out var severity))
                {
                    issues.Add($"Docs has invalid severity for {id}.");
                    continue;
                }

                if (result.ContainsKey(id))
                {
                    issues.Add($"Docs has duplicate entry for {id}.");
                    continue;
                }

                result.Add(id, new DocInfo(title, severity));
            }

            return result;
        }
    }

    private static class AnalyzerReleasesParser
    {
        public static ReleaseData Parse(
            AdditionalText shipped,
            AdditionalText unshipped,
            CancellationToken ct,
            List<string> issues)
        {
            var shippedData = ParseFile(shipped, ct, issues);
            var unshippedData = ParseFile(unshipped, ct, issues);

            Merge(shippedData, unshippedData, issues);
            return shippedData;
        }

        private static ReleaseData ParseFile(AdditionalText file, CancellationToken ct, List<string> issues)
        {
            var newRules = new Dictionary<string, ReleaseRule>(StringComparer.Ordinal);
            var changedRules = new Dictionary<string, ReleaseRule>(StringComparer.Ordinal);
            var removedRuleIds = new HashSet<string>(StringComparer.Ordinal);

            var text = file.GetText(ct);
            if (text is null)
            {
                issues.Add($"Release file unreadable: {file.Path}");
                return new ReleaseData(newRules, changedRules, removedRuleIds);
            }

            var section = ReleaseSection.None;
            var table = TableType.None;

            foreach (var rawLine in text.Lines)
            {
                var line = rawLine.ToString().Trim();
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

                if (line.IndexOf('|') < 0)
                {
                    table = TableType.None;
                    continue;
                }

                if (table == TableType.Standard)
                {
                    var cells = SplitRow(line, 4, file.Path, issues);
                    if (cells.Length is 0)
                        continue;

                    var id = cells[0];
                    if (!RuleIdParser.IsMatch(id))
                        continue;

                    if (section == ReleaseSection.NewRules)
                    {
                        var category = cells[1];
                        if (!SeverityParser.TryParse(cells[2], out var severity))
                        {
                            issues.Add($"Release notes invalid severity for {id} in {file.Path}.");
                            continue;
                        }

                        var notes = cells[3];
                        AddUnique(newRules, id,
                            new ReleaseRule(category, severity, notes, true),
                            file.Path,
                            issues);
                    }
                    else if (section == ReleaseSection.RemovedRules)
                    {
                        removedRuleIds.Add(id);
                    }

                    continue;
                }

                if (table == TableType.Changed)
                {
                    var cells = SplitRow(line, 6, file.Path, issues);
                    if (cells.Length is 0)
                        continue;

                    var id = cells[0];
                    if (!RuleIdParser.IsMatch(id))
                        continue;

                    if (section == ReleaseSection.ChangedRules)
                    {
                        var category = cells[1];
                        if (!SeverityParser.TryParse(cells[2], out var severity))
                        {
                            issues.Add($"Release notes invalid severity for {id} in {file.Path}.");
                            continue;
                        }

                        var notes = cells[5];
                        AddUnique(changedRules, id,
                            new ReleaseRule(category, severity, notes, false),
                            file.Path,
                            issues);
                    }
                }
            }

            return new ReleaseData(newRules, changedRules, removedRuleIds);
        }

        private static void Merge(ReleaseData target, ReleaseData source, List<string> issues)
        {
            foreach (var rule in source.NewRules)
                AddUnique(target.NewRules, rule.Key, rule.Value, "merged release data", issues);

            foreach (var rule in source.ChangedRules)
                AddUnique(target.ChangedRules, rule.Key, rule.Value, "merged release data", issues);

            target.RemovedRuleIds.UnionWith(source.RemovedRuleIds);
        }

        private static void AddUnique(
            Dictionary<string, ReleaseRule> target,
            string id,
            ReleaseRule rule,
            string source,
            List<string> issues)
        {
            if (target.ContainsKey(id))
            {
                issues.Add($"Release notes duplicate entry for {id} in {source}.");
                return;
            }

            target.Add(id, rule);
        }

        private static string[] SplitRow(string line, int expectedColumns, string path, List<string> issues)
        {
            var cells = line.Split('|')
                .Select(static cell => cell.Trim())
                .ToArray();

            if (cells.Length < expectedColumns)
            {
                issues.Add($"Release notes invalid table row in {path}: '{line}'.");
                return [];
            }

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
        public static bool TryParse(string value, out DiagnosticSeverity severity)
        {
            if (value.Equals("Error", StringComparison.OrdinalIgnoreCase))
            {
                severity = DiagnosticSeverity.Error;
                return true;
            }

            if (value.Equals("Warning", StringComparison.OrdinalIgnoreCase))
            {
                severity = DiagnosticSeverity.Warning;
                return true;
            }

            if (value.Equals("Info", StringComparison.OrdinalIgnoreCase))
            {
                severity = DiagnosticSeverity.Info;
                return true;
            }

            if (value.Equals("Hidden", StringComparison.OrdinalIgnoreCase))
            {
                severity = DiagnosticSeverity.Hidden;
                return true;
            }

            if (value.Equals("Suggestion", StringComparison.OrdinalIgnoreCase))
            {
                severity = DiagnosticSeverity.Info;
                return true;
            }

            severity = DiagnosticSeverity.Hidden;
            return false;
        }
    }

    private static class RuleIdParser
    {
        private static readonly Regex RuleIdRegex = new(@"^[A-Z]+\d+$", RegexOptions.Compiled);

        public static bool IsMatch(string value)
        {
            return RuleIdRegex.IsMatch(value);
        }
    }
}