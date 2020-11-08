﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal enum TypeAnnotationKind
    {
        ThisParamType,
        OriginalType
    }

    internal partial class TypeSymbol
    {
        internal TypeAnnotationKind? AnnotationTypeKind { get; set; }

        internal TypeSymbol AnnotationType { get; set; }

        internal virtual TypeSymbol Self => this;

        /// <summary>
        /// Represents the method by which this type implements a given interface type
        /// and/or the corresponding diagnostics.
        /// </summary>
        protected class SymbolAndDiagnostics
        {
            public static readonly SymbolAndDiagnostics Empty = new SymbolAndDiagnostics(null, ImmutableArray<Diagnostic>.Empty);

            public readonly Symbol Symbol;
            public readonly ImmutableArray<Diagnostic> Diagnostics;

            public SymbolAndDiagnostics(Symbol symbol, ImmutableArray<Diagnostic> diagnostics)
            {
                this.Symbol = symbol;
                this.Diagnostics = diagnostics;
            }
        }
    }
}
