import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { appConfig } from './app.config';
import { FakePlannerData } from './core/data/fake-planner-data';
import { PlannerData } from './core/data/planner-data';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      // A configuração real, mas com dados falsos no lugar da API (os testes não dependem de um servidor).
      providers: [...appConfig.providers, { provide: PlannerData, useClass: FakePlannerData }],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
