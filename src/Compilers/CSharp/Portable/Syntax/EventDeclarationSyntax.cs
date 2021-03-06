﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.ComponentModel;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class EventDeclarationSyntax
    {
        public EventDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken eventKeyword, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, TypeSyntax type, AccessorListSyntax accessorList)
        {
            return Update(attributeLists, modifiers, eventKeyword, explicitInterfaceSpecifier, identifier, type, accessorList, semicolonToken: default);
        }

        public EventDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken eventKeyword, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, TypeSyntax type, SyntaxToken semicolonToken)
        {
            return Update(attributeLists, modifiers, eventKeyword, explicitInterfaceSpecifier, identifier, type, accessorList: null, semicolonToken);
        }

        public bool HasExplicitReturnType()
        {
            var noExplicitReturnType = Type.Kind() == SyntaxKind.IdentifierName && Type.Width == 0;
            return !noExplicitReturnType;
        }
    }
}
