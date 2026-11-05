package com.trackwise.driver.model

import com.trackwise.driver.api.models.DropReturnReason
import com.trackwise.driver.api.models.LoadStatus
import com.trackwise.driver.api.models.TripDto
import com.trackwise.driver.api.models.TripEventKind
import com.trackwise.driver.api.models.TripEventSource
import com.trackwise.driver.api.models.TripStatus
import com.trackwise.driver.api.models.TripStopType
import kotlin.time.Duration.Companion.hours
import kotlin.time.Duration.Companion.minutes
import kotlin.time.Instant

/** How far back the crew may set a time; older ones are the office's to enter. Mirrors the API. */
val CrewBackdateWindow = 2.hours

/** How long the crew can take back their own tap. Mirrors the API. */
val CrewUndoWindow = 10.minutes

/** The next thing the crew records at a block, in the order a stop runs; null once it's left. */
fun StopBlock.nextEvent(): TripEventKind? = when {
    arrivedAt == null -> TripEventKind.ARRIVED
    departedAt != null -> null
    handlingStartedAt == null && handlingFinishedAt == null -> TripEventKind.HANDLING_STARTED
    handlingFinishedAt == null -> TripEventKind.HANDLING_FINISHED
    else -> TripEventKind.DEPARTED
}

/** The crew's last tap on the trip, while it can still be taken back. */
data class UndoableEvent(val kind: TripEventKind, val stopId: String?)

/**
 * The trip's most recently entered event, if this crew entered it from the app within
 * [CrewUndoWindow]. The API refuses an undo once anything was recorded after it, so only the
 * latest one qualifies.
 */
fun TripDto.undoableEvent(now: Instant): UndoableEvent? {
    if (status !in listOf(TripStatus.DISPATCHED, TripStatus.IN_TRANSIT)) return null

    data class Entered(val kind: TripEventKind, val stopId: String?, val at: Instant?, val source: TripEventSource?)

    val events = buildList {
        if (garageDepartedAt != null) {
            add(Entered(TripEventKind.GARAGE_DEPARTED, null, garageDepartureRecordedAt, garageDepartureSource))
        }
        if (garageArrivedAt != null) {
            add(Entered(TripEventKind.GARAGE_ARRIVED, null, garageArrivalRecordedAt, garageArrivalSource))
        }
        stops.orEmpty().forEach { s ->
            if (s.arrivedAt != null) add(Entered(TripEventKind.ARRIVED, s.id, s.arrivalRecordedAt, s.arrivalSource))
            if (s.handlingStartedAt != null) {
                add(Entered(TripEventKind.HANDLING_STARTED, s.id, s.handlingStartedRecordedAt, s.handlingStartedSource))
            }
            if (s.handlingFinishedAt != null) {
                add(Entered(TripEventKind.HANDLING_FINISHED, s.id, s.handlingFinishedRecordedAt, s.handlingFinishedSource))
            }
            if (s.departedAt != null) add(Entered(TripEventKind.DEPARTED, s.id, s.departureRecordedAt, s.departureSource))
        }
    }

    val latest = events.filter { it.at != null }.maxByOrNull { it.at!! } ?: return null
    return if (latest.source == TripEventSource.DRIVER_APP && now - latest.at!! <= CrewUndoWindow) {
        UndoableEvent(latest.kind, latest.stopId)
    } else {
        null
    }
}

/** When the trip's own truck last left a stop; it can't have reached another one before that. */
val TripDto.lastDepartedAt: Instant?
    get() = stops.orEmpty().filter { it.rescueId == null }.mapNotNull { it.departedAt }.maxOrNull()

/** Whether the trip's own truck has left every drop, so it can head back to the garage. */
val TripDto.allDropsLeft: Boolean
    get() = stops.orEmpty()
        .filter { it.type == TripStopType.DROP_OFF && it.rescueId == null }
        .let { drops -> drops.isNotEmpty() && drops.all { it.departedAt != null } }

/** What the crew enters with a tap. Null fields are left out. */
data class TripEventInput(
    val at: Instant? = null,
    val temperature: Double? = null,
    val outcome: LoadStatus? = null,
    val returnedBoxes: Int? = null,
    val returnedBags: Int? = null,
    val returnReason: DropReturnReason? = null,
    val note: String? = null
)

/** A drop's outcome as the crew picks it; "some returned" is a delivery with boxes or bags back. */
enum class DropOutcomeChoice(val status: LoadStatus) {
    DELIVERED(LoadStatus.DELIVERED),
    SOME_RETURNED(LoadStatus.DELIVERED),
    FOUL_TRIP(LoadStatus.FOUL_TRIP),
    CANCELLED_BY_CLIENT(LoadStatus.CANCELLED_BY_CLIENT)
}

/** A tap the crew is about to record: what, where, and when GPS saw the truck arrive. */
data class TripEventDraft(
    val trip: TripDto,
    val block: StopBlock?,
    val kind: TripEventKind,
    val gpsArrivedAt: Instant? = null
)
