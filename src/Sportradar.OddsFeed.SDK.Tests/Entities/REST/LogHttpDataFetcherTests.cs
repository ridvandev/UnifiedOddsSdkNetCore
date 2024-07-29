﻿// Copyright (C) Sportradar AG.See LICENSE for full license governing this code

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sportradar.OddsFeed.SDK.Api.Internal.ApiAccess;
using Sportradar.OddsFeed.SDK.Api.Internal.Config;
using Sportradar.OddsFeed.SDK.Common.Exceptions;
using Sportradar.OddsFeed.SDK.Common.Internal;
using Sportradar.OddsFeed.SDK.Entities.Rest.Internal;
using Sportradar.OddsFeed.SDK.Messages.Rest;
using Sportradar.OddsFeed.SDK.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Sportradar.OddsFeed.SDK.Tests.Entities.Rest;

[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
public class LogHttpDataFetcherTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly IncrementalSequenceGenerator _incrementalSequenceGenerator;
    private readonly SdkHttpClient _sdkHttpClient;
    private LogHttpDataFetcher _logHttpDataFetcher;
    private readonly LogHttpDataFetcher _logHttpDataFetcherPool;
    private readonly Uri _badUri = new Uri("http://www.unexisting-url.com");
    private readonly Uri _getUri = new Uri("http://test.domain.com/get");
    private readonly Uri _postUri = new Uri("http://test.domain.com/post");
    private readonly StubMessageHandler _stubMessageHandler;

    public LogHttpDataFetcherTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _stubMessageHandler = new StubMessageHandler(_outputHelper, 100, 50);
        var httpClient = new HttpClient(_stubMessageHandler);
        httpClient.DefaultRequestHeaders.Add(UofSdkBootstrap.HttpClientDefaultRequestHeaderForAccessToken, "aaa");
        httpClient.DefaultRequestHeaders.Add(UofSdkBootstrap.HttpClientDefaultRequestHeaderForUserAgent, "UofSdk-Net");
        _sdkHttpClient = new SdkHttpClient(httpClient);
        var sdkHttpClientPool = new SdkHttpClientPool("aaa", 20, TimeSpan.FromSeconds(5), _stubMessageHandler);

        _incrementalSequenceGenerator = new IncrementalSequenceGenerator(new NullLogger<IncrementalSequenceGenerator>());
        _logHttpDataFetcher = new LogHttpDataFetcher(_sdkHttpClient, _incrementalSequenceGenerator, new Deserializer<response>(), new NullLogger<LogHttpDataFetcher>());
        _logHttpDataFetcherPool = new LogHttpDataFetcher(sdkHttpClientPool, _incrementalSequenceGenerator, new Deserializer<response>(), new NullLogger<LogHttpDataFetcher>());
    }

    [Fact]
    public void SdkHttpClientWithoutAccessTokenFails()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add(UofSdkBootstrap.HttpClientDefaultRequestHeaderForUserAgent, "UofSdk-Net");

        Assert.Throws<InvalidOperationException>(() => new SdkHttpClient(httpClient));
    }

    [Fact]
    public void SdkHttpClientWithoutUserAgentFails()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add(UofSdkBootstrap.HttpClientDefaultRequestHeaderForAccessToken, "aaa");

        Assert.Throws<InvalidOperationException>(() => new SdkHttpClient(httpClient));
    }

    [Fact]
    public async Task PerformanceOf100SequentialRequests()
    {
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < 100; i++)
        {
            var result = await _logHttpDataFetcher.GetDataAsync(_getUri);
            Assert.NotNull(result);
            Assert.True(result.CanRead);
        }
        _outputHelper.WriteLine($"Elapsed {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task PerformanceOfManyParallelRequests()
    {
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task>();
        for (var i = 0; i < 1000; i++)
        {
            var task = _logHttpDataFetcher.GetDataAsync(GetRequestUri(false));
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);

        Assert.True(tasks.TrueForAll(a => a.IsCompletedSuccessfully));
        _outputHelper.WriteLine($"Elapsed {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task PerformancePoolOfManyParallelRequests()
    {
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task>();
        for (var i = 0; i < 1000; i++)
        {
            var task = _logHttpDataFetcherPool.GetDataAsync(GetRequestUri(false));
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);

        Assert.True(tasks.TrueForAll(a => a.IsCompletedSuccessfully));
        _outputHelper.WriteLine($"Elapsed {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task PerformanceOfManyUniqueParallelRequests()
    {
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task>();
        for (var i = 0; i < 1000; i++)
        {
            var task = _logHttpDataFetcher.GetDataAsync(GetRequestUri(true));
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);

        Assert.True(tasks.TrueForAll(a => a.IsCompletedSuccessfully));
        _outputHelper.WriteLine($"Elapsed {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task PerformancePoolOfManyUniqueParallelRequests()
    {
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task>();
        for (var i = 0; i < 1000; i++)
        {
            var task = _logHttpDataFetcherPool.GetDataAsync(GetRequestUri(true));
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);

        Assert.True(tasks.TrueForAll(a => a.IsCompletedSuccessfully));
        _outputHelper.WriteLine($"Elapsed {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task PerformanceOfManyUniqueUriParallelRequests()
    {
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task>();
        for (var i = 0; i < 1000; i++)
        {
            var task = _logHttpDataFetcher.GetDataAsync(GetRandomUri(true));
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);

        Assert.True(tasks.TrueForAll(a => a.IsCompletedSuccessfully));
        _outputHelper.WriteLine($"Elapsed {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task PerformancePoolOfManyUniqueUriParallelRequests()
    {
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task>();
        for (var i = 0; i < 1000; i++)
        {
            var task = _logHttpDataFetcherPool.GetDataAsync(GetRandomUri(true));
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);

        Assert.True(tasks.TrueForAll(a => a.IsCompletedSuccessfully));
        _outputHelper.WriteLine($"Elapsed {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task GetDataAsyncWhneNormalUri()
    {
        // in logRest file there should be result for this call
        var result = await _logHttpDataFetcher.GetDataAsync(_getUri);

        Assert.NotNull(result);
        Assert.True(result.CanRead);

        var s = await new StreamReader(result).ReadToEndAsync();
        Assert.True(!string.IsNullOrEmpty(s));
    }

    [Fact]
    public void GetDataWhenNormalUriOnDebugLog()
    {
        var mockLocker = new Mock<ILogger>();
        mockLocker.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);
        var logHttpDataFetcher = new LogHttpDataFetcher(_sdkHttpClient, _incrementalSequenceGenerator, new Deserializer<response>(), mockLocker.Object);

        var result = logHttpDataFetcher.GetData(_getUri);

        Assert.NotNull(result);
        Assert.True(result.CanRead);

        var s = new StreamReader(result).ReadToEnd();
        Assert.True(!string.IsNullOrEmpty(s));
    }

    [Fact]
    public async Task GetDataAsyncWhenWrongUrlThenReturnsBadGateway()
    {
        _stubMessageHandler.SetWantedResponse(new HttpResponseMessage(HttpStatusCode.BadGateway));
        Stream result = null;

        var e = await Assert.ThrowsAsync<CommunicationException>(async () => result = await _logHttpDataFetcher.GetDataAsync(_badUri));

        Assert.Null(result);
        Assert.IsType<CommunicationException>(e);
        if (e.InnerException != null)
        {
            Assert.IsType<DeserializationException>(e.InnerException);
        }
    }

    [Fact]
    public async Task GetDataAsyncWhenWrongUrlThenThrowsHttpRequestException()
    {
        _stubMessageHandler.SetWantedResponse(new HttpRequestException("any-message", new SocketException(), HttpStatusCode.BadGateway));
        Stream result = null;

        var e = await Assert.ThrowsAsync<CommunicationException>(async () => result = await _logHttpDataFetcher.GetDataAsync(_badUri));

        Assert.Null(result);
        Assert.IsType<CommunicationException>(e);
        if (e.InnerException != null)
        {
            Assert.IsType<HttpRequestException>(e.InnerException);
        }
    }

    [Fact]
    public async Task GetDataAsyncWhenWrongUrlThenThrowsSocketException()
    {
        _stubMessageHandler.SetWantedResponse(new SocketException());
        Stream result = null;

        var e = await Assert.ThrowsAsync<CommunicationException>(async () => result = await _logHttpDataFetcher.GetDataAsync(_badUri));

        Assert.Null(result);
        Assert.IsType<CommunicationException>(e);
        if (e.InnerException != null)
        {
            Assert.IsType<SocketException>(e.InnerException);
        }
    }

    [Fact]
    public async Task PostDataWhenNormalUri()
    {
        var result = await _logHttpDataFetcher.PostDataAsync(_postUri);

        Assert.NotNull(result);
        Assert.True(result.IsSuccessStatusCode);
    }

    [Fact]
    public async Task PostData_DebugLog()
    {
        var httpClient = new HttpClient(_stubMessageHandler);
        httpClient.DefaultRequestHeaders.Add(UofSdkBootstrap.HttpClientDefaultRequestHeaderForAccessToken, "aaa");
        httpClient.DefaultRequestHeaders.Add(UofSdkBootstrap.HttpClientDefaultRequestHeaderForUserAgent, "UofSdk-Net");
        var sdkHttpClient = new SdkHttpClient(httpClient);
        var mockLocker = new Mock<ILogger>();
        mockLocker.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);
        var logHttpDataFetcher = new LogHttpDataFetcher(sdkHttpClient, _incrementalSequenceGenerator, new Deserializer<response>(), mockLocker.Object);

        var result = await logHttpDataFetcher.PostDataAsync(_getUri);

        Assert.NotNull(result);
        Assert.True(result.IsSuccessStatusCode);
    }

    [Fact]
    public async Task PostDataWhenDebugLogWithContent()
    {
        var mockLocker = new Mock<ILogger>();
        mockLocker.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);
        var logHttpDataFetcher = new LogHttpDataFetcher(_sdkHttpClient, _incrementalSequenceGenerator, new Deserializer<response>(), mockLocker.Object);

        var result = await logHttpDataFetcher.PostDataAsync(_getUri, new StringContent("test string"));

        Assert.NotNull(result);
        Assert.True(result.IsSuccessStatusCode);
    }

    [Fact]
    public async Task PostDataWhenDebugLogWithContentThenFails()
    {
        _stubMessageHandler.SetWantedResponse(new HttpResponseMessage(HttpStatusCode.BadGateway));
        var mockLocker = new Mock<ILogger>();
        mockLocker.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);
        var logHttpDataFetcher = new LogHttpDataFetcher(_sdkHttpClient, _incrementalSequenceGenerator, new Deserializer<response>(), mockLocker.Object);

        var result = await logHttpDataFetcher.PostDataAsync(_getUri, new StringContent("test string"));

        Assert.NotNull(result);
        Assert.False(result.IsSuccessStatusCode);
    }

    [Fact]
    public async Task PostDataWhenDebugLogWithContentAndNullUrlThenFails()
    {
        _stubMessageHandler.SetWantedResponse(new HttpResponseMessage(HttpStatusCode.BadGateway));
        var mockLocker = new Mock<ILogger>();
        mockLocker.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);
        var logHttpDataFetcher = new LogHttpDataFetcher(_sdkHttpClient, _incrementalSequenceGenerator, new Deserializer<response>(), mockLocker.Object);

        HttpResponseMessage result = null;
        var ex = await Assert.ThrowsAsync<NullReferenceException>(async () => result = await logHttpDataFetcher.PostDataAsync(null, new StringContent("test string")));

        Assert.Null(result);
        Assert.IsType<NullReferenceException>(ex);
    }

    [Fact]
    public async Task PostDataWhenEmptyResponseContent()
    {
        _stubMessageHandler.SetWantedResponse(new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new ByteArrayContent(Array.Empty<byte>())
        });
        var result = await _logHttpDataFetcher.PostDataAsync(_postUri);

        Assert.NotNull(result);
        Assert.True(result.IsSuccessStatusCode);
    }

    [Fact]
    public async Task PostDataAsyncTestWithWrongUrl()
    {
        _stubMessageHandler.SetWantedResponse(new HttpRequestException("any-message", new SocketException(), HttpStatusCode.BadGateway));
        HttpResponseMessage result = null;

        var ex = await Assert.ThrowsAsync<CommunicationException>(async () => result = await _logHttpDataFetcher.PostDataAsync(_badUri));

        Assert.Null(result);
        Assert.IsType<CommunicationException>(ex);
        if (ex.InnerException != null)
        {
            Assert.IsType<HttpRequestException>(ex.InnerException);
        }
    }

    [Fact]
    public async Task PostDataAsyncWhenWithContent()
    {
        var result = await _logHttpDataFetcher.PostDataAsync(_postUri, new StringContent("test string"));

        Assert.NotNull(result);
        Assert.True(result.IsSuccessStatusCode);
    }

    //[Fact]
    public async Task ConsecutivePostFailure()
    {
        const int loopCount = 10;
        var errCount = 0;
        var allErrCount = 0;
        _stubMessageHandler.SetWantedResponse(new HttpRequestException("any-message", new SocketException(), HttpStatusCode.BadGateway));
        for (var i = 0; i < loopCount; i++)
        {
            try
            {
                var result = await _logHttpDataFetcher.PostDataAsync(_badUri);
                Assert.NotNull(result);
                Assert.True(result.IsSuccessStatusCode);
            }
            catch (Exception e)
            {
                allErrCount++;
                if (e.InnerException?.Message == "Failed to execute request due to previous failures.")
                {
                    errCount++;
                }
                _outputHelper.WriteLine(e.ToString());
            }
            Assert.Equal(i, allErrCount - 1);
        }
        Assert.Equal(loopCount - 5, errCount);
    }

    //[Fact]
    public async Task ConsecutivePostAndGetFailure()
    {
        const int loopCount = 10;
        var errCount = 0;
        var allPostErrCount = 0;
        var allGetErrCount = 0;
        //_httpClient.DataFetcher.UriReplacements.Add(new Tuple<string, string>(_badUri.ToString(), "-1"));
        //_httpClient.DataFetcher.PostResponses.Add(new Tuple<string, int, HttpResponseMessage>(_badUri.ToString(), -1, null));
        _stubMessageHandler.SetWantedResponse(new HttpResponseMessage(HttpStatusCode.BadGateway));

        for (var i = 0; i < loopCount; i++)
        {
            try
            {
                var result = await _logHttpDataFetcher.PostDataAsync(_badUri);
                Assert.NotNull(result);
                Assert.False(result.IsSuccessStatusCode);
            }
            catch (Exception e)
            {
                allPostErrCount++;
                if (e.InnerException?.Message == "Failed to execute request due to previous failures.")
                {
                    errCount++;
                }
                _outputHelper.WriteLine(e.ToString());
            }
            Assert.Equal(i, allPostErrCount - 1);

            try
            {
                var result = await _logHttpDataFetcher.GetDataAsync(_badUri);
                Assert.NotNull(result);
            }
            catch (Exception e)
            {
                allGetErrCount++;
                if (e.InnerException?.Message == "Failed to execute request due to previous failures.")
                {
                    errCount++;
                }
                _outputHelper.WriteLine(e.ToString());
            }
            Assert.Equal(i, allGetErrCount - 1);
        }
        Assert.Equal((loopCount * 2) - 5, errCount);
    }

    //[Fact]
    public async Task ExceptionAfterConsecutivePostFailures()
    {
        await ConsecutivePostFailure();
        try
        {
            var result = new HttpResponseMessage();
            await Assert.ThrowsAsync<CommunicationException>(async () => result = await _logHttpDataFetcher.PostDataAsync(_getUri));
            Assert.NotNull(result);
            Assert.False(result.IsSuccessStatusCode);
        }
        catch (Exception e)
        {
            if (e.InnerException is CommunicationException)
            {
                throw e.InnerException;
            }
        }
    }

    //[Fact]
    public async Task SuccessAfterConsecutiveFailuresResets()
    {
        var httpClient = new TestHttpClient();
        _logHttpDataFetcher = new LogHttpDataFetcher(httpClient, _incrementalSequenceGenerator, new Deserializer<response>(), new NullLogger<LogHttpDataFetcher>(), 5, 1);
        await ConsecutivePostFailure();
        await Task.Delay(1000);
        var result = await _logHttpDataFetcher.GetDataAsync(_getUri);
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    private static Uri GetRequestUri(bool isRandom)
    {
        var id = isRandom ? SdkInfo.GetRandom() : 1;
        return new Uri($"http://test.domain.com/api/v1/sr:match:{id}/summary.xml");
    }

    private static Uri GetRandomUri(bool isRandom)
    {
        var id = isRandom ? SdkInfo.GetRandom() : 1;
        return new Uri($"http://test.domain.com/api/v1/sr:match:{id}.xml");
    }
}
