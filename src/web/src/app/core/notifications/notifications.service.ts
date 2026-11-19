import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface Notification {
  readonly id: string;
  readonly kind: string;
  readonly title: string;
  readonly body: string | null;
  readonly link: string | null;
  readonly createdAt: string;
  readonly isRead: boolean;
}

export interface NotificationFeed {
  readonly items: readonly Notification[];
  readonly unreadCount: number;
}

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly http = inject(HttpClient);

  readonly items = signal<readonly Notification[]>([]);
  readonly unreadCount = signal(0);

  async refresh(): Promise<void> {
    const feed = await firstValueFrom(this.http.get<NotificationFeed>('/api/v1/notifications'));
    this.items.set(feed.items);
    this.unreadCount.set(feed.unreadCount);
  }

  async markRead(id: string): Promise<void> {
    await firstValueFrom(this.http.put<void>(`/api/v1/notifications/${id}/read`, {}));
    this.items.set(
      this.items().map((item) => (item.id === id ? { ...item, isRead: true } : item)),
    );
    this.unreadCount.set(Math.max(0, this.unreadCount() - 1));
  }

  async markAllRead(): Promise<void> {
    await firstValueFrom(this.http.put<void>('/api/v1/notifications/read-all', {}));
    this.items.set(this.items().map((item) => ({ ...item, isRead: true })));
    this.unreadCount.set(0);
  }

  clear(): void {
    this.items.set([]);
    this.unreadCount.set(0);
  }
}
