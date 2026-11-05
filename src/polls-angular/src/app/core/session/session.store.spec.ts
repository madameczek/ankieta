import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { SessionStore } from './session.store';

describe('SessionStore', () => {
  let store: SessionStore;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    store = TestBed.inject(SessionStore);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('load() sets the current user from GET /api/session', async () => {
    const loading = store.load();
    backend.expectOne('/api/session').flush({ user: { id: 7, name: 'Ala' } });
    await loading;

    expect(store.user()).toEqual({ id: 7, name: 'Ala' });
    expect(store.isUser()).toBe(true);
  });

  it('load() starts as visitor when the call fails', async () => {
    const loading = store.load();
    backend.expectOne('/api/session').flush(null, { status: 500, statusText: 'x' });
    await loading;

    expect(store.isUser()).toBe(false);
  });

  it('load() retries once when the first call hangs (e.g. during a deploy)', async () => {
    vi.useFakeTimers();
    try {
      const loading = store.load();
      backend.expectOne('/api/session'); // never answered
      await vi.advanceTimersByTimeAsync(SessionStore.loadTimeoutMs);
      backend.expectOne('/api/session').flush({ user: { id: 7, name: 'Ala' } });
      await loading;

      expect(store.user()).toEqual({ id: 7, name: 'Ala' });
    } finally {
      vi.useRealTimers();
    }
  });

  it('load() starts as visitor when both attempts hang', async () => {
    vi.useFakeTimers();
    try {
      const loading = store.load();
      backend.expectOne('/api/session');
      await vi.advanceTimersByTimeAsync(SessionStore.loadTimeoutMs);
      backend.expectOne('/api/session');
      await vi.advanceTimersByTimeAsync(SessionStore.loadTimeoutMs);
      await loading;

      expect(store.isUser()).toBe(false);
    } finally {
      vi.useRealTimers();
    }
  });

  it('become() switches to the returned user', async () => {
    const becoming = store.become(3);
    const request = backend.expectOne('/api/session/3');
    expect(request.request.method).toBe('POST');
    request.flush({ id: 3, name: 'Ola' });

    expect(await becoming).toEqual({ id: 3, name: 'Ola' });
    expect(store.user()?.id).toBe(3);
  });

  it('leave() returns to visitor', async () => {
    const becoming = store.become(3);
    backend.expectOne('/api/session/3').flush({ id: 3, name: 'Ola' });
    await becoming;

    const leaving = store.leave();
    backend.expectOne((r) => r.method === 'DELETE' && r.url === '/api/session').flush(null, { status: 204, statusText: 'x' });
    await leaving;

    expect(store.user()).toBeNull();
  });
});
