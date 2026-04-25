using System.Xml.Linq;

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
