import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { forkJoin, Subscription } from 'rxjs';
import { HotelService } from '../../services/hotel.service';

@Component({
  selector: 'app-hotel-detail',
  templateUrl: './hotel-detail.component.html',
  styleUrls: ['./hotel-detail.css'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HotelDetailComponent implements OnInit, OnDestroy {
  hotel: any;
  rooms: any[] = [];
  loading = true;
  errorMessage = '';
  private readonly subscriptions = new Subscription();

  constructor(
    private route: ActivatedRoute,
    private hotelService: HotelService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit() {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      const hotelId = +idParam;
      this.loadHotelDetails(hotelId);
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  loadHotelDetails(id: number) {
    this.loading = true;
    this.errorMessage = '';
    this.cdr.markForCheck();

    this.subscriptions.add(
      forkJoin({
        hotel: this.hotelService.getHotelById(id),
        rooms: this.hotelService.getRoomsByHotel(id),
      }).subscribe({
        next: ({ hotel, rooms }) => {
          this.hotel = hotel;
          this.rooms = rooms;
          this.loading = false;
          this.cdr.markForCheck();
        },
        error: (err) => {
          console.error('Error loading hotel details', err);
          this.errorMessage = err.error?.message || 'Unable to load hotel details at the moment.';
          this.loading = false;
          this.cdr.markForCheck();
        },
      }),
    );
  }
}
