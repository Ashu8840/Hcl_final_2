using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HotelBookingAPI.Models;

public partial class Facility
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
