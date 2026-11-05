import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, WritableSignal, computed, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { PollDetail, PollQuestion } from '../../../core/api/api.models';
import { PollContext } from '../poll-context';
import { Editor } from './editor';

@Component({ template: '' })
class Blank {}

/** The editor's protected state, reached for the tests. */
interface EditorState {
  title: WritableSignal<string>;
  description: WritableSignal<string>;
  newQuestion: WritableSignal<string>;
  editingId: WritableSignal<number | null>;
  editText: WritableSignal<string>;
  savePoll(): Promise<void>;
  deletePoll(): void;
}

const question = (id: number, text: string): PollQuestion =>
  ({ id, question: text, sortOrder: id, myAnswer: null, myAnswerModifiedAt: null }) as PollQuestion;

describe('Editor – unsaved changes (D46)', () => {
  let fixture: ComponentFixture<Editor>;
  let editor: Editor;
  let state: EditorState;
  let backend: HttpTestingController;
  let current: WritableSignal<PollDetail | undefined>;

  const poll: PollDetail = {
    id: 1,
    title: 'Obiad',
    description: 'co dziś jemy?',
    role: 'owner',
    questions: [question(10, 'zupa?'), question(11, 'deser?')],
  };

  beforeEach(async () => {
    current = signal<PollDetail | undefined>(poll);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'polls', component: Blank }]),
        {
          provide: PollContext,
          useValue: {
            current,
            isOwner: computed(() => current()?.role === 'owner'),
            url: (path = '') => `/api/polls/1${path}`,
            detail: { reload: vi.fn() },
          },
        },
        { provide: MatDialog, useValue: { open: () => ({ afterClosed: () => of(true) }) } },
      ],
    });
    fixture = TestBed.createComponent(Editor);
    editor = fixture.componentInstance;
    state = editor as unknown as EditorState;
    backend = TestBed.inject(HttpTestingController);
    await fixture.whenStable();
  });

  afterEach(() => backend.verify());

  it('has nothing unsaved after load', () => {
    expect(editor.hasUnsavedChanges()).toBe(false);
  });

  it('ignores surrounding whitespace: the API trims title and description', () => {
    state.title.set('  Obiad  ');
    state.description.set('co dziś jemy?\n');

    expect(editor.hasUnsavedChanges()).toBe(false);
  });

  it('is clean after saving a title with spaces, also once the reload brings the trimmed value', async () => {
    state.title.set('  Kolacja  ');
    expect(editor.hasUnsavedChanges()).toBe(true);

    const saving = state.savePoll();
    const request = backend.expectOne('/api/polls/1');
    expect(request.request.body).toEqual({ title: 'Kolacja', description: 'co dziś jemy?' });
    request.flush(null);
    await saving;
    expect(editor.hasUnsavedChanges()).toBe(false);

    current.set({ ...poll, title: 'Kolacja' });
    expect(editor.hasUnsavedChanges()).toBe(false);
  });

  it('counts a typed new question, but not whitespace only', () => {
    state.newQuestion.set('   ');
    expect(editor.hasUnsavedChanges()).toBe(false);

    state.newQuestion.set('napoje?');
    expect(editor.hasUnsavedChanges()).toBe(true);
  });

  it('counts an open question edit only when the text changed', () => {
    state.editingId.set(10);
    state.editText.set('zupa?');
    expect(editor.hasUnsavedChanges()).toBe(false);

    state.editText.set('zupa dnia?');
    expect(editor.hasUnsavedChanges()).toBe(true);
  });

  it('closes the edit when the question disappears on a reload', async () => {
    state.editingId.set(10);
    state.editText.set('zupa dnia?');

    current.set({ ...poll, questions: [question(11, 'deser?')] });
    await fixture.whenStable();

    expect(state.editingId()).toBeNull();
    expect(editor.hasUnsavedChanges()).toBe(false);
  });

  it('leaves without asking once the poll is deleted', async () => {
    state.newQuestion.set('napoje?');

    state.deletePoll();
    backend.expectOne('/api/polls/1').flush(null);
    await fixture.whenStable();

    expect(editor.hasUnsavedChanges()).toBe(false);
  });
});
