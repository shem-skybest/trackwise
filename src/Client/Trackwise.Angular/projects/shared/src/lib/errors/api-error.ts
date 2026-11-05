/** A failure body from the API - the wire shape of `Result.Fail(message, errorCode)`. */
export interface ApiError {
  message: string;
  errorCode?: string | null;
}

/**
 * Reads the API's own failure body off a thrown HTTP error.
 *
 * The doubled name callers used to hand-roll (`err.error.error`) is two different things: Angular's
 * `HttpErrorResponse.error` is the response body, and the body's own `error` is the message. Returns
 * null for transport-level failures that carry no API body at all.
 */
export function getApiError(err: unknown): ApiError | null {
  const body = parseBody((err as { error?: unknown })?.error);

  if (typeof body?.error !== "string") {
    return null;
  }

  return {
    message: body.error,
    errorCode: typeof body.errorCode === "string" ? body.errorCode : null,
  };
}

/**
 * Endpoints that return no data are generated with `responseType: 'text'`, so their failure body
 * arrives as the raw JSON string rather than an object.
 */
function parseBody(body: unknown): { error?: unknown; errorCode?: unknown } | null {
  if (typeof body !== "string") {
    return (body as { error?: unknown; errorCode?: unknown }) ?? null;
  }

  try {
    const parsed: unknown = JSON.parse(body);
    return typeof parsed === "object" ? (parsed as { error?: unknown; errorCode?: unknown }) : null;
  } catch {
    return null;
  }
}

/** The API's own message, or `fallback` when the failure carried no API body. */
export function getApiErrorMessage(err: unknown, fallback: string): string {
  return getApiError(err)?.message ?? fallback;
}
