﻿using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System;
using System.Linq;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class CSharpNewlineFormattingRule : BaseFormattingRule
    {
        internal CSharpNewlineFormattingRule()
        {
        }

        public override AdjustNewLinesOperation GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation, FormattingReason reason)
        {
            if (currentToken.IsKind(SyntaxKind.EndOfFileToken))
                return base.GetAdjustNewLinesOperation(previousToken, currentToken, nextOperation, reason);

            var op = EvalNewlineForPropertyAccessors(previousToken, currentToken, reason);
            if (op != null) return op;

            op = EvalNewlineForBraces(previousToken, currentToken, reason);
            if (op != null) return op;

            op = EvalNewlineForSemicolon(previousToken, currentToken, reason);
            if (op != null) return op;

            op = EvalNewlineForTopStatements(previousToken, currentToken, reason);
            if (op != null) return op;

            op = EvalNewlineForGeneratedSymbols(previousToken, currentToken, reason);
            if (op != null) return op;

            op = base.GetAdjustNewLinesOperation(previousToken, currentToken, nextOperation, reason);
            return op;
        }

        private AdjustNewLinesOperation EvalNewlineForPropertyAccessors(SyntaxToken previousToken, SyntaxToken currentToken, FormattingReason reason)
        {
            if (currentToken.Width() == 0) return null;

            if (currentToken.IsKind(SyntaxKind.GetKeyword, SyntaxKind.SetKeyword, SyntaxKind.CloseBraceToken))
            {
                // getter should be on newline is some cases ... that is if it's a "simple" property declaration
                var propDecl = currentToken.Parent?.GetAncestorOrThis(n => IsMemberDeclaration(n)) as PropertyDeclarationSyntax;
                if (propDecl == null) return null;

                var anyAccessorWithBody = propDecl.AccessorList?.Accessors.Any(a => (a.Body != null || a.ExpressionBody != null) && a.Keyword.Width() > 0) == true;
                if (anyAccessorWithBody)
                {
                    // if there is any accessor with a body, we must put it on a newline
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLines);
                }
            }

            return null;
        }

        private AdjustNewLinesOperation EvalNewlineForBraces(SyntaxToken previousToken, SyntaxToken currentToken, FormattingReason reason)
        {
            // only consider "real tokens" ... which have some actual length ... otherwise it may be "fake tokens"
            if (currentToken.Width() == 0) return null;

            // always add a newline for code gen if the previous was a brace too!
            if (reason == FormattingReason.CodeGen)
            {
                // } }
                if (previousToken.IsKind(SyntaxKind.CloseBraceToken) && currentToken.IsKind(SyntaxKind.CloseBraceToken))
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLines);
                }

                // }; }
                if (previousToken.IsKind(SyntaxKind.CloseBraceToken) && currentToken.IsKind(SyntaxKind.SemicolonToken) && currentToken.GetNextToken().IsKind(SyntaxKind.CloseBraceToken) == true)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLines);
                }

                return null;
            }

            if (currentToken.IsKind(
                    SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken,
                    SyntaxKind.OpenBracketToken, SyntaxKind.CloseBracketToken,
                    SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken,
                    SyntaxKind.LessThanToken, SyntaxKind.GreaterThanToken
                ))
            {
                // don't adjust anything for brace pairs
                return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
            }

            return null;
        }

        private AdjustNewLinesOperation EvalNewlineForSemicolon(SyntaxToken previousToken, SyntaxToken currentToken, FormattingReason reason)
        {
            if (reason != FormattingReason.CopyPasteAction && reason != FormattingReason.CommandAction) return null;

            var currentDecl = currentToken.Parent?.GetAncestorOrThis(n => IsTopDeclaration(n));
            // we only handle for supported top statements
            if (currentDecl is null) return null;

            // we only handle special case for copy & paste actions
            if (previousToken.Width() == 0) previousToken = currentToken.GetPreviousToken();
            var prevDecl = previousToken.Parent?.GetAncestorOrThis(n => IsTopDeclaration(n));

            // calculate the line diff
            var lineDiff = previousToken.GetLineDiff(currentToken);
            var onSameLine = lineDiff == 0;

            // if it's a fake semicomma token and it's on a newline... force it back on the previous line so that it stays together with the declaration
            if (!onSameLine && currentToken.IsKind(SyntaxKind.SemicolonToken) && currentToken.Width() == 0 && prevDecl == currentDecl)
            {
                // preserve the lines, but decrease with 1
                var lines = 0;
                var options = AdjustNewLinesOption.ForceLines;

                var nextToken = currentToken.GetNextToken();
                var nextLineDiff = currentToken.GetLineDiff(nextToken);
                if (nextLineDiff > 0)
                {
                    // keep additional lines if the next token isn't on the same line
                    lines = lineDiff;
                    options = AdjustNewLinesOption.PreserveLines;
                }

                // for members / variables, we don't do anything
                switch (currentDecl)
                {
                    case NamespaceDeclarationSyntax:
                    case UsingDirectiveSyntax:
                    case AttributeSyntax:
                        return CreateAdjustNewLinesOperation(lines, options);
                    case MethodDeclarationSyntax:
                        // we only handle methods for code gen action
                        if (reason != FormattingReason.CodeGen) return null;
                        return CreateAdjustNewLinesOperation(lines, options);
                }
            }

            return null;
        }

        private AdjustNewLinesOperation EvalNewlineForGeneratedSymbols(SyntaxToken previousToken, SyntaxToken currentToken, FormattingReason reason)
        {
            if (reason != FormattingReason.DefaultFormatAction && reason != FormattingReason.CodeGen) return null;

            var currentDecl = currentToken.Parent?.GetAncestorOrThis(n => IsMemberDeclaration(n));

            // if we have a "fake token" ... check the previous "visible token" instead
            if (previousToken.Width() == 0) previousToken = currentToken.GetPreviousToken();
            var prevDecl = previousToken.Parent?.GetAncestorOrThis(n => IsMemberDeclaration(n));

            // we don't do anything if they are same declaration
            if (prevDecl == currentDecl) return null;

            // special handling for properties without any get/set body - need an additional line for those if it's a code gen action ... since those properties generate a big "funky syntax" with hidden tokens and such...
            if (reason == FormattingReason.CodeGen && prevDecl is PropertyDeclarationSyntax prevPropDecl)
            {
                var anyAccessorWithBody = prevPropDecl.AccessorList?.Accessors.Any(a => (a.Body != null || a.ExpressionBody != null) && a.Keyword.Width() > 0) == true;
                var hasExprBody = prevPropDecl.ExpressionBody?.Expression != null;
                if (!anyAccessorWithBody && !hasExprBody)
                {
                    var lineDiff = previousToken.GetLineDiff(currentToken);
                    if (lineDiff <= 1)
                        return CreateAdjustNewLinesOperation(3, AdjustNewLinesOption.ForceLines);
                }
            }

            return CreateAdjustNewLinesOperation(2, AdjustNewLinesOption.ForceLines);
        }

        private AdjustNewLinesOperation EvalNewlineForTopStatements(SyntaxToken previousToken, SyntaxToken currentToken, FormattingReason reason)
        {
            var currentDecl = currentToken.Parent?.GetAncestorOrThis(n => IsTopDeclaration(n));
            // we only handle for supported top statements
            if (currentDecl is null) return null;

            // don't apply this to "members" that are not directly beneath a namespace
            if (!(currentDecl.Parent is NamespaceDeclarationSyntax))
                return null;

            // if we have a "fake token" ... check the previous "visible token" instead
            if (previousToken.Width() == 0) previousToken = currentToken.GetPreviousToken();
            var prevDecl = previousToken.Parent?.GetAncestorOrThis(n => IsTopDeclaration(n));

            // calculate the line diff
            var lineDiff = previousToken.GetLineDiff(currentToken);
            var onSameLine = lineDiff == 0;
            var linesCount = 0;

            // if the first token of the declaration (any of the ones) is the current token - it should always be on a newline
            var currentDeclFirstToken = currentDecl?.GetFirstToken();
            if (currentToken == currentDeclFirstToken)
            {
                linesCount += 1;

                if (currentDecl is UsingDirectiveSyntax && prevDecl is NamespaceDeclarationSyntax)
                {
                    // import following a namespace should have 1 additional line space
                    linesCount += 1;
                }
                else if (IsGlobalMember(currentDecl))
                {
                    // a global member that has a previous neighbour that is a global member ... no space is needed
                    if (IsGlobalMember(prevDecl)) return null;

                    // previous is import or namespace?
                    if (IsUsingOrNamespace(prevDecl))
                    {
                        // a "member" that has a namespace or using directive before, should have an additional line
                        if (lineDiff <= 1)
                            linesCount += 1;
                    }
                }
            }

            // no need to force anything if no expected line count
            if (linesCount <= 0) return null;

            if (reason == FormattingReason.CodeGenFromFileTemplate && linesCount <= 1)
            {
                // code gen from template only needs lines if it's more than 1
                return null;
            }

            return CreateAdjustNewLinesOperation(linesCount, AdjustNewLinesOption.PreserveLines);
        }

        private bool IsUsingOrNamespace(SyntaxNode n)
        {
            return
                n is UsingDirectiveSyntax ||
                n is NamespaceDeclarationSyntax;
        }

        private bool IsGlobalMember(SyntaxNode n)
        {
            return
                n is MethodDeclarationSyntax ||
                n is PropertyDeclarationSyntax ||
                n is FieldDeclarationSyntax;
        }

        private bool IsMemberDeclaration(SyntaxNode n)
        {
            switch (n.Kind())
            {
                case SyntaxKind.UsingDirective:
                case SyntaxKind.NamespaceDeclaration:

                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.EnumDeclaration:
                case SyntaxKind.DelegateDeclaration:

                case SyntaxKind.Attribute:
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.MethodDeclaration:
                    return true;
            }

            return false;
        }

        private bool IsTopDeclaration(SyntaxNode n)
        {
            switch(n.Kind())
            {
                case SyntaxKind.UsingDirective:
                case SyntaxKind.NamespaceDeclaration:

                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.EnumDeclaration:
                case SyntaxKind.DelegateDeclaration:

                case SyntaxKind.Attribute:
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.MethodDeclaration:
                    return true;
            }

            return false;
        }
    }
}
