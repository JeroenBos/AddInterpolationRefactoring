using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AddInterpolationRefactoring
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(AddInterpolationRefactoringCodeRefactoringProvider)), Shared]
	public class AddInterpolationRefactoringCodeRefactoringProvider : CodeRefactoringProvider
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
			catch
			{

			}
		}

		private async Task<LiteralExpressionSyntax> TryGetStringLiteralExpressionAt(CodeRefactoringContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);


			// if context is in a LiteralExpressionSyntax
			var node = root.FindNode(context.Span, getInnermostNodeForTie: true); // inner most node is useful e.g. when the string literal is an argument in a method call
			if (node.IsKind(SyntaxKind.StringLiteralExpression))
			{
				//(node as ArgumentSyntax).Expression
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
			catch
			{
				return null;
			}
		}
	}
	public static class Extensions
	{
		public static InterpolatedStringExpressionSyntax WithInterpolationStartToken(this LiteralExpressionSyntax literalSyntax)
		{
			var startTokenKind = literalSyntax.IsVerbatimStringLiteral() ? SyntaxKind.InterpolatedVerbatimStringStartToken : SyntaxKind.InterpolatedStringStartToken;
			var startToken = Token(literalSyntax.GetLeadingTrivia(), startTokenKind, TriviaList());

			var getEndToken = literalSyntax.HasEndDoubleQuote() ? Token : (Func<SyntaxTriviaList, SyntaxKind, SyntaxTriviaList, SyntaxToken>)MissingToken;
			var endToken = getEndToken(TriviaList(), SyntaxKind.InterpolatedStringEndToken, literalSyntax.GetTrailingTrivia());

			string cSharptext = literalSyntax.Token.ValueText;
			string lexicalText = GetLexicalText(literalSyntax.Token.Text, startToken, endToken);
			var textToken = Token(TriviaList(), SyntaxKind.InterpolatedStringTextToken, lexicalText, cSharptext, TriviaList());

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
		public static string GetLexicalText(string fullLexicalText, SyntaxToken startToken, SyntaxToken endToken)
		{
			int skipCount = startToken.IsMissing ? 0 : (startToken.Text.Length - "$".Length);
			int endSkipCount = endToken.IsMissing ? 0 : endToken.Text.Length;

			string result = fullLexicalText.Substring(skipCount, fullLexicalText.Length - skipCount - endSkipCount);
			return result;
		}
	}
}