
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Collections;
using System;
using System.Collections.Generic;

namespace AddInterpolationRefactoring
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(AddInterpolationRefactoringCodeRefactoringProvider)), Shared]
    internal class AddInterpolationRefactoringCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            try
            {
                LiteralExpressionSyntax stringLiteralNode = await TryGetStringLiteralExpressionAt(context);

                // Find the node at the selection.
                if (stringLiteralNode != null)
                {
                    var action = CodeAction.Create("Add interpolation to string", c => AddInterpolationAsync(context.Document, stringLiteralNode, c));
                    context.RegisterRefactoring(action);
                }
            }
            catch(Exception e)
            {

            }
        }

        private async Task<LiteralExpressionSyntax> TryGetStringLiteralExpressionAt(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);


            // if context is in a LiteralExpressionSyntax
            var node = root.FindNode(context.Span);
            if (node.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return (LiteralExpressionSyntax)node;
            }

            // if context is at the end of a LiteralExpressionSyntax
            if (context.Span.Start != 0)
            {
                var previousNode = root.FindNode(new TextSpan(context.Span.Start - 1, 0));
                if (previousNode.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    return (LiteralExpressionSyntax)previousNode;
                }
            }

            return null;
        }

        private async Task<Solution> AddInterpolationAsync(Document document, LiteralExpressionSyntax literalExpr, CancellationToken cancellationToken)
        {
            try
            {
                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
                var interpolatedExpression = literalExpr.WithInterpolationStartToken();
                Contract.Assert(interpolatedExpression.GetText().Length == literalExpr.GetText().Length + 1);
                syntaxRoot = syntaxRoot.ReplaceNode(literalExpr, interpolatedExpression);
                return document.Project.Solution.WithDocumentSyntaxRoot(document.Id, syntaxRoot);
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
    public static class Extensions
    {
        public static InterpolatedStringExpressionSyntax WithInterpolationStartToken(this LiteralExpressionSyntax literalSyntax)
        {
            string text = literalSyntax.Token.ValueText;
            var startTokenKind = literalSyntax.IsVerbatimStringLiteral() ? SyntaxKind.InterpolatedVerbatimStringStartToken : SyntaxKind.InterpolatedStringStartToken;
            var startToken = Token(literalSyntax.GetLeadingTrivia(), startTokenKind, TriviaList());

            var textToken = Token(TriviaList(), SyntaxKind.InterpolatedStringTextToken, text, text, TriviaList());

            var getEndToken = literalSyntax.HasEndDoubleQuote() ? Token : (Func<SyntaxTriviaList, SyntaxKind, SyntaxTriviaList, SyntaxToken>)MissingToken;
            var endToken = getEndToken(TriviaList(), SyntaxKind.InterpolatedStringEndToken, literalSyntax.GetTrailingTrivia());


            var trail = literalSyntax.GetTrailingTrivia();
            var leaf = literalSyntax.GetLeadingTrivia();
            var result = InterpolatedStringExpression(startToken, SingletonList<InterpolatedStringContentSyntax>(InterpolatedStringText(textToken)), endToken);

            var debugTrail = result.GetTrailingTrivia();
            var debugLead = result.GetLeadingTrivia();
            return result;
        }

        public static bool IsVerbatimStringLiteral(this LiteralExpressionSyntax literalExpression)
        {
            if (literalExpression == null)
                throw new ArgumentNullException(nameof(literalExpression));

            return literalExpression.IsKind(SyntaxKind.StringLiteralExpression)
                && literalExpression.Token.Text.StartsWith("@", StringComparison.Ordinal);
        }
        public static bool IsVerbatim(this InterpolatedStringExpressionSyntax interpolatedString)
        {
            if (interpolatedString == null)
                throw new ArgumentNullException(nameof(interpolatedString));

            return interpolatedString.StringStartToken.ValueText.Contains("@");
        }
        public static bool HasEndDoubleQuote(this LiteralExpressionSyntax literalExpression)
        {

            if (literalExpression == null)
                throw new ArgumentNullException(nameof(literalExpression));

            return literalExpression.IsKind(SyntaxKind.StringLiteralExpression)
                && literalExpression.Token.Text.EndsWith("\"", StringComparison.Ordinal);

        }
    }
}