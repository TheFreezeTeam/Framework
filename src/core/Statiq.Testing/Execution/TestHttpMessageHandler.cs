﻿using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Statiq.Testing.Execution
{
    internal class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpMessageHandler, HttpResponseMessage> _httpResponseFunc;
        private readonly HttpMessageHandler _httpMessageHandler;

        public TestHttpMessageHandler(
            Func<HttpRequestMessage, HttpMessageHandler, HttpResponseMessage> httpResponseFunc,
            HttpMessageHandler httpMessageHandler)
        {
            _httpResponseFunc = httpResponseFunc ?? throw new ArgumentNullException(nameof(httpResponseFunc));
            _httpMessageHandler = httpMessageHandler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_httpResponseFunc(request, _httpMessageHandler));
    }
}
