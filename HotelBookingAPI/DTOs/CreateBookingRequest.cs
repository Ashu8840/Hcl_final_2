namespace HotelBookingAPI.DTOs;

public class CreateBookingRequest
{
    public int RoomId { get; set; }

    public DateTime CheckInDate { get; set; }

    public DateTime CheckOutDate { get; set; }
}
