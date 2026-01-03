import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ReservationDto {
  id: number;
  userId: number;
  userName: string;
  eventId: number;
  eventName: string;
  eventDate: string;
  status: string;
  totalAmount: number;
  createdAt: string;
  expiresAt?: string;
  seats: ReservationSeatDto[];
  tickets?: TicketDto[];
}

export interface ReservationSeatDto {
  seatId: number;
  row: string;
  number: number;
  zoneName: string;
  priceAtBooking: number;
}

export interface TicketDto {
  id: number;
  ticketNumber: string;
  qrCodeUrl: string;
  qrCodeData: string;
  generatedAt: string;
  isUsed: boolean;
  seatId: number;
  row: string;
  number: number;
  zoneName: string;
  price: number;
  eventName: string;
  eventDate: string;
}

export interface CreateReservationDto {
  eventId: number;
  seatIds: number[];
}

export interface ProcessPaymentDto {
  reservationId: number;
  paymentMethod: string;
  cardNumber?: string;
  cardName?: string;
  cardExpiry?: string;
  cardCVV?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ReservationService {
  private readonly API_URL = 'https://localhost:7287/api/Reservation';
  private readonly PAYMENT_URL = 'https://localhost:7287/api/Payment';
  private readonly TICKET_URL = 'https://localhost:7287/api/Ticket';

  constructor(private http: HttpClient) {}

  getMyReservations(pageNumber: number = 1, pageSize: number = 10): Observable<any> {
    const params = {
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString()
    };
    return this.http.get<any>(`${this.API_URL}/my`, { params });
  }

  getReservation(id: number): Observable<ReservationDto> {
    return this.http.get<ReservationDto>(`${this.API_URL}/${id}`);
  }

  createReservation(dto: CreateReservationDto): Observable<ReservationDto> {
    return this.http.post<ReservationDto>(this.API_URL, dto);
  }

  cancelReservation(id: number): Observable<any> {
    return this.http.delete(`${this.API_URL}/${id}`);
  }

  processPayment(dto: ProcessPaymentDto): Observable<ReservationDto> {
    return this.http.post<ReservationDto>(`${this.PAYMENT_URL}/process`, dto);
  }

  getMyTickets(): Observable<TicketDto[]> {
    return this.http.get<TicketDto[]>(`${this.TICKET_URL}/my`);
  }

  getTicket(id: number): Observable<TicketDto> {
    return this.http.get<TicketDto>(`${this.TICKET_URL}/${id}`);
  }
}
