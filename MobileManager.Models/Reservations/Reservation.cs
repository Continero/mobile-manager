using System;
using System.Collections.Generic;
using MobileManager.Models.Devices;
using MobileManager.Models.Reservations.enums;
using MobileManager.Models.Reservations.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MobileManager.Models.Reservations
{
    /// <inheritdoc />
    /// <summary>
    /// Reservation.
    /// </summary>
    public class Reservation : IReservation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:MobileManager.Models.Reservations.Reservation"/> class.
        /// </summary>
        public Reservation()
        {
            Id = Guid.NewGuid().ToString("n").Substring(0, 24);
            DateCreated = DateTime.Now;
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Gets or sets the date created.
        /// </summary>
        /// <value>The date created.</value>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the requested devices.
        /// </summary>
        /// <value>The requested devices.</value>

        //[ForeignKey("ReservationRefId")]
        public List<RequestedDevices> RequestedDevices { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Gets or sets the failed to apply counter.
        /// </summary>
        /// <value>The failed to apply.</value>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int FailedToApply { get; set; }


        /// <inheritdoc />
        [JsonConverter(typeof(StringEnumConverter))]
        public ReservationType ReservationType { get; set; } = ReservationType.Appium;
    }
}
