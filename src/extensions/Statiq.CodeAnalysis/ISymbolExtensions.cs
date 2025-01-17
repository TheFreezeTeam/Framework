﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Statiq.Common.Util;

namespace Statiq.CodeAnalysis
{
    public static class ISymbolExtensions
    {
        /// <summary>
        /// Gets a unique ID for the symbol. Note that the symbol ID is
        /// not fully-qualified and is therefore only unique within a namespace.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>A unique (within a namespace) ID.</returns>
        public static string GetId(this ISymbol symbol)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            if (symbol is IAssemblySymbol)
            {
                return symbol.Name + ".dll";
            }
            if (symbol is INamespaceOrTypeSymbol)
            {
                char[] id = symbol.MetadataName.ToCharArray();
                for (int c = 0; c < id.Length; c++)
                {
                    if (!char.IsLetterOrDigit(id[c]) && id[c] != '-' && id[c] != '.')
                    {
                        id[c] = '_';
                    }
                }
                return new string(id);
            }

            // Get a hash for anything other than namespaces or types
            return BitConverter.ToString(BitConverter.GetBytes(Crc32.Calculate(symbol.GetDocumentationCommentId() ?? GetFullName(symbol)))).Replace("-", string.Empty);
        }

        /// <summary>
        /// Gets the full name of the symbol. For namespaces, this is the name of the namespace.
        /// For types, this includes all generic type parameters.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The full name of the symbol.</returns>
        public static string GetFullName(this ISymbol symbol)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            return symbol.ToDisplayString(new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType,
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes));
        }

        /// <summary>
        /// Gets the qualified name of the symbol which includes all containing namespaces.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The qualified name of the symbol.</returns>
        public static string GetQualifiedName(this ISymbol symbol)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            return symbol.ToDisplayString(new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters));
        }

        /// <summary>
        /// Gets a display name for the symbol.
        /// For namespaces this is the same as the qualified name.
        /// For types this is the same as the full name.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The display name.</returns>
        public static string GetDisplayName(this ISymbol symbol)
        {
            if (symbol is IAssemblySymbol)
            {
                // Add .dll to assembly names
                return symbol.Name + ".dll";
            }
            if (symbol.Kind == SymbolKind.Namespace)
            {
                // Use "global" for the global namespace display name since it's a reserved keyword and it's used to refer to the global namespace in code
                return symbol.ContainingNamespace == null ? "global" : GetQualifiedName(symbol);
            }
            return GetFullName(symbol);
        }
    }
}
