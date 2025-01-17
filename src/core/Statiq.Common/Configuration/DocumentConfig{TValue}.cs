﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Statiq.Common.Documents;
using Statiq.Common.Execution;

namespace Statiq.Common.Configuration
{
    /// <summary>
    /// A union configuration value that can be either a delegate
    /// that uses a document and context or a simple value. Use the factory methods
    /// in the <see cref="Config"/> class to create one. Instances can also be created
    /// through implicit casting from the value type. Note that due to overload ambiguity,
    /// if a value type of object is used, then all overloads should also be <see cref="DocumentConfig{T}"/>.
    /// </summary>
    /// <typeparam name="TValue">The value type for this config data.</typeparam>
    public class DocumentConfig<TValue>
    {
        private readonly Func<IDocument, IExecutionContext, Task<TValue>> _delegate;

        internal DocumentConfig(Func<IDocument, IExecutionContext, Task<TValue>> func, bool requiresDocument = true)
        {
            _delegate = func;
            RequiresDocument = requiresDocument;
        }

        public bool RequiresDocument { get; }

        // This should only be accessed via the extension method(s) that guard against null so that null coalescing operators can be used
        // See the discussion at https://github.com/dotnet/roslyn/issues/7171
        internal async Task<TValue> GetAndTransformValueAsync(IDocument document, IExecutionContext context, Func<TValue, TValue> transform = null)
        {
            TValue value = await _delegate(document, context);
            return transform == null ? value : transform(value);
        }

        public static implicit operator DocumentConfig<TValue>(TValue value) => new DocumentConfig<TValue>((_, __) => Task.FromResult(value), false);

        // These special casting operators for object variants ensure we don't accidentally "wrap" an existing ContextConfig/DocumentConfig

        public static implicit operator DocumentConfig<IEnumerable<object>>(DocumentConfig<TValue> documentConfig)
        {
            if (typeof(IEnumerable).IsAssignableFrom(typeof(TValue)))
            {
                return new DocumentConfig<IEnumerable<object>>(async (doc, ctx) => ((IEnumerable)await documentConfig._delegate(doc, ctx)).Cast<object>(), documentConfig.RequiresDocument);
            }
            return new DocumentConfig<IEnumerable<object>>(async (doc, ctx) => new object[] { await documentConfig._delegate(doc, ctx) }, documentConfig.RequiresDocument);
        }

        public static implicit operator DocumentConfig<object>(DocumentConfig<TValue> documentConfig) =>
            new DocumentConfig<object>(async (doc, ctx) => await documentConfig._delegate(doc, ctx), documentConfig.RequiresDocument);
    }
}
