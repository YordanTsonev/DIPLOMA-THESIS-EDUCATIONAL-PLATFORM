import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UserRole } from '../../core/auth/auth.models';
import { AuthService } from '../../core/auth/auth.service';
import { SchoolClassesComponent } from './classes';

const YEAR = {
  id: 'year-1',
  name: '2026/2027',
  startsOn: '2026-09-15',
  endsOn: '2027-06-30',
  isCurrent: true,
};

const USERS_URL = '/api/v1/users';

async function setup(role: UserRole): Promise<{
  http: HttpTestingController;
  fixture: ComponentFixture<SchoolClassesComponent>;
}> {
  TestBed.configureTestingModule({
    imports: [SchoolClassesComponent],
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: AuthService, useValue: { role: signal(role) } },
    ],
  });

  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(SchoolClassesComponent);
  fixture.detectChanges();

  http.expectOne('/api/v1/academic-years').flush([YEAR]);
  await fixture.whenStable();

  http.expectOne((request) => request.url === '/api/v1/classes').flush([]);
  await fixture.whenStable();

  return { http, fixture };
}

function text(fixture: ComponentFixture<SchoolClassesComponent>): string {
  fixture.detectChanges();
  return (fixture.nativeElement as HTMLElement).textContent ?? '';
}

describe('SchoolClassesComponent', () => {
  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
  });

  it('never calls the admin-only users endpoint for a teacher', async () => {
    const { http } = await setup(UserRole.Teacher);

    http.expectNone((request) => request.url === USERS_URL);
  });

  it('hides the create-class form from a teacher', async () => {
    const { fixture } = await setup(UserRole.Teacher);

    expect(text(fixture)).not.toContain('Нов клас');
  });

  it('shows no error banner to a teacher', async () => {
    const { fixture } = await setup(UserRole.Teacher);

    expect(fixture.nativeElement.querySelector('.ep-alert.is-error')).toBeNull();
  });

  it('loads teachers and subjects for an admin', async () => {
    const { http } = await setup(UserRole.Admin);

    http
      .expectOne((request) => request.url === USERS_URL)
      .flush({
        items: [],
        page: 1,
        pageSize: 100,
        totalCount: 0,
        totalPages: 0,
      });
    http.expectOne((request) => request.url === '/api/v1/subjects').flush([]);
  });

  it('shows the create-class form to an admin', async () => {
    const { http, fixture } = await setup(UserRole.Admin);

    http
      .expectOne((request) => request.url === USERS_URL)
      .flush({
        items: [],
        page: 1,
        pageSize: 100,
        totalCount: 0,
        totalPages: 0,
      });
    http.expectOne((request) => request.url === '/api/v1/subjects').flush([]);
    await fixture.whenStable();

    expect(text(fixture)).toContain('Нов клас');
  });
});
