package com.trackwise.driver.service

import com.trackwise.driver.api.ApiException
import com.trackwise.driver.api.DriverApi
import com.trackwise.driver.api.HttpClientFactory
import com.trackwise.driver.api.TripEventAlreadyRecordedCode
import com.trackwise.driver.api.models.RecordDriverTripEventCommand
import com.trackwise.driver.api.throwIfFailed
import com.trackwise.driver.model.PendingTripEvent
import com.trackwise.driver.util.Logger
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlin.time.Duration.Companion.seconds

/** A saved tap the API refused once it was sent, with the API's reason, for the crew to pass on. */
data class UnsentTripEvent(val event: PendingTripEvent, val reason: String)

/**
 * Crew taps made without signal. They are kept on the phone, survive the app being closed, and go
 * out in the order they were tapped: a later tap can't be sent ahead of an earlier one, because the
 * API checks each against what's already recorded (no departure before the arrival).
 */
class TripEventOutbox(
    private val driverApi: DriverApi,
    private val preferencesManager: PreferencesManager
) {
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.Default)
    private val sending = Mutex()
    private var retryJob: Job? = null

    val pending: StateFlow<List<PendingTripEvent>> = preferencesManager.pendingTripEvents
        .map(::decode)
        .stateIn(scope, SharingStarted.Eagerly, emptyList())

    private val _unsent = MutableStateFlow<List<UnsentTripEvent>>(emptyList())
    val unsent: StateFlow<List<UnsentTripEvent>> = _unsent.asStateFlow()

    private val _delivered = MutableSharedFlow<Unit>(extraBufferCapacity = 1)

    /** Fires when waiting taps got through, so screens can load the trip as it's now on record. */
    val delivered: SharedFlow<Unit> = _delivered.asSharedFlow()

    suspend fun current(): List<PendingTripEvent> = decode(preferencesManager.pendingTripEvents.first())

    suspend fun add(event: PendingTripEvent) {
        preferencesManager.updatePendingTripEvents { encode(decode(it) + event) }
        keepRetrying()
    }

    /** Takes back a tap that hasn't gone out yet; the crew's undo for it. */
    suspend fun remove(id: String) {
        preferencesManager.updatePendingTripEvents { json -> encode(decode(json).filterNot { it.id == id }) }
    }

    fun dismissUnsent() {
        _unsent.value = emptyList()
    }

    /**
     * Sends what's waiting, oldest first. Stops at the first tap that still can't get through, so
     * the order holds; true when nothing is left.
     */
    suspend fun send(): Boolean = sending.withLock {
        val waiting = current()
        if (waiting.isEmpty()) return@withLock true
        try {
            sendInOrder(waiting)
        } finally {
            if (current().size < waiting.size) _delivered.tryEmit(Unit)
        }
    }

    private suspend fun sendInOrder(waiting: List<PendingTripEvent>): Boolean {
        for (event in waiting) {
            when (val result = trySend(event.command)) {
                SendResult.Saved -> remove(event.id)
                is SendResult.Refused -> {
                    remove(event.id)
                    _unsent.value += UnsentTripEvent(event, result.reason)
                }
                SendResult.NotReached -> return false
            }
        }
        return true
    }

    /** One attempt at the API. Anything but an HTTP answer means the tap never got there. */
    suspend fun trySend(command: RecordDriverTripEventCommand): SendResult = try {
        driverApi.recordDriverTripEvent(command.tripId.orEmpty(), command).throwIfFailed()
        SendResult.Saved
    } catch (e: CancellationException) {
        throw e
    } catch (e: ApiException) {
        when {
            e.errorCode == TripEventAlreadyRecordedCode -> SendResult.Saved
            e.statusCode == 400 -> SendResult.Refused(e.message)
            else -> SendResult.NotReached
        }
    } catch (e: Exception) {
        Logger.e("Trip tap didn't reach the API", e)
        SendResult.NotReached
    }

    /** While anything is waiting, try again every little while; signal comes back on its own. */
    fun keepRetrying() {
        if (retryJob?.isActive == true) return
        retryJob = scope.launch {
            while (current().isNotEmpty()) {
                delay(RetryEvery)
                send()
            }
        }
    }

    private fun decode(json: String?): List<PendingTripEvent> =
        json?.let { runCatching { HttpClientFactory.jsonConfig.decodeFromString<List<PendingTripEvent>>(it) }.getOrNull() }
            .orEmpty()

    private fun encode(events: List<PendingTripEvent>): String? =
        events.takeIf { it.isNotEmpty() }?.let { HttpClientFactory.jsonConfig.encodeToString(it) }

    sealed interface SendResult {
        data object Saved : SendResult
        data class Refused(val reason: String) : SendResult
        data object NotReached : SendResult
    }

    private companion object {
        val RetryEvery = 20.seconds
    }
}
