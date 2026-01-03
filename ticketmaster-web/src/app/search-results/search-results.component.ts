import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-search-results',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './search-results.component.html',
  styleUrl: './search-results.component.scss'
})
export class SearchResultsComponent implements OnInit {
  searchQuery = '';
  results: any[] = [];
  loading = true;
  defaultImage = 'default-event.jpeg';

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.searchQuery = params['q'] || '';
      this.searchEvents();
    });
  }

  private searchEvents(): void {
    this.loading = true;
    // Récupérer TOUS les événements pour la recherche (pageSize=9999)
    this.http.get<any>('https://localhost:7287/api/Event?pageNumber=1&pageSize=9999')
      .subscribe({
        next: (response) => {
          const allEvents = response.items || [];

          if (this.searchQuery.trim() && this.searchQuery.length >= 3) {
            const query = this.searchQuery.toLowerCase();
            // Recherche PARTIELLE (il suffit de 3 lettres)
            this.results = allEvents.filter((e: any) =>
              e.name.toLowerCase().includes(query) ||
              e.type?.toLowerCase().includes(query) ||
              e.description?.toLowerCase().includes(query)
            );
          } else if (this.searchQuery.trim()) {
            // Moins de 3 caractères: message d'avertissement
            this.results = [];
          } else {
            // Pas de recherche: afficher tout
            this.results = allEvents;
          }
          this.loading = false;
        },
        error: (err) => {
          console.error('Erreur lors de la recherche:', err);
          this.loading = false;
        }
      });
  }
}
