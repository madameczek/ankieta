import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { SessionStore } from './session.store';
import { userGuard } from './user.guard';

describe('userGuard', () => {
  function run(isUser: boolean): boolean | UrlTree {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: SessionStore, useValue: { isUser: signal(isUser) } }],
    });
    return TestBed.runInInjectionContext(
      () => userGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot) as boolean | UrlTree,
    );
  }

  it('lets a user through', () => {
    expect(run(true)).toBe(true);
  });

  it('sends a visitor to the user list', () => {
    const result = run(false);

    expect(result).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/users');
  });
});
