﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Statiq.Common.Documents;
using Statiq.Common.Execution;
using Statiq.Common.Shortcodes;

namespace Statiq.Core.Shortcodes.Html
{
    /// <summary>
    /// Embeds a CodePen pen.
    /// </summary>
    /// <remarks>
    /// You need the path of the pen (essentially everything after the domain in the URL):
    /// <code>
    /// https://codepen.io/edanny/pen/JXwgdK
    /// </code>
    /// </remarks>
    /// <example>
    /// <code>
    /// &lt;?# CodePen edanny/pen/JXwgdK /?&gt;
    /// </code>
    /// </example>
    /// <parameter>The path of the pen.</parameter>
    public class CodePen : Embed
    {
        public override async Task<IDocument> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context) =>
            await ExecuteAsync("https://codepen.io/api/oembed", $"https://codepen.io/{args.SingleValue()}", new[] { "format=json" }, context);
    }
}
