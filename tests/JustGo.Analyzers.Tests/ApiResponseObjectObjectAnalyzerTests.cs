using System.Collections.Immutable;
using JustGo.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JustGo.Analyzers.Tests;

public class ApiResponseObjectObjectAnalyzerTests
{
    [Fact]
    public async Task Reports_Ok_WithUntypedApiResponse()
    {
        var diagnostics = await GetDiagnosticsAsync("""
            using JustGo.Authentication.Infrastructure.Utilities;

            public class AccountsController : ControllerBase
            {
                public IActionResult Authenticate()
                {
                    var user = new AuthenticateResponse();
                    return Ok(new ApiResponse<object, object>(user));
                }
            }
            """);

        Assert.Single(diagnostics);
        Assert.Equal(ApiResponseObjectObjectAnalyzer.DiagnosticId, diagnostics[0].Id);
    }

    [Fact]
    public async Task Reports_MultilineUntypedApiResponse()
    {
        var diagnostics = await GetDiagnosticsAsync("""
            using JustGo.Authentication.Infrastructure.Utilities;

            public class AccountsController : ControllerBase
            {
                public IActionResult Authenticate()
                {
                    var user = new AuthenticateResponse();
                    return Ok(new ApiResponse<
                        object,
                        object>(user));
                }
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Reports_FullyQualifiedUntypedApiResponse()
    {
        var diagnostics = await GetDiagnosticsAsync("""
            public class AccountsController : ControllerBase
            {
                public IActionResult Authenticate()
                {
                    var user = new AuthenticateResponse();
                    return Ok(new JustGo.Authentication.Infrastructure.Utilities.ApiResponse<object, object>(user));
                }
            }
            """);

        Assert.Single(diagnostics);
    }

    [Theory]
    [InlineData("return Ok(new ApiResponse<AuthenticateResponse, object>(user));")]
    [InlineData("return BadRequest(new ApiResponse<object, object>(\"Invalid token\"));")]
    [InlineData("return Ok(new ApiResponse<string, object>(\"value\"));")]
    [InlineData("return Ok(new ApiResponse<bool, object>(true));")]
    public async Task DoesNotReport_AllowedResponses(string statement)
    {
        var diagnostics = await GetDiagnosticsAsync($$"""
            using JustGo.Authentication.Infrastructure.Utilities;

            public class AccountsController : ControllerBase
            {
                public IActionResult Authenticate()
                {
                    var user = new AuthenticateResponse();
                    {{statement}}
                }
            }
            """);

        Assert.Empty(diagnostics);
    }

    private static async Task<IReadOnlyList<Diagnostic>> GetDiagnosticsAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source + StubSource);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new ApiResponseObjectObjectAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        return diagnostics
            .Where(diagnostic => diagnostic.Id == ApiResponseObjectObjectAnalyzer.DiagnosticId)
            .OrderBy(diagnostic => diagnostic.Location.SourceSpan.Start)
            .ToList();
    }

    private const string StubSource = """
        namespace JustGo.Authentication.Infrastructure.Utilities
        {
            public class ApiResponse<TData, TPermissions>
            {
                public ApiResponse(TData data)
                {
                }
            }
        }

        public interface IActionResult
        {
        }

        public sealed class ActionResult : IActionResult
        {
        }

        public class ControllerBase
        {
            public IActionResult Ok(object value) => new ActionResult();
            public IActionResult Created(object value) => new ActionResult();
            public IActionResult Accepted(object value) => new ActionResult();
            public IActionResult BadRequest(object value) => new ActionResult();
        }

        public sealed class AuthenticateResponse
        {
        }

        """;
}
