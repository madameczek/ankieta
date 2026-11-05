import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable, of } from 'rxjs';
import { SessionStore } from '../core/session/session.store';
import { HasUnsavedChanges, unsavedChangesGuard } from './unsaved-changes.guard';

describe('unsavedChangesGuard', () => {
  let open: ReturnType<typeof vi.fn>;

  function run(isUser: boolean, dirty: boolean): boolean | Observable<boolean> {
    open = vi.fn().mockReturnValue({ afterClosed: () => of(true) });
    TestBed.configureTestingModule({
      providers: [
        { provide: SessionStore, useValue: { isUser: signal(isUser) } },
        { provide: MatDialog, useValue: { open } },
      ],
    });
    const component: HasUnsavedChanges = { hasUnsavedChanges: () => dirty };
    return TestBed.runInInjectionContext(
      () =>
        unsavedChangesGuard(
          component,
          {} as ActivatedRouteSnapshot,
          {} as RouterStateSnapshot,
          {} as RouterStateSnapshot,
        ) as boolean | Observable<boolean>,
    );
  }

  it('lets the user leave when nothing is unsaved', () => {
    expect(run(true, false)).toBe(true);
    expect(open).not.toHaveBeenCalled();
  });

  it('asks when answers are unsaved (D46)', () => {
    const result = run(true, true);

    expect(result).toBeInstanceOf(Observable);
    expect(open).toHaveBeenCalled();
  });

  it('does not ask after the session expired (401 → user list)', () => {
    expect(run(false, true)).toBe(true);
    expect(open).not.toHaveBeenCalled();
  });
});
