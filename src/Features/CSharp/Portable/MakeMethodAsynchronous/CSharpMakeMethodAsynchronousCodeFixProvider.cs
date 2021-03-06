﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MakeMethodAsynchronous;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.MakeMethodAsynchronous
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpMakeMethodAsynchronousCodeFixProvider : AbstractMakeMethodAsynchronousCodeFixProvider
    {
        private const string CS4032 = nameof(CS4032); // The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
        private const string CS4033 = nameof(CS4033); // The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
        private const string CS4034 = nameof(CS4034); // The 'await' operator can only be used within an async lambda expression. Consider marking this method with the 'async' modifier.

        private static readonly SyntaxToken s_asyncToken = SyntaxFactory.Token(SyntaxKind.AsyncKeyword);

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpMakeMethodAsynchronousCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(CS4032, CS4033, CS4034);

        protected override string GetMakeAsyncTaskFunctionResource()
            => CSharpFeaturesResources.Make_method_async;

        protected override string GetMakeAsyncVoidFunctionResource()
            => CSharpFeaturesResources.Make_method_async_remain_void;

        protected override bool IsAsyncSupportingFunctionSyntax(SyntaxNode node)
            => node.IsAsyncSupportingFunctionSyntax();

        protected override bool IsAsyncReturnType(ITypeSymbol type, KnownTypes knownTypes)
        {
            return IsIAsyncEnumerableOrEnumerator(type, knownTypes)
                || IsTaskLike(type, knownTypes);
        }

        protected override SyntaxNode AddAsyncTokenAndFixReturnType(
            bool keepVoid, IMethodSymbol methodSymbolOpt, SyntaxNode node,
            KnownTypes knownTypes)
        {
            switch (node)
            {
                case MethodDeclarationSyntax method: return FixMethod(keepVoid, methodSymbolOpt, method, knownTypes);
                case LocalFunctionStatementSyntax localFunction: return FixLocalFunction(keepVoid, methodSymbolOpt, localFunction, knownTypes);
                case AnonymousMethodExpressionSyntax method: return FixAnonymousMethod(method);
                case ParenthesizedLambdaExpressionSyntax lambda: return FixParenthesizedLambda(lambda);
                case SimpleLambdaExpressionSyntax lambda: return FixSimpleLambda(lambda);
                default: return node;
            }
        }

        private static SyntaxNode FixMethod(
            bool keepVoid,
            IMethodSymbol methodSymbol,
            MethodDeclarationSyntax method,
            KnownTypes knownTypes
            )
        {
            var nameIdentifier = method.Identifier;
            var newModifiers = AddAsyncModifierWithCorrectedTrivia(method.Modifiers, ref nameIdentifier);
            return method.WithIdentifier(nameIdentifier).WithModifiers(newModifiers);
        }

        private static SyntaxNode FixLocalFunction(
            bool keepVoid,
            IMethodSymbol methodSymbol,
            LocalFunctionStatementSyntax localFunction,
            KnownTypes knownTypes
            )
        {
            var nameIdentifier = localFunction.Identifier;
            var newModifiers = AddAsyncModifierWithCorrectedTrivia(localFunction.Modifiers, ref nameIdentifier);
            return localFunction.WithIdentifier(nameIdentifier).WithModifiers(newModifiers);
        }

        private static bool IsIterator(IMethodSymbol x)
        {
            return x.Locations.Any(l => ContainsYield(l.FindNode(cancellationToken: default)));

            bool ContainsYield(SyntaxNode node)
                => node.DescendantNodes(n => n == node || !n.IsReturnableConstruct()).Any(n => IsYield(n));

            static bool IsYield(SyntaxNode node)
                => node.IsKind(SyntaxKind.YieldBreakStatement, SyntaxKind.YieldReturnStatement);
        }

        private static bool IsIAsyncEnumerableOrEnumerator(ITypeSymbol returnType, KnownTypes knownTypes)
            => returnType.OriginalDefinition.Equals(knownTypes._iAsyncEnumerableOfTTypeOpt) ||
                returnType.OriginalDefinition.Equals(knownTypes._iAsyncEnumeratorOfTTypeOpt);

        private static bool IsIEnumerable(ITypeSymbol returnType, KnownTypes knownTypes)
            => returnType.OriginalDefinition.Equals(knownTypes._iEnumerableOfTType);

        private static bool IsIEnumerator(ITypeSymbol returnType, KnownTypes knownTypes)
            => returnType.OriginalDefinition.Equals(knownTypes._iEnumeratorOfTType);

        private static SyntaxTokenList AddAsyncModifierWithCorrectedTrivia(SyntaxTokenList modifiers, ref SyntaxToken nameIdentifier)
        {
            if (modifiers.Any())
                return modifiers.Add(s_asyncToken);

            // Move the leading trivia from the return type to the new modifiers list.
            var result = SyntaxFactory.TokenList(s_asyncToken.WithLeadingTrivia(nameIdentifier.GetLeadingTrivia()));
            nameIdentifier = nameIdentifier.WithoutLeadingTrivia();
            return result;
        }

        private static SyntaxNode FixParenthesizedLambda(ParenthesizedLambdaExpressionSyntax lambda)
        {
            return lambda.WithoutLeadingTrivia()
                         .WithAsyncKeyword(s_asyncToken.WithPrependedLeadingTrivia(lambda.GetLeadingTrivia()));
        }

        private static SyntaxNode FixSimpleLambda(SimpleLambdaExpressionSyntax lambda)
        {
            return lambda.WithoutLeadingTrivia()
                         .WithAsyncKeyword(s_asyncToken.WithPrependedLeadingTrivia(lambda.GetLeadingTrivia()));
        }

        private static SyntaxNode FixAnonymousMethod(AnonymousMethodExpressionSyntax method)
        {
            return method.WithoutLeadingTrivia()
                         .WithAsyncKeyword(s_asyncToken.WithPrependedLeadingTrivia(method.GetLeadingTrivia()));
        }
    }
}
