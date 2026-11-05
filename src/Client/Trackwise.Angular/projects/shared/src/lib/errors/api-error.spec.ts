import { HttpErrorResponse } from "@angular/common/http";
import { getApiError, getApiErrorMessage } from "./api-error";

const MESSAGE = "Drop 1 is already delivered, so the trip ran.";

describe("getApiError", () => {
  it("reads the message off a JSON body", () => {
    const err = new HttpErrorResponse({ status: 400, error: { error: MESSAGE, errorCode: "X" } });

    expect(getApiError(err)).toEqual({ message: MESSAGE, errorCode: "X" });
  });

  // An endpoint with no response data is generated with responseType 'text', so its failure body
  // arrives unparsed - and the toast used to show it as raw `{"error":"..."}`.
  it("reads the message off a body that arrived as a JSON string", () => {
    const err = new HttpErrorResponse({ status: 400, error: JSON.stringify({ error: MESSAGE }) });

    expect(getApiError(err)).toEqual({ message: MESSAGE, errorCode: null });
  });

  it("returns null for a plain-text body", () => {
    const err = new HttpErrorResponse({ status: 502, error: "Bad Gateway" });

    expect(getApiError(err)).toBeNull();
  });

  it("returns null for a JSON string that isn't an API failure", () => {
    const err = new HttpErrorResponse({ status: 400, error: '"just a string"' });

    expect(getApiError(err)).toBeNull();
  });

  it("returns null for a transport failure", () => {
    expect(getApiError(new HttpErrorResponse({ status: 0 }))).toBeNull();
  });
});

describe("getApiErrorMessage", () => {
  it("falls back when there is no API body", () => {
    expect(getApiErrorMessage(new HttpErrorResponse({ status: 0 }), "Failed")).toBe("Failed");
  });
});
