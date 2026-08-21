import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HealthReport, SystemInfo, SystemService } from '../../core/system.service';

interface DependencyStatus {
  readonly name: string;
  readonly status: string;
}

/**
 * Phase 0 smoke screen. It carries no business meaning — it exists to prove, from the
 * browser, that the full chain works: Angular to the API, the API to PostgreSQL, Redis
 * and MinIO. Real feature screens replace it from Phase 1 onwards.
 */
@Component({
  selector: 'app-system-status',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe],
  templateUrl: './system-status.html',
  styleUrl: './system-status.scss',
})
export class SystemStatusComponent {
  readonly #system = inject(SystemService);

  protected readonly info = signal<SystemInfo | null>(null);
  protected readonly dependencies = signal<readonly DependencyStatus[]>([]);
  protected readonly overallStatus = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly loading = signal(true);

  constructor() {
    void this.refresh();
  }

  protected async refresh(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const [info, health] = await Promise.all([
        this.#system.getInfo(),
        this.#system.getHealth().catch((): HealthReport | null => null),
      ]);

      this.info.set(info);

      if (health) {
        this.overallStatus.set(health.status);
        this.dependencies.set(
          Object.entries(health.entries).map(([name, entry]) => ({ name, status: entry.status })),
        );
      } else {
        // The readiness probe answers 503 when a dependency is down, and the browser
        // surfaces that as an error. Not reaching the API at all is the real failure.
        this.overallStatus.set('Unhealthy');
        this.dependencies.set([]);
      }
    } catch {
      this.error.set('The API is not reachable. Is it running on http://localhost:5000?');
    } finally {
      this.loading.set(false);
    }
  }

  protected isHealthy(status: string): boolean {
    return status === 'Healthy';
  }
}
