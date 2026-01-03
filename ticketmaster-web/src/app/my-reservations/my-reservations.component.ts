import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ReservationService, ReservationDto, TicketDto } from '../services/reservation.service';
import { AuthService } from '../services/auth.service';
import jsPDF from 'jspdf';
import html2canvas from 'html2canvas';

@Component({
  selector: 'app-my-reservations',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="max-w-7xl mx-auto px-6 py-8">
      <h1 class="text-3xl font-bold mb-8">Mes Réservations</h1>

      <div *ngIf="loading" class="text-center py-12">
        <p class="text-gray-500">Chargement...</p>
      </div>

      <div *ngIf="!loading && reservations.length === 0" class="text-center py-12">
        <p class="text-gray-500 mb-4">Vous n'avez aucune réservation.</p>
        <a routerLink="/" class="text-purple-600 hover:text-purple-700 font-semibold">Voir les événements</a>
      </div>

      <div class="space-y-4">
        <div *ngFor="let reservation of reservations" class="bg-white rounded-xl border p-6 hover:shadow-lg transition">
          <div class="flex justify-between items-start mb-4">
            <div>
              <h3 class="text-xl font-bold">{{ reservation.eventName }}</h3>
              <p class="text-gray-500 text-sm">{{ reservation.eventDate | date:'EEE, d MMM y • HH:mm' }}</p>
            </div>
            <span class="px-3 py-1 rounded-full text-sm font-semibold"
              [class.bg-orange-100]="reservation.status === 'Pending'"
              [class.text-orange-700]="reservation.status === 'Pending'"
              [class.bg-green-100]="reservation.status === 'Paid'"
              [class.text-green-700]="reservation.status === 'Paid'"
              [class.bg-gray-100]="reservation.status === 'Canceled'"
              [class.text-gray-700]="reservation.status === 'Canceled'">
              {{ reservation.status }}
            </span>
          </div>

          <div class="grid grid-cols-2 gap-4 mb-4">
            <div>
              <p class="text-sm text-gray-500">Sièges</p>
              <p class="font-semibold">{{ reservation.seats.length }} siège(s)</p>
            </div>
            <div>
              <p class="text-sm text-gray-500">Montant total</p>
              <p class="font-semibold">{{ reservation.totalAmount | currency:'EUR' }}</p>
            </div>
          </div>

          <div class="flex gap-2">
            <a *ngIf="reservation.status === 'Pending'" [routerLink]="['/event', reservation.eventId]" [queryParams]="{reservationId: reservation.id}"
              class="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700">
              Continuer le Paiement
            </a>
            <button *ngIf="reservation.status === 'Pending'" (click)="cancelReservation(reservation.id)"
              class="px-4 py-2 border border-red-600 text-red-600 rounded-lg hover:bg-red-50">
              Annuler
            </button>
            <button *ngIf="reservation.status === 'Paid' && reservation.tickets?.length" (click)="viewTickets(reservation)"
              class="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700">
              Voir les billets
            </button>
          </div>
        </div>
      </div>

      <!-- Pagination -->
      <div *ngIf="!loading && reservations.length > 0" class="flex justify-center items-center gap-4 mt-8">
        <button
          (click)="prevPage()"
          [disabled]="currentPage === 1"
          class="px-4 py-2 bg-purple-600 text-white rounded hover:bg-purple-700 disabled:bg-gray-400 disabled:cursor-not-allowed">
          Précédent
        </button>
        <span class="text-gray-700">Page {{ currentPage }} / {{ totalPages }}</span>
        <button
          (click)="nextPage()"
          [disabled]="currentPage === totalPages"
          class="px-4 py-2 bg-purple-600 text-white rounded hover:bg-purple-700 disabled:bg-gray-400 disabled:cursor-not-allowed">
          Suivant
        </button>
      </div>

      <!-- Modal billets -->
      <div *ngIf="showTicketsModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4" (click)="closeTicketsModal()">
        <div class="bg-white rounded-2xl p-8 max-w-4xl w-full max-h-[90vh] overflow-y-auto" (click)="$event.stopPropagation()">
          <div class="flex justify-between items-center mb-6">
            <h2 class="text-2xl font-bold">Billets - {{ selectedReservation?.eventName }}</h2>
            <button (click)="closeTicketsModal()" class="text-gray-500 hover:text-gray-700 text-3xl">&times;</button>
          </div>

          <!-- Bouton télécharger tous les billets -->
          <div class="mb-6">
            <button (click)="downloadAllTickets()"
                    class="w-full bg-gradient-to-r from-purple-600 to-blue-600 text-white px-6 py-3 rounded-lg font-semibold hover:from-purple-700 hover:to-blue-700 transition">
              📄 Télécharger tous les billets (PDF)
            </button>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div *ngFor="let ticket of selectedReservation?.tickets" class="border rounded-lg p-4">
              <div class="flex justify-between items-start mb-3">
                <div>
                  <p class="text-sm text-gray-500">Siège</p>
                  <p class="font-bold">{{ ticket.row }}-{{ ticket.number }}</p>
                </div>
                <div class="text-right">
                  <p class="text-sm text-gray-500">Zone</p>
                  <p class="font-semibold">{{ ticket.zoneName }}</p>
                </div>
              </div>
              <div class="text-center mb-3">
                <img [src]="ticket.qrCodeUrl" alt="QR Code" class="mx-auto w-32 h-32"/>
              </div>
              <p class="text-xs text-gray-500 text-center mb-3">{{ ticket.ticketNumber }}</p>

              <!-- Bouton télécharger billet individuel -->
              <button (click)="downloadTicket(ticket)"
                      class="w-full bg-purple-600 text-white px-4 py-2 rounded-lg hover:bg-purple-700 transition text-sm">
                📥 Télécharger PDF
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class MyReservationsComponent implements OnInit {
  reservations: ReservationDto[] = [];
  loading = true;
  selectedReservation: ReservationDto | null = null;
  showTicketsModal = false;

  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalPages = 1;
  totalCount = 0;

  constructor(
    private reservationService: ReservationService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadReservations();
  }

  loadReservations(): void {
    this.reservationService.getMyReservations(this.currentPage, this.pageSize).subscribe({
      next: (data: any) => {
        this.reservations = data.items || data;
        this.totalCount = data.totalCount || 0;
        this.totalPages = data.totalPages || 1;
        this.currentPage = data.pageNumber || 1;
        this.loading = false;
      },
      error: (err) => {
        console.error('Erreur chargement réservations:', err);
        this.loading = false;
      }
    });
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadReservations();
    }
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadReservations();
    }
  }

  cancelReservation(id: number): void {
    if (!confirm('Voulez-vous vraiment annuler cette réservation ?')) {
      return;
    }

    this.reservationService.cancelReservation(id).subscribe({
      next: () => {
        // Supprimer immédiatement de la liste sans refresh
        this.reservations = this.reservations.filter(r => r.id !== id);
      },
      error: (err) => {
        console.error('Erreur annulation:', err);
        alert('Erreur lors de l\'annulation de la réservation.');
      }
    });
  }

  viewTickets(reservation: ReservationDto): void {
    this.selectedReservation = reservation;
    this.showTicketsModal = true;
  }

  closeTicketsModal(): void {
    this.showTicketsModal = false;
    this.selectedReservation = null;
  }

  async downloadTicket(ticket: TicketDto) {
    try {
      const currentUser = this.authService.getCurrentUser();
      const userName = currentUser?.name || 'Client';
      const userEmail = currentUser?.email || '';

      // Convertir le QR code en base64 pour éviter les erreurs CORS
      const qrCodeBase64 = await this.imageUrlToBase64(ticket.qrCodeUrl);

      // Create a temporary div with the invoice
      const tempDiv = document.createElement('div');
      tempDiv.style.position = 'absolute';
      tempDiv.style.left = '-9999px';
      tempDiv.style.width = '800px';
      tempDiv.style.padding = '40px';
      tempDiv.style.backgroundColor = '#ffffff';
      tempDiv.innerHTML = `
        <div style="font-family: Arial, sans-serif; color: #333;">
          <!-- Header -->
          <div style="text-align: center; margin-bottom: 30px; border-bottom: 3px solid #667eea; padding-bottom: 20px;">
            <h1 style="color: #667eea; font-size: 36px; margin: 0;">🎫 TicketMaster</h1>
            <p style="color: #666; font-size: 14px; margin: 5px 0 0 0;">Facture & Billet Électronique</p>
          </div>

          <!-- Client & Event Info -->
          <div style="display: flex; justify-content: space-between; margin-bottom: 30px;">
            <div style="flex: 1;">
              <h3 style="color: #667eea; font-size: 14px; text-transform: uppercase; margin: 0 0 10px 0;">Informations Client</h3>
              <p style="margin: 5px 0; font-size: 13px;"><strong>Nom:</strong> ${userName}</p>
              <p style="margin: 5px 0; font-size: 13px;"><strong>Email:</strong> ${userEmail}</p>
            </div>
            <div style="flex: 1; text-align: right;">
              <h3 style="color: #667eea; font-size: 14px; text-transform: uppercase; margin: 0 0 10px 0;">Détails Événement</h3>
              <p style="margin: 5px 0; font-size: 13px;"><strong>Événement:</strong> ${ticket.eventName}</p>
              <p style="margin: 5px 0; font-size: 13px;"><strong>Date:</strong> ${new Date(ticket.eventDate).toLocaleDateString('fr-FR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit' })}</p>
            </div>
          </div>

          <!-- Ticket Details Table -->
          <div style="margin-bottom: 30px;">
            <table style="width: 100%; border-collapse: collapse;">
              <thead>
                <tr style="background: #667eea; color: white;">
                  <th style="padding: 12px; text-align: left; font-size: 13px; border: 1px solid #667eea;">Zone</th>
                  <th style="padding: 12px; text-align: left; font-size: 13px; border: 1px solid #667eea;">Rangée</th>
                  <th style="padding: 12px; text-align: left; font-size: 13px; border: 1px solid #667eea;">Siège</th>
                  <th style="padding: 12px; text-align: right; font-size: 13px; border: 1px solid #667eea;">Prix</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td style="padding: 12px; border: 1px solid #ddd; font-size: 13px;">${ticket.zoneName}</td>
                  <td style="padding: 12px; border: 1px solid #ddd; font-size: 13px;">${ticket.row}</td>
                  <td style="padding: 12px; border: 1px solid #ddd; font-size: 13px;">#${ticket.number}</td>
                  <td style="padding: 12px; border: 1px solid #ddd; text-align: right; font-size: 13px;">${ticket.price.toFixed(2)} €</td>
                </tr>
              </tbody>
              <tfoot>
                <tr style="background: #f5f5f5; font-weight: bold;">
                  <td colspan="3" style="padding: 12px; border: 1px solid #ddd; text-align: right; font-size: 14px;">TOTAL</td>
                  <td style="padding: 12px; border: 1px solid #ddd; text-align: right; font-size: 14px; color: #667eea;">${ticket.price.toFixed(2)} €</td>
                </tr>
              </tfoot>
            </table>
          </div>

          <!-- QR Code Section -->
          <div style="text-align: center; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 25px; border-radius: 12px; margin-bottom: 20px;">
            <h3 style="color: white; margin: 0 0 15px 0; font-size: 16px;">Votre Billet d'Entrée</h3>
            <div style="background: white; padding: 20px; border-radius: 8px; display: inline-block;">
              <img src="${qrCodeBase64}" style="width: 200px; height: 200px; display: block; margin: 0 auto;">
            </div>
            <p style="color: white; font-family: monospace; font-size: 12px; margin: 15px 0 0 0; opacity: 0.9;">
              ${ticket.ticketNumber}
            </p>
            <p style="color: white; font-size: 11px; margin: 10px 0 0 0; opacity: 0.8;">
              Présentez ce QR code à l'entrée de l'événement
            </p>
          </div>

          <!-- Footer -->
          <div style="border-top: 2px solid #eee; padding-top: 20px; text-align: center; color: #999; font-size: 11px;">
            <p style="margin: 5px 0;">Merci de votre confiance | TicketMaster © ${new Date().getFullYear()}</p>
            <p style="margin: 5px 0;">Ce billet est personnel et non transférable</p>
            <p style="margin: 5px 0;">Généré le ${new Date().toLocaleDateString('fr-FR')} à ${new Date().toLocaleTimeString('fr-FR')}</p>
          </div>
        </div>
      `;
      document.body.appendChild(tempDiv);

      // Attendre un peu pour que le DOM soit bien rendu
      await new Promise(resolve => setTimeout(resolve, 100));

      const canvas = await html2canvas(tempDiv, {
        scale: 2,
        logging: false,
        backgroundColor: '#ffffff'
      });

      document.body.removeChild(tempDiv);

      const imgData = canvas.toDataURL('image/png');
      const pdf = new jsPDF('p', 'mm', 'a4');

      const pdfWidth = pdf.internal.pageSize.getWidth();
      const pdfHeight = pdf.internal.pageSize.getHeight();
      const imgWidth = pdfWidth;
      const imgHeight = (canvas.height * imgWidth) / canvas.width;

      pdf.addImage(imgData, 'PNG', 0, 0, imgWidth, imgHeight);
      pdf.save(`facture-billet-${ticket.ticketNumber}.pdf`);
    } catch (error) {
      console.error('Erreur génération PDF:', error);
      alert('Erreur lors de la génération du PDF');
    }
  }

  async downloadAllTickets() {
    if (!this.selectedReservation?.tickets?.length) return;

    try {
      const currentUser = this.authService.getCurrentUser();
      const userName = currentUser?.name || 'Client';
      const userEmail = currentUser?.email || '';
      const totalAmount = this.selectedReservation.tickets.reduce((sum, t) => sum + t.price, 0);

      // Convertir TOUTES les images QR code en base64
      const ticketsWithBase64 = await Promise.all(
        this.selectedReservation.tickets.map(async (t) => ({
          ...t,
          qrCodeBase64: await this.imageUrlToBase64(t.qrCodeUrl)
        }))
      );

      const pdf = new jsPDF('p', 'mm', 'a4');
      const pdfWidth = pdf.internal.pageSize.getWidth();
      const pdfHeight = pdf.internal.pageSize.getHeight();

      // PAGE 1: Summary Invoice
      const ticketRows = this.selectedReservation.tickets.map(t => `
        <tr>
          <td style="padding: 12px; border: 1px solid #ddd; font-size: 12px;">${t.zoneName}</td>
          <td style="padding: 12px; border: 1px solid #ddd; font-size: 12px;">${t.row}</td>
          <td style="padding: 12px; border: 1px solid #ddd; font-size: 12px;">#${t.number}</td>
          <td style="padding: 12px; border: 1px solid #ddd; text-align: right; font-size: 12px;">${t.price.toFixed(2)} €</td>
        </tr>
      `).join('');

      const summaryDiv = document.createElement('div');
      summaryDiv.style.position = 'absolute';
      summaryDiv.style.left = '-9999px';
      summaryDiv.style.width = '800px';
      summaryDiv.style.padding = '40px';
      summaryDiv.style.backgroundColor = '#ffffff';
      summaryDiv.innerHTML = `
        <div style="font-family: Arial, sans-serif; color: #333;">
          <div style="text-align: center; margin-bottom: 30px; border-bottom: 3px solid #667eea; padding-bottom: 20px;">
            <h1 style="color: #667eea; font-size: 36px; margin: 0;">🎫 TicketMaster</h1>
            <p style="color: #666; font-size: 14px; margin: 5px 0 0 0;">Facture & Billets Électroniques</p>
          </div>

          <div style="display: flex; justify-content: space-between; margin-bottom: 30px;">
            <div style="flex: 1;">
              <h3 style="color: #667eea; font-size: 14px; text-transform: uppercase; margin: 0 0 10px 0;">Informations Client</h3>
              <p style="margin: 5px 0; font-size: 13px;"><strong>Nom:</strong> ${userName}</p>
              <p style="margin: 5px 0; font-size: 13px;"><strong>Email:</strong> ${userEmail}</p>
            </div>
            <div style="flex: 1; text-align: right;">
              <h3 style="color: #667eea; font-size: 14px; text-transform: uppercase; margin: 0 0 10px 0;">Détails Événement</h3>
              <p style="margin: 5px 0; font-size: 13px;"><strong>Événement:</strong> ${this.selectedReservation.eventName}</p>
              <p style="margin: 5px 0; font-size: 13px;"><strong>Date:</strong> ${new Date(this.selectedReservation.eventDate).toLocaleDateString('fr-FR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit' })}</p>
              <p style="margin: 5px 0; font-size: 13px;"><strong>Nombre de billets:</strong> ${this.selectedReservation.tickets.length}</p>
            </div>
          </div>

          <div style="margin-bottom: 30px;">
            <table style="width: 100%; border-collapse: collapse;">
              <thead>
                <tr style="background: #667eea; color: white;">
                  <th style="padding: 12px; text-align: left; font-size: 13px; border: 1px solid #667eea;">Zone</th>
                  <th style="padding: 12px; text-align: left; font-size: 13px; border: 1px solid #667eea;">Rangée</th>
                  <th style="padding: 12px; text-align: left; font-size: 13px; border: 1px solid #667eea;">Siège</th>
                  <th style="padding: 12px; text-align: right; font-size: 13px; border: 1px solid #667eea;">Prix</th>
                </tr>
              </thead>
              <tbody>
                ${ticketRows}
              </tbody>
              <tfoot>
                <tr style="background: #f5f5f5; font-weight: bold;">
                  <td colspan="3" style="padding: 12px; border: 1px solid #ddd; text-align: right; font-size: 16px;">MONTANT TOTAL</td>
                  <td style="padding: 12px; border: 1px solid #ddd; text-align: right; font-size: 16px; color: #667eea;">${totalAmount.toFixed(2)} €</td>
                </tr>
              </tfoot>
            </table>
          </div>

          <div style="border-top: 2px solid #eee; padding-top: 20px; text-align: center; color: #999; font-size: 11px;">
            <p style="margin: 5px 0;">Merci de votre confiance | TicketMaster © ${new Date().getFullYear()}</p>
            <p style="margin: 5px 0;">Ces billets sont personnels et non transférables</p>
            <p style="margin: 5px 0;">Généré le ${new Date().toLocaleDateString('fr-FR')} à ${new Date().toLocaleTimeString('fr-FR')}</p>
          </div>
        </div>
      `;
      document.body.appendChild(summaryDiv);

      await new Promise(resolve => setTimeout(resolve, 100));

      const summaryCanvas = await html2canvas(summaryDiv, {
        scale: 2,
        logging: false,
        backgroundColor: '#ffffff'
      });

      document.body.removeChild(summaryDiv);

      const summaryImgData = summaryCanvas.toDataURL('image/png');
      const summaryImgWidth = pdfWidth;
      const summaryImgHeight = (summaryCanvas.height * summaryImgWidth) / summaryCanvas.width;

      pdf.addImage(summaryImgData, 'PNG', 0, 0, summaryImgWidth, Math.min(summaryImgHeight, pdfHeight));

      // PAGES 2+: One page per ticket with QR code
      for (let i = 0; i < ticketsWithBase64.length; i++) {
        const ticket = ticketsWithBase64[i];

        pdf.addPage();

        const ticketDiv = document.createElement('div');
        ticketDiv.style.position = 'absolute';
        ticketDiv.style.left = '-9999px';
        ticketDiv.style.width = '800px';
        ticketDiv.style.padding = '40px';
        ticketDiv.style.backgroundColor = '#ffffff';
        ticketDiv.innerHTML = `
          <div style="font-family: Arial, sans-serif; color: #333;">
            <div style="text-align: center; margin-bottom: 30px; border-bottom: 3px solid #667eea; padding-bottom: 20px;">
              <h1 style="color: #667eea; font-size: 36px; margin: 0;">🎫 Billet ${i + 1}/${this.selectedReservation.tickets.length}</h1>
              <p style="color: #666; font-size: 14px; margin: 5px 0 0 0;">${this.selectedReservation.eventName}</p>
            </div>

            <div style="text-align: center; margin-bottom: 30px;">
              <p style="font-size: 16px; color: #666; margin: 5px 0;">
                ${new Date(this.selectedReservation.eventDate).toLocaleDateString('fr-FR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit' })}
              </p>
            </div>

            <div style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px; border-radius: 12px; margin-bottom: 30px;">
              <div style="text-align: center; margin-bottom: 30px;">
                <h2 style="color: white; font-size: 24px; margin: 0 0 20px 0;">Zone ${ticket.zoneName}</h2>
                <div style="display: flex; justify-content: center; gap: 40px; margin-bottom: 30px;">
                  <div>
                    <p style="color: rgba(255,255,255,0.8); font-size: 12px; margin: 0;">Rangée</p>
                    <p style="color: white; font-size: 28px; font-weight: bold; margin: 5px 0 0 0;">${ticket.row}</p>
                  </div>
                  <div>
                    <p style="color: rgba(255,255,255,0.8); font-size: 12px; margin: 0;">Siège</p>
                    <p style="color: white; font-size: 28px; font-weight: bold; margin: 5px 0 0 0;">#${ticket.number}</p>
                  </div>
                  <div>
                    <p style="color: rgba(255,255,255,0.8); font-size: 12px; margin: 0;">Prix</p>
                    <p style="color: white; font-size: 28px; font-weight: bold; margin: 5px 0 0 0;">${ticket.price.toFixed(2)} €</p>
                  </div>
                </div>
              </div>

              <div style="background: white; padding: 30px; border-radius: 12px; text-align: center;">
                <h3 style="color: #667eea; margin: 0 0 20px 0; font-size: 18px;">Code QR d'Entrée</h3>
                <img src="${ticket.qrCodeBase64}" style="width: 250px; height: 250px; display: block; margin: 0 auto; border: 4px solid #667eea; border-radius: 8px;">
                <p style="color: #333; font-family: monospace; font-size: 13px; margin: 20px 0 0 0; font-weight: bold;">
                  ${ticket.ticketNumber}
                </p>
                <p style="color: #666; font-size: 11px; margin: 15px 0 0 0;">
                  Présentez ce QR code à l'entrée
                </p>
              </div>
            </div>

            <div style="text-align: center; color: #999; font-size: 11px;">
              <p style="margin: 5px 0;">Client: ${userName} (${userEmail})</p>
              <p style="margin: 5px 0;">Ce billet est personnel et non transférable</p>
            </div>
          </div>
        `;
        document.body.appendChild(ticketDiv);

        await new Promise(resolve => setTimeout(resolve, 100));

        const ticketCanvas = await html2canvas(ticketDiv, {
          scale: 2,
          logging: false,
          backgroundColor: '#ffffff'
        });

        document.body.removeChild(ticketDiv);

        const ticketImgData = ticketCanvas.toDataURL('image/png');
        const ticketImgWidth = pdfWidth;
        const ticketImgHeight = (ticketCanvas.height * ticketImgWidth) / ticketCanvas.width;

        pdf.addImage(ticketImgData, 'PNG', 0, 0, ticketImgWidth, Math.min(ticketImgHeight, pdfHeight));
      }

      pdf.save(`facture-complete-${this.selectedReservation.eventName}.pdf`);
    } catch (error) {
      console.error('Erreur génération PDF:', error);
      alert('Erreur lors de la génération du PDF');
    }
  }

  // Helper pour précharger les images avant la génération du PDF
  private preloadImage(url: string): Promise<void> {
    return new Promise((resolve, reject) => {
      const img = new Image();
      img.crossOrigin = 'anonymous';
      img.onload = () => resolve();
      img.onerror = () => resolve(); // Continuer même si l'image ne charge pas
      img.src = url;
    });
  }

  // Convertir une image URL en base64 pour éviter les problèmes CORS
  private async imageUrlToBase64(url: string): Promise<string> {
    try {
      const response = await fetch(url);
      const blob = await response.blob();
      return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result as string);
        reader.onerror = reject;
        reader.readAsDataURL(blob);
      });
    } catch (error) {
      console.error('Erreur conversion image:', error);
      return url; // Retourner l'URL originale si échec
    }
  }
}
