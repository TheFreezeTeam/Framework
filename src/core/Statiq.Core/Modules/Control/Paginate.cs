﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Statiq.Common;
using Statiq.Common.Configuration;
using Statiq.Common.Documents;
using Statiq.Common.Execution;
using Statiq.Common.Meta;
using Statiq.Common.Modules;

namespace Statiq.Core.Modules.Control
{
    /// <summary>
    /// Splits a sequence of documents into multiple pages.
    /// </summary>
    /// <remarks>
    /// This module forms pages from the output documents of the specified modules.
    /// Each input document is cloned for each page and metadata related
    /// to the pages, including the sequence of documents for each page,
    /// is added to each clone. For example, if you have 2 input documents
    /// and the result of paging is 3 pages, this module will output 6 documents.
    /// Note that if there are no documents to paginate, this module will still
    /// output an empty page without any documents inside the page.
    /// </remarks>
    /// <example>
    /// If your input document is a Razor template for a blog archive, you can use
    /// Paginate to get pages of 10 blog posts each. If you have 50 blog posts, the
    /// result of the Paginate module will be 5 copies of your input archive template,
    /// one for each page. Your configuration file might look something like this:
    /// <code>
    /// Pipelines.Add("Posts",
    ///     ReadFiles("*.md"),
    ///     Markdown(),
    ///     WriteFiles("html")
    /// );
    ///
    /// Pipelines.Add("Archive",
    ///     ReadFiles("archive.cshtml"),
    ///     Paginate(10,
    ///         Documents("Posts")
    ///     ),
    ///     Razor(),
    ///     WriteFiles(string.Format("archive-{0}.html", @doc["CurrentPage"]))
    /// );
    /// </code>
    /// </example>
    /// <metadata cref="Keys.PageDocuments" usage="Output" />
    /// <metadata cref="Keys.CurrentPage" usage="Output" />
    /// <metadata cref="Keys.TotalPages" usage="Output" />
    /// <metadata cref="Keys.TotalItems" usage="Output" />
    /// <metadata cref="Keys.HasNextPage" usage="Output" />
    /// <metadata cref="Keys.HasPreviousPage" usage="Output" />
    /// <metadata cref="Keys.NextPage" usage="Output" />
    /// <metadata cref="Keys.PreviousPage" usage="Output" />
    /// <category>Control</category>
    public class Paginate : ContainerModule
    {
        private readonly int _pageSize;
        private readonly Dictionary<string, DocumentConfig<object>> _pageMetadata = new Dictionary<string, DocumentConfig<object>>();
        private DocumentConfig<bool> _predicate;
        private int _takePages = int.MaxValue;
        private int _skipPages = 0;

        /// <summary>
        /// Partitions the result of the specified modules into the specified number of pages. The
        /// input documents to Paginate are used as the initial input documents to the specified modules.
        /// </summary>
        /// <param name="pageSize">The number of documents on each page.</param>
        /// <param name="modules">The modules to execute to get the documents to page.</param>
        public Paginate(int pageSize, params IModule[] modules)
            : this(pageSize, (IEnumerable<IModule>)modules)
        {
        }

        /// <summary>
        /// Partitions the result of the specified modules into the specified number of pages. The
        /// input documents to Paginate are used as the initial input documents to the specified modules.
        /// </summary>
        /// <param name="pageSize">The number of documents on each page.</param>
        /// <param name="modules">The modules to execute to get the documents to page.</param>
        public Paginate(int pageSize, IEnumerable<IModule> modules)
            : base(modules)
        {
            if (pageSize <= 0)
            {
                throw new ArgumentException(nameof(pageSize));
            }

            _pageSize = pageSize;
        }

        /// <summary>
        /// Limits the documents to be paged to those that satisfy the supplied predicate.
        /// </summary>
        /// <param name="predicate">A delegate that should return a <c>bool</c>.</param>
        /// <returns>The current module instance.</returns>
        public Paginate Where(DocumentConfig<bool> predicate)
        {
            _predicate = _predicate.CombineWith(predicate);
            return this;
        }

        /// <summary>
        /// Only outputs a specific number of pages.
        /// </summary>
        /// <param name="count">The number of pages to output.</param>
        /// <returns>The current module instance.</returns>
        public Paginate TakePages(int count)
        {
            _takePages = count;
            return this;
        }

        /// <summary>
        /// Skips a specified number of pages before outputting pages.
        /// </summary>
        /// <param name="count">The number of pages to skip.</param>
        /// <returns>The current module instance.</returns>
        public Paginate SkipPages(int count)
        {
            _skipPages = count;
            return this;
        }

        /// <summary>
        /// Adds the specified metadata to each page index document. This must be performed
        /// within the paginate module. If you attempt to process the page index documents
        /// from the paginate module after execution, it will "disconnect" metadata for
        /// those documents like <see cref="Keys.NextPage"/> since you're effectivly
        /// creating new documents and the ones those keys refer to will be outdated.
        /// </summary>
        /// <param name="key">The key of the metadata to add.</param>
        /// <param name="metadata">A delegate with the value for the metadata.</param>
        /// <returns>The current module instance.</returns>
        public Paginate WithPageMetadata(string key, DocumentConfig<object> metadata)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _pageMetadata[key] = metadata ?? throw new ArgumentNullException(nameof(metadata));
            return this;
        }

        /// <inheritdoc />
        public override async Task<IEnumerable<IDocument>> ExecuteAsync(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            // Partition the pages
            IReadOnlyList<IDocument> documents = (await (await context.ExecuteAsync(Children, inputs)).FilterAsync(_predicate, context)).ToList();
            IDocument[][] partitions =
                Partition(documents, _pageSize)
                .ToArray();
            int totalItems = partitions.Sum(x => x.Length);

            // Create the documents
            if (inputs.Count > 0)
            {
                return await inputs.SelectManyAsync(context, GetDocuments);
            }
            return await GetDocuments(null);

            async Task<IEnumerable<IDocument>> GetDocuments(IDocument input)
            {
                // Create the pages
                Page[] pages = partitions
                        .Skip(_skipPages)
                        .Take(_takePages)
                        .Select(x => new Page
                        {
                            PageDocuments = x
                        })
                        .ToArray();

                // Special case for no pages, create an empty one
                if (pages.Length == 0)
                {
                    pages = new[]
                    {
                        new Page
                        {
                            PageDocuments = Array.Empty<IDocument>()
                        }
                    };
                }

                // Create the documents per page
                for (int i = 0; i < pages.Length; i++)
                {
                    // Get the current page document
                    int currentI = i;  // Avoid modified closure for previous/next matadata delegate
                    Dictionary<string, object> metadata = new Dictionary<string, object>
                    {
                        { Keys.PageDocuments, pages[i].PageDocuments },
                        { Keys.CurrentPage, i + 1 },
                        { Keys.TotalPages, pages.Length },
                        { Keys.TotalItems, totalItems },
                        { Keys.HasNextPage, pages.Length > i + 1 },
                        { Keys.HasPreviousPage, i != 0 },
                        { Keys.NextPage, new CachedDelegateMetadataValue(_ => pages.Length > currentI + 1 ? pages[currentI + 1].Document : null) },
                        { Keys.PreviousPage, new CachedDelegateMetadataValue(_ => currentI != 0 ? pages[currentI - 1].Document : null) }
                    };
                    IDocument document = input?.Clone(metadata) ?? context.GetDocument(metadata);

                    // Apply any page metadata
                    if (_pageMetadata.Count > 0)
                    {
                        IEnumerable<KeyValuePair<string, object>> pageMetadata = await _pageMetadata
                            .SelectAsync(async kvp => new KeyValuePair<string, object>(kvp.Key, await kvp.Value.GetValueAsync(document, context)));
                        document = document.Clone(pageMetadata);
                    }

                    pages[i].Document = document;
                }
                return pages.Select(x => x.Document);
            }
        }

        // Interesting discussion of partitioning at
        // http://stackoverflow.com/questions/419019/split-list-into-sublists-with-linq
        // Note that this implementation won't work for very long sequences because it enumerates twice per chunk
        private static IEnumerable<T[]> Partition<T>(IReadOnlyList<T> source, int size)
        {
            int pos = 0;
            while (source.Skip(pos).Any())
            {
                yield return source.Skip(pos).Take(size).ToArray();
                pos += size;
            }
        }

        private class Page
        {
            public IDocument Document { get; set; }
            public IDocument[] PageDocuments { get; set; }
        }
    }
}
