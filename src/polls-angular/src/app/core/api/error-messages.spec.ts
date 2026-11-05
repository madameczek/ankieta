import { FallbackMessage, messageFor } from './error-messages';

describe('messageFor', () => {
  it('returns Polish text for a known code', () => {
    expect(messageFor('poll.not_found')).toBe('Ankieta nie istnieje lub nie masz do niej dostępu.');
  });

  it.each([undefined, null, '', 'some.new_code'])('falls back for %s', (code) => {
    expect(messageFor(code)).toBe(FallbackMessage);
  });

  it('covers every error code listed in docs/api.md', () => {
    const codes = [
      'validation.failed', 'request.invalid', 'name.required', 'name.too_long', 'search.too_long', 'page.invalid',
      'pageSize.invalid', 'title.required', 'title.too_long', 'description.too_long', 'question.required',
      'question.too_long', 'answer.too_long', 'questionIds.required', 'userId.required', 'session.required',
      'poll.not_found', 'question.not_found', 'user.not_found', 'poll.owner_required', 'user.name_taken',
      'participant.is_owner', 'questions.order_mismatch', 'rate_limited', 'limit.users_reached',
      'limit.polls_reached', 'route.not_found', 'method.not_allowed', 'server.error',
    ];

    expect(codes.filter((code) => messageFor(code) === FallbackMessage)).toEqual([]);
  });
});
