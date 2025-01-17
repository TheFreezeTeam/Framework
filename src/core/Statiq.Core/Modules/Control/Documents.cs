using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Statiq.Common;
using Statiq.Common.Configuration;
using Statiq.Common.Documents;
using Statiq.Common.Execution;
using Statiq.Common.Modules;

namespace Statiq.Core.Modules.Control
{
    /// <summary>
    /// Inserts documents into the current pipeline.
    /// </summary>
    /// <remarks>
    /// Documents can be inserted either by replacing pipeline documents with previously
    /// processed ones or by creating new ones. If getting previously processed documents from another pipeline,
    /// this module copies the documents and places them into the current pipeline. Note that because this module
    /// does not remove the documents from their original pipeline it's likely you will end up with documents that
    /// have the same content and metadata in two different pipelines. This module does not include the input
    /// documents as part of it's output. If you want to concatenate the result of this module with the input
    /// documents, wrap it with the <see cref="Concat"/> module.
    /// </remarks>
    /// <category>Control</category>
    public class Documents : IModule
    {
        private readonly List<string> _pipelines = new List<string>();
        private readonly DocumentConfig<IEnumerable<IDocument>> _documents;
        private DocumentConfig<bool> _predicate;

        /// <summary>
        /// This outputs all existing documents from all pipelines (except the current one).
        /// </summary>
        public Documents()
        {
        }

        /// <summary>
        /// This outputs the documents from the specified pipeline.
        /// </summary>
        /// <param name="pipeline">The pipeline to output documents from.</param>
        public Documents(string pipeline)
        {
            _pipelines.Add(pipeline);
        }

        /// <summary>
        /// This will get documents based on each input document. The output will be the
        /// aggregate of all returned documents for each input document. The return value
        /// is expected to be a <c>IEnumerable&lt;IDocument&gt;</c>.
        /// </summary>
        /// <param name="documents">A delegate that should return
        /// a <c>IEnumerable&lt;IDocument&gt;</c> containing the documents to
        /// output for each input document.</param>
        public Documents(DocumentConfig<IEnumerable<IDocument>> documents)
        {
            _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        }

        /// <summary>
        /// Generates a specified number of new empty documents.
        /// </summary>
        /// <param name="count">The number of new documents to output.</param>
        public Documents(int count)
        {
            _documents = Config.FromContext(ctx =>
            {
                List<IDocument> documents = new List<IDocument>();
                for (int c = 0; c < count; c++)
                {
                    documents.Add(ctx.GetDocument());
                }
                return (IEnumerable<IDocument>)documents;
            });
        }

        /// <summary>
        /// Generates new documents with the specified content.
        /// </summary>
        /// <param name="content">The content for each output document.</param>
        public Documents(params string[] content)
        {
            _documents = Config.FromContext(async ctx => await content.SelectAsync(async x => ctx.GetDocument(await ctx.GetContentProviderAsync(x))));
        }

        /// <summary>
        /// Generates new documents with the specified metadata.
        /// </summary>
        /// <param name="metadata">The metadata for each output document.</param>
        public Documents(params IEnumerable<KeyValuePair<string, object>>[] metadata)
        {
            _documents = Config.FromContext(ctx => metadata.Select(x => ctx.GetDocument(x)));
        }

        /// <summary>
        /// Generates new documents with the specified content and metadata.
        /// </summary>
        /// <param name="contentAndMetadata">The content and metadata for each output document.</param>
        public Documents(params Tuple<string, IEnumerable<KeyValuePair<string, object>>>[] contentAndMetadata)
        {
            _documents = Config.FromContext(async ctx => await contentAndMetadata.SelectAsync(async x => ctx.GetDocument(x.Item2, await ctx.GetContentProviderAsync(x.Item1))));
        }

        /// <summary>
        /// Only documents that satisfy the predicate will be output.
        /// </summary>
        /// <param name="predicate">A delegate that should return a <c>bool</c>.</param>
        /// <returns>The current module instance.</returns>
        public Documents Where(DocumentConfig<bool> predicate)
        {
            _predicate = _predicate.CombineWith(predicate);
            return this;
        }

        /// <summary>
        /// Gets documents from additional pipeline(s). The final sequence of documents will
        /// be in the order they appear from all specified pipelines. If the empty constructor
        /// is used that outputs documents from all pipelines, this will override that behavior
        /// and only output the specified pipelines. Likewise, if another constructor was used
        /// that relies on a configuration delegate then
        /// using this method will throw <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="pipelines">The additional pipelines to get documents from.</param>
        /// <returns>The current module instance.</returns>
        public Documents FromPipelines(params string[] pipelines)
        {
            if (_documents != null)
            {
                throw new InvalidOperationException("Pipelines cannot be specified if the module is generating new documents using a delegate");
            }
            _pipelines.AddRange(pipelines);
            return this;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<IDocument>> ExecuteAsync(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            IEnumerable<IDocument> documents;

            if (_documents != null)
            {
                documents = _documents.RequiresDocument
                    ? await inputs.SelectManyAsync(context, x => _documents.GetValueAsync(x, context))
                    : await _documents.GetValueAsync(null, context);
            }
            else
            {
                documents = _pipelines.Count == 0
                    ? context.Documents.ExceptPipeline(context.PipelineName)
                    : _pipelines.SelectMany(x => context.Documents.FromPipeline(x));
            }

            return await documents.FilterAsync(_predicate, context);
        }
    }
}