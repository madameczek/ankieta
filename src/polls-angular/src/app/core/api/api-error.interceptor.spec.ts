import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Notifier } from '../notifier';
import { SessionStore } from '../session/session.store';
import { ApiError } from './api-error';
import { apiErrorInterceptor } from './api-error.interceptor';

describe('apiErrorInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let notifier: { info: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let session: { clear: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    notifier = { info: vi.fn(), error: vi.fn() };
    router = { navigate: vi.fn().mockResolvedValue(true) };
    session = { clear: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([apiErrorInterceptor])),
        provideHttpClientTesting(),
        { provide: Notifier, useValue: notifier },
        { provide: Router, useValue: router },
        { provide: SessionStore, useValue: session },
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  async function failWith(status: number, body: object | string): Promise<ApiError> {
    const result = firstValueFrom(http.get('/api/polls'));
    backend.expectOne('/api/polls').flush(body, { status, statusText: 'x' });
    return result.then(
      () => {
        throw new Error('expected an error');
      },
      (e: unknown) => e as ApiError,
    );
  }

  it('maps ProblemDetails to ApiError with code and field errors', async () => {
    const error = await failWith(400, { code: 'validation.failed', errors: { title: ['title.required'] } });

    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(400);
    expect(error.code).toBe('validation.failed');
    expect(error.fieldCode('title')).toBe('title.required');
    expect(notifier.error).not.toHaveBeenCalled();
  });

  it('on 401 clears the session, goes to the user list and tells the user', async () => {
    await failWith(401, { code: 'session.required' });

    expect(session.clear).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/users']);
    expect(notifier.error).toHaveBeenCalledWith('session.required');
  });

  it.each([
    [403, 'poll.owner_required'],
    [429, 'rate_limited'],
    [500, 'server.error'],
  ])('shows %i (%s) as a message', async (status, code) => {
    await failWith(status, { code });

    expect(notifier.error).toHaveBeenCalledWith(code);
    expect(session.clear).not.toHaveBeenCalled();
  });

  it.each([404, 409, 503])('leaves %i to the screen', async (status) => {
    await failWith(status, { code: 'any' });

    expect(notifier.error).not.toHaveBeenCalled();
  });

  it('uses server.error when the body has no code', async () => {
    const error = await failWith(502, 'Bad gateway');

    expect(error.code).toBe('server.error');
  });
});
