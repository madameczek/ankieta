import { Injector, runInInjectionContext } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { App, isNewScreen } from './app';
import { SessionStore } from './core/session/session.store';

/** "Wyjdź": navigate to /users first (the unsaved-changes guard may cancel), only then end the session (D46). */
describe('App.leave()', () => {
  let session: { leave: ReturnType<typeof vi.fn>; clear: ReturnType<typeof vi.fn> };
  let router: { url: string; navigate: ReturnType<typeof vi.fn>; events: Subject<unknown> };

  function leave(): Promise<void> {
    TestBed.configureTestingModule({
      providers: [
        { provide: SessionStore, useValue: session },
        { provide: Router, useValue: router },
      ],
    });
    const app = runInInjectionContext(TestBed.inject(Injector), () => new App());
    return (app as unknown as { leave(): Promise<void> }).leave();
  }

  beforeEach(() => {
    session = { leave: vi.fn().mockResolvedValue(undefined), clear: vi.fn() };
    router = { url: '/polls/1/answers', navigate: vi.fn().mockResolvedValue(true), events: new Subject() };
  });

  it('navigates to /users, then ends the session', async () => {
    await leave();

    expect(router.navigate).toHaveBeenCalledWith(['/users']);
    expect(session.leave).toHaveBeenCalled();
  });

  it('stays signed in when the unsaved-changes dialog cancels the navigation', async () => {
    router.navigate.mockResolvedValue(false);

    await leave();

    expect(session.leave).not.toHaveBeenCalled();
  });

  it('stays signed in when the navigation is superseded', async () => {
    router.navigate.mockResolvedValue(null);

    await leave();

    expect(session.leave).not.toHaveBeenCalled();
  });

  it('stays signed in when the navigation fails', async () => {
    router.navigate.mockRejectedValue(new Error('chunk failed'));

    await leave();

    expect(session.leave).not.toHaveBeenCalled();
  });

  it('skips the navigation when already on /users (also with a query)', async () => {
    router.url = '/users?search=ala';

    await leave();

    expect(router.navigate).not.toHaveBeenCalled();
    expect(session.leave).toHaveBeenCalled();
  });

  it('clears the session locally when the API call fails', async () => {
    session.leave.mockRejectedValue(new Error('network'));

    await leave();

    expect(session.clear).toHaveBeenCalled();
  });
});

/** The content area (not the window) scrolls: a new screen starts at the top, a tab switch keeps the position. */
describe('isNewScreen', () => {
  it('treats the first navigation and a change of screen as new', () => {
    expect(isNewScreen(null, '/polls')).toBe(true);
    expect(isNewScreen('/polls', '/polls/3/answers')).toBe(true);
    expect(isNewScreen('/polls/3/results', '/users')).toBe(true);
    expect(isNewScreen('/polls/3/edit', '/polls/4/edit')).toBe(true);
  });

  it('keeps the position for a tab switch inside one poll or a query change', () => {
    expect(isNewScreen('/polls/3/answers', '/polls/3/results')).toBe(false);
    expect(isNewScreen('/polls/3', '/polls/3/answers')).toBe(false);
    expect(isNewScreen('/users?search=a', '/users?search=al')).toBe(false);
  });
});
