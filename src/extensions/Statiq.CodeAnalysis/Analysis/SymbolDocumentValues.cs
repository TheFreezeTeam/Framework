using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Statiq.Common.Documents;
using Statiq.Common.Meta;

namespace Statiq.CodeAnalysis.Analysis
{
    internal class SymbolDocumentValues : IMetadataValue
    {
        private readonly IEnumerable<ISymbol> _symbols;
        private readonly AnalyzeSymbolVisitor _visitor;
        private bool _cached;
        private ImmutableArray<IDocument> _values;

        public SymbolDocumentValues(IEnumerable<ISymbol> symbols, AnalyzeSymbolVisitor visitor)
        {
            _symbols = symbols ?? Array.Empty<ISymbol>();
            _visitor = visitor;
        }

        public object Get(IMetadata metadata)
        {
            if (!_cached)
            {
                _values = _symbols
                    .Where(x => x != null)
                    .Select(x =>
                    {
                        IDocument document;
                        if (!_visitor.TryGetDocument(x, out document))
                        {
                            // Visit the symbol and try again
                            x.Accept(_visitor);
                            return !_visitor.TryGetDocument(x, out document) ? null : document;
                        }
                        return document;
                    })
                    .Where(x => x != null)
                    .ToImmutableArray();
                _cached = true;
            }
            return _values;
        }
    }
}