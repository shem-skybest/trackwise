package com.trackwise.driver.api

import com.trackwise.driver.api.models.ErrorResponse
import com.trackwise.driver.infrastructure.HttpResponse
import io.ktor.util.reflect.typeInfo

/**
 * Exception thrown when an API call returns a non-success status code.
 *
 * [message] is shown verbatim in full-screen error states, so it is written for a driver, not a log.
 */
class ApiException(
    val statusCode: Int,
    override val message: String = messageForStatus(statusCode),
    /** The API's machine-readable reason (`ErrorCodes` on the server), for branching; never shown. */
    val errorCode: String? = null
) : Exception(message)

private const val ServerFaultCode = "SERVER_FAULT"

/** The tap is already on record, from an earlier send or from the office: as good as saved. */
const val TripEventAlreadyRecordedCode = "TRIP_EVENT_ALREADY_RECORDED"

private fun messageForStatus(statusCode: Int): String = when (statusCode) {
    400, 422 -> "That request wasn't accepted. Check the details and try again."
    401 -> "Your session has expired. Please sign in again."
    403 -> "You don't have permission to do this. Your account's role may need updating in the TMS portal."
    404 -> "We couldn't find that. It may have been removed or reassigned."
    409 -> "This was already changed somewhere else. Refresh and try again."
    429 -> "Too many requests. Wait a moment and try again."
    in 500..599 -> "Something went wrong on our end. Try again in a moment."
    else -> "The request failed (status $statusCode). Try again."
}

/**
 * Extracts the body from an [HttpResponse], throwing [ApiException] if the response
 * indicates failure. Use this in repositories to ensure consistent error handling.
 */
suspend fun <T : Any> HttpResponse<T>.bodyOrThrow(): T {
    if (!success) {
        throw ApiException(status)
    }
    return body()
}

/**
 * Throws [ApiException] if the call failed. A rejected request (400) carries the API's own reason,
 * such as "Arrival can't be earlier than the garage departure", which tells the driver what to fix.
 */
suspend fun HttpResponse<*>.throwIfFailed() {
    if (success) return
    val body = if (status == 400) {
        runCatching { typedBody<ErrorResponse>(typeInfo<ErrorResponse>()) }.getOrNull()
    } else {
        null
    }
    // A database fault comes back as a 400 too, but its message isn't a reason the driver can fix.
    if (body?.errorCode == ServerFaultCode) throw ApiException(500)
    val reason = body?.error
    throw ApiException(status, reason?.takeIf { it.isNotBlank() } ?: messageForStatus(status), body?.errorCode)
}
