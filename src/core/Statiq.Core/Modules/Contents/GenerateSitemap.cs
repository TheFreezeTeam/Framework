﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Statiq.Common.Configuration;
using Statiq.Common.Documents;
using Statiq.Common.Execution;
using Statiq.Common.IO;
using Statiq.Common.Meta;
using Statiq.Common.Modules;
using Statiq.Common.Modules.Contents;

namespace Statiq.Core.Modules.Contents
{
    /// <summary>
    /// Generates a sitemap from the input documents.
    /// </summary>
    /// <remarks>
    /// This module generates a sitemap from the input documents. The output document contains the sitemap XML as it's content.
    /// You can supply a location for the each item in the sitemap as a <c>string</c> (with an optional function to format it
    /// into an absolute HTML path) or you can supply a <c>SitemapItem</c> for more control. You can also specify the
    /// <c>Hostname</c> metadata key (as a <c>string</c>) for each input document, which will be prepended to all locations.
    /// </remarks>
    /// <metadata cref="Keys.SitemapItem" usage="Input" />
    /// <category>Content</category>
    public class GenerateSitemap : IModule
    {
        private static readonly string[] ChangeFrequencies = { "always", "hourly", "daily", "weekly", "monthly", "yearly", "never" };

        private readonly DocumentConfig<object> _sitemapItemOrLocation;
        private readonly Func<string, string> _locationFormatter;

        /// <summary>
        /// Creates a sitemap using the metadata key <c>SitemapItem</c> which should contain either a <c>string</c> that
        /// contains the location for each input document or a <c>SitemapItem</c> instance with the location
        /// and other information. If the key <c>SitemapItem</c> is not found or does not contain the correct type of object,
        /// a link to the document will be used.
        /// </summary>
        /// <param name="locationFormatter">A location formatter that will be applied to the location of each input after
        /// getting the value of the <c>SitemapItem</c> metadata key.</param>
        public GenerateSitemap(Func<string, string> locationFormatter = null)
            : this(Config.FromDocument(doc => doc.Get(Keys.SitemapItem)), locationFormatter)
        {
        }

        /// <summary>
        /// Creates a sitemap using the specified metadata key which should contain either a <c>string</c> that
        /// contains the location for each input document or a <c>SitemapItem</c> instance with the location
        /// and other information. If the metadata key is not found or does not contain the correct type of object,
        /// a link to the document will be used.
        /// </summary>
        /// <param name="sitemapItemOrLocationMetadataKey">A metadata key that contains either a <c>SitemapItem</c> or
        /// a <c>string</c> location for each input document.</param>
        /// <param name="locationFormatter">A location formatter that will be applied to the location of each input after
        /// getting the value of the specified metadata key.</param>
        public GenerateSitemap(string sitemapItemOrLocationMetadataKey, Func<string, string> locationFormatter = null)
            : this(Config.FromDocument(doc => doc.Get(sitemapItemOrLocationMetadataKey)), locationFormatter)
        {
            if (string.IsNullOrEmpty(sitemapItemOrLocationMetadataKey))
            {
                throw new ArgumentException("Argument is null or empty", nameof(sitemapItemOrLocationMetadataKey));
            }
        }

        /// <summary>
        /// Creates a sitemap using the specified delegate which should return either a <c>string</c> that
        /// contains the location for each input document or a <c>SitemapItem</c> instance with the location
        /// and other information. If the delegate returns <c>null</c> or does not return the correct type of object,
        /// a link to the document will be used.
        /// </summary>
        /// <param name="sitemapItemOrLocation">A delegate that either returns a <c>SitemapItem</c> instance or a <c>string</c>
        /// with the desired item location. If the delegate returns <c>null</c>, the input document is not added to the sitemap.</param>
        /// <param name="locationFormatter">A location formatter that will be applied to the location of each input after
        /// getting the value of the specified metadata key.</param>
        public GenerateSitemap(DocumentConfig<object> sitemapItemOrLocation, Func<string, string> locationFormatter = null)
        {
            _sitemapItemOrLocation = sitemapItemOrLocation ?? throw new ArgumentNullException(nameof(sitemapItemOrLocation));
            _locationFormatter = locationFormatter;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<IDocument>> ExecuteAsync(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
            await context.ForEachAsync(inputs, AddToSiteMapAsync);
            sb.Append("</urlset>");

            // Always output the sitemap document, even if it's empty
            return new[] { context.GetDocument(await context.GetContentProviderAsync(sb.ToString())) };

            async Task AddToSiteMapAsync(IDocument input)
            {
                // Try to get a SitemapItem
                object delegateResult = await _sitemapItemOrLocation.GetValueAsync(input, context);
                SitemapItem sitemapItem = delegateResult as SitemapItem
                    ?? new SitemapItem((delegateResult as string) ?? context.GetLink(input));

                // Add a sitemap entry if we got an item and valid location
                if (!string.IsNullOrWhiteSpace(sitemapItem?.Location))
                {
                    string location = sitemapItem.Location;

                    // Apply the location formatter if there is one
                    if (_locationFormatter != null)
                    {
                        location = _locationFormatter(location);
                    }

                    // Apply the hostname if defined (and the location formatter didn't already set a hostname)
                    if (!string.IsNullOrWhiteSpace(location))
                    {
                        if (!location.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase)
                            && !location.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                        {
                            location = context.GetLink(new FilePath(location), true);
                        }
                    }

                    // Location being null signals that this document should not be included in the sitemap
                    if (!string.IsNullOrWhiteSpace(location))
                    {
                        sb.Append("<url>");
                        sb.AppendFormat("<loc>{0}</loc>", location);

                        if (sitemapItem.LastModUtc.HasValue)
                        {
                            sb.AppendFormat("<lastmod>{0}</lastmod>", sitemapItem.LastModUtc.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                        }

                        if (sitemapItem.ChangeFrequency.HasValue)
                        {
                            sb.AppendFormat("<changefreq>{0}</changefreq>", ChangeFrequencies[(int)sitemapItem.ChangeFrequency.Value]);
                        }

                        if (sitemapItem.Priority.HasValue)
                        {
                            sb.AppendFormat(CultureInfo.InvariantCulture, "<priority>{0}</priority>", sitemapItem.Priority.Value);
                        }

                        sb.Append("</url>");
                    }
                }
            }
        }
    }
}
