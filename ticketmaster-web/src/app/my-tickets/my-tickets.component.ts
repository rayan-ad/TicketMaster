import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReservationService, TicketDto } from '../services/reservation.service';

@Component({
  selector: 'app-my-tickets',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="max-w-7xl mx-auto px-6 py-8">
      <h1 class="text-3xl font-bold mb-8">Mes Billets</h1>

      <div *ngIf="loading" class="text-center py-12">
        <p class="text-gray-500">Chargement...</p>
      </div>

      <div *ngIf="!loading && tickets.length === 0" class="text-center py-12">
        <p class="text-gray-500">Vous n'avez aucun billet.</p>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <div *ngFor="let ticket of tickets" class="bg-white rounded-xl border overflow-hidden hover:shadow-lg transition">
          <div class="bg-gradient-to-r from-purple-600 to-blue-600 text-white p-6">
            <h3 class="text-xl font-bold mb-2">{{ ticket.eventName }}</h3>
            <p class="text-purple-100 text-sm">{{ ticket.eventDate | date:'EEE, d MMM y • HH:mm' }}</p>
          </div>

          <div class="p-6">
            <div class="grid grid-cols-2 gap-4 mb-4">
              <div>
                <p class="text-sm text-gray-500">Zone</p>
                <p class="font-semibold">{{ ticket.zoneName }}</p>
              </div>
              <div>
                <p class="text-sm text-gray-500">Prix</p>
                <p class="font-semibold">{{ ticket.price | currency:'EUR' }}</p>
              </div>
              <div>
                <p class="text-sm text-gray-500">Rangée</p>
                <p class="font-semibold">{{ ticket.row }}</p>
              </div>
              <div>
                <p class="text-sm text-gray-500">Siège</p>
                <p class="font-semibold">#{{ ticket.number }}</p>
              </div>
            </div>

            <div class="text-center">
              <img [src]="ticket.qrCodeUrl" alt="QR Code" class="w-48 h-48 mx-auto border-4 border-purple-600 rounded-lg" />
              <p class="mt-2 text-xs text-gray-500 font-mono">{{ ticket.ticketNumber }}</p>
            </div>

            <div *ngIf="ticket.isUsed" class="mt-4 bg-gray-100 text-gray-700 text-center py-2 rounded-lg">
              Billet utilisé
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class MyTicketsComponent implements OnInit {
  tickets: TicketDto[] = [];
  loading = true;

  constructor(private reservationService: ReservationService) {}

  ngOnInit(): void {
    this.reservationService.getMyTickets().subscribe({
      next: (data) => {
        this.tickets = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Erreur chargement billets:', err);
        this.loading = false;
      }
    });
  }
}
