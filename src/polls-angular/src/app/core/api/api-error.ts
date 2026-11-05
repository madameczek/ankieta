import { HttpErrorResponse } from '@angular/common/http';

/** An API failure: HTTP status, the stable `code` from ProblemDetails and field error codes (validation). */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly code: string,
    readonly fieldErrors: Readonly<Record<string, string[]>> = {},
  ) {
    super(code);
    this.name = 'ApiError';
  }

  /** First error code for a form field, if any. */
  fieldCode(field: string): string | undefined {
    return this.fieldErrors[field]?.[0];
  }
}

/**
 * Statuses a screen shows itself; all others (401, 403, 429, 5xx except 503, network) are already shown by
 * the interceptor, so a screen must not repeat them.
 */
export function isScreenError(error: ApiError): boolean {
  return [400, 404, 409, 503].includes(error.status);
}

export const NetworkErrorCode = 'network.error';
export const ServerErrorCode = 'server.error';

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) {
    return error;
  }

  if (error instanceof HttpErrorResponse) {
    const body = error.error as { code?: unknown; errors?: unknown } | null;
    const code =
      typeof body?.code === 'string' ? body.code : error.status === 0 ? NetworkErrorCode : ServerErrorCode;
    const fieldErrors = body?.errors && typeof body.errors === 'object' ? (body.errors as Record<string, string[]>) : {};
    return new ApiError(error.status, code, fieldErrors);
  }

  return new ApiError(0, ServerErrorCode);
}
