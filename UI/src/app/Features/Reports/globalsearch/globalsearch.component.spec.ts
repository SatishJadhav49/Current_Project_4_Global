import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GlobalsearchComponent } from './globalsearch.component';

describe('GlobalsearchComponent', () => {
  let component: GlobalsearchComponent;
  let fixture: ComponentFixture<GlobalsearchComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GlobalsearchComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GlobalsearchComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
