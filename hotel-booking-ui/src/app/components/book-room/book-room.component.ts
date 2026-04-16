import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HotelService } from '../../services/hotel.service';

@Component({
  selector: 'app-book-room',
  templateUrl: './book-room.component.html',
  styleUrls: ['./book-room.css'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BookRoomComponent implements OnInit {
  room: any;
  loading = true;
  submitting = false;
  errorMessage = '';

  // Calculate minimum date for date picker (today)
  today = new Date().toISOString().split('T')[0];

  bookingData = {
    roomId: 0,
    checkInDate: '',
    checkOutDate: '',
  };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private hotelService: HotelService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit() {
    const roomIdParam = this.route.snapshot.paramMap.get('roomId');
    if (roomIdParam) {
      const roomId = +roomIdParam;
      this.bookingData.roomId = roomId;

      this.hotelService.getRoomById(roomId).subscribe({
        next: (data) => {
          this.room = data;
          this.loading = false;
          this.cdr.markForCheck();
        },
        error: (err) => {
          console.error('Error fetching room', err);
          this.errorMessage = 'Failed to load room details.';
          this.loading = false;
          this.cdr.markForCheck();
        },
      });
    }
  }

  calculateTotal() {
    if (!this.bookingData.checkInDate || !this.bookingData.checkOutDate || !this.room) {
      return 0;
    }
    const checkin = new Date(this.bookingData.checkInDate);
    const checkout = new Date(this.bookingData.checkOutDate);
    const timeDiff = checkout.getTime() - checkin.getTime();
    let days = Math.ceil(timeDiff / (1000 * 3600 * 24));

    // Prevent negative days
    if (days <= 0) days = 0;

    return days * this.room.pricePerNight;
  }

  submitBooking() {
    this.errorMessage = '';

    if (!this.bookingData.checkInDate || !this.bookingData.checkOutDate) {
      this.errorMessage = 'Please fill out all fields.';
      return;
    }

    const checkin = new Date(this.bookingData.checkInDate);
    const checkout = new Date(this.bookingData.checkOutDate);

    if (checkout <= checkin) {
      this.errorMessage = 'Check-out date must be after check-in date.';
      return;
    }

    this.submitting = true;
    this.cdr.markForCheck();

    this.hotelService.createBooking(this.bookingData).subscribe({
      next: () => {
        this.submitting = false;
        this.cdr.markForCheck();
        this.router.navigate(['/my-bookings']);
      },
      error: (err) => {
        console.error('Error creating booking', err);
        this.errorMessage = 'Failed to create booking. ' + (err.error?.message || '');
        this.submitting = false;
        this.cdr.markForCheck();
      },
    });
  }
}
