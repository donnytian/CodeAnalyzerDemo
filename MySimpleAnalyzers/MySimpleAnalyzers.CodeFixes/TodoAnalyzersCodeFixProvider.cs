using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;

namespace MySimpleAnalyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TodoAnalyzersCodeFixProvider)), Shared]
    public class TodoAnalyzersCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(TodoAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the comment identified by the diagnostic.
            var comment = root.FindTrivia(diagnosticSpan.Start, true);

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => AddTicketNumberAsync(context.Document, comment, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<Document> AddTicketNumberAsync(Document document, SyntaxTrivia comment, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var token = comment.Token;
            var newComment = SyntaxFactory.Comment(comment + " JIRA-0000");

            var newToken = token.LeadingTrivia.Contains(comment)
                ? token.WithLeadingTrivia(token.LeadingTrivia.Replace(comment, newComment))
                : token.WithTrailingTrivia(token.TrailingTrivia.Replace(comment, newComment));
            var newRoot = root.ReplaceToken(token, newToken);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
