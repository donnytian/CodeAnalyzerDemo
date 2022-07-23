using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MySimpleAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DeepLoopAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        = ImmutableArray.Create(RuleDescriptor);

    public const string DiagnosticId = "MA00002";
    public static int MaxLevel { get; private set; } = 3;

    public static readonly DiagnosticDescriptor RuleDescriptor = new(
        id: DiagnosticId,
        title: "Deep loop detected",
        messageFormat: "Too many nested loops",
        category: "Style",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: string.Empty);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(RegisterCompilationStart);

        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void RegisterCompilationStart(CompilationStartAnalysisContext startContext)
    {
        var optionsProvider = startContext.Options.AnalyzerConfigOptionsProvider;
        startContext.RegisterCodeBlockAction(actionContext => AnalyzeCodeBlock(actionContext, optionsProvider));
    }

    private static void AnalyzeCodeBlock(CodeBlockAnalysisContext context, AnalyzerConfigOptionsProvider optionsProvider)
    {
        // The options contains the .editorconfig settings
        var options = optionsProvider.GetOptions(context.CodeBlock.SyntaxTree);
        if (options.TryGetValue($"dotnet_diagnostic.{DiagnosticId}.max", out var countString) &&
            int.TryParse(countString, out var value) && value > 1)
        {
            MaxLevel = value;
        }
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;

        var loopChecker = new MethodLoopWalker(MaxLevel);
        loopChecker.Visit(method);
        loopChecker.ReportDiagnosticIfAny(context, RuleDescriptor);
    }

    private class MethodLoopWalker : CSharpSyntaxWalker
    {
        private readonly int _maxLevel;
        public MethodLoopWalker(int maxLevel)
        {
            _maxLevel = maxLevel;
        }

        private readonly Stack<SyntaxNode> _nodeStack = new();
        public IList<SyntaxNode> NodesToReport { get; } = new List<SyntaxNode>();

        public void ReportDiagnosticIfAny(SyntaxNodeAnalysisContext context, DiagnosticDescriptor rule)
        {
            foreach (var node in NodesToReport)
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, node.GetLocation()));
            }
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            VisitLoop(node);
            base.VisitForEachStatement(node);
            _nodeStack.Pop();
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            VisitLoop(node);
            base.VisitForStatement(node);
            _nodeStack.Pop();
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            VisitLoop(node);
            base.VisitWhileStatement(node);
            _nodeStack.Pop();
        }

        private void VisitLoop(SyntaxNode node)
        {
            if (_nodeStack.Count >= _maxLevel)
            {
                NodesToReport.Add(node);
            }

            _nodeStack.Push(node);
        }
    }
}