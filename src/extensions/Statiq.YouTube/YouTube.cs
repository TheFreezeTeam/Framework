﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Statiq.Common.Documents;
using Statiq.Common.Execution;
using Statiq.Common.IO;
using Statiq.Common.Modules;
using Statiq.Common.Tracing;

namespace Statiq.YouTube
{
    /// <summary>
    /// Outputs metadata for information from YouTube.
    /// </summary>
    /// <remarks>
    /// This modules uses the Google.Apis.YouTube.v3 library and associated types to submit requests to GitHub. Because
    /// of the large number of different kinds of requests, this module does not attempt to provide a fully abstract wrapper
    /// around the Google.Apis.YouTube.v3 library. Instead, it simplifies the housekeeping involved in setting up an
    /// Google.Apis.YouTube.v3 client and requires you to provide functions that fetch whatever data you need. Each request
    /// will be sent for each input document.
    /// </remarks>
    /// <category>Metadata</category>
    public class YouTube : IModule, IDisposable
    {
        private readonly YouTubeService _youtube;

        private readonly Dictionary<string, Func<IDocument, IExecutionContext, YouTubeService, object>> _requests
            = new Dictionary<string, Func<IDocument, IExecutionContext, YouTubeService, object>>();

        /// <summary>
        /// Creates a connection to the YouTube API with authenticated access.
        /// </summary>
        /// <param name="apiKey">The apikey to use.</param>
        public YouTube(string apiKey)
        {
            _youtube = new YouTubeService(
                new BaseClientService.Initializer
                {
                    ApplicationName = "Statiq",
                    ApiKey = apiKey
                });
        }

        public void Dispose() => _youtube.Dispose();

        /// <summary>
        /// Submits a request to the YouTube client. This allows you to incorporate data from the execution context in your request.
        /// </summary>
        /// <param name="key">The metadata key in which to store the return value of the request function.</param>
        /// <param name="request">A function with the request to make.</param>
        /// <returns>The current module instance.</returns>
        public YouTube WithRequest(string key, Func<IExecutionContext, YouTubeService, object> request)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Argument is null or empty", nameof(key));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            _requests[key] = (doc, ctx, github) => request(ctx, github);
            return this;
        }

        /// <summary>
        /// Submits a request to the YouTube client. This allows you to incorporate data from the execution context and current document in your request.
        /// </summary>
        /// <param name="key">The metadata key in which to store the return value of the request function.</param>
        /// <param name="request">A function with the request to make.</param>
        /// <returns>The current module instance.</returns>
        public YouTube WithRequest(string key, Func<IDocument, IExecutionContext, YouTubeService, object> request)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Argument is null or empty", nameof(key));
            }

            _requests[key] = request ?? throw new ArgumentNullException(nameof(request));
            return this;
        }

        /// <inheritdoc />
        public Task<IEnumerable<IDocument>> ExecuteAsync(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            ParallelQuery<IDocument> outputs = inputs.AsParallel().Select(context, input =>
            {
                ConcurrentDictionary<string, object> results = new ConcurrentDictionary<string, object>();
                foreach (KeyValuePair<string, Func<IDocument, IExecutionContext, YouTubeService, object>> request in _requests.AsParallel())
                {
                    Trace.Verbose("Submitting {0} YouTube request for {1}", request.Key, input.Source.ToDisplayString());
                    try
                    {
                        results[request.Key] = request.Value(input, context, _youtube);
                    }
                    catch (Exception ex)
                    {
                        Trace.Warning("Exception while submitting {0} YouTube request for {1}: {2}", request.Key, input.Source.ToDisplayString(), ex.ToString());
                    }
                }
                return input.Clone(results);
            });
            return Task.FromResult<IEnumerable<IDocument>>(outputs);
        }
    }
}
