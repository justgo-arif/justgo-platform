using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JustGo.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ApiResponseObjectObjectAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "JG0001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Use typed ApiResponse data",
        "Do not use ApiResponse<object, object> for successful controller responses. Use ApiResponse<TData, TPermissions> with a concrete TData.",
        "JustGo.ApiContract",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly ImmutableHashSet<string> SuccessResponseMethods = ImmutableHashSet.Create(
        "Ok",
        "Created",
        "Accepted");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (!IsSuccessfulControllerResponse(invocation))
        {
            return;
        }

        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            var objectCreation = FindObjectCreation(argument.Expression);

            if (objectCreation is null || !IsUntypedApiResponse(objectCreation, context.SemanticModel, context.CancellationToken))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, objectCreation.Type.GetLocation()));
        }
    }

    private static bool IsSuccessfulControllerResponse(InvocationExpressionSyntax invocation)
    {
        var methodName = invocation.Expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            _ => null
        };

        return methodName is not null && SuccessResponseMethods.Contains(methodName);
    }

    private static ObjectCreationExpressionSyntax? FindObjectCreation(ExpressionSyntax expression)
    {
        return expression switch
        {
            ObjectCreationExpressionSyntax objectCreation => objectCreation,
            CastExpressionSyntax cast => FindObjectCreation(cast.Expression),
            ParenthesizedExpressionSyntax parenthesized => FindObjectCreation(parenthesized.Expression),
            _ => null
        };
    }

    private static bool IsUntypedApiResponse(
        ObjectCreationExpressionSyntax objectCreation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var type = semanticModel.GetTypeInfo(objectCreation, cancellationToken).Type as INamedTypeSymbol;

        if (type is null
            || !type.Name.Equals("ApiResponse", StringComparison.Ordinal)
            || !GetFullNamespace(type).Equals("JustGo.Authentication.Infrastructure.Utilities", StringComparison.Ordinal)
            || type.TypeArguments.Length != 2)
        {
            return false;
        }

        return type.TypeArguments[0].SpecialType == SpecialType.System_Object
            && type.TypeArguments[1].SpecialType == SpecialType.System_Object;
    }

    private static string GetFullNamespace(INamedTypeSymbol type)
    {
        var namespaceSymbol = type.ContainingNamespace;

        if (namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace)
        {
            return string.Empty;
        }

        return namespaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty);
    }
}
