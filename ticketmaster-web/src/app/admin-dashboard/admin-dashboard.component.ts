import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss'
})
export class AdminDashboardComponent implements OnInit {
  private readonly API_URL = 'https://localhost:7287/api';

  // Navigation
  activeTab: 'stats' | 'events' | 'venues' | 'users' = 'stats';

  // Stats
  stats = {
    totalEvents: 0,
    totalReservations: 0,
    totalRevenue: 0,
    occupancyRate: 0,
    totalUsers: 0
  };

  // Events
  events: any[] = [];
  eventsDetailed: any[] = []; // Pour le tableau des stats avec revenue/reservations
  selectedEvent: any = null;
  showEventModal = false;
  eventForm: any = {
    name: '',
    date: '',
    type: '',
    description: '',
    venueId: null,
    imageEvent: ''
  };

  // Venues
  venues: any[] = [];
  selectedVenue: any = null;
  showVenueModal = false;
  venueForm: any = {
    name: '',
    capacity: 0,
    pricingZones: []
  };

  // Options de zones tarifaires disponibles
  availablePricingZones = [
    { name: 'VIP', selected: false, price: 0, seatCount: 0, color: '#FFD700' },      // Or/Gold
    { name: 'Orchestre', selected: false, price: 0, seatCount: 0, color: '#E74C3C' }, // Rouge vif
    { name: 'Balcon', selected: false, price: 0, seatCount: 0, color: '#3498DB' },    // Bleu
    { name: 'Standard', selected: false, price: 0, seatCount: 0, color: '#9B59B6' },  // Violet
    { name: 'Eco', selected: false, price: 0, seatCount: 0, color: '#27AE60' }        // Vert
  ];

  // Users
  users: any[] = [];

  // Pagination
  eventsPagination = {
    currentPage: 1,
    pageSize: 9,
    totalPages: 1,
    totalCount: 0
  };

  venuesPagination = {
    currentPage: 1,
    pageSize: 9,
    totalPages: 1,
    totalCount: 0
  };

  usersPagination = {
    currentPage: 1,
    pageSize: 10,
    totalPages: 1,
    totalCount: 0
  };

  // UI
  loading = false;
  showNotification = false;
  notificationMessage = '';
  notificationType: 'success' | 'error' = 'success';

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadStats();
    this.loadEvents();
    this.loadVenues();
    this.loadUsers();
  }

  // ==================== STATS ====================

  loadStats(): void {
    // Récupérer TOUS les événements (pageSize=9999 pour avoir tout)
    this.http.get<any>(`${this.API_URL}/Event?pageNumber=1&pageSize=9999`).subscribe(response => {
      const eventList = response.items || [];
      this.stats.totalEvents = response.totalCount || eventList.length;
      this.stats.totalReservations = 0;
      this.stats.totalRevenue = 0;
      this.eventsDetailed = [];

      // Pour chaque événement, récupérer les détails pour avoir les stats complètes
      eventList.forEach((event: any) => {
        this.http.get<any>(`${this.API_URL}/Event/${event.id}`).subscribe(eventDetail => {
          // Ajouter aux événements détaillés pour le tableau
          this.eventsDetailed.push(eventDetail);

          // SoldSeats = nombre de réservations payées
          this.stats.totalReservations += eventDetail.soldSeats || 0;
          // ActualRevenue = revenus réels (sièges payés)
          this.stats.totalRevenue += eventDetail.actualRevenue || 0;
        });
      });
    });

    // Charger le nombre d'utilisateurs
    this.http.get<any>(`${this.API_URL}/Auth/users/count`).subscribe({
      next: (data) => {
        this.stats.totalUsers = data.count || 0;
      },
      error: (err) => {
        console.error('Erreur chargement nombre utilisateurs:', err);
      }
    });
  }

  // ==================== EVENTS ====================

  loadEvents(): void {
    this.loading = true;
    const params = {
      pageNumber: this.eventsPagination.currentPage.toString(),
      pageSize: this.eventsPagination.pageSize.toString()
    };

    this.http.get<any>(`${this.API_URL}/Event`, { params }).subscribe({
      next: (data) => {
        this.events = data.items || [];
        this.eventsPagination.totalCount = data.totalCount || 0;
        this.eventsPagination.totalPages = data.totalPages || 1;
        this.eventsPagination.currentPage = data.pageNumber || 1;
        this.loading = false;
      },
      error: (err) => {
        console.error('Erreur chargement événements:', err);
        this.showToast('Erreur lors du chargement des événements.', 'error');
        this.loading = false;
      }
    });
  }

  openEventModal(event?: any): void {
    if (event) {
      // Mode édition - charger TOUS les détails de l'event
      this.http.get<any>(`${this.API_URL}/Event/${event.id}`).subscribe({
        next: (fullEvent) => {
          this.selectedEvent = fullEvent;
          this.eventForm = {
            id: fullEvent.id,
            name: fullEvent.name || '',
            date: fullEvent.date ? new Date(fullEvent.date).toISOString().slice(0, 16) : '',
            type: fullEvent.type || '',
            description: fullEvent.description || '',
            venueId: fullEvent.venueId || null,
            imageEvent: fullEvent.imageEvent || ''
          };
          console.log('[EDIT] Modifying event (complete details):', this.eventForm);
          this.showEventModal = true;
        },
        error: (err) => {
          console.error('[ERROR] Error loading event details:', err);
          this.showToast('Erreur lors du chargement des détails.', 'error');
        }
      });
    } else {
      // Create mode
      this.selectedEvent = null;
      this.eventForm = {
        name: '',
        date: '',
        type: '',
        description: '',
        venueId: null,
        imageEvent: ''
      };
      console.log('[CREATE] New event');
      this.showEventModal = true;
    }
  }

  saveEvent(form: NgForm): void {
    // Marquer tous les champs comme touchés pour afficher les erreurs
    Object.keys(form.controls).forEach(key => {
      form.controls[key].markAsTouched();
    });

    if (form.invalid) {
      this.showToast('Veuillez remplir correctement tous les champs obligatoires.', 'error');
      return;
    }

    // Préparer les données en nettoyant les champs vides
    const eventData: any = {
      name: this.eventForm.name.trim(),
      date: new Date(this.eventForm.date).toISOString(),
      type: this.eventForm.type,
      venueId: parseInt(this.eventForm.venueId)
    };

    // Ajouter description seulement si elle existe
    if (this.eventForm.description?.trim()) {
      eventData.description = this.eventForm.description.trim();
    }

    // Ajouter imageEvent seulement si c'est une URL valide
    if (this.eventForm.imageEvent?.trim()) {
      eventData.imageEvent = this.eventForm.imageEvent.trim();
    }

    console.log('[SEND] Sending event:', eventData);

    if (this.selectedEvent) {
      // UPDATE
      eventData.id = this.selectedEvent.id;
      this.http.put(`${this.API_URL}/Event/${this.selectedEvent.id}`, eventData).subscribe({
        next: () => {
          this.showToast('Événement modifié avec succès!', 'success');
          this.loadEvents();
          this.loadStats();
          this.closeEventModal();
        },
        error: (err) => {
          console.error('[ERROR] Error modifying event:', err);
          let message = 'Erreur lors de la modification.';
          if (err.error?.errors) {
            message = Object.values(err.error.errors).flat().join(', ');
          } else if (err.error?.message) {
            message = err.error.message;
          } else if (typeof err.error === 'string') {
            message = err.error;
          }
          this.showToast(message, 'error');
        }
      });
    } else {
      // CREATE
      this.http.post(`${this.API_URL}/Event`, eventData).subscribe({
        next: (response) => {
          console.log('[SUCCESS] Event created:', response);
          this.showToast('Événement créé avec succès!', 'success');
          this.loadEvents();
          this.loadStats();
          this.closeEventModal();
        },
        error: (err) => {
          console.error('[ERROR] Error creating event:', err);
          let message = 'Erreur lors de la création.';
          if (err.error?.errors) {
            // Erreurs de validation ASP.NET
            const errors = err.error.errors;
            const errorMessages = Object.keys(errors).map(key =>
              `${key}: ${errors[key].join(', ')}`
            );
            message = errorMessages.join(' | ');
          } else if (err.error?.message) {
            message = err.error.message;
          } else if (typeof err.error === 'string') {
            message = err.error;
          }
          this.showToast(message, 'error');
        }
      });
    }
  }

  deleteEvent(eventId: number): void {
    if (!confirm('Voulez-vous vraiment supprimer cet événement ?')) {
      return;
    }

    console.log('[DELETE] Deleting event ID:', eventId);

    this.http.delete(`${this.API_URL}/Event/${eventId}`).subscribe({
      next: (response) => {
        console.log('[SUCCESS] Event deleted:', response);
        this.showToast('Événement supprimé avec succès!', 'success');
        this.loadEvents();
        this.loadStats();
      },
      error: (err) => {
        console.error('[ERROR] Error deleting event:', err);
        let message = 'Erreur lors de la suppression.';
        if (err.error?.message) {
          message = err.error.message;
        } else if (typeof err.error === 'string') {
          message = err.error;
        }
        this.showToast(message, 'error');
      }
    });
  }

  closeEventModal(): void {
    this.showEventModal = false;
    this.selectedEvent = null;
  }

  // ==================== VENUES ====================

  loadVenues(): void {
    const params = {
      pageNumber: this.venuesPagination.currentPage.toString(),
      pageSize: this.venuesPagination.pageSize.toString()
    };

    this.http.get<any>(`${this.API_URL}/Venue/list`, { params }).subscribe({
      next: (data) => {
        this.venues = data.items || [];
        this.venuesPagination.totalCount = data.totalCount || 0;
        this.venuesPagination.totalPages = data.totalPages || 1;
        this.venuesPagination.currentPage = data.pageNumber || 1;
      },
      error: (err) => {
        console.error('Erreur chargement salles:', err);
        this.showToast('Erreur lors du chargement des salles.', 'error');
      }
    });
  }

  openVenueModal(venue?: any): void {
    if (venue) {
      // Mode édition
      this.selectedVenue = venue;
      this.venueForm = {
        id: venue.id,
        name: venue.name,
        capacity: venue.capacity
      };
    } else {
      // Mode création - réinitialiser les zones
      this.selectedVenue = null;
      this.venueForm = {
        name: '',
        capacity: 0,
        pricingZones: []
      };
      // Réinitialiser la sélection des zones
      this.availablePricingZones.forEach(zone => {
        zone.selected = false;
        zone.price = 0;
        zone.seatCount = 0;
      });
      console.log('[CREATE] New venue');
    }
    this.showVenueModal = true;
  }

  saveVenue(form: NgForm): void {
    // Marquer tous les champs comme touchés pour afficher les erreurs
    Object.keys(form.controls).forEach(key => {
      form.controls[key].markAsTouched();
    });

    if (form.invalid) {
      this.showToast('Veuillez remplir correctement tous les champs obligatoires.', 'error');
      return;
    }

    // Préparer les données proprement
    const venueData: any = {
      name: this.venueForm.name.trim(),
      capacity: parseInt(this.venueForm.capacity)
    };

    // Pour la création, ajouter les zones tarifaires sélectionnées
    if (!this.selectedVenue) {
      const selectedZones = this.availablePricingZones
        .filter(zone => zone.selected && zone.price > 0 && zone.seatCount > 0)
        .map(zone => ({
          name: zone.name,
          price: parseFloat(zone.price.toString()),
          color: zone.color,
          seatCount: zone.seatCount
        }));

      if (selectedZones.length === 0) {
        this.showToast('Veuillez sélectionner au moins une zone tarifaire avec un prix et un nombre de sièges valides.', 'error');
        return;
      }

      // VALIDATION: Vérifier que le nombre total de sièges ne dépasse pas la capacité
      const totalSeats = selectedZones.reduce((sum, zone) => sum + zone.seatCount, 0);
      if (totalSeats > venueData.capacity) {
        this.showToast(`Le nombre total de sièges (${totalSeats}) dépasse la capacité du venue (${venueData.capacity}).`, 'error');
        return;
      }

      venueData.pricingZones = selectedZones;
    }

    console.log('[SEND] Sending venue:', venueData);

    if (this.selectedVenue) {
      // UPDATE - add ID
      venueData.id = this.selectedVenue.id;
      this.http.put(`${this.API_URL}/Venue/updateVenue`, venueData).subscribe({
        next: (response) => {
          console.log('[SUCCESS] Venue modified:', response);
          this.showToast('Salle modifiée avec succès!', 'success');
          this.loadVenues();
          this.closeVenueModal();
        },
        error: (err) => {
          console.error('[ERROR] Error modifying venue:', err);
          let message = 'Erreur lors de la modification.';
          if (err.error?.errors) {
            const errors = err.error.errors;
            const errorMessages = Object.keys(errors).map(key =>
              `${key}: ${errors[key].join(', ')}`
            );
            message = errorMessages.join(' | ');
          } else if (err.error?.message) {
            message = err.error.message;
          } else if (typeof err.error === 'string') {
            message = err.error;
          }
          this.showToast(message, 'error');
        }
      });
    } else {
      // CREATE - no ID, with pricing zones
      this.http.post(`${this.API_URL}/Venue/createVenue`, venueData).subscribe({
        next: (response) => {
          console.log('[SUCCESS] Venue created:', response);
          this.showToast('Salle créée avec sièges générés automatiquement!', 'success');
          this.loadVenues();
          this.closeVenueModal();
        },
        error: (err) => {
          console.error('[ERROR] Error creating venue:', err);
          let message = 'Erreur lors de la création.';
          if (err.error?.errors) {
            const errors = err.error.errors;
            const errorMessages = Object.keys(errors).map(key =>
              `${key}: ${errors[key].join(', ')}`
            );
            message = errorMessages.join(' | ');
          } else if (err.error?.message) {
            message = err.error.message;
          } else if (typeof err.error === 'string') {
            message = err.error;
          }
          this.showToast(message, 'error');
        }
      });
    }
  }

  deleteVenue(venueId: number): void {
    if (!confirm('Voulez-vous vraiment supprimer cette salle ?')) {
      return;
    }

    console.log('[DELETE] Deleting venue ID:', venueId);

    this.http.delete(`${this.API_URL}/Venue/deleteVenue/${venueId}`).subscribe({
      next: (response: any) => {
        console.log('[SUCCESS] Venue deleted:', response);
        this.showToast(response.message || 'Salle supprimée avec succès!', 'success');
        this.loadVenues();
      },
      error: (err) => {
        console.error('[ERROR] Error deleting venue:', err);

        // Si 404, la salle n'existe plus
        if (err.status === 404) {
          this.showToast('Salle introuvable ou déjà supprimée.', 'success');
          this.loadVenues();
        }
        // Si 400, c'est une erreur métier (venue a des événements)
        else if (err.status === 400) {
          let message = 'Impossible de supprimer cette salle.';
          if (err.error?.message) {
            message = err.error.message;
          }
          this.showToast(message, 'error');
        }
        else {
          let message = 'Erreur lors de la suppression.';
          if (err.error?.message) {
            message = err.error.message;
          } else if (typeof err.error === 'string') {
            message = err.error;
          }
          this.showToast(message, 'error');
        }
      }
    });
  }

  closeVenueModal(): void {
    this.showVenueModal = false;
    this.selectedVenue = null;
  }

  // ==================== UI ====================

  private showToast(message: string, type: 'success' | 'error'): void {
    this.notificationMessage = message;
    this.notificationType = type;
    this.showNotification = true;

    // Afficher les erreurs beaucoup plus longtemps pour avoir le temps de les lire
    // Les erreurs de validation peuvent être longues avec plusieurs champs
    const duration = type === 'error' ? 10000 : 4000;

    setTimeout(() => {
      this.showNotification = false;
    }, duration);
  }

  // ==================== USERS ====================

  loadUsers(): void {
    const params = {
      pageNumber: this.usersPagination.currentPage.toString(),
      pageSize: this.usersPagination.pageSize.toString()
    };

    this.http.get<any>(`${this.API_URL}/Auth/users`, { params }).subscribe({
      next: (data) => {
        this.users = data.items || [];
        this.usersPagination.totalCount = data.totalCount || 0;
        this.usersPagination.totalPages = data.totalPages || 1;
        this.usersPagination.currentPage = data.pageNumber || 1;
      },
      error: (err) => {
        console.error('Erreur chargement utilisateurs:', err);
        this.showToast('Erreur lors du chargement des utilisateurs.', 'error');
      }
    });
  }

  updateUserRole(userId: number, newRole: string): void {
    // Confirmation avant changement
    if (!confirm(`Êtes-vous sûr de vouloir changer le rôle de cet utilisateur en "${newRole}" ?`)) {
      // Annuler le changement - recharger la page pour reset le select
      this.loadUsers();
      return;
    }

    // Mapper string -> enum number (Admin=0, Organisateur=1, Client=2)
    const roleMap: { [key: string]: number } = {
      'Admin': 0,
      'Organisateur': 1,
      'Client': 2
    };

    const roleValue = roleMap[newRole];
    if (roleValue === undefined) {
      this.showToast('Rôle invalide.', 'error');
      return;
    }

    this.http.put(`${this.API_URL}/Auth/users/${userId}/role`, { role: roleValue }).subscribe({
      next: () => {
        this.showToast('Rôle mis à jour avec succès!', 'success');
        this.loadUsers();
        this.loadStats(); // Recharger les stats au cas où
      },
      error: (err) => {
        console.error('Erreur mise à jour rôle:', err);
        this.showToast('Erreur lors de la mise à jour du rôle.', 'error');
        this.loadUsers(); // Recharger pour annuler visuellement
      }
    });
  }

  deleteUser(userId: number): void {
    if (!confirm('Voulez-vous vraiment supprimer cet utilisateur ?')) {
      return;
    }

    this.http.delete(`${this.API_URL}/Auth/users/${userId}`).subscribe({
      next: () => {
        this.showToast('Utilisateur supprimé avec succès!', 'success');
        this.loadUsers();
        this.loadStats(); // Recharger les stats
      },
      error: (err) => {
        console.error('Erreur suppression utilisateur:', err);
        let message = 'Erreur lors de la suppression.';
        if (err.error?.message) {
          message = err.error.message;
        }
        this.showToast(message, 'error');
      }
    });
  }

  // ==================== HELPER METHODS ====================

  getSeatsForZone(venue: any, zoneId: number): number {
    if (!venue.seats || venue.seats.length === 0) {
      return 0;
    }
    return venue.seats.filter((seat: any) => seat.pricingZoneId === zoneId).length;
  }

  isAdminOrOrganisateur(): boolean {
    const user = JSON.parse(localStorage.getItem('current_user') || '{}');
    return user.role === 'Admin' || user.role === 'Organisateur';
  }

  // ==================== PAGINATION CONTROLS ====================

  // Events pagination
  nextEventsPage(): void {
    if (this.eventsPagination.currentPage < this.eventsPagination.totalPages) {
      this.eventsPagination.currentPage++;
      this.loadEvents();
    }
  }

  prevEventsPage(): void {
    if (this.eventsPagination.currentPage > 1) {
      this.eventsPagination.currentPage--;
      this.loadEvents();
    }
  }

  // Venues pagination
  nextVenuesPage(): void {
    if (this.venuesPagination.currentPage < this.venuesPagination.totalPages) {
      this.venuesPagination.currentPage++;
      this.loadVenues();
    }
  }

  prevVenuesPage(): void {
    if (this.venuesPagination.currentPage > 1) {
      this.venuesPagination.currentPage--;
      this.loadVenues();
    }
  }

  // Users pagination
  nextUsersPage(): void {
    if (this.usersPagination.currentPage < this.usersPagination.totalPages) {
      this.usersPagination.currentPage++;
      this.loadUsers();
    }
  }

  prevUsersPage(): void {
    if (this.usersPagination.currentPage > 1) {
      this.usersPagination.currentPage--;
      this.loadUsers();
    }
  }
}
