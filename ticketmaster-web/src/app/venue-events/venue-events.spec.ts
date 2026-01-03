import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VenueEvents } from './venue-events';

describe('VenueEvents', () => {
  let component: VenueEvents;
  let fixture: ComponentFixture<VenueEvents>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VenueEvents]
    })
    .compileComponents();

    fixture = TestBed.createComponent(VenueEvents);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
