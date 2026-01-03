import { NgIf, DatePipe, NgFor, CommonModule, registerLocaleData } from '@angular/common';
import { Component, inject, ElementRef, ViewChild, OnDestroy, ChangeDetectorRef, NgZone, LOCALE_ID } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../services/auth.service';
import { ReservationService, CreateReservationDto, ProcessPaymentDto } from '../services/reservation.service';
import { SignalRService } from '../services/signalr.service';
import jsPDF from 'jspdf';
import html2canvas from 'html2canvas';
import localeFr from '@angular/common/locales/fr';

// Enregistrer la locale française
registerLocaleData(localeFr);

@Component({
  selector: 'app-event-details',
  imports: [CommonModule, NgIf, NgFor, DatePipe, RouterLink, FormsModule],
  standalone: true,
  templateUrl: './event-details.html',
  styleUrl: './event-details.scss',
  providers: [{ provide: LOCALE_ID, useValue: 'fr-FR' }]
})
export class EventDetails implements OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private reservationService = inject(ReservationService);
  private signalRService = inject(SignalRService);
  private cdr = inject(ChangeDetectorRef);
  private zone = inject(NgZone);
  private base = 'https://localhost:7287/api';

  currentReservationId: number | null = null;

  // IMPORTANT: add #viewport in your HTML on the container with the canvas
  // <div #viewport class="... map-viewport" ...>
  @ViewChild('viewport', { static: false }) viewportRef!: ElementRef<HTMLElement>;

  event: any = null;
  seats: any[] = [];
  filteredSeats: any[] = [];
  selected: any[] = [];
  zoneList: string[] = [];
  filterZone = '';

  // Tooltip au hover
  hoveredSeat: any = null;
  tooltipX = 0;
  tooltipY = 0;

  zones: any[] = [];
  defaultImage = 'default-event.jpeg';

  loading = true;
  loadingPay = false;
  error: string | null = null;

  // Zoom/Pan functionality
  zoom = 1;
  panX = 0;
  panY = 0;

  readonly minZoom = 0.5;
  readonly maxZoom = 24.0; // Zoom max augmenté pour voir les détails

  // Cercle de fond dynamique basé sur le nombre de sièges
  get backgroundCircleRadius(): number {
    const totalSeats = this.seats.length;
    if (totalSeats <= 200) {
      return 49; // Taille normale pour petits événements
    } else {
      // Plus de sièges = cercle plus grand
      const scaleFactor = Math.sqrt(totalSeats / 200);
      return Math.min(49 * scaleFactor, 95); // Max 95
    }
  }

  private dragging = false;
  private lastX = 0;
  private lastY = 0;

  // Notifications
  toastMessage = '';
  toastType: 'success' | 'error' | 'warning' = 'success';
  showToast = false;

  // Payment modal
  showPaymentModal = false;
  paymentMethod: 'card' | 'paypal' | 'bancontact' = 'card';
  cardNumber = '';
  cardName = '';
  cardExpiry = '';
  cardCVV = '';

  // Tickets avec QR codes
  generatedTickets: any[] = [];
  showTicketsModal = false;

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) { this.loading = false; this.error = 'Identifiant invalide.'; return; }

    // Check for existing reservation to continue
    const reservationId = this.route.snapshot.queryParamMap.get('reservationId');
    if (reservationId) {
      this.currentReservationId = Number(reservationId);
    }

    // Connect to SignalR for real-time updates
    this.signalRService.startConnection().then(() => {
      this.subscribeToSignalREvents();
    }).catch(err => {
      console.error('SignalR connection failed:', err);
    });

    this.http.get<any>(`${this.base}/event/${id}`).subscribe({
      next: (e) => {
        this.event = e;
        this.loadSeats(id);
      },
      error: (err) => {
        console.error('GET /event/{id} failed:', err);
        this.error = 'Événement introuvable.';
        this.loading = false;
      }
    });
  }

  ngOnDestroy() {
    this.signalRService.stopConnection();
  }

  // ========== SESSION PERSISTENCE ==========

  /**
   * Save selection state to localStorage
   * Allows restoring selected seats after page refresh
   */
  private saveSelectionToLocalStorage() {
    if (!this.event || this.selected.length === 0) {
      this.clearSelectionFromLocalStorage();
      return;
    }

    const storageKey = `seat-selection-event-${this.event.id}`;
    const data = {
      eventId: this.event.id,
      reservationId: this.currentReservationId,
      seatIds: this.selected.map(s => s.id),
      expiresAt: this.currentReservationId ? new Date(Date.now() + 15 * 60 * 1000).toISOString() : null,
      savedAt: new Date().toISOString()
    };

    localStorage.setItem(storageKey, JSON.stringify(data));
    console.log('[STORAGE] Selection saved to localStorage:', data);
  }

  /**
   * Restore selected seats from localStorage
   * Verify that the reservation has not expired
   */
  private restoreSelectionFromLocalStorage() {
    if (!this.event) return;

    const storageKey = `seat-selection-event-${this.event.id}`;
    const savedData = localStorage.getItem(storageKey);

    if (!savedData) {
      console.log('[INFO] No saved selection found');
      return;
    }

    try {
      const data = JSON.parse(savedData);

      // Check if reservation has expired
      if (data.expiresAt) {
        const expirationDate = new Date(data.expiresAt);
        if (expirationDate < new Date()) {
          console.log('[TIMER] Saved reservation has expired');
          this.clearSelectionFromLocalStorage();
          return;
        }
      }

      console.log('[STORAGE] Restoring selection from localStorage:', data);

      // Restore currentReservationId
      if (data.reservationId) {
        this.currentReservationId = data.reservationId;
      }

      // Restore selected seats
      // Mark _selected BEFORE canSelect() to allow restoring ReservedTemp seats
      data.seatIds.forEach((seatId: number) => {
        const seat = this.seats.find(s => s.id === seatId);
        if (seat) {
          // Mark as selected BEFORE verification
          seat._selected = true;
          // Now canSelect() will return true due to the _selected flag
          if (this.canSelect(seat)) {
            this.selected.push(seat);
          } else {
            // If seat is no longer available (sold, etc), unmark it
            seat._selected = false;
          }
        }
      });

      if (this.selected.length > 0) {
        this.seats = [...this.seats];
        this.applyFilter();
        this.showNotification(
          `${this.selected.length} seat(s) restored from your previous session.`,
          'success'
        );
      } else if (data.seatIds.length > 0) {
        // Seats existed but are no longer available
        this.clearSelectionFromLocalStorage();
        this.showNotification(
          'Your seats are no longer available.',
          'warning'
        );
      }
    } catch (err) {
      console.error('[ERROR] Error during restoration:', err);
      this.clearSelectionFromLocalStorage();
    }
  }

  /**
   * Clear saved selection from localStorage
   */
  private clearSelectionFromLocalStorage() {
    if (!this.event) return;
    const storageKey = `seat-selection-event-${this.event.id}`;
    localStorage.removeItem(storageKey);
    console.log('[CLEANUP] Selection cleared from localStorage');
  }

  // ========== FIN SESSION PERSISTENCE ==========

  private subscribeToSignalREvents() {
    const currentUserId = this.authService.getCurrentUser()?.userId;

    console.log(`[ID] My userId: ${currentUserId} (type: ${typeof currentUserId})`);

    // Listen to main backend event
    this.signalRService.seatStatusChanged$.subscribe(data => {
      console.log('[SIGNALR] Update received:', data);
      console.log(`[DEBUG] Comparison: data.userId=${data.userId} (${typeof data.userId}) vs currentUserId=${currentUserId} (${typeof currentUserId})`);
      console.log(`[DEBUG] Event check: data.eventId=${data.eventId} vs this.event?.id=${this.event?.id}, seatIds=${data.seatIds?.length || 0}`);

      // Use this.event?.id instead of captured currentEventId
      if (data.eventId === this.event?.id && data.seatIds && data.seatIds.length > 0) {
        // Execute in Angular zone to force detection
        this.zone.run(() => {
          let needsUpdate = false;

          data.seatIds.forEach((seatId: number) => {
            const seatIndex = this.seats.findIndex(s => s.id === seatId);
            if (seatIndex === -1) return;

            const seat = this.seats[seatIndex];

            // Convert both to Number for strict comparison
            const dataUserId = Number(data.userId);
            const myUserId = Number(currentUserId);

            // Only update if it's not our own action
            if (dataUserId !== myUserId) {
              // Don't update if this seat is in our active selection
              const isInOurSelection = this.selected.some(s => s.id === seatId);

              if (isInOurSelection) {
                console.log(`[SKIP] Skip update for seat ${seatId} (in our active selection)`);
                return; // IMPORTANT: don't update this seat
              }

              console.log(`[UPDATE] Update seat ${seatId}: ${seat.state} → ${data.status} (userId ${dataUserId} ≠ ${myUserId})`);

              // Créer un nouveau objet siège SANS _selected (réservé par quelqu'un d'autre)
              this.seats[seatIndex] = { ...seat, state: data.status, _selected: false };
              needsUpdate = true;

              // If another user has reserved a seat we had selected
              if (data.status === 'ReservedTemp' && seat._selected) {
                this.selected = this.selected.filter(s => s.id !== seatId);
                this.showNotification(`Seat ${seat.row}-${seat.number} reserved by another user.`, 'warning');
              }
            } else {
              console.log(`[SKIP] Skip our own action for seat ${seatId} (userId ${dataUserId} === ${myUserId})`);
            }
          });

          // Force Angular change detection to update UI
          if (needsUpdate) {
            // Create new array reference to force detection
            this.seats = [...this.seats];
            this.applyFilter();
            console.log('[UPDATE] UI updated after SignalR');
          }
        });
      }
    });

    // Listen to connection status
    console.log('[SIGNALR] Connected:', this.signalRService.isConnected);
  }

  // 2) Sièges
  private loadSeats(eventId: number) {
    this.http.get<any[]>(`${this.base}/event/${eventId}/seats`).subscribe({
      next: (list) => {
        this.seats = (list ?? []).map(s => ({ ...s }));
        this.autoLayoutStadium(this.seats);
        this.zoneList = [...new Set(this.seats.map(s => s.zone?.name).filter(Boolean))] as string[];
        this.applyFilter();
        console.error('seats number :', this.seats.length);
        this.buildZoneLabels();
        this.loading = false;

        // Avoid duplicates by choosing the correct restoration source
        // If coming from "My Reservations" with reservationId, load from API
        // Otherwise, restore from localStorage
        if (this.currentReservationId) {
          // If continuing from existing reservation, load it and open payment modal
          console.log('[LOAD] Loading reservation from API (reservationId in URL)');
          this.loadExistingReservation(this.currentReservationId);
        } else {
          // Restore selection from localStorage (if it exists)
          console.log('[STORAGE] Restoring from localStorage');
          this.restoreSelectionFromLocalStorage();
        }
      },
      error: (err) => {
        console.error('[ERROR] GET /event/{id}/seats failed:', err);
        console.error('[DEBUG] seats number:', this.seats.length);
        this.loading = false;
      }
    });
  }

  /** Layout circulaire */
  private autoLayoutStadium(seats: any[]) {
    const byZone = new Map<string, Map<string, any[]>>();
    for (const s of seats) {
      const z = s.zone?.name || 'Zone';
      if (!byZone.has(z)) byZone.set(z, new Map());
      const rows = byZone.get(z)!;
      const r = (s.row ?? '').toString();
      if (!rows.has(r)) rows.set(r, []);
      rows.get(r)!.push(s);
    }

    const cx = 50, cy = 50;

    // Rayons plus grands pour utiliser plus d'espace du cercle
    const minRadius = 15;
    const maxRadius = 46;

    // Seats properly sized to stay within the circle
    const seatSize = 1.3;
    const totalSeats = seats.length;

    // Espacement dynamique basé sur le nombre de sièges
    const minSpacingBetweenSeats = totalSeats > 200 ? 0.4 : 0.2;

    const zoneNames = [...byZone.keys()].sort();
    const zoneCount = zoneNames.length || 1;
    const anglePerZone = 360 / zoneCount;

    let maxRowsInAnyZone = 0;
    byZone.forEach(rows => {
      maxRowsInAnyZone = Math.max(maxRowsInAnyZone, rows.size);
    });

    // Spacing entre rangées: si > 200 sièges, on augmente x2.5 pour bien voir la séparation
    const ringGapMultiplier = totalSeats > 200 ? 2.5 : 1.0;
    const ringGap = ((maxRadius - minRadius) / Math.max(1, maxRowsInAnyZone - 1)) * ringGapMultiplier;

    zoneNames.forEach((zName, zIndex) => {
      const rows = byZone.get(zName)!;
      const rowKeys = [...rows.keys()].sort((a,b) => a.localeCompare(b, undefined, { numeric: true }));

      const startAngle = -90 + zIndex * anglePerZone;
      const endAngle = -90 + (zIndex + 1) * anglePerZone;
      const padding = 8; // Espacement augmenté entre les colonnes de sièges
      const theta0 = (startAngle + padding) * Math.PI / 180;
      const theta1 = (endAngle - padding) * Math.PI / 180;

      rowKeys.forEach((rowKey, rowIdx) => {
        const arr = rows.get(rowKey)!;
        arr.sort((a,b) => (a.number ?? 0) - (b.number ?? 0));

        const radius = minRadius + rowIdx * ringGap;
        const seatCount = arr.length;

        const arcLength = radius * (theta1 - theta0);
        const availableSpace = arcLength;
        const totalSeatWidth = seatCount * seatSize;
        // Guarantee minimum spacing between seats
        const spacing = seatCount > 1
          ? Math.max(minSpacingBetweenSeats, (availableSpace - totalSeatWidth) / (seatCount - 1))
          : 0;

        arr.forEach((seat, seatIdx) => {
          let angleOffset;
          if (seatCount === 1) {
            angleOffset = (theta0 + theta1) / 2;
          } else {
            const arcPos = seatIdx * (seatSize + spacing);
            angleOffset = theta0 + arcPos / radius;
          }

          const x = cx + radius * Math.cos(angleOffset);
          const y = cy + radius * Math.sin(angleOffset);

          seat._x = x;
          seat._y = y;
          seat._size = seatSize;
          seat._angle = angleOffset * 180 / Math.PI;

          seat.price = seat.price ?? seat.zone?.price ?? 0;
          seat._zoneName = seat.zone?.name || 'Zone';
        });
      });
    });
  }

  private buildZoneLabels() {
    const names = [...new Set(this.seats.map(s => s.zone?.name).filter(Boolean))] as string[];
    names.sort();

    const anglePerZone = 360 / (names.length || 1);
    const cx = 50, cy = 50;
    const labelRadius = 27;

    this.zones = names.map((name, i) => {
      const startAngle = -90 + i * anglePerZone;
      const endAngle = -90 + (i + 1) * anglePerZone;
      const middleAngle = ((startAngle + endAngle) / 2) * Math.PI / 180;

      return {
        name,
        labelX: cx + labelRadius * Math.cos(middleAngle),
        labelY: cy + labelRadius * Math.sin(middleAngle),
      };
    });
  }

  // ---- Filtre / sélection ----
  applyFilter() {
    // Always create new reference to force re-render
    if (this.filterZone) {
      this.filteredSeats = this.seats.filter(s => (s.zone?.name || '') === this.filterZone);
    } else {
      this.filteredSeats = [...this.seats];
    }
    console.log(`[FILTER] applyFilter: ${this.filteredSeats.length} seats filtered`);
  }

  canSelect(s: any) {
    const st = (s.state || '').toLowerCase();
    if (s._selected) return true;
    return st === 'free' || st === 'libre';
  }

  seatFill(s: any) {
    // PRIORITY 1: Seats selected by ME = BLUE (even if reserved)
    if (s._selected) {
      return '#3b82f6'; // Blue - MY seats
    }

    const st = (s.state || '').toLowerCase();

    // PRIORITY 2: Paid seats = RED
    if (st === 'paid' || st.includes('paid') || st.includes('sold') || st.includes('vend')) {
      return '#ef4444'; // Red
    }

    // PRIORITY 3: Seats reserved by OTHERS = ORANGE
    if (st.includes('hold') || st.includes('reserv') || st === 'reservedtemp') {
      return '#f59e0b'; // Orange - reserved by others
    }

    // PRIORITY 4: Free seats = GREEN
    return '#10b981'; // Green
  }

  // Clean CLICK handler (prevents propagation)
  onSeatClick(e: MouseEvent, s: any) {
    e.stopPropagation();
    e.preventDefault();
    this.toggleSeat(s);
  }

  // Clean PAN handler (drag only on background)
  onPointerDown(e: PointerEvent) {
    // Don't start drag if clicking on a seat
    const target = e.target as HTMLElement;
    if (target.tagName.toLowerCase() === 'rect' || target.closest('.seat-square')) {
      return; // Let the seat click event trigger
    }

    this.dragging = true;
    this.lastX = e.clientX;
    this.lastY = e.clientY;
    (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
  }

  onPointerMove(e: PointerEvent) {
    if (!this.dragging) return;

    const dx = e.clientX - this.lastX;
    const dy = e.clientY - this.lastY;

    this.lastX = e.clientX;
    this.lastY = e.clientY;

    this.panX += dx;
    this.panY += dy;
  }

  onPointerUp() {
    this.dragging = false;
  }

  // Clean ZOOM handler (around mouse position)
  onWheel(e: WheelEvent) {
    e.preventDefault();

    const viewport = this.viewportRef?.nativeElement ?? (e.currentTarget as HTMLElement);
    const rect = viewport.getBoundingClientRect();

    const mx = e.clientX - rect.left;
    const my = e.clientY - rect.top;

    const factor = e.deltaY < 0 ? 1.1 : 0.9;
    const newZoom = this.clamp(this.zoom * factor, this.minZoom, this.maxZoom);

    // World point under the mouse
    const wx = (mx - this.panX) / this.zoom;
    const wy = (my - this.panY) / this.zoom;

    this.zoom = newZoom;

    // Keep the point under the mouse fixed
    this.panX = mx - wx * this.zoom;
    this.panY = my - wy * this.zoom;
  }

  zoomIn() { this.zoom = this.clamp(this.zoom * 1.15, this.minZoom, this.maxZoom); }
  zoomOut() { this.zoom = this.clamp(this.zoom * 0.87, this.minZoom, this.maxZoom); }
  resetZoom() { this.zoom = 1; this.panX = 0; this.panY = 0; }
  private clamp(v: number, a: number, b: number) { return Math.max(a, Math.min(b, v)); }

  // SELECT / DESELECT - Reserve immediately on backend
  toggleSeat(s: any) {
    const st = (s.state || '').toLowerCase();

    if (st.includes('sold') || st.includes('vend') || st === 'paid') {
      this.showNotification('Ce siège est déjà vendu.', 'error');
      return;
    }

    if (st.includes('reserv') && !s._selected) {
      this.showNotification('Ce siège est déjà réservé par quelqu\'un d\'autre.', 'warning');
      return;
    }

    // Check authentication before allowing selection
    if (!this.authService.isAuthenticated()) {
      this.showNotification('Veuillez vous connecter pour réserver.', 'warning');
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }

    // Block selection for Admins and Organizers
    const currentUser = this.authService.getCurrentUser();
    if (currentUser && (currentUser.role === 'Admin' || currentUser.role === 'Organisateur')) {
      this.showNotification('Les administrateurs et les organisateur ne peuvent pas réserver de sièges.', 'warning');
      return;
    }

    // Toggle selection
    if (!s._selected) {
      this.selectSeatImmediately(s);
    } else {
      this.deselectSeatImmediately(s);
    }
  }

  // Reserve seat immediately on backend
  private selectSeatImmediately(s: any) {
    // Optimistically update UI
    s._selected = true;
    this.selected = [...this.selected, s];

    // Le backend annule automatiquement les anciennes réservations pending
    // Donc on crée juste une nouvelle réservation avec TOUS les sièges sélectionnés
    const dto: CreateReservationDto = {
      eventId: this.event.id,
      seatIds: this.selected.map(seat => seat.id)
    };

    console.log(`[CREATE] Creating reservation with ${dto.seatIds.length} seat(s):`, dto.seatIds);

    this.reservationService.createReservation(dto).subscribe({
      next: (reservation) => {
        this.currentReservationId = reservation.id;
        console.log(`[SUCCESS] Reservation ${reservation.id} created successfully`);

        // Update ALL selected seats to ReservedTemp
        this.selected.forEach(seat => {
          const seatIndex = this.seats.findIndex(st => st.id === seat.id);
          if (seatIndex !== -1) {
            this.seats[seatIndex] = { ...this.seats[seatIndex], state: 'ReservedTemp', _selected: true };
          }
        });

        // Force change detection
        this.seats = [...this.seats];
        this.selected = [...this.selected];
        this.applyFilter();

        this.showNotification(`${this.selected.length} seat(s) reserved! Expires in 15 min.`, 'success');

        // Save selection to localStorage
        this.saveSelectionToLocalStorage();
      },
      error: (err) => {
        console.error('[ERROR] Error creating reservation:', err);
        // Rollback optimistic update
        s._selected = false;
        this.selected = this.selected.filter(x => x.id !== s.id);
        this.applyFilter();

        const msg = err?.error?.message || 'Impossible de réserver ce siège.';
        this.showNotification(msg, 'error');
      }
    });
  }

  // Deselect seat and update reservation on backend
  private deselectSeatImmediately(s: any) {
    const seatId = s.id;

    // Optimistically update UI
    const seatIndex = this.seats.findIndex(seat => seat.id === seatId);
    if (seatIndex !== -1) {
      this.seats[seatIndex] = { ...this.seats[seatIndex], state: 'Free', _selected: false };
      this.seats = [...this.seats];
    }

    this.selected = this.selected.filter(x => x.id !== seatId);

    // TOUJOURS annuler la réservation actuelle
    if (!this.currentReservationId) {
      this.applyFilter();
      return;
    }

    const oldReservationId = this.currentReservationId;
    this.currentReservationId = null;

    this.reservationService.cancelReservation(oldReservationId).subscribe({
      next: () => {
        console.log(`[SUCCESS] Reservation ${oldReservationId} cancelled`);

        // Si il reste des sièges, créer une NOUVELLE réservation
        if (this.selected.length > 0) {
          const dto: CreateReservationDto = {
            eventId: this.event.id,
            seatIds: this.selected.map(seat => seat.id)
          };

          this.reservationService.createReservation(dto).subscribe({
            next: (reservation) => {
              this.currentReservationId = reservation.id;
              console.log(`[SUCCESS] New reservation ${reservation.id} created with ${this.selected.length} seat(s)`);
              this.applyFilter();

              // Save new selection
              this.saveSelectionToLocalStorage();
            },
            error: (err) => {
              console.error('[ERROR] Error creating new reservation:', err);
              this.showNotification('Erreur lors de la mise à jour.', 'error');
            }
          });
        } else {
          this.showNotification('Réservation annulée.', 'success');
          this.applyFilter();

          // No more selected seats - clear localStorage
          this.clearSelectionFromLocalStorage();
        }
      },
      error: (err) => {
        console.error('Erreur annulation réservation:', err);
        // Rollback
        if (seatIndex !== -1) {
          this.seats[seatIndex] = { ...this.seats[seatIndex], state: 'ReservedTemp', _selected: true };
          this.seats = [...this.seats];
          this.selected = [...this.selected, this.seats[seatIndex]];
          this.currentReservationId = oldReservationId;
        }
        this.applyFilter();
        this.showNotification('Erreur lors de l\'annulation.', 'error');
      }
    });
  }

  private showNotification(message: string, type: 'success' | 'error' | 'warning') {
    this.toastMessage = message;
    this.toastType = type;
    this.showToast = true;

    setTimeout(() => {
      this.showToast = false;
    }, 3000);
  }

  remove(s: any) { if (s._selected) this.toggleSeat(s); }
  total() { return this.selected.reduce((t, s) => t + (s.price || 0), 0); }

  trackBySeatId(index: number, seat: any): any {
    return seat.id;
  }

  // ---- Tooltip au hover ----
  onSeatMouseEnter(event: MouseEvent, seat: any) {
    this.hoveredSeat = seat;
    this.tooltipX = event.clientX;
    this.tooltipY = event.clientY;
  }

  onSeatMouseLeave() {
    this.hoveredSeat = null;
  }

  // ---- Proceed to payment (seats already reserved) ----
  checkout() {
    if (this.selected.length === 0) {
      this.showNotification('Aucun siège sélectionné!', 'warning');
      return;
    }

    if (!this.currentReservationId) {
      this.showNotification('Erreur: aucune réservation en cours.', 'error');
      return;
    }

    // Seats are already reserved, just open payment modal
    this.showPaymentModal = true;
  }

  processPayment() {
    if (!this.currentReservationId) {
      this.showNotification('Aucune réservation en cours.', 'error');
      return;
    }

    if (this.paymentMethod === 'card') {
      if (!this.cardNumber || !this.cardName || !this.cardExpiry || !this.cardCVV) {
        this.showNotification('Veuillez remplir tous les champs.', 'error');
        return;
      }

      // Validate card number (13-19 digits only)
      const cardNumberClean = this.cardNumber.replace(/\s/g, '');
      if (!/^\d{13,19}$/.test(cardNumberClean)) {
        this.showNotification('Le numéro de carte doit contenir entre 13 et 19 chiffres.', 'error');
        return;
      }

      // Validate CVV (exactly 3 digits)
      if (!/^\d{3}$/.test(this.cardCVV)) {
        this.showNotification('Le CVV doit contenir exactement 3 chiffres.', 'error');
        return;
      }

      // Validate expiration date (MM/YY, not in the past)
      const expiryMatch = this.cardExpiry.match(/^(\d{2})\/(\d{2})$/);
      if (!expiryMatch) {
        this.showNotification('La date d\'expiration doit être au format MM/YY.', 'error');
        return;
      }

      const month = parseInt(expiryMatch[1], 10);
      const year = parseInt(expiryMatch[2], 10) + 2000;

      if (month < 1 || month > 12) {
        this.showNotification('Le mois doit être entre 01 et 12.', 'error');
        return;
      }

      const now = new Date();
      const currentYear = now.getFullYear();
      const currentMonth = now.getMonth() + 1;

      if (year < currentYear || (year === currentYear && month < currentMonth)) {
        this.showNotification('La carte est expirée.', 'error');
        return;
      }
    }

    this.loadingPay = true;

    const paymentDto: ProcessPaymentDto = {
      reservationId: this.currentReservationId,
      paymentMethod: this.paymentMethod,
      cardNumber: this.paymentMethod === 'card' ? this.cardNumber : undefined,
      cardName: this.paymentMethod === 'card' ? this.cardName : undefined,
      cardExpiry: this.paymentMethod === 'card' ? this.cardExpiry : undefined,
      cardCVV: this.paymentMethod === 'card' ? this.cardCVV : undefined
    };

    this.reservationService.processPayment(paymentDto).subscribe({
      next: (reservation) => {
        // Update seat states to Paid
        this.selected.forEach(s => {
          s.state = 'Paid';
          s._selected = false;
        });

        this.selected = [];
        this.currentReservationId = null;
        this.showPaymentModal = false;
        this.loadingPay = false;

        // Clear selection from localStorage (payment successful)
        this.clearSelectionFromLocalStorage();

        // Store generated tickets and show modal
        this.generatedTickets = reservation.tickets || [];
        this.showTicketsModal = true;

        this.showNotification(`Paiement réussi! ${reservation.tickets?.length || 0} ticket(s) généré(s).`, 'success');
      },
      error: (err) => {
        console.error('[ERROR] Payment failed:', err);
        const msg = err?.error?.message || err?.error || 'Erreur lors du paiement.';
        this.showNotification(msg, 'error');
        this.loadingPay = false;
      }
    });
  }

  showTickets() {
    this.showTicketsModal = true;
  }

  async downloadAllTicketsPDF() {
    const printContent = document.getElementById('tickets-print-area');
    if (!printContent) {
      this.showNotification('Impossible de générer le PDF', 'error');
      return;
    }

    try {
      this.showNotification('Génération du PDF en cours...', 'success');

      // Précharger toutes les images QR code
      const qrImages = printContent.querySelectorAll('img');
      await Promise.all(Array.from(qrImages).map(img => this.preloadImage(img.src)));

      // Wait for DOM to be properly rendered
      await new Promise(resolve => setTimeout(resolve, 100));

      const canvas = await html2canvas(printContent, {
        scale: 2,
        allowTaint: true,
        logging: false,
        backgroundColor: '#ffffff'
      });

      const imgData = canvas.toDataURL('image/png');
      const pdf = new jsPDF('p', 'mm', 'a4');

      const pdfWidth = pdf.internal.pageSize.getWidth();
      const pdfHeight = pdf.internal.pageSize.getHeight();
      const imgWidth = pdfWidth - 20;
      const imgHeight = (canvas.height * imgWidth) / canvas.width;

      let heightLeft = imgHeight;
      let position = 10;

      pdf.addImage(imgData, 'PNG', 10, position, imgWidth, imgHeight);
      heightLeft -= pdfHeight;

      while (heightLeft > 0) {
        position = heightLeft - imgHeight + 10;
        pdf.addPage();
        pdf.addImage(imgData, 'PNG', 10, position, imgWidth, imgHeight);
        heightLeft -= pdfHeight;
      }

      pdf.save(`billets-${this.event?.name || 'event'}.pdf`);
      this.showNotification('PDF téléchargé avec succès!', 'success');
    } catch (error) {
      console.error('[ERROR] PDF generation error:', error);
      this.showNotification('Erreur lors de la génération du PDF', 'error');
    }
  }

  async downloadSingleTicket(ticket: any) {
    try {
      this.showNotification('Génération du PDF en cours...', 'success');

      // Précharger l'image QR code
      await this.preloadImage(ticket.qrCodeUrl);

      // Create a temporary div with the ticket
      const tempDiv = document.createElement('div');
      tempDiv.style.position = 'absolute';
      tempDiv.style.left = '-9999px';
      tempDiv.style.width = '400px';
      tempDiv.innerHTML = `
        <div style="
          border: 3px solid #667eea;
          border-radius: 12px;
          padding: 20px;
          background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
          color: white;
          font-family: Arial, sans-serif;
        ">
          <div style="font-size: 24px; font-weight: bold; margin-bottom: 15px; border-bottom: 2px solid white; padding-bottom: 10px;">
            ${ticket.eventName}
          </div>
          <div style="margin: 20px 0;">
            <div style="background: rgba(255,255,255,0.2); padding: 10px; border-radius: 6px; margin-bottom: 10px;">
              <div style="font-size: 11px; text-transform: uppercase; font-weight: 600;">Zone</div>
              <div style="font-size: 18px; font-weight: bold; margin-top: 5px;">${ticket.zone}</div>
            </div>
            <div style="background: rgba(255,255,255,0.2); padding: 10px; border-radius: 6px; margin-bottom: 10px;">
              <div style="font-size: 11px; text-transform: uppercase; font-weight: 600;">Rangée</div>
              <div style="font-size: 18px; font-weight: bold; margin-top: 5px;">${ticket.row}</div>
            </div>
            <div style="background: rgba(255,255,255,0.2); padding: 10px; border-radius: 6px; margin-bottom: 10px;">
              <div style="font-size: 11px; text-transform: uppercase; font-weight: 600;">Siège</div>
              <div style="font-size: 18px; font-weight: bold; margin-top: 5px;">#${ticket.number}</div>
            </div>
            <div style="background: rgba(255,255,255,0.2); padding: 10px; border-radius: 6px; margin-bottom: 10px;">
              <div style="font-size: 11px; text-transform: uppercase; font-weight: 600;">Prix</div>
              <div style="font-size: 18px; font-weight: bold; margin-top: 5px;">${ticket.price} €</div>
            </div>
          </div>
          <div style="text-align: center; background: white; padding: 20px; border-radius: 8px; margin-top: 20px;">
            <img src="${ticket.qrCodeUrl}" style="max-width: 200px; border: 4px solid #667eea; border-radius: 8px;">
            <div style="font-size: 12px; font-family: monospace; margin-top: 10px; color: #333; font-weight: bold;">
              ${ticket.ticketId}
            </div>
          </div>
        </div>
      `;
      document.body.appendChild(tempDiv);

      // Wait for DOM to be properly rendered
      await new Promise(resolve => setTimeout(resolve, 100));

      const canvas = await html2canvas(tempDiv, {
        scale: 2,
        allowTaint: true,
        logging: false,
        backgroundColor: '#ffffff'
      });

      document.body.removeChild(tempDiv);

      const imgData = canvas.toDataURL('image/png');
      const pdf = new jsPDF('p', 'mm', 'a4');

      const pdfWidth = pdf.internal.pageSize.getWidth();
      const imgWidth = pdfWidth - 40;
      const imgHeight = (canvas.height * imgWidth) / canvas.width;

      pdf.addImage(imgData, 'PNG', 20, 20, imgWidth, imgHeight);
      pdf.save(`billet-${ticket.ticketId}.pdf`);

      this.showNotification('PDF téléchargé avec succès!', 'success');
    } catch (error) {
      console.error('[ERROR] PDF generation error:', error);
      this.showNotification('Erreur lors de la génération du PDF', 'error');
    }
  }

  /**
   * Load an existing pending reservation and pre-select its seats
   */
  private loadExistingReservation(reservationId: number) {
    this.reservationService.getReservation(reservationId).subscribe({
      next: (reservation) => {
        if (reservation.status !== 'Pending') {
          this.showNotification('Cette réservation n\'est plus en attente.', 'warning');
          this.currentReservationId = null;
          return;
        }

        // Pre-select seats from the reservation
        // Mark _selected BEFORE canSelect() to allow selecting ReservedTemp seats
        reservation.seats.forEach((reservedSeat: any) => {
          const seat = this.seats.find(s => s.id === reservedSeat.seatId);
          if (seat) {
            // Mark as selected BEFORE verification
            seat._selected = true;
            // Now canSelect() will return true
            if (this.canSelect(seat)) {
              this.selected.push(seat);
            } else {
              // If seat is no longer available, unmark it
              seat._selected = false;
            }
          }
        });

        // Force change detection
        this.seats = [...this.seats];
        this.applyFilter();

        if (this.selected.length > 0) {
          // Open payment modal immediately
          this.showPaymentModal = true;
          this.showNotification('Continuez votre paiement pour finaliser la réservation.', 'success');

          // Save to localStorage in case user refreshes during payment
          this.saveSelectionToLocalStorage();
        } else {
          this.showNotification('Les sièges de cette réservation ne sont plus disponibles.', 'warning');
          this.currentReservationId = null;
        }
      },
      error: (err) => {
        console.error('[ERROR] Failed to load reservation:', err);
        this.showNotification('Impossible de charger la réservation.', 'error');
        this.currentReservationId = null;
      }
    });
  }

  // Helper to preload images before PDF generation
  private preloadImage(url: string): Promise<void> {
    return new Promise((resolve, reject) => {
      const img = new Image();
      img.crossOrigin = 'anonymous';
      img.onload = () => resolve();
      img.onerror = () => resolve(); // Continue even if image doesn't load
      img.src = url;
    });
  }
}
