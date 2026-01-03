import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-home',
  imports: [RouterLink, CommonModule, FormsModule],
  standalone:true,
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  events: any[] = [];
  allEvents: any[] = [];
  latestEvent: any = null;
  defaultImage = 'default-event.jpeg';
  searchQuery = '';
  selectedType = '';
  suggestions: any[] = [];
  showSuggestions = false;

  // Pagination
  currentPage = 1;
  pageSize = 12;
  totalPages = 1;
  totalCount = 0;

  constructor(
    private http: HttpClient,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit() {
    // Listen to query params for filtering
    this.route.queryParams.subscribe(params => {
      this.selectedType = params['type'] || '';
      this.getAllEvent();
    });
  }

  private getAllEvent(){
    const params = {
      pageNumber: this.currentPage.toString(),
      pageSize: this.pageSize.toString()
    };

    this.http.get<any>('https://localhost:7287/api/Event', { params })
      .subscribe({
        next: (data) => {
          this.allEvents = data.items || [];
          this.totalCount = data.totalCount || 0;
          this.totalPages = data.totalPages || 1;
          this.currentPage = data.pageNumber || 1;
          this.applyFilters();
          console.log('Événements chargés :', this.events);
        },
        error: (err) => {
          console.error('Erreur lors du chargement des événements :', err);
        }
      });
  }

  applyFilters() {
    let filtered = [...this.allEvents];

    // Filter by type if selected
    if (this.selectedType) {
      filtered = filtered.filter(e => e.type === this.selectedType);
    }

    this.events = filtered;

    // Get the most recent event by date (not by array position)
    // Sort by ID descending (higher ID = more recent) or by creation date if available
    if (this.allEvents.length > 0) {
      const sortedByRecent = [...this.allEvents].sort((a, b) => {
        // If there's a createdAt or similar field, use it
        // Otherwise use ID as proxy (higher ID = created later)
        return b.id - a.id;
      });
      this.latestEvent = sortedByRecent[0];
    } else {
      this.latestEvent = null;
    }
  }

  onSearchInput() {
    if (this.searchQuery.trim()) {
      const query = this.searchQuery.toLowerCase();

      // Chercher dans les événements déjà filtrés par type (si un type est sélectionné)
      const eventsToSearch = this.selectedType ? this.events : this.allEvents;

      this.suggestions = eventsToSearch.filter(e =>
        e.name.toLowerCase().includes(query)
      );
      this.showSuggestions = true;
    } else {
      this.suggestions = [];
      this.showSuggestions = false;
    }
  }

  goToSearchResults() {
    const query = this.searchQuery.trim();

    // Minimum 3 caractères pour lancer une recherche
    if (query.length >= 3) {
      this.router.navigate(['/search'], { queryParams: { q: this.searchQuery } });
      this.closeSuggestions();
    } else if (query.length > 0) {
      // Message si moins de 3 caractères
      alert('Veuillez entrer au moins 3 caractères pour effectuer une recherche.');
    }
  }

  closeSuggestions() {
    this.showSuggestions = false;
  }

  // Pagination controls
  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.getAllEvent();
    }
  }

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.getAllEvent();
    }
  }
}
