// Copyright (C) Sportradar AG.See LICENSE for full license governing this code

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Sportradar.OddsFeed.SDK.Entities.Rest.Enums;

namespace Sportradar.OddsFeed.SDK.Entities.Rest
{
    /// <summary>
    /// Defines a contract implemented by classes representing sport events regardless to which sport they belong
    /// </summary>
    public interface ICompetition : ISportEvent
    {
        /// <summary>
        /// Gets a <see cref="ICompetitionStatus"/> instance containing information about the progress of a sport event associated with the current instance
        /// </summary>
        /// <returns>A <see cref="ICompetitionStatus"/> instance containing information about the progress of the sport event</returns>
        Task<ICompetitionStatus> GetStatusAsync();

        /// <summary>
        /// Asynchronously gets a <see cref="BookingStatus"/> enum member providing booking status for the associated entity or a null reference if booking status is not known
        /// </summary>
        /// <returns>Returns a <see cref="BookingStatus"/> enum member providing booking status for the associated entity or a null reference if booking status is not known</returns>
        Task<BookingStatus?> GetBookingStatusAsync();

        /// <summary>
        /// Asynchronously gets a <see cref="IVenue"/> instance representing a venue where the sport event associated with the
        /// current instance will take place
        /// </summary>
        /// <returns>A <see cref="Task{IVenue}"/> representing the retrieval operation</returns>
        Task<IVenue> GetVenueAsync();

        /// <summary>
        /// Asynchronously gets a <see cref="ISportEventConditions"/> instance representing live conditions of the sport event associated with the current instance
        /// </summary>
        /// <returns>A <see cref="Task{IVenue}"/> representing the retrieval operation</returns>
        /// <remarks>A Fixture is a sport event that has been arranged for a particular time and place</remarks>
        Task<ISportEventConditions> GetConditionsAsync();

        /// <summary>
        /// Asynchronously gets a <see cref="IEnumerable{T}"/> representing competitors in the sport event associated with the current instance
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the retrieval operation</returns>
        Task<IEnumerable<ICompetitor>> GetCompetitorsAsync();

        /// <summary>
        /// Gets the event status asynchronous
        /// </summary>
        /// <returns>Get the event status</returns>
        Task<EventStatus?> GetEventStatusAsync();

        /// <summary>
        /// Asynchronously gets a <see cref="SportEventType"/> for the associated sport event.
        /// </summary>
        /// <returns>A <see cref="SportEventType"/> for the associated sport event.</returns>
        Task<SportEventType?> GetSportEventTypeAsync();

        /// <summary>
        /// Asynchronously gets a liveOdds
        /// </summary>
        /// <returns>A liveOdds</returns>
        Task<string> GetLiveOddsAsync();

        /// <summary>
        /// Asynchronously gets a <see cref="IEnumerable{T}"/> representing competitors in the sport event associated with the current instance
        /// </summary>
        /// <param name="culture">The culture in which we want to return competitor data</param>
        /// <returns>A <see cref="Task{T}"/> representing the retrieval operation</returns>
        Task<IEnumerable<ICompetitor>> GetCompetitorsAsync(CultureInfo culture);
    }
}
