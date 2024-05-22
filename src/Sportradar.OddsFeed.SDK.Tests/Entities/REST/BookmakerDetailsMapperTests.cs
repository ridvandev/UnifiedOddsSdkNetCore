﻿// Copyright (C) Sportradar AG.See LICENSE for full license governing this code

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Sportradar.OddsFeed.SDK.Api.Internal;
using Sportradar.OddsFeed.SDK.Entities.Rest;
using Sportradar.OddsFeed.SDK.Entities.Rest.Internal;
using Sportradar.OddsFeed.SDK.Entities.Rest.Internal.Dto;
using Sportradar.OddsFeed.SDK.Entities.Rest.Internal.EntitiesImpl;
using Sportradar.OddsFeed.SDK.Entities.Rest.Internal.Mapping;
using Sportradar.OddsFeed.SDK.Messages.Rest;
using Sportradar.OddsFeed.SDK.Tests.Common;
using Xunit;

namespace Sportradar.OddsFeed.SDK.Tests.Entities.Rest;

[SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method")]
public class BookmakerDetailsMapperTests
{
    private const string InputXml = "bookmaker_details.xml";
    private readonly BookmakerDetailsDto _entity;
    private readonly BookmakerDetailsFetcher _bookmakerDetailsFetcher;

    public BookmakerDetailsMapperTests()
    {
        var deserializer = new Deserializer<bookmaker_details>();
        var dataFetcher = new TestDataFetcher();
        var mapperFactory = new BookmakerDetailsMapperFactory();

        var dataProvider = new DataProvider<bookmaker_details, BookmakerDetailsDto>(
            TestData.RestXmlPath + InputXml,
            dataFetcher,
            deserializer,
            mapperFactory);
        _entity = dataProvider.GetDataAsync("", TestData.Culture.TwoLetterISOLanguageName).GetAwaiter().GetResult();

        _bookmakerDetailsFetcher = new BookmakerDetailsFetcher(dataProvider);
    }

    [Fact]
    public void TestInstanceIsNotNull()
    {
        Assert.NotNull(_entity);
    }

    [Fact]
    public void Mapping()
    {
        var details = new BookmakerDetails(_entity);
        ValidateBookmakerDetailsFromXml(details);
    }

    [Fact]
    public void WhoAmI()
    {
        var details = _bookmakerDetailsFetcher.WhoAmIAsync().GetAwaiter().GetResult();
        ValidateBookmakerDetailsFromXml(details);
    }

    private static void ValidateBookmakerDetailsFromXml(IBookmakerDetails details)
    {
        Assert.Equal(TestData.BookmakerId, details.BookmakerId);
        Assert.Equal(TestData.VirtualHost, details.VirtualHost);
        Assert.Null(details.Message);
        Assert.Equal(HttpStatusCode.OK, details.ResponseCode);
        Assert.Equal(DateTime.Parse("2016-07-26T17:44:24Z").ToUniversalTime(), details.ExpireAt.ToUniversalTime());
    }
}
