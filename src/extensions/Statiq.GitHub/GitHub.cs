﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;
using Statiq.Common.Documents;
using Statiq.Common.Modules;
using Statiq.Common.Execution;
using Statiq.Common.Tracing;
using Statiq.Common;
using Statiq.Common.IO;

namespace Statiq.GitHub
{
    /// <summary>
    /// Outputs metadata for information from GitHub.
    /// </summary>
    /// <remarks>
    /// This modules uses the Octokit library and associated types to submit requests to GitHub. Because of the
    /// large number of different kinds of requests, this module does not attempt to provide a fully abstract wrapper
    /// around the Octokit library. Instead, it simplifies the housekeeping involved in setting up an Octokit client
    /// and requires you to provide functions that fetch whatever data you need. Each request will be sent for each input
    /// document.
    /// </remarks>
    /// <category>Metadata</category>
    public class GitHub : IModule
    {
        private readonly Credentials _credentials;

        private readonly Dictionary<string, Func<IDocument, IExecutionContext, GitHubClient, Task<object>>> _requests
            = new Dictionary<string, Func<IDocument, IExecutionContext, GitHubClient, Task<object>>>();

        private Uri _url;

        /// <summary>
        /// Creates a connection to the GitHub API with basic authenticated access.
        /// </summary>
        /// <param name="username">The username to use.</param>
        /// <param name="password">The password to use.</param>
        public GitHub(string username, string password)
        {
            _credentials = new Credentials(username, password);
        }

        /// <summary>
        /// Creates a connection to the GitHub API with OAuth authentication.
        /// </summary>
        /// <param name="token">The token to use.</param>
        public GitHub(string token)
        {
            _credentials = new Credentials(token);
        }

        /// <summary>
        /// Creates an unauthenticated connection to the GitHub API.
        /// </summary>
        public GitHub()
        {
        }

        /// <summary>
        /// Specifies and alternate API URL (such as to an Enterprise GitHub endpoint).
        /// </summary>
        /// <param name="url">The URL to use.</param>
        /// <returns>The current module instance.</returns>
        public GitHub WithUrl(string url)
        {
            _url = new Uri(url);
            return this;
        }

        /// <summary>
        /// Submits a request to the GitHub client.
        /// </summary>
        /// <param name="key">The metadata key in which to store the return value of the request function.</param>
        /// <param name="request">A function with the request to make.</param>
        /// <returns>The current module instance.</returns>
        public GitHub WithRequest(string key, Func<GitHubClient, Task<object>> request)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Argument is null or empty", nameof(key));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            _requests[key] = (doc, ctx, github) => request(github);
            return this;
        }

        /// <summary>
        /// Submits a request to the GitHub client. This allows you to incorporate data from the execution context in your request.
        /// </summary>
        /// <param name="key">The metadata key in which to store the return value of the request function.</param>
        /// <param name="request">A function with the request to make.</param>
        /// <returns>The current module instance.</returns>
        public GitHub WithRequest(string key, Func<IExecutionContext, GitHubClient, Task<object>> request)
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
        /// Submits a request to the GitHub client. This allows you to incorporate data from the execution context and current document in your request.
        /// </summary>
        /// <param name="key">The metadata key in which to store the return value of the request function.</param>
        /// <param name="request">A function with the request to make.</param>
        /// <returns>The current module instance.</returns>
        public GitHub WithRequest(string key, Func<IDocument, IExecutionContext, GitHubClient, Task<object>> request)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Argument is null or empty", nameof(key));
            }

            _requests[key] = request ?? throw new ArgumentNullException(nameof(request));
            return this;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<IDocument>> ExecuteAsync(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            GitHubClient github = new GitHubClient(new ProductHeaderValue("Statiq"), _url ?? GitHubClient.GitHubApiUrl);
            if (_credentials != null)
            {
                github.Credentials = _credentials;
            }
            return await inputs.ParallelSelectAsync(context, async input =>
            {
                ConcurrentDictionary<string, object> results = new ConcurrentDictionary<string, object>();
                await _requests.ParallelForEachAsync(async request =>
                {
                    Trace.Verbose("Submitting {0} GitHub request for {1}", request.Key, input.Source.ToDisplayString());
                    try
                    {
                        results[request.Key] = await request.Value(input, context, github);
                    }
                    catch (Exception ex)
                    {
                        Trace.Warning("Exception while submitting {0} GitHub request for {1}: {2}", request.Key, input.Source.ToDisplayString(), ex.ToString());
                    }
                });
                return input.Clone(results);
            });
        }
    }
}
