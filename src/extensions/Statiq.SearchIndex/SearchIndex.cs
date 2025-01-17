﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Statiq.Common.Configuration;
using Statiq.Common.Documents;
using Statiq.Common.IO;
using Statiq.Common.Modules;
using Statiq.Common.Execution;
using Statiq.Common.Meta;
using Statiq.Common.Tracing;

namespace Statiq.SearchIndex
{
    /// <summary>
    /// Generates a JavaScript-based search index from the input documents.
    /// </summary>
    /// <remarks>
    /// This module generates a search index that can be imported into the JavaScript <a href="http://lunrjs.com/">Lunr.js</a> search engine.
    /// Each input document should either specify the <c>SearchIndexItem</c> metadata key or a delegate that returns a <c>SearchIndexItem</c>
    /// instance.
    /// </remarks>
    /// <example>
    /// The client-side JavaScript code for importing the search index should look something like this (assuming you have an HTML <c>input</c>
    /// with an ID of <c>#search</c> and a <c>div</c> with an ID of <c>#search-results</c>):
    /// <code>
    /// function runSearch(query) {
    ///     $("#search-results").empty();
    ///     if (query.length &lt; 2)
    ///     {
    ///         return;
    ///     }
    ///     var results = searchModule.search(query);
    ///     var listHtml = "&lt;ul&gt;";
    ///     listHtml += "&lt;li&gt;&lt;strong&gt;Search Results&lt;/strong&gt;&lt;/li&gt;";
    ///     if (results.length == 0)
    ///     {
    ///         listHtml += "&lt;li&gt;No results found&lt;/li&gt;";
    ///     }
    ///     else
    ///     {
    ///         for (var i = 0; i &lt; results.length; ++i)
    ///         {
    ///             var res = results[i];
    ///             listHtml += "&lt;li&gt;&lt;a href='" + res.url + "'&gt;" + res.title + "&lt;/a&gt;&lt;/li&gt;";
    ///         }
    ///     }
    ///     listHtml += "&lt;/ul&gt;";
    ///     $("#search-results").append(listHtml);
    /// }
    ///
    /// $(document).ready(function() {
    ///     $("#search").on('input propertychange paste', function() {
    ///         runSearch($("#search").val());
    ///     });
    /// });
    /// </code>
    /// </example>
    /// <metadata cref="SearchIndexKeys.SearchIndexItem" usage="Input" />
    /// <category>Content</category>
    public class SearchIndex : IModule
    {
        private static readonly Regex StripHtmlAndSpecialChars = new Regex(@"<[^>]+>|&[a-zA-Z]{2,};|&#\d+;|[^a-zA-Z-#]", RegexOptions.Compiled);
        private readonly DocumentConfig<ISearchIndexItem> _searchIndexItem;
        private FilePath _stopwordsPath;
        private bool _enableStemming;
        private FilePath _destination = new FilePath("searchIndex.js");
        private bool _includeHost = false;
        private Func<StringBuilder, IExecutionContext, string> _script = (builder, _) => builder.ToString();

        /// <summary>
        /// Creates the search index by looking for a <c>SearchIndexItem</c> metadata key in each input document that
        /// contains a <c>SearchIndexItem</c> instance.
        /// </summary>
        /// <param name="stopwordsPath">A file to use that contains a set of stopwords.</param>
        /// <param name="enableStemming">If set to <c>true</c>, stemming is enabled.</param>
        public SearchIndex(FilePath stopwordsPath = null, bool enableStemming = false)
            : this(Config.FromDocument(doc => doc.Get<ISearchIndexItem>(SearchIndexKeys.SearchIndexItem)), stopwordsPath, enableStemming)
        {
        }

        /// <summary>
        /// Creates the search index by looking for a specified metadata key in each input document that
        /// contains a <c>SearchIndexItem</c> instance.
        /// </summary>
        /// <param name="searchIndexItemMetadataKey">The metadata key that contains the <c>SearchIndexItem</c> instance.</param>
        /// <param name="stopwordsPath">A file to use that contains a set of stopwords.</param>
        /// <param name="enableStemming">If set to <c>true</c>, stemming is enabled.</param>
        public SearchIndex(string searchIndexItemMetadataKey, FilePath stopwordsPath = null, bool enableStemming = false)
            : this(Config.FromDocument(doc => doc.Get<ISearchIndexItem>(searchIndexItemMetadataKey)), stopwordsPath, enableStemming)
        {
        }

        /// <summary>
        /// Creates the search index by using a delegate that returns a <c>SearchIndexItem</c> instance for each input document.
        /// </summary>
        /// <param name="searchIndexItem">A delegate that should return a <c>ISearchIndexItem</c>.</param>
        /// <param name="stopwordsPath">A file to use that contains a set of stopwords.</param>
        /// <param name="enableStemming">If set to <c>true</c>, stemming is enabled.</param>
        public SearchIndex(DocumentConfig<ISearchIndexItem> searchIndexItem, FilePath stopwordsPath = null, bool enableStemming = false)
        {
            _searchIndexItem = searchIndexItem ?? throw new ArgumentNullException(nameof(searchIndexItem));
            _stopwordsPath = stopwordsPath;
            _enableStemming = enableStemming;
        }

        /// <summary>
        /// Indicates whether the host should be automatically included in generated links.
        /// </summary>
        /// <param name="includeHost"><c>true</c> to include the host.</param>
        /// <returns>The current module instance.</returns>
        public SearchIndex IncludeHost(bool includeHost = true)
        {
            _includeHost = includeHost;
            return this;
        }

        /// <summary>
        /// Sets the path to a stopwords file.
        /// </summary>
        /// <param name="stopwordsPath">A file to use that contains a set of stopwords.</param>
        /// <returns>The current module instance.</returns>
        public SearchIndex WithStopwordsPath(FilePath stopwordsPath)
        {
            _stopwordsPath = stopwordsPath;
            return this;
        }

        /// <summary>
        /// Controls whether stemming is turned on.
        /// </summary>
        /// <param name="enableStemming">If set to <c>true</c>, stemming is enabled.</param>
        /// <returns>The current module instance.</returns>
        public SearchIndex EnableStemming(bool enableStemming = true)
        {
            _enableStemming = enableStemming;
            return this;
        }

        /// <summary>
        /// Controls the output path of the result document (by default the
        /// destination of the result document is "searchIndex.js").
        /// </summary>
        /// <param name="destination">The destination path.</param>
        /// <returns>The current module instance.</returns>
        public SearchIndex WithDestination(FilePath destination)
        {
            _destination = destination;
            return this;
        }

        /// <summary>
        /// This allows you to customize the Lunr.js JavaScript that this module creates.
        /// </summary>
        /// <param name="script">A script transformation function. The <see cref="StringBuilder"/> contains
        /// the generated script content. You can manipulate as appropriate and then return the final
        /// script as a <c>string</c>.</param>
        /// <returns>The current module instance.</returns>
        public SearchIndex WithScript(Func<StringBuilder, IExecutionContext, string> script)
        {
            _script = script ?? throw new ArgumentNullException(nameof(script));
            return this;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<IDocument>> ExecuteAsync(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            ISearchIndexItem[] searchIndexItems =
                (await inputs.SelectAsync(context, async x => await _searchIndexItem.GetValueAsync(x, context)))
                .Where(x => !string.IsNullOrEmpty(x?.Title) && !string.IsNullOrEmpty(x.Content))
                .ToArray();

            if (searchIndexItems.Length == 0)
            {
                Trace.Warning("It's not possible to build the search index because no documents contain the necessary metadata.");
                return Array.Empty<IDocument>();
            }

            string[] stopwords = await GetStopwordsAsync(context);
            StringBuilder scriptBuilder = BuildScript(searchIndexItems, stopwords, context);
            string script = _script(scriptBuilder, context);

            return new[] { context.GetDocument(_destination, await context.GetContentProviderAsync(script)) };
        }

        private StringBuilder BuildScript(IList<ISearchIndexItem> searchIndexItems, string[] stopwords, IExecutionContext context)
        {
            StringBuilder scriptBuilder = new StringBuilder($@"
var searchModule = function() {{
    var documents = [];
    var idMap = [];
    function a(a,b) {{ 
        documents.push(a);
        idMap.push(b); 
    }}
");

            for (int i = 0; i < searchIndexItems.Count; ++i)
            {
                ISearchIndexItem itm = searchIndexItems[i];

                // Get the URL and skip if not valid
                string url = itm.GetLink(context, _includeHost);
                if (string.IsNullOrEmpty(url))
                {
                    continue;
                }

                scriptBuilder.Append($@"
    a(
        {{
            id:{i},
            title:{CleanString(itm.Title, stopwords)},
            content:{CleanString(itm.Content, stopwords)},
            description:{CleanString(itm.Description, stopwords)},
            tags:'{itm.Tags}'
        }},
        {{
            url:'{url}',
            title:{ToJsonString(itm.Title)},
            description:{ToJsonString(itm.Description)}
        }}
    );");
            }

            scriptBuilder.Append($@"
    var idx = lunr(function() {{
        this.field('title');
        this.field('content');
        this.field('description');
        this.field('tags');
        this.ref('id');

        this.pipeline.remove(lunr.stopWordFilter);
        {(_enableStemming ? string.Empty : "this.pipeline.remove(lunr.stemmer);")}
        documents.forEach(function (doc) {{ this.add(doc) }}, this)
    }});
");
            scriptBuilder.AppendLine($@"
    return {{
        search: function(q) {{
            return idx.search(q).map(function(i) {{
                return idMap[i.ref];
            }});
        }}
    }};
}}();");

            return scriptBuilder;
        }

        private static string CleanString(string input, string[] stopwords)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "''";
            }

            string clean = StripHtmlAndSpecialChars.Replace(input, " ").Trim();
            clean = Regex.Replace(clean, @"\s{2,}", " ");
            clean = string.Join(" ", clean.Split(' ').Where(f => f.Length > 1 && !stopwords.Contains(f, StringComparer.InvariantCultureIgnoreCase)).ToArray());
            clean = ToJsonString(clean);

            return clean;
        }

        private static string ToJsonString(string s) => Newtonsoft.Json.JsonConvert.ToString(s);

        private async Task<string[]> GetStopwordsAsync(IExecutionContext context)
        {
            string[] stopwords = new string[0];

            if (_stopwordsPath != null)
            {
                IFile stopwordsFile = await context.FileSystem.GetInputFileAsync(_stopwordsPath);
                if (await stopwordsFile.GetExistsAsync())
                {
                    stopwords = (await stopwordsFile.ReadAllTextAsync())
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(f => f.Trim().ToLowerInvariant())
                        .Where(f => f.Length > 1)
                        .ToArray();
                }
            }

            return stopwords;
        }
    }
}
