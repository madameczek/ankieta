// D28: the API returns English codes; the UI shows Polish text.

const messages: Readonly<Record<string, string>> = {
  'validation.failed': 'Popraw zaznaczone pola.',
  'request.invalid': 'Nieprawidłowe żądanie.',

  'name.required': 'Podaj nazwę użytkownika.',
  'name.too_long': 'Nazwa może mieć najwyżej 100 znaków.',
  'search.too_long': 'Wyszukiwana fraza jest za długa.',
  'page.invalid': 'Nieprawidłowa strona listy.',
  'pageSize.invalid': 'Nieprawidłowy rozmiar strony.',
  'user.name_taken': 'Użytkownik o tej nazwie już istnieje.',
  'user.not_found': 'Użytkownik nie istnieje.',
  'userId.required': 'Wybierz użytkownika.',
  'session.required': 'Wybierz użytkownika.',

  'title.required': 'Podaj tytuł ankiety.',
  'title.too_long': 'Tytuł może mieć najwyżej 200 znaków.',
  'description.too_long': 'Opis może mieć najwyżej 2000 znaków.',
  'question.required': 'Podaj treść pytania.',
  'question.too_long': 'Pytanie może mieć najwyżej 1000 znaków.',
  'answer.too_long': 'Odpowiedź może mieć najwyżej 4000 znaków.',
  'questionIds.required': 'Brak kolejności pytań.',
  'questions.order_mismatch': 'Lista pytań zmieniła się – odśwież stronę.',

  'poll.not_found': 'Ankieta nie istnieje lub nie masz do niej dostępu.',
  'poll.owner_required': 'Tylko właściciel może to zrobić.',
  'question.not_found': 'Pytanie nie istnieje.',
  'participant.is_owner': 'Nie można usunąć właściciela ankiety.',

  'limit.users_reached': 'Osiągnięto limit użytkowników – tworzenie nowych jest zablokowane.',
  'limit.polls_reached': 'Osiągnięto limit ankiet – tworzenie nowych jest zablokowane.',
  rate_limited: 'Zbyt wiele żądań, spróbuj za chwilę.',

  'route.not_found': 'Nie znaleziono.',
  'method.not_allowed': 'Nieobsługiwana operacja.',
  'server.error': 'Błąd serwera.',
  'network.error': 'Brak połączenia z serwerem.',
};

export const FallbackMessage = 'Wystąpił błąd.';

export function messageFor(code: string | undefined | null): string {
  return (code && messages[code]) || FallbackMessage;
}
