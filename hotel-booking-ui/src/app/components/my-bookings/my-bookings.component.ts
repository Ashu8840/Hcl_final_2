import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { HotelService } from '../../services/hotel.service';

@Component({
  selector: 'app-my-bookings',
  templateUrl: './my-bookings.component.html',
  styleUrls: ['./my-bookings.css'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyBookingsComponent implements OnInit {
  bookings: any[] = [];
  loading = true;

  constructor(
    private hotelService: HotelService,
    private router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit() {
    this.loadBookings();
  }

  loadBookings() {
    if (!this.hotelService.currentUserId) {
      this.router.navigate(['/auth/login']);
      return;
    }

    this.loading = true;
    this.cdr.markForCheck();

    this.hotelService.getMyBookings().subscribe({
      next: (data) => {
        this.bookings = data;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error fetching bookings', err);
        this.loading = false;
        this.cdr.markForCheck();
      },
    });
  }

  cancelBooking(id: number) {
    if (confirm('Are you sure you want to cancel this booking?')) {
      this.hotelService.cancelBooking(id).subscribe({
        next: () => {
          // reload the list
          this.loadBookings();
        },
        error: (err) => {
          alert('Failed to cancel booking. ' + (err.error?.message || ''));
        },
      });
    }
  }

  rebook(roomId: number) {
    this.router.navigate(['/book', roomId]);
  }
}
