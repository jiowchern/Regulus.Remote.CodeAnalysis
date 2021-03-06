﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Regulus.Remote.CodeAnalysis
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FixableReturnTypeAnalysisCodeFixProvider)), Shared]
    public class FixableReturnTypeAnalysisCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(FixableReturnTypeAnalyzer.DiagnosticId); }
        }
        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.

            var declaration = root.FindNode(diagnosticSpan) as Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax;          
        
            context.RegisterCodeFix(
                Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedSolution: c => _MakeRemoteValue(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<Solution> _MakeRemoteValue(Document document, Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax declaration, CancellationToken c)
        {
            var root = await document.GetSyntaxRootAsync(c).ConfigureAwait(false);
            var retType = declaration.ReturnType;
            var retStr = retType.ToString();            
            var remoteReturn = SyntaxFactory.ParseTypeName($"Regulus.Remote.Value<{retStr}> ");
            var newNode = declaration.WithReturnType(remoteReturn);
            
            var newRoot = root.ReplaceNode(declaration, new[] { newNode } );            
            return document.Project.Solution.WithDocumentSyntaxRoot(document.Id, newRoot);
            
        }


    }
}