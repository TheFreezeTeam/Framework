﻿using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Statiq.Common.Configuration;
using Statiq.Common.Documents;
using Statiq.Common.Modules;
using Statiq.Common.Execution;

namespace Statiq.Core.Modules.Contents
{
    /// <summary>
    /// Replaces a search string in the specified content with the content of input documents.
    /// </summary>
    /// <remarks>
    /// This is sort of like the inverse of the Replace module and is very useful for simple
    /// template substitution.
    /// </remarks>
    /// <category>Content</category>
    public class ReplaceWithContent : ContentModule
    {
        private readonly string _search;
        private bool _isRegex;
        private RegexOptions _regexOptions = RegexOptions.None;

        /// <summary>
        /// Replaces all occurrences of the search string in the string value of the returned
        /// object with the content of each input document. This allows you to specify different
        /// content for each document depending on the input document.
        /// </summary>
        /// <param name="search">The string to search for.</param>
        /// <param name="content">A delegate that returns the content within which
        /// to search for the search string.</param>
        public ReplaceWithContent(string search, DocumentConfig<string> content)
            : base(content)
        {
            _search = search;
        }

        /// <summary>
        /// The specified modules are executed against an empty initial document and all
        /// occurrences of the search string in the resulting document content are replaced by the content of
        /// each input document (possibly creating more than one output document for each input document).
        /// </summary>
        /// <param name="search">The string to search for.</param>
        /// <param name="modules">Modules that output the content within which
        /// to search for the search string.</param>
        public ReplaceWithContent(string search, params IModule[] modules)
            : base(modules)
        {
            _search = search;
        }

        /// <summary>
        /// Indicates that the search string(s) should be treated as a regular expression(s)
        /// with the specified options.
        /// </summary>
        /// <param name="regexOptions">The options to use (if any).</param>
        /// <returns>The current module instance.</returns>
        public ReplaceWithContent IsRegex(RegexOptions regexOptions = RegexOptions.None)
        {
            _isRegex = true;
            _regexOptions = regexOptions;
            return this;
        }

        /// <inheritdoc />
        protected override async Task<IDocument> ExecuteAsync(string content, IDocument input, IExecutionContext context)
        {
            if (input == null)
            {
                return null;
            }
            if (content == null)
            {
                content = string.Empty;
            }
            if (string.IsNullOrEmpty(_search))
            {
                return input.Clone(await context.GetContentProviderAsync(content));
            }
            string inputContent = await input.GetStringAsync();
            string replaced = _isRegex
                ? Regex.Replace(inputContent, _search, content, _regexOptions)
                : content.Replace(_search, inputContent);
            return input.Clone(await context.GetContentProviderAsync(replaced));
        }
    }
}
