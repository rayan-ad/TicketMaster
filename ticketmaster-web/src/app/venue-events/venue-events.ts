import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-venue-events',
  imports: [CommonModule, RouterLink],
  templateUrl: './venue-events.html',
  styleUrl: './venue-events.scss',
})
export class VenueEvents implements OnInit {
  private readonly API_URL = 'https://localhost:7287/api';

  venueId!: number;
  venue: any = null;
  events: any[] = [];
  loading = true;

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.venueId = +params['id'];
      this.loadVenueInfo();
      this.loadVenueEvents();
    });
  }

  loadVenueInfo(): void {
    this.http.get(`${this.API_URL}/Venue/get/${this.venueId}`).subscribe({
      next: (data) => {
        this.venue = data;
      },
      error: (err) => {
        console.error('Erreur chargement venue:', err);
      }
    });
  }

  loadVenueEvents(): void {
    this.loading = true;
    this.http.get<any[]>(`${this.API_URL}/Event/venue/${this.venueId}`).subscribe({
      next: (data) => {
        // Trier du plus récent au plus lointain (date croissante)
        this.events = data.sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
        this.loading = false;
      },
      error: (err) => {
        console.error('Erreur chargement événements:', err);
        this.events = [];
        this.loading = false;
      }
    });
  }
}
