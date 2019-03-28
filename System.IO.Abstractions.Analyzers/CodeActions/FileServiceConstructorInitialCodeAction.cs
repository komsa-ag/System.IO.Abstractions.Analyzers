using System.IO.Abstractions.Analyzers.RoslynToken;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using SyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace System.IO.Abstractions.Analyzers.CodeActions
{
	public class FileServiceConstructorInitialCodeAction : CodeAction
	{
		private readonly ClassDeclarationSyntax _class;

		private readonly Document _document;

		public FileServiceConstructorInitialCodeAction(string title, Document document, ClassDeclarationSyntax @class)
		{
			_class = @class;
			_document = document;
			Title = title;
		}

		public override string Title { get; }

		public override string EquivalenceKey => Title;

		protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
		{
			var editor = await DocumentEditor.CreateAsync(_document, cancellationToken).ConfigureAwait(false);

			if (!RoslynClassFyleSystem.HasFileSystemProperty(_class))
			{
				editor.InsertMembers(_class,
					0,
					new SyntaxNode[]
					{
						RoslynClassFyleSystem.CreateFileSystemPropertyDeclaration()
					});
			}

			ConstructorAddParameter(_class, editor);

			var compilationUnitSyntax = RoslynClassFyleSystem.GetCompilationUnit(_class);

			if (compilationUnitSyntax.Usings.Any())
			{
				editor.ReplaceNode(RoslynClassFyleSystem.GetSystemIoUsing(compilationUnitSyntax),
					RoslynClassFyleSystem.GetFileSystemUsing());
			}

			return editor.GetChangedDocument();
		}

		private static ExpressionStatementSyntax CreateAssignmentExpression()
		{
			return SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
				SyntaxFactory.IdentifierName(Constants.FieldFileSystemName),
				SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(Constants.FileSystemClassName))
					.WithArgumentList(SyntaxFactory.ArgumentList())));
		}

		private static void ConstructorAddParameter(ClassDeclarationSyntax classDeclaration, SyntaxEditor editor)
		{
			var constructor = RoslynClassFyleSystem.HasConstructor(classDeclaration)
				? RoslynClassFyleSystem.GetConstructor(classDeclaration)
				: SF.ConstructorDeclaration(classDeclaration.Identifier)
					.WithModifiers(SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));

			var newConstructor = constructor.WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation)
				.NormalizeWhitespace();

			if (!RoslynClassFyleSystem.ConstructorHasAssignmentExpression(newConstructor))
			{
				newConstructor = newConstructor.AddBodyStatements(CreateAssignmentExpression());
			}

			if (RoslynClassFyleSystem.HasConstructor(classDeclaration))
			{
				editor.ReplaceNode(constructor, newConstructor);
			} else
			{
				editor.InsertBefore(RoslynClassFyleSystem.GetMethod(classDeclaration), newConstructor);
			}
		}
	}
}