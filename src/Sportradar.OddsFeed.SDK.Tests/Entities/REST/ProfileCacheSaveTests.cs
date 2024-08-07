﻿/*
* Copyright (C) Sportradar AG. See LICENSE for full license governing this code
*/

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sportradar.OddsFeed.SDK.Common;
using Sportradar.OddsFeed.SDK.Entities.Rest.Internal.Caching.CI;
using Sportradar.OddsFeed.SDK.Entities.Rest.Internal.Dto;
using Sportradar.OddsFeed.SDK.Entities.Rest.Internal.Enums;
using Sportradar.OddsFeed.SDK.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Sportradar.OddsFeed.SDK.Tests.Entities.Rest;

public class ProfileCacheSaveTests
{
    private readonly ScheduleData _scheduleData;
    private readonly TestSportEntityFactoryBuilder _sportEntityFactoryBuilder;

    public ProfileCacheSaveTests(ITestOutputHelper outputHelper)
    {
        _sportEntityFactoryBuilder = new TestSportEntityFactoryBuilder(outputHelper, ScheduleData.Cultures3);
        _scheduleData = new ScheduleData(new TestSportEntityFactoryBuilder(outputHelper, ScheduleData.Cultures3), outputHelper);
    }

    private static Urn CreateSimpleTeamUrn(int competitorId)
    {
        return new Urn("sr", "simple_team", competitorId);
    }

    [Fact]
    public void SimpleTeamProfileGetsCached()
    {
        Assert.NotNull(_sportEntityFactoryBuilder.ProfileMemoryCache);
        Assert.Equal(0, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());

        var competitorNames = _sportEntityFactoryBuilder.ProfileCache.GetCompetitorProfileAsync(CreateSimpleTeamUrn(1), ScheduleData.Cultures3, true).GetAwaiter().GetResult();

        Assert.NotNull(competitorNames);
        Assert.Equal(1, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());

        Assert.Equal(ScheduleData.Cultures3.Count, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointCompetitor));

        //if we call again, should not fetch again
        competitorNames = _sportEntityFactoryBuilder.ProfileCache.GetCompetitorProfileAsync(CreateSimpleTeamUrn(1), ScheduleData.Cultures3, true).GetAwaiter().GetResult();
        Assert.NotNull(competitorNames);
        Assert.Equal(1, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());

        Assert.Equal(ScheduleData.Cultures3.Count, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointCompetitor));
    }

    [Fact]
    public void NumberOfSimpleTeamProviderCallsMatchIsCorrect()
    {
        var competitorNames1 = _sportEntityFactoryBuilder.ProfileCache.GetCompetitorProfileAsync(CreateSimpleTeamUrn(1), ScheduleData.Cultures3, true).GetAwaiter().GetResult();
        Assert.NotNull(competitorNames1);

        Assert.Equal(ScheduleData.Cultures3.Count, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointCompetitor));

        var competitorNames2 = _sportEntityFactoryBuilder.ProfileCache.GetCompetitorProfileAsync(CreateSimpleTeamUrn(2), ScheduleData.Cultures3, true).GetAwaiter().GetResult();
        Assert.NotNull(competitorNames2);

        Assert.NotEqual(competitorNames1.Id, competitorNames2.Id);
        Assert.Equal(ScheduleData.Cultures3.Count * 2, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointCompetitor));
    }

    [Fact]
    public void SimpleTeamIsCachedWithoutBetradarId()
    {
        Assert.Equal(0, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());
        var competitorNamesCacheItem = _sportEntityFactoryBuilder.ProfileCache.GetCompetitorProfileAsync(CreateSimpleTeamUrn(1), ScheduleData.Cultures3, true).GetAwaiter().GetResult();
        Assert.NotNull(competitorNamesCacheItem);
        Assert.NotNull(competitorNamesCacheItem.ReferenceId);
        Assert.NotNull(competitorNamesCacheItem.ReferenceId.ReferenceIds);
        Assert.True(competitorNamesCacheItem.ReferenceId.ReferenceIds.Any());
        Assert.Single(competitorNamesCacheItem.ReferenceId.ReferenceIds);
        Assert.Equal(1, competitorNamesCacheItem.ReferenceId.BetradarId);
    }

    [Fact]
    public void SimpleTeamIsCachedWithoutReferenceIds()
    {
        Assert.Equal(0, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());
        var simpleTeamDto = CacheSimpleTeam(1, null);

        var competitorNamesCacheItem = _sportEntityFactoryBuilder.ProfileCache.GetCompetitorProfileAsync(simpleTeamDto.Competitor.Id, ScheduleData.Cultures3, true).GetAwaiter().GetResult();
        Assert.NotNull(competitorNamesCacheItem);
        Assert.Equal(simpleTeamDto.Competitor.Id, competitorNamesCacheItem.Id);
        Assert.NotNull(competitorNamesCacheItem.ReferenceId);
        Assert.NotNull(competitorNamesCacheItem.ReferenceId.ReferenceIds);
        Assert.True(competitorNamesCacheItem.ReferenceId.ReferenceIds.Any());
        Assert.Single(competitorNamesCacheItem.ReferenceId.ReferenceIds);
        Assert.Equal(simpleTeamDto.Competitor.Id.Id, competitorNamesCacheItem.ReferenceId.BetradarId);
    }

    [Fact]
    public void SimpleTeamIsCachedWithBetradarId()
    {
        Assert.Equal(0, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());
        var competitorNamesCacheItem = _sportEntityFactoryBuilder.ProfileCache.GetCompetitorProfileAsync(CreateSimpleTeamUrn(2), ScheduleData.Cultures3, true).GetAwaiter().GetResult();
        Assert.NotNull(competitorNamesCacheItem);
        Assert.NotNull(competitorNamesCacheItem.ReferenceId);
        Assert.NotNull(competitorNamesCacheItem.ReferenceId.ReferenceIds);
        Assert.True(competitorNamesCacheItem.ReferenceId.ReferenceIds.Any());
        Assert.Equal(2, competitorNamesCacheItem.ReferenceId.ReferenceIds.Count);
        Assert.Equal("555", competitorNamesCacheItem.ReferenceId.BetradarId.ToString());
    }

    [Fact]
    public void SimpleTeamCanBeRemovedFromCache()
    {
        Assert.Equal(0, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());
        var simpleTeamDto = CacheSimpleTeam(654321, null);

        Assert.Equal(1, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());
        var cacheItem = (CompetitorCacheItem)_sportEntityFactoryBuilder.ProfileMemoryCache.Get(simpleTeamDto.Competitor.Id.ToString());
        Assert.NotNull(cacheItem);
        Assert.Equal(simpleTeamDto.Competitor.Id, cacheItem.Id);

        _sportEntityFactoryBuilder.CacheManager.RemoveCacheItem(simpleTeamDto.Competitor.Id, CacheItemType.Competitor, "Test");
        Assert.Equal(0, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());
        cacheItem = (CompetitorCacheItem)_sportEntityFactoryBuilder.ProfileMemoryCache.Get(simpleTeamDto.Competitor.Id.ToString());
        Assert.Null(cacheItem);
    }

    [Fact]
    public void CachePlayerDataFromPlayerProfile()
    {
        _sportEntityFactoryBuilder.ProfileCache.GetPlayerProfileAsync(ScheduleData.MatchCompetitor1PlayerId1, ScheduleData.Cultures1, true).GetAwaiter().GetResult();
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.TotalRestCalls);
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointPlayerProfile));
        Assert.Equal(1, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());

        var playerNames = _sportEntityFactoryBuilder.ProfileCache.GetPlayerNamesAsync(ScheduleData.MatchCompetitor1PlayerId1, ScheduleData.Cultures3, false).GetAwaiter().GetResult();
        Assert.NotNull(playerNames);
        Assert.Equal(_scheduleData.MatchCompetitor1Player1.GetName(ScheduleData.Cultures3.First()), playerNames[ScheduleData.Cultures3.First()]);
        Assert.Equal(string.Empty, playerNames[ScheduleData.Cultures3.Skip(1).First()]);
        Assert.Equal(string.Empty, playerNames[ScheduleData.Cultures3.Skip(2).First()]);
        Assert.Equal(1, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.TotalRestCalls);
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointPlayerProfile));

        playerNames = _sportEntityFactoryBuilder.ProfileCache.GetPlayerNamesAsync(ScheduleData.MatchCompetitor1PlayerId1, ScheduleData.Cultures3, true).GetAwaiter().GetResult();
        Assert.NotNull(playerNames);
        Assert.Equal(_scheduleData.MatchCompetitor1Player1.GetName(ScheduleData.Cultures3.First()), playerNames[ScheduleData.Cultures3.First()]);
        Assert.Equal(_scheduleData.MatchCompetitor1Player1.GetName(ScheduleData.Cultures3.Skip(1).First()), playerNames[ScheduleData.Cultures3.Skip(1).First()]);
        Assert.Equal(_scheduleData.MatchCompetitor1Player1.GetName(ScheduleData.Cultures3.Skip(2).First()), playerNames[ScheduleData.Cultures3.Skip(2).First()]);
        Assert.Equal(1, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());
        Assert.Equal(3, _sportEntityFactoryBuilder.DataRouterManager.TotalRestCalls);
        Assert.Equal(3, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointPlayerProfile));
    }

    [Fact]
    public void CacheCompetitorDataFromCompetitorProfile()
    {
        _sportEntityFactoryBuilder.ProfileCache.GetCompetitorProfileAsync(ScheduleData.MatchCompetitorId1, ScheduleData.Cultures1, true).GetAwaiter().GetResult();
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.TotalRestCalls);
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointCompetitor));
        Assert.Equal(ScheduleData.MatchCompetitor1PlayerCount + 1, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());

        var competitorNames = _sportEntityFactoryBuilder.ProfileCache.GetCompetitorNamesAsync(ScheduleData.MatchCompetitorId1, ScheduleData.Cultures3, false).GetAwaiter().GetResult();
        Assert.NotNull(competitorNames);
        Assert.Equal(_scheduleData.MatchCompetitor1.GetName(ScheduleData.Cultures3.First()), competitorNames[ScheduleData.Cultures3.First()]);
        Assert.Equal(string.Empty, competitorNames[ScheduleData.Cultures3.Skip(1).First()]);
        Assert.Equal(string.Empty, competitorNames[ScheduleData.Cultures3.Skip(2).First()]);
        Assert.Equal(ScheduleData.MatchCompetitor1PlayerCount + 1, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.TotalRestCalls);
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointCompetitor));

        competitorNames = _sportEntityFactoryBuilder.ProfileCache.GetCompetitorNamesAsync(ScheduleData.MatchCompetitorId1, ScheduleData.Cultures3, true).GetAwaiter().GetResult();
        Assert.NotNull(competitorNames);
        Assert.Equal(_scheduleData.MatchCompetitor1.GetName(ScheduleData.Cultures3.First()), competitorNames[ScheduleData.Cultures3.First()]);
        Assert.Equal(_scheduleData.MatchCompetitor1.GetName(ScheduleData.Cultures3.Skip(1).First()), competitorNames[ScheduleData.Cultures3.Skip(1).First()]);
        Assert.Equal(_scheduleData.MatchCompetitor1.GetName(ScheduleData.Cultures3.Skip(2).First()), competitorNames[ScheduleData.Cultures3.Skip(2).First()]);
        Assert.Equal(ScheduleData.MatchCompetitor1PlayerCount + 1, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());
        Assert.Equal(3, _sportEntityFactoryBuilder.DataRouterManager.TotalRestCalls);
        Assert.Equal(3, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointCompetitor));
    }

    [Fact]
    public void CacheCompetitorDataFromMatchSummary()
    {
        _sportEntityFactoryBuilder.DataRouterManager.GetSportEventSummaryAsync(ScheduleData.MatchId, ScheduleData.CultureEn, null).GetAwaiter().GetResult();
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.TotalRestCalls);
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointSportEventSummary));
        Assert.Equal(2, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());

        var competitorNames = _sportEntityFactoryBuilder.ProfileCache.GetCompetitorNamesAsync(ScheduleData.MatchCompetitorId1, ScheduleData.Cultures3, false).GetAwaiter().GetResult();
        Assert.NotNull(competitorNames);
        Assert.Equal(_scheduleData.MatchCompetitor1.GetName(ScheduleData.Cultures3.First()), competitorNames[ScheduleData.Cultures3.First()]);
        Assert.Equal(string.Empty, competitorNames[ScheduleData.Cultures3.Skip(1).First()]);
        Assert.Equal(string.Empty, competitorNames[ScheduleData.Cultures3.Skip(2).First()]);
        Assert.Equal(2, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.TotalRestCalls);
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointSportEventSummary));

        competitorNames = _sportEntityFactoryBuilder.ProfileCache.GetCompetitorNamesAsync(ScheduleData.MatchCompetitorId1, ScheduleData.Cultures3, true).GetAwaiter().GetResult();
        Assert.NotNull(competitorNames);
        Assert.Equal(_scheduleData.MatchCompetitor1.GetName(ScheduleData.Cultures3.First()), competitorNames[ScheduleData.Cultures3.First()]);
        Assert.Equal(_scheduleData.MatchCompetitor1.GetName(ScheduleData.Cultures3.Skip(1).First()), competitorNames[ScheduleData.Cultures3.Skip(1).First()]);
        Assert.Equal(_scheduleData.MatchCompetitor1.GetName(ScheduleData.Cultures3.Skip(2).First()), competitorNames[ScheduleData.Cultures3.Skip(2).First()]);
        Assert.Equal(2, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());
        Assert.Equal(3, _sportEntityFactoryBuilder.DataRouterManager.TotalRestCalls);
        Assert.Equal(3, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointSportEventSummary));
    }

    [Fact]
    public void CacheCompetitorDataFromTournamentSummary()
    {
        _sportEntityFactoryBuilder.DataRouterManager.GetSportEventSummaryAsync(ScheduleData.MatchTournamentId, ScheduleData.CultureEn, null).GetAwaiter().GetResult();
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.TotalRestCalls);
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointSportEventSummary));
        Assert.Equal(ScheduleData.MatchTournamentCompetitorCount, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());

        var competitorNames = _sportEntityFactoryBuilder.ProfileCache.GetCompetitorNamesAsync(ScheduleData.MatchCompetitorId1, ScheduleData.Cultures3, false).GetAwaiter().GetResult();
        Assert.NotNull(competitorNames);
        Assert.Equal(_scheduleData.MatchCompetitor1.GetName(ScheduleData.Cultures3.First()), competitorNames[ScheduleData.Cultures3.First()]);
        Assert.Equal(string.Empty, competitorNames[ScheduleData.Cultures3.Skip(1).First()]);
        Assert.Equal(string.Empty, competitorNames[ScheduleData.Cultures3.Skip(2).First()]);
        Assert.Equal(ScheduleData.MatchTournamentCompetitorCount, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.TotalRestCalls);
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointSportEventSummary));

        competitorNames = _sportEntityFactoryBuilder.ProfileCache.GetCompetitorNamesAsync(ScheduleData.MatchCompetitorId1, ScheduleData.Cultures3, true).GetAwaiter().GetResult();
        Assert.NotNull(competitorNames);
        Assert.Equal(_scheduleData.MatchCompetitor1.GetName(ScheduleData.Cultures3.First()), competitorNames[ScheduleData.Cultures3.First()]);
        Assert.Equal(_scheduleData.MatchCompetitor1.GetName(ScheduleData.Cultures3.Skip(1).First()), competitorNames[ScheduleData.Cultures3.Skip(1).First()]);
        Assert.Equal(_scheduleData.MatchCompetitor1.GetName(ScheduleData.Cultures3.Skip(2).First()), competitorNames[ScheduleData.Cultures3.Skip(2).First()]);
        Assert.Equal(ScheduleData.MatchTournamentCompetitorCount, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());
        Assert.Equal(3, _sportEntityFactoryBuilder.DataRouterManager.TotalRestCalls);
        Assert.Equal(3, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointSportEventSummary));
    }

    [Fact]
    public void CacheCompetitorDataFromSeasonSummary()
    {
        _sportEntityFactoryBuilder.DataRouterManager.GetSportEventSummaryAsync(ScheduleData.MatchSeasonId, ScheduleData.CultureEn, null).GetAwaiter().GetResult();
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.TotalRestCalls);
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointSportEventSummary));
        Assert.Equal(ScheduleData.MatchSeasonCompetitorCount, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());

        var competitorNames = _sportEntityFactoryBuilder.ProfileCache.GetCompetitorNamesAsync(ScheduleData.MatchCompetitorId1, ScheduleData.Cultures3, false).GetAwaiter().GetResult();
        Assert.NotNull(competitorNames);
        Assert.Equal(_scheduleData.MatchCompetitor1.GetName(ScheduleData.Cultures3.First()), competitorNames[ScheduleData.Cultures3.First()]);
        Assert.Equal(string.Empty, competitorNames[ScheduleData.Cultures3.Skip(1).First()]);
        Assert.Equal(string.Empty, competitorNames[ScheduleData.Cultures3.Skip(2).First()]);
        Assert.Equal(ScheduleData.MatchSeasonCompetitorCount, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.TotalRestCalls);
        Assert.Equal(1, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointSportEventSummary));

        competitorNames = _sportEntityFactoryBuilder.ProfileCache.GetCompetitorNamesAsync(ScheduleData.MatchCompetitorId1, ScheduleData.Cultures3, true).GetAwaiter().GetResult();
        Assert.NotNull(competitorNames);
        Assert.Equal(_scheduleData.MatchCompetitor1.GetName(ScheduleData.Cultures3.First()), competitorNames[ScheduleData.Cultures3.First()]);
        Assert.Equal(_scheduleData.MatchCompetitor1.GetName(ScheduleData.Cultures3.Skip(1).First()), competitorNames[ScheduleData.Cultures3.Skip(1).First()]);
        Assert.Equal(_scheduleData.MatchCompetitor1.GetName(ScheduleData.Cultures3.Skip(2).First()), competitorNames[ScheduleData.Cultures3.Skip(2).First()]);
        Assert.Equal(ScheduleData.MatchSeasonCompetitorCount, _sportEntityFactoryBuilder.ProfileMemoryCache.Count());
        Assert.Equal(3, _sportEntityFactoryBuilder.DataRouterManager.TotalRestCalls);
        Assert.Equal(3, _sportEntityFactoryBuilder.DataRouterManager.GetCallCount(TestDataRouterManager.EndpointSportEventSummary));
    }

    private SimpleTeamProfileDto CacheSimpleTeam(int id, IDictionary<string, string> referenceIds)
    {
        var simpleTeam = MessageFactoryRest.GetSimpleTeamCompetitorProfileEndpoint(id, referenceIds);
        var simpleTeamDto = new SimpleTeamProfileDto(simpleTeam);
        _sportEntityFactoryBuilder.CacheManager.SaveDto(simpleTeamDto.Competitor.Id, simpleTeamDto, CultureInfo.CurrentCulture, DtoType.SimpleTeamProfile, null);
        return simpleTeamDto;
    }
}
