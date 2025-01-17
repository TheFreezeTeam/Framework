﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Statiq.Common.Documents;
using Statiq.Common.Modules;
using Statiq.Common.Execution;
using Statiq.Common.Tracing;
using Statiq.Common;
using Statiq.Common.IO;

namespace Statiq.Json
{
    /// <summary>
    /// Parses JSON content for each input document and stores the result in it's metadata.
    /// </summary>
    /// <remarks>
    /// Parses the content for each input document and then stores a dynamic object
    /// representing the JSON in metadata with the specified key. If no key is specified,
    /// then the dynamic object is not added. You can also flatten the JSON to add top-level items directly
    /// to the document metadata.
    /// </remarks>
    /// <category>Metadata</category>
    public class Json : IModule
    {
        private readonly bool _flatten;
        private readonly string _key;

        /// <summary>
        /// The content of the input document is parsed as JSON. All root-level items are added to the input document's
        /// metadata. This is best for simple key-value JSON documents.
        /// </summary>
        public Json()
        {
            _flatten = true;
        }

        /// <summary>
        /// The content of the input document is parsed as JSON. A dynamic object representing the JSON content
        /// is set as the value for the given metadata key. If flatten is true, all root-level items are also added
        /// to the input document's metadata.
        /// </summary>
        /// <param name="key">The metadata key in which to set the dynamic JSON object.</param>
        /// <param name="flatten">If set to <c>true</c>, all root-level items are also added to the input document's metadata.</param>
        public Json(string key, bool flatten = false)
        {
            _key = key;
            _flatten = flatten;
        }

        /// <inheritdoc />
        public Task<IEnumerable<IDocument>> ExecuteAsync(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            // Don't use the built-in exception tracing so that we can return the original document on error
            return inputs.ParallelSelectAsync(ProcessJsonAsync);

            async Task<IDocument> ProcessJsonAsync(IDocument input)
            {
                try
                {
                    JsonSerializer serializer = new JsonSerializer();
                    Dictionary<string, object> items = new Dictionary<string, object>();
                    ExpandoObject json;
                    using (TextReader contentReader = new StreamReader(await input.GetStreamAsync()))
                    {
                        using (JsonReader jsonReader = new JsonTextReader(contentReader))
                        {
                            json = serializer.Deserialize<ExpandoObject>(jsonReader);
                        }
                    }
                    if (json != null)
                    {
                        if (!string.IsNullOrEmpty(_key))
                        {
                            items[_key] = json;
                        }
                        if (_flatten)
                        {
                            foreach (KeyValuePair<string, object> item in json)
                            {
                                items[item.Key] = item.Value;
                            }
                        }
                        return input.Clone(items);
                    }
                }
                catch (Exception ex)
                {
                    Trace.Error("Error processing JSON for {0}: {1}", input.Source.ToDisplayString(), ex.ToString());
                }
                return input;
            }
        }
    }
}
