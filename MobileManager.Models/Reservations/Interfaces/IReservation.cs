using System;
using MobileManager.Models.Reservations.enums;

namespace MobileManager.Models.Reservations.Interfaces
{
    /// <summary>
    /// Reservation.
    /// </summary>
    public interface IReservation
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        string Id { get; set; }

        /// <summary>
        /// Gets or sets the date created.
        /// </summary>
        /// <value>The date created.</value>
        DateTime DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the failed to apply counter.
        /// </summary>
        /// <value>The failed to apply.</value>
        int FailedToApply { get; set; }

        /// <summary>
        /// Gets or sets the ReservationType flag.
        /// </summary>
        ReservationType  ReservationType { get; set; }
    }
}
