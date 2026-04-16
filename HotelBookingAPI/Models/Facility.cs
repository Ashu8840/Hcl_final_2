using System;
using System.Collections.Generic;

namespace HotelBookingAPI.Models;

public partial class Facility
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
