import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';

export interface SeatUpdate {
  eventId: number;
  seatId?: number;
  seatIds?: number[];
  userId?: number;
  state: string;
  timestamp: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection?: signalR.HubConnection;
  private readonly HUB_URL = 'https://localhost:7287/hubs/seat';

  public seatStatusChanged$ = new Subject<any>();
  public seatReserved$ = new Subject<SeatUpdate>();
  public seatReleased$ = new Subject<SeatUpdate>();
  public seatPaid$ = new Subject<SeatUpdate>();
  public seatsReserved$ = new Subject<SeatUpdate>();
  public seatsReleased$ = new Subject<SeatUpdate>();
  public seatsPaid$ = new Subject<SeatUpdate>();

  public isConnected = false;

  constructor() {}

  public startConnection(): Promise<void> {
    console.log('[SIGNALR] Attempting connection to:', this.HUB_URL);

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.HUB_URL)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Debug)
      .build();

    this.registerEventListeners();

    return this.hubConnection
      .start()
      .then(() => {
        this.isConnected = true;
        console.log('[SUCCESS] SignalR connected! ConnectionId:', this.hubConnection?.connectionId);
        console.log('[DEBUG] State:', this.hubConnection?.state);
      })
      .catch(err => {
        console.error('[ERROR] SignalR connection error:', err);
        console.error('[DEBUG] URL:', this.HUB_URL);
        console.error('[DEBUG] Details:', err.message);
        this.isConnected = false;
      });
  }

  public stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop().then(() => {
        this.isConnected = false;
        console.log('SignalR déconnecté');
      });
    }
  }

  private registerEventListeners(): void {
    if (!this.hubConnection) return;

    console.log('[SIGNALR] Registering event listeners...');

    // Main event sent from backend
    this.hubConnection.on('SeatStatusChanged', (data: any) => {
      console.log('[SIGNALR] SeatStatusChanged received:', data);
      this.seatStatusChanged$.next(data);
    });

    this.hubConnection.on('SeatReserved', (data: SeatUpdate) => {
      this.seatReserved$.next(data);
    });

    this.hubConnection.on('SeatReleased', (data: SeatUpdate) => {
      this.seatReleased$.next(data);
    });

    this.hubConnection.on('SeatPaid', (data: SeatUpdate) => {
      this.seatPaid$.next(data);
    });

    this.hubConnection.on('SeatsReserved', (data: SeatUpdate) => {
      this.seatsReserved$.next(data);
    });

    this.hubConnection.on('SeatsReleased', (data: SeatUpdate) => {
      this.seatsReleased$.next(data);
    });

    this.hubConnection.on('SeatsPaid', (data: SeatUpdate) => {
      this.seatsPaid$.next(data);
    });

    this.hubConnection.onreconnecting((error) => {
      this.isConnected = false;
      console.log('Reconnexion SignalR...', error);
    });

    this.hubConnection.onreconnected((connectionId) => {
      this.isConnected = true;
      console.log('SignalR reconnecté:', connectionId);
    });

    this.hubConnection.onclose((error) => {
      this.isConnected = false;
      console.log('SignalR fermé:', error);
    });
  }
}
