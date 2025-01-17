﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebMarkupMin.Core;
using Statiq.Common.Documents;
using Statiq.Common.Execution;
using Statiq.Common.Modules;

namespace Statiq.Minification
{
    /// <summary>
    /// Minifies the XML content.
    /// </summary>
    /// <remarks>
    /// This module takes the XML content and uses minification to reduce the output.
    /// </remarks>
    /// <example>
    /// <code>
    /// Pipelines.Add("Blog posts",
    ///     ReadFiles("posts/*.md"),
    ///     FrontMatter(Yaml()),
    ///     Markdown(),
    ///     WriteFiles(".html"),
    ///     Rss(siteUri: "http://example.org",
    ///         rssPath: new FilePath("posts/feed.rss"),
    ///         feedTitle: "My awesome blog",
    ///         feedDescription: "Blog about something"
    ///     ),
    ///     MinifyXml(),
    ///     WriteFiles()
    /// );
    /// </code>
    /// </example>
    /// <category>Content</category>
    public class MinifyXml : MinifierBase, IModule
    {
        private readonly XmlMinificationSettings _minificationSettings;

        /// <summary>
        /// Minifies the XML content.
        /// </summary>
        /// <param name="useEmptyMinificationSettings">
        /// Boolean to specify whether to use empty minification settings.
        /// Default value is <c>false</c>, this will use commonly accepted settings.
        /// </param>
        public MinifyXml(bool useEmptyMinificationSettings = false)
        {
            // https://github.com/Taritsyn/WebMarkupMin/wiki/XML-Minifier
            _minificationSettings = new XmlMinificationSettings(useEmptyMinificationSettings);
        }

        /// <summary>
        /// Flag for whether to remove all XML comments.
        /// </summary>
        /// <param name="removeXmlComments">Default value is <c>true</c>.</param>
        /// <returns>The current instance.</returns>
        public MinifyXml RemoveXmlComments(bool removeXmlComments = true)
        {
            _minificationSettings.RemoveXmlComments = removeXmlComments;
            return this;
        }

        /// <summary>
        /// Updates the minification settings.
        /// </summary>
        /// <param name="action">A function to update the minification settings with.</param>
        /// <returns>The current instance.</returns>
        /// <example>
        /// <code>
        /// MinifyXml()
        ///     .WithSettings(settings => settings.RemoveXmlComments = false)
        /// </code>
        /// </example>
        public MinifyXml WithSettings(Action<XmlMinificationSettings> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            action(_minificationSettings);

            return this;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<IDocument>> ExecuteAsync(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            XmlMinifier minifier = new XmlMinifier(_minificationSettings);

            return await MinifyAsync(inputs, context, minifier.Minify, "XML");
        }
    }
}