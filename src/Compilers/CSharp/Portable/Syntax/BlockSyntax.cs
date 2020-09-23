﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class BlockSyntax
    {
        public BlockSyntax Update(SyntaxToken openBraceToken, SyntaxList<StatementSyntax> statements, SyntaxToken closeBraceToken)
            => Update(attributeLists: default, openBraceToken, statements, closeBraceToken);

        public bool IsInlineStatement()
        {
            return OpenBraceToken.Width == 0 && CloseBraceToken.Width == 0 && Statements.Count == 1;
        }

        public bool TryGetInlineStatement(out StatementSyntax statement)
        {
            statement = null;
            if (!IsInlineStatement()) return false;
            statement = Statements[0];
            return true;
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static BlockSyntax Block(SyntaxToken openBraceToken, SyntaxList<StatementSyntax> statements, SyntaxToken closeBraceToken)
            => Block(attributeLists: default, openBraceToken, statements, closeBraceToken);
    }
}
