import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface SystemInfo {
  readonly application: string;
  readonly version: string;
  readonly environment: string;
  readonly serverTimeUtc: string;
}

export interface HealthEntry {
  readonly status: string;
}

export interface HealthReport {
  readonly status: string;
  readonly totalDuration: string;
  readonly entries: Record<string, HealthEntry>;
}

/**
 * Talks to the API's diagnostic endpoints.
 *
 * URLs are relative on purpose. In development `proxy.conf.json` forwards them to the
 * API on port 5000; in production the ingress serves the app and the API from one origin.
 * Neither case needs a base URL baked into the bundle.
 */
@Injectable({ providedIn: 'root' })
export class SystemService {
  readonly #http = inject(HttpClient);

  getInfo(): Promise<SystemInfo> {
    return firstValueFrom(this.#http.get<SystemInfo>('/api/v1/system/info'));
  }

  getHealth(): Promise<HealthReport> {
    return firstValueFrom(this.#http.get<HealthReport>('/health/ready'));
  }
}
