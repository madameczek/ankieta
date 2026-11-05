// Shapes returned by the REST API (docs/api.md). Dates are ISO strings in UTC.

export interface User {
  id: number;
  name: string;
}

export interface UserListItem extends User {
  createdAt: string;
}

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface Session {
  user: User | null;
}

export type PollRole = 'owner' | 'participant';

export interface PollListItem {
  id: number;
  title: string;
  role: PollRole;
  answered: number;
  questionCount: number;
  modifiedAt: string;
}

export interface PollQuestion {
  id: number;
  sortOrder: number;
  question: string;
  myAnswer: string | null;
  myAnswerModifiedAt: string | null;
}

export interface PollDetail {
  id: number;
  title: string;
  description: string | null;
  role: PollRole;
  questions: PollQuestion[];
}

export interface Created {
  id: number;
}

export interface SavedAnswer {
  modifiedAt: string;
}

export interface Participant {
  userId: number;
  name: string;
  isOwner: boolean;
  answered: number;
}

export interface ResultAnswer {
  userId: number;
  name: string;
  answer: string;
  modifiedAt: string;
}

export interface ResultQuestion {
  id: number;
  sortOrder: number;
  question: string;
  answers: ResultAnswer[];
}

export interface PollResults {
  participantCount: number;
  questions: ResultQuestion[];
}

/** Lengths enforced by the API (the same limits as the database). */
export const Limits = {
  userName: 100,
  title: 200,
  description: 2000,
  question: 1000,
  answer: 4000,
} as const;
