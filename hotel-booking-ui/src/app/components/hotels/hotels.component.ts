import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { HotelService } from '../../services/hotel.service';

@Component({
  selector: 'app-hotels',
  templateUrl: './hotels.component.html',
  styleUrls: ['./hotels.css'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HotelsComponent implements OnInit, OnDestroy {
  hotels: any[] = [];
  filteredHotels: any[] = [];
  loading = true;
  errorMessage = '';
  searchTerm = '';
  private readonly subscriptions = new Subscription();

  constructor(
    private hotelService: HotelService,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit() {
    // Get search term from URL query params (if any)
    this.subscriptions.add(
      this.route.queryParams.subscribe((params) => {
        this.searchTerm = params['search'] || '';
        this.loadHotels();
      }),
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  loadHotels() {
    this.loading = true;
    this.errorMessage = '';
    this.cdr.markForCheck();

    this.hotelService.getAllHotels().subscribe({
      next: (data) => {
        this.hotels = data;
        this.filterHotels();
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error fetching hotels', err);
        this.errorMessage = err.error?.message || 'Unable to load hotels right now.';
        this.loading = false;
        this.cdr.markForCheck();
      },
    });
  }

  filterHotels() {
    if (!this.searchTerm.trim()) {
      this.filteredHotels = this.hotels;
    } else {
      const term = this.searchTerm.toLowerCase();
      this.filteredHotels = this.hotels.filter(
        (h) =>
          (h.name ?? '').toLowerCase().includes(term) ||
          (h.location ?? '').toLowerCase().includes(term),
      );
    }

    this.cdr.markForCheck();
  }

  onSearchChange() {
    this.filterHotels();
  }
}
