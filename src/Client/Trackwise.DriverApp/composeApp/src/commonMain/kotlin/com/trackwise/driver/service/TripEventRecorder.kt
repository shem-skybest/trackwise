package com.trackwise.driver.service

import com.trackwise.driver.api.ApiException
import com.trackwise.driver.api.models.RecordDriverTripEventCommand
import com.trackwise.driver.api.models.TripDto
import com.trackwise.driver.api.models.TripEventKind
import com.trackwise.driver.model.CrewBackdateWindow
import com.trackwise.driver.model.PendingTripEvent
import com.trackwise.driver.model.StopBlock
import com.trackwise.driver.model.TripEventDraft
import com.trackwise.driver.model.TripEventInput
import com.trackwise.driver.model.lastDepartedAt
import kotlin.random.Random
import kotlin.time.Clock

/** How a tap ended: on record at the office, or kept on the phone until there's signal. */
enum class RecordResult { SAVED, KEPT_ON_PHONE }

/** One crew tap, whether from Today's blocks or Confirm Pick Up / Delivery on Load Details. */
class TripEventRecorder(
    private val proximityWatcher: LoadProximityWatcher,
    private val outbox: TripEventOutbox
) {
    /**
     * The sheet for a tap. An arrival offers the time GPS saw the truck reach the stop, unless it is
     * older than the crew may enter or earlier than the truck left its last stop.
     */
    fun draft(trip: TripDto, block: StopBlock?, kind: TripEventKind): TripEventDraft {
        val leftLastStop = trip.lastDepartedAt
        val seen = block
            ?.takeIf { kind == TripEventKind.ARRIVED }
            ?.let { proximityWatcher.arrivalSeenAt(it.loadIds, atPickup = !it.isDrop) }
            ?.takeIf { Clock.System.now() - it <= CrewBackdateWindow }
            ?.takeIf { leftLastStop == null || it >= leftLastStop }
        return TripEventDraft(trip, block, kind, seen)
    }

    /**
     * Sends the tap, or keeps it on the phone when it can't get through. The time is fixed at the tap,
     * so a tap sent later still says when it happened. Throws [ApiException] when the API refuses it,
     * with a reason the crew can act on in the sheet.
     */
    suspend fun record(draft: TripEventDraft, input: TripEventInput): RecordResult {
        val tripId = draft.trip.id ?: error("This trip can't be found. Refresh and try again.")
        val command = RecordDriverTripEventCommand(
            tripId = tripId,
            stopId = draft.block?.stopId,
            kind = draft.kind,
            at = input.at ?: Clock.System.now(),
            temperature = input.temperature,
            outcome = input.outcome,
            returnedBoxes = input.returnedBoxes,
            returnedBags = input.returnedBags,
            returnReason = input.returnReason,
            outcomeNote = input.note
        )

        // Taps that are already waiting go first, or the API would see this one out of order.
        if (outbox.current().isEmpty()) {
            // Sent straight away, "now" is left to the API's clock, so it isn't read as a typed time.
            when (val result = outbox.trySend(command.copy(at = input.at))) {
                TripEventOutbox.SendResult.Saved -> return RecordResult.SAVED
                is TripEventOutbox.SendResult.Refused -> throw ApiException(400, result.reason)
                TripEventOutbox.SendResult.NotReached -> Unit
            }
        }

        outbox.add(
            PendingTripEvent(
                id = "${Clock.System.now().toEpochMilliseconds()}-${Random.nextInt(1_000_000)}",
                command = command,
                stopIds = draft.block?.stopIds.orEmpty(),
                isDrop = draft.block?.isDrop == true,
                place = draft.block?.place?.ifBlank { null }
            )
        )
        return if (outbox.send() && outbox.unsent.value.none { it.event.command == command }) {
            RecordResult.SAVED
        } else {
            RecordResult.KEPT_ON_PHONE
        }
    }
}
