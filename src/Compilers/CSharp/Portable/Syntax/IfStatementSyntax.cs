﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class IfStatementSyntax
    {
        public SyntaxToken GetLastHeaderToken()
        {
            var lastToken = CloseParenToken;
            if (lastToken.IsMissing || lastToken.Width == 0) lastToken = Condition.GetLastToken();
            if (lastToken.IsMissing || lastToken.Width == 0) lastToken = OpenParenToken;
            if (lastToken.IsMissing || lastToken.Width == 0) lastToken = IfKeyword;
            return lastToken;
        }

        public IfStatementSyntax Update(SyntaxToken ifKeyword, SyntaxToken openParenToken, ExpressionSyntax condition, SyntaxToken closeParenToken, StatementSyntax statement, ElseClauseSyntax @else)
            => Update(attributeLists: default, ifKeyword, openParenToken, condition, closeParenToken, statement, @else);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static IfStatementSyntax IfStatement(ExpressionSyntax condition, StatementSyntax statement, ElseClauseSyntax @else)
            => IfStatement(attributeLists: default, condition, statement, @else);

        public static IfStatementSyntax IfStatement(SyntaxToken ifKeyword, SyntaxToken openParenToken, ExpressionSyntax condition, SyntaxToken closeParenToken, StatementSyntax statement, ElseClauseSyntax @else)
            => IfStatement(attributeLists: default, ifKeyword, openParenToken, condition, closeParenToken, statement, @else);
    }
}
