import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { HotelService, RoomSearchFilters } from '../../services/hotel.service';

@Component({
  selector: 'app-rooms',
  templateUrl: './rooms.component.html',
  styleUrls: ['./rooms.css'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomsComponent implements OnInit {
  rooms: any[] = [];
  loading = true;
  errorMessage = '';

  filters: {
    location: string;
    checkIn: string;
    checkOut: string;
    minPrice: number | null;
    maxPrice: number | null;
    guests: number | null;
    amenitiesText: string;
  } = {
    location: '',
    checkIn: '',
    checkOut: '',
    minPrice: null,
    maxPrice: null,
    guests: null,
    amenitiesText: '',
  };

  today = new Date().toISOString().split('T')[0];

  constructor(
    private hotelService: HotelService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit() {
    this.searchRooms();
  }

  searchRooms() {
    this.loading = true;
    this.errorMessage = '';
    this.cdr.markForCheck();

    const amenities = this.filters.amenitiesText
      .split(',')
      .map((item) => item.trim())
      .filter((item) => item.length > 0);

    const searchPayload: RoomSearchFilters = {
      location: this.filters.location,
      checkIn: this.filters.checkIn,
      checkOut: this.filters.checkOut,
      minPrice: this.filters.minPrice,
      maxPrice: this.filters.maxPrice,
      guests: this.filters.guests,
      amenities,
    };

    this.hotelService.searchRooms(searchPayload).subscribe({
      next: (data) => {
        this.rooms = data;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error loading rooms', err);
        this.errorMessage = err.error?.message || 'Unable to load rooms with the selected filters.';
        this.loading = false;
        this.cdr.markForCheck();
      },
    });
  }

  resetFilters() {
    this.filters = {
      location: '',
      checkIn: '',
      checkOut: '',
      minPrice: null,
      maxPrice: null,
      guests: null,
      amenitiesText: '',
    };

    this.searchRooms();
  }
}
