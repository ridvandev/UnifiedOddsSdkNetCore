﻿// Copyright (C) Sportradar AG.See LICENSE for full license governing this code

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sportradar.OddsFeed.SDK.Common.Internal;
using Sportradar.OddsFeed.SDK.Common.Internal.Telemetry;

namespace Sportradar.OddsFeed.SDK.Api.Internal.ApiAccess
{
    internal class SdkHttpClientPool : ISdkHttpClient
    {
        private readonly IList<HttpClient> _httpClientPool;
        private int _latest;
        private readonly object _lock = new object();

        public TimeSpan Timeout { get; }

        /// <inheritdoc />
        public HttpRequestHeaders DefaultRequestHeaders { get; }

        public SdkHttpClientPool(string accessToken, int poolSize, int timeoutSecond, int maxServerConnections)
        : this(accessToken, poolSize, TimeSpan.FromSeconds(timeoutSecond), maxServerConnections)
        {
        }

        public SdkHttpClientPool(string accessToken, int poolSize, TimeSpan timeout, int maxServerConnections)
        {
            if (poolSize < 1)
            {
                poolSize = 1;
            }
            var httpClientHandler = new HttpClientHandler
            {
                MaxConnectionsPerServer = maxServerConnections,
                AllowAutoRedirect = true
            };
            _httpClientPool = new List<HttpClient>(poolSize);
            for (var i = 0; i < poolSize; i++)
            {
                var httpClient = new HttpClient(httpClientHandler) { Timeout = timeout };
                httpClient.DefaultRequestHeaders.Add("x-access-token", accessToken);
                httpClient.DefaultRequestHeaders.Add("User-Agent", $"UfSdk-{SdkInfo.SdkType}/{SdkInfo.GetVersion()} (NET: {Environment.Version}, OS: {Environment.OSVersion}, Init: {SdkInfo.Created:yyyyMMddHHmm})");
                _httpClientPool.Add(httpClient);
            }

            SdkLoggerFactory.GetLoggerForExecution(typeof(SdkHttpClientPool)).LogDebug($"SdkHttpClientPool with size {poolSize} and timeout {timeout.TotalSeconds}s created.");
            DefaultRequestHeaders = _httpClientPool.First().DefaultRequestHeaders;
            Timeout = TimeSpan.FromSeconds(_httpClientPool.First().Timeout.TotalSeconds);
        }

        internal SdkHttpClientPool(string accessToken, int poolSize, TimeSpan timeout, HttpMessageHandler httpMessageHandler)
        {
            if (poolSize < 1)
            {
                poolSize = 1;
            }
            _httpClientPool = new List<HttpClient>(poolSize);
            for (var i = 0; i < poolSize; i++)
            {
                var httpClient = new HttpClient(httpMessageHandler) { Timeout = timeout };
                httpClient.DefaultRequestHeaders.Add("x-access-token", accessToken);
                httpClient.DefaultRequestHeaders.Add("User-Agent", $"UfSdk-{SdkInfo.SdkType}/{SdkInfo.GetVersion()} (NET: {Environment.Version}, OS: {Environment.OSVersion}, Init: {SdkInfo.Created:yyyyMMddHHmm})");
                _httpClientPool.Add(httpClient);
            }

            SdkLoggerFactory.GetLoggerForExecution(typeof(SdkHttpClientPool)).LogDebug($"SdkHttpClientPool with size {poolSize} and timeout {timeout.TotalSeconds}s created.");
            DefaultRequestHeaders = _httpClientPool.First().DefaultRequestHeaders;
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetAsync(Uri requestUri)
        {
            var httpClient = GetHttpClient();
            var result = await httpClient.GetAsync(requestUri).ConfigureAwait(false);
            return result;
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content)
        {
            var httpClient = GetHttpClient();
            return await httpClient.PostAsync(requestUri, content).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> DeleteAsync(Uri requestUri)
        {
            var httpClient = GetHttpClient();
            return await httpClient.DeleteAsync(requestUri).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content)
        {
            var httpClient = GetHttpClient();
            return await httpClient.PutAsync(requestUri, content).ConfigureAwait(false);
        }

        private HttpClient GetHttpClient()
        {
            if (_httpClientPool.Count == 1)
            {
                return _httpClientPool[0];
            }

            lock (_lock)
            {
                _latest = _latest == _httpClientPool.Count - 1 ? 0 : _latest + 1;
                return _httpClientPool[_latest];
            }
        }
    }
}
