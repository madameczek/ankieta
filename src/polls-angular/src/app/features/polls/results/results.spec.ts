import { answeredLabel } from './results';

describe('answeredLabel', () => {
  it('uses the genitive singular for a single participant', () => {
    expect(answeredLabel(0, 1)).toBe('Odpowiedziało 0 z 1 uczestnika');
    expect(answeredLabel(1, 1)).toBe('Odpowiedziało 1 z 1 uczestnika');
  });

  it('uses the genitive plural otherwise', () => {
    expect(answeredLabel(1, 2)).toBe('Odpowiedziało 1 z 2 uczestników');
    expect(answeredLabel(3, 5)).toBe('Odpowiedziało 3 z 5 uczestników');
    expect(answeredLabel(4, 22)).toBe('Odpowiedziało 4 z 22 uczestników');
  });
});
