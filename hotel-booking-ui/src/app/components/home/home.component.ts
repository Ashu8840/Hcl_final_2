import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.css'],
  standalone: false,
})
export class HomeComponent {
  searchTerm = '';

  constructor(private router: Router) {}

  search() {
    // Navigate to hotels page with search query
    this.router.navigate(['/hotels'], { queryParams: { search: this.searchTerm } });
  }
}
