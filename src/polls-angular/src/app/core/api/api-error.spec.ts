import { HttpErrorResponse } from '@angular/common/http';
import { ApiError, isScreenError, toApiError } from './api-error';

describe('api-error', () => {
  it.each([400, 404, 409, 503])('%i is shown by the screen', (status) => {
    expect(isScreenError(new ApiError(status, 'x'))).toBe(true);
  });

  it.each([0, 401, 403, 429, 500, 502])('%i is left to the interceptor', (status) => {
    expect(isScreenError(new ApiError(status, 'x'))).toBe(false);
  });

  it('reads code and field errors from ProblemDetails', () => {
    const error = toApiError(
      new HttpErrorResponse({ status: 400, error: { code: 'validation.failed', errors: { name: ['name.required'] } } }),
    );

    expect(error.code).toBe('validation.failed');
    expect(error.fieldCode('name')).toBe('name.required');
    expect(error.fieldCode('title')).toBeUndefined();
  });

  it('uses network.error when the server was not reached', () => {
    expect(toApiError(new HttpErrorResponse({ status: 0 })).code).toBe('network.error');
  });
});
