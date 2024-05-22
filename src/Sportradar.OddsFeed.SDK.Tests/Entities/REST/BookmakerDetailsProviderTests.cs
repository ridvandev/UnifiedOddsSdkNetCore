﻿// Copyright (C) Sportradar AG.See LICENSE for full license governing this code

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Sportradar.OddsFeed.SDK.Api.Internal.ApiAccess;
using Sportradar.OddsFeed.SDK.Common.Exceptions;
using Sportradar.OddsFeed.SDK.Entities.Rest.Internal;
using Sportradar.OddsFeed.SDK.Entities.Rest.Internal.Dto;
using Sportradar.OddsFeed.SDK.Entities.Rest.Internal.Mapping;
using Sportradar.OddsFeed.SDK.Messages.Rest;
using Sportradar.OddsFeed.SDK.Tests.Common;
using Xunit;

namespace Sportradar.OddsFeed.SDK.Tests.Entities.Rest;

[SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
[SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method")]
public class BookmakerDetailsProviderTests
{
    private const string InputXml = "whoami.xml";

    private static IDataProvider<BookmakerDetailsDto> BuildProvider(string apiKey)
    {
        var fetcher = string.IsNullOrEmpty(apiKey)
            ? new TestDataFetcher()
            : (IDataFetcher)new HttpDataFetcher(new TestHttpClient(), new Deserializer<response>());
        var url = string.IsNullOrEmpty(apiKey)
            ? TestData.RestXmlPath
            : "https://api.betradar.com/v1/users/";
        var deserializer = new Deserializer<bookmaker_details>();
        var mapperFactory = new BookmakerDetailsMapperFactory();

        return new DataProvider<bookmaker_details, BookmakerDetailsDto>(
            url + InputXml,
            fetcher,
            deserializer,
            mapperFactory);
    }

    [Fact]
    public void CorrectKeyProducesResponseStatusOk()
    {
        var provider = BuildProvider(null);
        var dto = provider.GetDataAsync(new string[1]).GetAwaiter().GetResult();

        Assert.NotNull(dto);
        Assert.Equal(HttpStatusCode.OK, dto.ResponseCode);
    }

    //TODO requires network
    //[Fact]
    public void IncorrectKeyProducesResponseCodeForbidden()
    {
        var provider = BuildProvider("aaaaaaaaaaa");
        CommunicationException exception = null;
        try
        {
            var dto = provider.GetDataAsync(new string[1]).GetAwaiter().GetResult();
            Assert.NotNull(dto);
        }
        catch (AggregateException ex)
        {
            exception = (CommunicationException)ex.InnerException;
        }

        Assert.NotNull(exception);
        Assert.Equal(HttpStatusCode.Forbidden, exception.ResponseCode);
    }
}
