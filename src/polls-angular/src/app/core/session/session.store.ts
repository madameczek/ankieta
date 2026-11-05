import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Session, User } from '../api/api.models';

/** D10: the current user. Loaded once at startup; null = visitor (D11). */
@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly http = inject(HttpClient);
  private readonly current = signal<User | null>(null);

  readonly user = this.current.asReadonly();
  readonly isUser = computed(() => this.current() !== null);

  /** GET /api/session; on any failure the app starts in visitor mode. */
  async load(): Promise<void> {
    try {
      const session = await firstValueFrom(this.http.get<Session>('/api/session'));
      this.current.set(session.user);
    } catch {
      this.current.set(null);
    }
  }

  async become(userId: number): Promise<User> {
    const user = await firstValueFrom(this.http.post<User>(`/api/session/${userId}`, null));
    this.current.set(user);
    return user;
  }

  /** Creates the user; the API enters their context. */
  async createUser(name: string): Promise<User> {
    const user = await firstValueFrom(this.http.post<User>('/api/users', { name }));
    this.current.set(user);
    return user;
  }

  async leave(): Promise<void> {
    await firstValueFrom(this.http.delete('/api/session'));
    this.current.set(null);
  }

  /** The API said the session is gone (401). */
  clear(): void {
    this.current.set(null);
  }
}
