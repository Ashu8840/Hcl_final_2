using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HotelBookingAPI.Models;

public partial class Room
{
    public int Id { get; set; }

    public int HotelId { get; set; }

    public string RoomNumber { get; set; } = null!;

    public decimal PricePerNight { get; set; }

    public int Capacity { get; set; }

    public bool? IsAvailable { get; set; }

    [JsonIgnore]
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Hotel Hotel { get; set; } = null!;

    public virtual ICollection<Facility> Facilities { get; set; } = new List<Facility>();
}
