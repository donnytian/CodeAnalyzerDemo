using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace MySimpleAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TodoAnalyzer : DiagnosticAnalyzer
    {
        public static readonly ImmutableHashSet<SyntaxKind> CommentTypes = ImmutableHashSet.Create(
            SyntaxKind.SingleLineCommentTrivia,
            SyntaxKind.MultiLineCommentTrivia,
            SyntaxKind.DocumentationCommentExteriorTrivia,
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxKind.MultiLineDocumentationCommentTrivia);

        public static readonly ImmutableArray<string> TodoKeywords = ImmutableArray.Create(
            "todo",
            "fixme"
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(RuleDescriptor);

        public DiagnosticDescriptor Rule { get; } = RuleDescriptor;
        public const string DiagnosticId = "MA00001";

        protected string[] Keywords { get; } = TodoKeywords.ToArray();

        public static readonly DiagnosticDescriptor RuleDescriptor = new(
            id: DiagnosticId,
            title: "TODO or FIXME has no JIRA ticket",
            messageFormat: "This code contains a comment referring to a task yet done (`TODO`, `FIXME`, or equivalent), but no JIRA reference to task or issue description",
            category: "Documentation",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: string.Empty);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        }

        protected virtual bool IsComment(SyntaxTrivia trivia)
        {
            return CommentTypes.Contains(trivia.Kind());
        }

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            if (!context.Tree.TryGetRoot(out var root))
            {
                return;
            }

            var commentNodes = root.DescendantTrivia().Where(IsComment);

            // Looks for key word in code comment and report issue if no JIRA ticket associated.
            foreach (var comment in commentNodes)
            {
                var content = comment.ToString();
                if (ContainsJiraTicket(content))
                {
                    continue;
                }

                foreach (var keyword in Keywords.Where(w => !string.IsNullOrWhiteSpace(w)))
                {
                    foreach (var location in GetKeywordLocations(context.Tree, comment, keyword))
                    {
                        var diagnostic = Diagnostic.Create(Rule, location, string.Empty);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static IEnumerable<Location> GetKeywordLocations(SyntaxTree tree, SyntaxTrivia comment, string word)
        {
            var text = comment.ToString();

            return AllIndexesOf(text, word)
                .Where(i => IsWordAt(text, i, word.Length))
                .Select(i =>
                {
                    var startLocation = comment.SpanStart + i;
                    var location = Location.Create(
                        tree,
                        TextSpan.FromBounds(startLocation, startLocation + word.Length));

                    return location;
                });
        }

        private static IEnumerable<int> AllIndexesOf(
            string text,
            string value,
            StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            var index = 0;
            while ((index = text.IndexOf(value, index, text.Length - index, comparisonType)) != -1)
            {
                yield return index;
                index += value.Length;
            }
        }

        private static bool IsWordAt(string text, int index, int count)
        {
            var leftBoundary = true;
            if (index > 0)
            {
                leftBoundary = !char.IsLetterOrDigit(text[index - 1]);
            }

            var rightBoundary = true;
            var rightOffset = index + count;
            if (rightOffset < text.Length)
            {
                rightBoundary = !char.IsLetterOrDigit(text[rightOffset]);
            }

            return leftBoundary && rightBoundary;
        }

        /// <summary>
        /// This regex is to catch ticket key in 
        /// the following formatting CODEREAD-12345
        /// </summary>
        private static readonly Regex JiraTicketRegex = new(@"(\w+|\d+)\-\d+", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
        public static bool ContainsJiraTicket(string input) =>
            !string.IsNullOrWhiteSpace(input) && JiraTicketRegex.IsMatch(input);
    }
}