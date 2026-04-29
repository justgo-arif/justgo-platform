using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace JustGo.ArchitectureTests;

public class ProjectDependencyRulesTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string ModulesRoot = Path.Combine(RepositoryRoot, "src", "Modules");

    [Fact]
    public void Application_projects_should_not_reference_sibling_application_projects()
    {
        var violations = GetModuleProjects("*.Application.csproj")
            .SelectMany(project => project.ProjectReferences
                .Where(reference => reference.FileName.EndsWith(".Application.csproj", StringComparison.OrdinalIgnoreCase))
                .Where(reference => !reference.ModuleName.Equals(project.ModuleName, StringComparison.OrdinalIgnoreCase))
                .Select(reference => $"{project.Name} -> {reference.Name}"))
            .OrderBy(violation => violation)
            .ToList();

        Assert.True(violations.Count == 0, BuildFailureMessage(
            "Module Application projects must not reference sibling Application projects directly.",
            violations));
    }

    [Fact]
    public void Domain_projects_should_not_reference_sibling_module_projects()
    {
        var violations = GetModuleProjects("*.Domain.csproj")
            .SelectMany(project => project.ProjectReferences
                .Where(reference => reference.IsModuleProject)
                .Where(reference => !reference.ModuleName.Equals(project.ModuleName, StringComparison.OrdinalIgnoreCase))
                .Select(reference => $"{project.Name} -> {reference.Name}"))
            .OrderBy(violation => violation)
            .ToList();

        Assert.True(violations.Count == 0, BuildFailureMessage(
            "Module Domain projects must not reference sibling module projects.",
            violations));
    }

    [Fact]
    public void Domain_projects_should_not_reference_shared_project()
    {
        var violations = GetModuleProjects("*.Domain.csproj")
            .SelectMany(project => project.ProjectReferences
                .Where(reference => reference.FileName.Equals("JustGoAPI.Shared.csproj", StringComparison.OrdinalIgnoreCase))
                .Select(reference => $"{project.Name} -> {reference.Name}"))
            .OrderBy(violation => violation)
            .ToList();

        Assert.True(violations.Count == 0, BuildFailureMessage(
            "Module Domain projects must not reference JustGoAPI.Shared.",
            violations));
    }

    [Fact]
    public void Controller_success_responses_should_use_typed_api_response_data()
    {
        var violations = Directory
            .EnumerateFiles(ModulesRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains(Path.DirectorySeparatorChar + "Controllers" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .SelectMany(GetUntypedSuccessApiResponseUsages)
            .OrderBy(violation => violation)
            .ToList();

        Assert.True(violations.Count == 0, BuildFailureMessage(
            "Controller success responses must use concrete ApiResponse<TData, TPermissions> data types instead of ApiResponse<object, object>.",
            violations));
    }

    [Fact]
    public void Controller_actions_should_declare_openapi_response_types()
    {
        var violations = Directory
            .EnumerateFiles(ModulesRoot, "*Controller.cs", SearchOption.AllDirectories)
            .SelectMany(GetActionsMissingProducesResponseType)
            .OrderBy(violation => violation)
            .ToList();

        Assert.True(violations.Count == 0, BuildFailureMessage(
            "Controller actions must declare [ProducesResponseType] so OpenAPI documents stable response schemas.",
            violations));
    }

    [Fact]
    public void Controller_success_responses_should_not_return_anonymous_objects()
    {
        var violations = Directory
            .EnumerateFiles(ModulesRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains(Path.DirectorySeparatorChar + "Controllers" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .SelectMany(GetAnonymousSuccessResponseUsages)
            .OrderBy(violation => violation)
            .ToList();

        Assert.True(violations.Count == 0, BuildFailureMessage(
            "Controller success responses must return named DTOs instead of anonymous objects so SDK generators can create stable models.",
            violations));
    }

    [Fact]
    public void Controller_route_parameters_should_use_explicit_constraints()
    {
        var violations = Directory
            .EnumerateFiles(ModulesRoot, "*Controller.cs", SearchOption.AllDirectories)
            .SelectMany(GetUnconstrainedRouteParameters)
            .OrderBy(violation => violation)
            .ToList();

        Assert.True(violations.Count == 0, BuildFailureMessage(
            "Controller route parameters must use explicit constraints, for example {id:guid:required}, so OpenAPI describes routes precisely.",
            violations));
    }

    private static IReadOnlyList<ProjectDefinition> GetModuleProjects(string searchPattern)
    {
        return Directory
            .EnumerateFiles(ModulesRoot, searchPattern, SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "tests" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .Select(ProjectDefinition.Load)
            .ToList();
    }

    private static string BuildFailureMessage(string header, IReadOnlyCollection<string> violations)
    {
        if (violations.Count == 0)
        {
            return header;
        }

        return header + Environment.NewLine + string.Join(Environment.NewLine, violations);
    }

    private static IEnumerable<string> GetUntypedSuccessApiResponseUsages(string filePath)
    {
        return File.ReadLines(filePath)
            .Select((line, index) => new { Line = line, LineNumber = index + 1 })
            .Where(sourceLine => IsUntypedSuccessApiResponseUsage(sourceLine.Line))
            .Select(sourceLine => $"{Path.GetRelativePath(RepositoryRoot, filePath)}:{sourceLine.LineNumber}");
    }

    private static bool IsUntypedSuccessApiResponseUsage(string line)
    {
        var normalizedLine = line.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\t", string.Empty, StringComparison.Ordinal);

        return normalizedLine.Contains("Ok(newApiResponse<object,object>", StringComparison.Ordinal)
            || normalizedLine.Contains("Created(newApiResponse<object,object>", StringComparison.Ordinal)
            || normalizedLine.Contains("Accepted(newApiResponse<object,object>", StringComparison.Ordinal);
    }

    private static IEnumerable<string> GetActionsMissingProducesResponseType(string filePath)
    {
        var lines = File.ReadAllLines(filePath);

        for (var index = 0; index < lines.Length; index++)
        {
            if (!IsPublicActionMethod(lines[index]))
            {
                continue;
            }

            var attributeLines = GetMethodAttributeLines(lines, index);

            if (attributeLines.Any(IsHttpMethodAttribute) && !attributeLines.Any(IsProducesResponseTypeAttribute))
            {
                yield return $"{Path.GetRelativePath(RepositoryRoot, filePath)}:{index + 1}";
            }
        }
    }

    private static IEnumerable<string> GetAnonymousSuccessResponseUsages(string filePath)
    {
        return File.ReadLines(filePath)
            .Select((line, index) => new { Line = line, LineNumber = index + 1 })
            .Where(sourceLine => IsAnonymousSuccessResponseUsage(sourceLine.Line))
            .Select(sourceLine => $"{Path.GetRelativePath(RepositoryRoot, filePath)}:{sourceLine.LineNumber}");
    }

    private static IEnumerable<string> GetUnconstrainedRouteParameters(string filePath)
    {
        return File.ReadLines(filePath)
            .Select((line, index) => new { Line = line, LineNumber = index + 1 })
            .SelectMany(sourceLine => GetUnconstrainedRouteParametersFromLine(sourceLine.Line)
                .Select(parameter => $"{Path.GetRelativePath(RepositoryRoot, filePath)}:{sourceLine.LineNumber} ({parameter})"));
    }

    private static bool IsPublicActionMethod(string line)
    {
        var trimmedLine = line.TrimStart();

        return trimmedLine.StartsWith("public ", StringComparison.Ordinal)
            && trimmedLine.Contains('(')
            && trimmedLine.Contains(')')
            && !trimmedLine.Contains(" class ", StringComparison.Ordinal)
            && !trimmedLine.Contains(" record ", StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> GetMethodAttributeLines(IReadOnlyList<string> lines, int methodLineIndex)
    {
        var attributeLines = new List<string>();

        for (var index = methodLineIndex - 1; index >= 0; index--)
        {
            var trimmedLine = lines[index].Trim();

            if (trimmedLine.Length == 0)
            {
                continue;
            }

            if (!trimmedLine.StartsWith("[", StringComparison.Ordinal))
            {
                break;
            }

            attributeLines.Add(trimmedLine);
        }

        return attributeLines;
    }

    private static bool IsHttpMethodAttribute(string line)
    {
        return Regex.IsMatch(line, @"^\[Http(Get|Post|Put|Delete|Patch|Head|Options)(\(|\])", RegexOptions.CultureInvariant);
    }

    private static bool IsProducesResponseTypeAttribute(string line)
    {
        return line.StartsWith("[ProducesResponseType", StringComparison.Ordinal);
    }

    private static bool IsAnonymousSuccessResponseUsage(string line)
    {
        var normalizedLine = line.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\t", string.Empty, StringComparison.Ordinal);

        return normalizedLine.Contains("Ok(new{", StringComparison.Ordinal)
            || normalizedLine.Contains("Created(new{", StringComparison.Ordinal)
            || normalizedLine.Contains("Accepted(new{", StringComparison.Ordinal);
    }

    private static IEnumerable<string> GetUnconstrainedRouteParametersFromLine(string line)
    {
        if (!IsHttpMethodAttribute(line.Trim()))
        {
            yield break;
        }

        foreach (Match match in Regex.Matches(line, @"\{(?<parameter>[^}:]+)(?<constraint>:[^}]+)?\}", RegexOptions.CultureInvariant))
        {
            if (!match.Groups["constraint"].Success)
            {
                yield return match.Groups["parameter"].Value;
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "JustGoAPI.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing JustGoAPI.sln.");
    }

    private sealed record ProjectDefinition(string Name, string ModuleName, string FilePath, IReadOnlyList<ProjectReferenceDefinition> ProjectReferences)
    {
        public static ProjectDefinition Load(string projectPath)
        {
            var document = XDocument.Load(projectPath);
            var projectDirectory = Path.GetDirectoryName(projectPath) ?? throw new InvalidOperationException($"Missing directory for {projectPath}.");
            var moduleDirectory = Directory.GetParent(projectDirectory)?.Parent
                ?? throw new InvalidOperationException($"Could not determine module folder for {projectPath}.");

            var references = document
                .Descendants()
                .Where(element => element.Name.LocalName == "ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(include => !string.IsNullOrWhiteSpace(include))
                .Select(include => ProjectReferenceDefinition.Load(projectDirectory, include!))
                .ToList();

            return new ProjectDefinition(
                Path.GetFileNameWithoutExtension(projectPath),
                moduleDirectory.Name,
                projectPath,
                references);
        }
    }

    private sealed record ProjectReferenceDefinition(string Name, string FileName, string FullPath, bool IsModuleProject, string ModuleName)
    {
        public static ProjectReferenceDefinition Load(string projectDirectory, string includePath)
        {
            var fullPath = Path.GetFullPath(Path.Combine(projectDirectory, includePath));
            var fileName = Path.GetFileName(fullPath);
            var name = Path.GetFileNameWithoutExtension(fullPath);
            var modulesPrefix = ModulesRoot + Path.DirectorySeparatorChar;
            var isModuleProject = fullPath.StartsWith(modulesPrefix, StringComparison.OrdinalIgnoreCase);
            var moduleName = string.Empty;

            if (isModuleProject)
            {
                var relativePath = Path.GetRelativePath(ModulesRoot, fullPath);
                moduleName = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)[0];
            }

            return new ProjectReferenceDefinition(name, fileName, fullPath, isModuleProject, moduleName);
        }
    }
}
