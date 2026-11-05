import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { computed, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PollDetail } from '../../../core/api/api.models';
import { SessionStore } from '../../../core/session/session.store';
import { PollContext } from '../poll-context';
import { Answers } from './answers';

/** D46: closing or reloading the tab with an unsaved answer lets the browser ask. */
describe('Answers – beforeunload', () => {
  let fixture: ComponentFixture<Answers>;
  const isUser = signal(true);

  const poll: PollDetail = {
    id: 1,
    title: 'Obiad',
    description: null,
    role: 'participant',
    questions: [{ id: 10, sortOrder: 1, question: 'zupa?', myAnswer: null, myAnswerModifiedAt: null }],
  };

  const unload = () => {
    const e = new Event('beforeunload', { cancelable: true });
    window.dispatchEvent(e);
    return e.defaultPrevented;
  };

  async function type(text: string): Promise<void> {
    const textarea = fixture.nativeElement.querySelector('textarea') as HTMLTextAreaElement;
    textarea.value = text;
    textarea.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  beforeEach(async () => {
    isUser.set(true);
    const current = signal<PollDetail | undefined>(poll);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: SessionStore, useValue: { isUser } },
        {
          provide: PollContext,
          useValue: { current, isOwner: computed(() => false), url: (path = '') => `/api/polls/1${path}` },
        },
      ],
    });
    fixture = TestBed.createComponent(Answers);
    await fixture.whenStable();
  });

  afterEach(() => fixture.destroy());

  it('does not ask without unsaved answers', () => {
    expect(unload()).toBe(false);
  });

  it('asks with an unsaved answer', async () => {
    await type('pomidorowa');

    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(true);
    expect(unload()).toBe(true);
  });

  it('does not ask once the session is gone', async () => {
    await type('pomidorowa');
    isUser.set(false);

    expect(unload()).toBe(false);
  });
});
