﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class TryStatementSyntax
    {
        public TryStatementSyntax Update(SyntaxToken tryKeyword, BlockSyntax block, SyntaxList<CatchClauseSyntax> catches, FinallyClauseSyntax @finally)
            => Update(attributeLists: default, tryKeyword, block, catches, @finally);

        public bool IsInlineBlockTryCatchStatement()
        {
            return !TryKeyword.IsMissing && TryKeyword.Width == 0 && Parent is BlockSyntax;
        }
    }

    public partial class CatchDeclarationSyntax
    {
        public bool HasExplicitType()
        {
            var noExplicitType = Type.Kind() == SyntaxKind.IdentifierName && Type.Width == 0;
            return !noExplicitType;
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static TryStatementSyntax TryStatement(BlockSyntax block, SyntaxList<CatchClauseSyntax> catches, FinallyClauseSyntax @finally)
            => TryStatement(attributeLists: default, block, catches, @finally);

        public static TryStatementSyntax TryStatement(SyntaxToken tryKeyword, BlockSyntax block, SyntaxList<CatchClauseSyntax> catches, FinallyClauseSyntax @finally)
            => TryStatement(attributeLists: default, tryKeyword, block, catches, @finally);
    }
}
