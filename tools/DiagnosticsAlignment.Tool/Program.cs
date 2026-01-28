using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static partial class Program
{
    private static readonly Regex DocHeadingRegex = MyRegex1();
    private static readonly Regex DocSeverityRegex = MyRegex();

    public static int Main(string[] args)
    {
        var root = ResolveRepoRoot(args);
        var docsPath = Path.Combine(root, "docs", "diagnostics.md");
        var descriptorsPath = Path.Combine(root, "src", "ErrorOrX.Generators", "Analyzers", "Descriptors.cs");
        var shippedPath = Path.Combine(root, "src", "ErrorOrX.Generators", "AnalyzerReleases.Shipped.md");
        var unshippedPath = Path.Combine(root, "src", "ErrorOrX.Generators", "AnalyzerReleases.Unshipped.md");

        var errors = new List<string>();

        var descriptors = LoadDescriptors(descriptorsPath, errors);
        var docs = LoadDocs(docsPath, errors);
        var releases = LoadReleaseData(shippedPath, unshippedPath, errors);

        ValidateDocs(descriptors, docs, errors);
        ValidateReleases(descriptors, releases, errors);

        if (errors.Count > 0)
        {
            Console.Error.WriteLine($"Diagnostics alignment failed ({errors.Count} issues):");
            foreach (var error in errors)
                Console.Error.WriteLine($"- {error}");

            return 1;
        }

        Console.WriteLine($"Diagnostics alignment OK ({descriptors.Count} rules).");
        return 0;
    }

    private static string ResolveRepoRoot(string[] args)
    {
        string? rootArg = null;
        for (var i = 0; i < args.Length; i++)
            if (string.Equals(args[i], "--root", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                rootArg = args[i + 1];
                break;
            }

        if (!string.IsNullOrWhiteSpace(rootArg))
            return Path.GetFullPath(rootArg);

        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ErrorOrX.slnx")))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root. Run from repo or pass --root <path>.");
    }

    private static Dictionary<string, DescriptorInfo> LoadDescriptors(string path, List<string> errors)
    {
        var descriptors = new Dictionary<string, DescriptorInfo>(StringComparer.Ordinal);
        if (!File.Exists(path))
        {
            errors.Add($"Missing descriptors file: {path}");
            return descriptors;
        }

        var text = File.ReadAllText(path);
        var tree = CSharpSyntaxTree.ParseText(text);
        var root = tree.GetRoot();

        var constants = ReadStringConstants(root);

        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            if (!IsDiagnosticDescriptorField(field))
                continue;

            var variable = field.Declaration.Variables.FirstOrDefault();
            if (variable?.Initializer?.Value is null)
                continue;

            if (!TryParseDescriptor(variable.Initializer.Value, constants, out var info, out var error))
            {
                if (!string.IsNullOrWhiteSpace(error))
                    errors.Add(error);

                continue;
            }

            if (!descriptors.TryAdd(info.Id, info))
                errors.Add($"Duplicate descriptor ID in Descriptors.cs: {info.Id}");
        }

        return descriptors;
    }

    private static Dictionary<string, DocInfo> LoadDocs(string path, List<string> errors)
    {
        var docs = new Dictionary<string, DocInfo>(StringComparer.Ordinal);
        if (!File.Exists(path))
        {
            errors.Add($"Missing docs file: {path}");
            return docs;
        }

        var lines = File.ReadAllLines(path);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var match = DocHeadingRegex.Match(line);
            if (!match.Success)
                continue;

            var id = match.Groups["id"].Value.Trim();
            var title = match.Groups["title"].Value.Trim();
            string? severity = null;

            for (var j = i + 1; j < lines.Length; j++)
            {
                var next = lines[j].Trim();
                if (DocHeadingRegex.IsMatch(next))
                    break;

                var severityMatch = DocSeverityRegex.Match(next);
                if (severityMatch.Success)
                {
                    severity = NormalizeSeverity(severityMatch.Groups["severity"].Value);
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(severity))
            {
                errors.Add($"Docs entry {id} is missing a Severity line.");
                continue;
            }

            if (docs.ContainsKey(id))
            {
                errors.Add($"Duplicate docs entry in diagnostics.md: {id}");
                continue;
            }

            docs[id] = new DocInfo(id, title, severity);
        }

        return docs;
    }

    private static ReleaseData LoadReleaseData(string shippedPath, string unshippedPath, List<string> errors)
    {
        var active = new Dictionary<string, ReleaseInfo>(StringComparer.Ordinal);
        var removed = new HashSet<string>(StringComparer.Ordinal);

        ParseReleaseFile(shippedPath, "shipped", active, removed, errors);
        ParseReleaseFile(unshippedPath, "unshipped", active, removed, errors);

        return new ReleaseData(active, removed);
    }

    private static void ParseReleaseFile(
        string path,
        string source,
        Dictionary<string, ReleaseInfo> active,
        HashSet<string> removed,
        List<string> errors)
    {
        if (!File.Exists(path))
        {
            errors.Add($"Missing release file: {path}");
            return;
        }

        var lines = File.ReadAllLines(path);
        var section = ReleaseSection.None;
        var inTable = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                section = line switch
                {
                    "### New Rules" => ReleaseSection.New,
                    "### Removed Rules" => ReleaseSection.Removed,
                    "### Changed Rules" => ReleaseSection.Changed,
                    _ => ReleaseSection.None
                };
                inTable = false;
                continue;
            }

            if (line.StartsWith("Rule ID", StringComparison.Ordinal))
            {
                inTable = true;
                continue;
            }

            if (!inTable || string.IsNullOrWhiteSpace(line) || line.StartsWith("-", StringComparison.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(line))
                    inTable = false;

                continue;
            }

            if (!line.Contains('|'))
                continue;

            var parts = line.Split('|', StringSplitOptions.TrimEntries);
            switch (section)
            {
                case ReleaseSection.Changed when parts.Length < 6:
                    errors.Add($"Invalid changed rule row in {path}: {line}");
                    continue;
                case ReleaseSection.Changed:
                {
                    var id = parts[0];
                    var newCategory = parts[1];
                    var newSeverity = NormalizeSeverity(parts[2]);
                    var notes = parts[5];
                    AddReleaseEntry(active, errors, id, newCategory, newSeverity, notes, $"{source}-changed");
                    break;
                }
                case ReleaseSection.New or ReleaseSection.Removed when parts.Length < 4:
                    errors.Add($"Invalid rule row in {path}: {line}");
                    continue;
                case ReleaseSection.New or ReleaseSection.Removed:
                {
                    var id = parts[0];
                    var category = parts[1];
                    var severity = NormalizeSeverity(parts[2]);
                    var notes = parts[3];

                    if (section == ReleaseSection.Removed)
                    {
                        removed.Add(id);
                        continue;
                    }

                    AddReleaseEntry(active, errors, id, category, severity, notes, source);
                    break;
                }
            }
        }
    }

    private static void AddReleaseEntry(
        Dictionary<string, ReleaseInfo> active,
        List<string> errors,
        string id,
        string category,
        string severity,
        string notes,
        string source)
    {
        if (active.TryGetValue(id, out var existing))
        {
            errors.Add($"Duplicate release entry for {id} ({existing.Source} vs {source}).");
            return;
        }

        active[id] = new ReleaseInfo(id, category, severity, notes, source);
    }

    private static void ValidateDocs(
        Dictionary<string, DescriptorInfo> descriptors,
        Dictionary<string, DocInfo> docs,
        List<string> errors)
    {
        foreach (var descriptor in descriptors.Values)
        {
            if (!docs.TryGetValue(descriptor.Id, out var doc))
            {
                errors.Add($"Docs missing {descriptor.Id} ({descriptor.Title}).");
                continue;
            }

            if (!string.Equals(descriptor.Title, doc.Title, StringComparison.Ordinal))
                errors.Add($"Docs title mismatch for {descriptor.Id}: '{doc.Title}' vs '{descriptor.Title}'.");

            if (!string.Equals(descriptor.Severity, doc.Severity, StringComparison.Ordinal))
                errors.Add($"Docs severity mismatch for {descriptor.Id}: '{doc.Severity}' vs '{descriptor.Severity}'.");
        }

        foreach (var doc in docs.Values)
            if (!descriptors.ContainsKey(doc.Id))
                errors.Add($"Docs include {doc.Id} but no descriptor exists.");
    }

    private static void ValidateReleases(
        Dictionary<string, DescriptorInfo> descriptors,
        ReleaseData releases,
        List<string> errors)
    {
        foreach (var descriptor in descriptors.Values)
        {
            if (!releases.Active.TryGetValue(descriptor.Id, out var release))
            {
                errors.Add($"Release tracking missing {descriptor.Id}.");
                continue;
            }

            if (!string.Equals(descriptor.Category, release.Category, StringComparison.Ordinal))
                errors.Add(
                    $"Release category mismatch for {descriptor.Id}: '{release.Category}' vs '{descriptor.Category}'.");

            if (!string.Equals(descriptor.Severity, release.Severity, StringComparison.Ordinal))
                errors.Add(
                    $"Release severity mismatch for {descriptor.Id}: '{release.Severity}' vs '{descriptor.Severity}'.");

            if (!string.Equals(descriptor.Title, release.Notes, StringComparison.Ordinal))
                errors.Add($"Release notes mismatch for {descriptor.Id}: '{release.Notes}' vs '{descriptor.Title}'.");
        }

        foreach (var release in releases.Active.Values)
            if (!descriptors.ContainsKey(release.Id))
                errors.Add($"Release tracking includes {release.Id} but no descriptor exists.");

        foreach (var removed in releases.Removed)
            if (descriptors.ContainsKey(removed))
                errors.Add($"Release tracking lists {removed} as removed, but descriptor still exists.");
    }

    private static Dictionary<string, string> ReadStringConstants(SyntaxNode root)
    {
        var constants = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            if (!field.Modifiers.Any(SyntaxKind.ConstKeyword))
                continue;

            if (field.Declaration.Type is not PredefinedTypeSyntax predefined ||
                !predefined.Keyword.IsKind(SyntaxKind.StringKeyword))
                continue;

            foreach (var variable in field.Declaration.Variables)
                if (variable.Initializer?.Value is LiteralExpressionSyntax literal &&
                    literal.IsKind(SyntaxKind.StringLiteralExpression))
                    constants[variable.Identifier.Text] = literal.Token.ValueText;
        }

        return constants;
    }

    private static bool IsDiagnosticDescriptorField(FieldDeclarationSyntax field)
    {
        switch (field.Declaration.Type)
        {
            case IdentifierNameSyntax identifier when
                string.Equals(identifier.Identifier.Text, "DiagnosticDescriptor", StringComparison.Ordinal):
            case QualifiedNameSyntax qualified when
                string.Equals(qualified.Right.Identifier.Text, "DiagnosticDescriptor", StringComparison.Ordinal):
                return true;
            default:
                return false;
        }
    }

    private static bool TryParseDescriptor(
        ExpressionSyntax expression,
        Dictionary<string, string> constants,
        [NotNullWhen(true)] out DescriptorInfo? info,
        out string? error)
    {
        info = null;
        error = null;

        var arguments = expression switch
        {
            ObjectCreationExpressionSyntax creation => creation.ArgumentList,
            ImplicitObjectCreationExpressionSyntax implicitCreation => implicitCreation.ArgumentList,
            _ => null
        };

        if (arguments is null)
            return false;

        var args = arguments.Arguments;
        var id = ReadStringArgument(args, 0, "id", constants);
        var title = ReadStringArgument(args, 1, "title", constants);
        var category = ReadStringArgument(args, 3, "category", constants);
        var severity = ReadSeverityArgument(args, 4, "defaultSeverity");

        if (id is null || title is null || category is null || severity is null)
        {
            error = "Unable to parse DiagnosticDescriptor in Descriptors.cs.";
            return false;
        }

        info = new DescriptorInfo(id, title, category, severity);
        return true;
    }

    private static string? ReadStringArgument(
        SeparatedSyntaxList<ArgumentSyntax> args,
        int index,
        string name,
        Dictionary<string, string> constants)
    {
        var expression = GetArgumentExpression(args, index, name);
        if (expression is null)
            return null;

        return expression switch
        {
            LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) => literal.Token
                .ValueText,
            IdentifierNameSyntax identifier when constants.TryGetValue(identifier.Identifier.Text, out var value) =>
                value,
            _ => null
        };
    }

    private static string? ReadSeverityArgument(SeparatedSyntaxList<ArgumentSyntax> args, int index, string name)
    {
        var expression = GetArgumentExpression(args, index, name);
        return expression switch
        {
            null => null,
            MemberAccessExpressionSyntax
            {
                Name: IdentifierNameSyntax memberName
            } => NormalizeSeverity(memberName.Identifier.Text),
            IdentifierNameSyntax identifier => NormalizeSeverity(identifier.Identifier.Text),
            _ => null
        };
    }

    private static ExpressionSyntax? GetArgumentExpression(SeparatedSyntaxList<ArgumentSyntax> args, int index,
        string name)
    {
        foreach (var arg in args)
            if (arg.NameColon?.Name.Identifier.Text == name)
                return arg.Expression;

        return args.Count > index ? args[index].Expression : null;
    }

    private static string NormalizeSeverity(string value)
    {
        return value.Trim() switch
        {
            "Error" => "Error",
            "Warning" => "Warning",
            "Info" => "Info",
            "Hidden" => "Hidden",
            "Suggestion" => "Info",
            _ => value.Trim()
        };
    }

    [GeneratedRegex(@"^\*\*Severity:\*\*\s+(?<severity>\w+)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex MyRegex();

    [GeneratedRegex(@"^###\s+(?<id>[A-Z]{3}\d{3})\s+-\s+(?<title>.+)$", RegexOptions.Compiled)]
    private static partial Regex MyRegex1();

    private sealed record DescriptorInfo(string Id, string Title, string Category, string Severity);

    private sealed record DocInfo(string Id, string Title, string Severity);

    private sealed record ReleaseInfo(string Id, string Category, string Severity, string Notes, string Source);

    private sealed record ReleaseData(Dictionary<string, ReleaseInfo> Active, HashSet<string> Removed);

    private enum ReleaseSection
    {
        None,
        New,
        Removed,
        Changed
    }
}