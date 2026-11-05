package com.trackwise.driver.model

import com.trackwise.driver.api.models.RecordDriverTripEventCommand
import com.trackwise.driver.api.models.TripDto
import com.trackwise.driver.api.models.TripEventKind
import com.trackwise.driver.api.models.TripEventSource
import com.trackwise.driver.api.models.TripStopDto
import kotlin.time.Instant
import kotlinx.serialization.Serializable

/** A crew tap saved on the phone because it couldn't reach the API. Its time is always set. */
@Serializable
data class PendingTripEvent(
    val id: String,
    val command: RecordDriverTripEventCommand,
    /** Every stop of the visit the tap covers, so Today can show it as done before it's sent. */
    val stopIds: List<String> = emptyList(),
    val isDrop: Boolean = false,
    /** The stop's name, for the notice if the API refuses it later. */
    val place: String? = null
)

/**
 * The trip as the crew sees it: what's on record plus the taps still waiting on the phone, so Today
 * moves on to the next step without signal. A field the record already has wins, the same "first
 * one wins" the API applies when the tap arrives.
 */
fun TripDto.withPending(events: List<PendingTripEvent>): TripDto =
    events.filter { it.command.tripId == id }.fold(this) { trip, event -> trip.withTap(event) }

/** The waiting tap the crew's undo should take back, if [event] hasn't gone out yet. */
fun List<PendingTripEvent>.matching(tripId: String?, event: UndoableEvent): PendingTripEvent? =
    lastOrNull {
        it.command.tripId == tripId && it.command.kind == event.kind &&
            (it.command.stopId == event.stopId || event.stopId in it.stopIds)
    }

private fun TripDto.withTap(event: PendingTripEvent): TripDto {
    val c = event.command
    val at = c.at ?: return this
    val kind = c.kind ?: return this
    val app = TripEventSource.DRIVER_APP
    return when (kind) {
        TripEventKind.GARAGE_DEPARTED -> if (garageDepartedAt != null) this else
            copy(garageDepartedAt = at, garageDepartureRecordedAt = at, garageDepartureSource = app)
        TripEventKind.GARAGE_ARRIVED -> if (garageArrivedAt != null) this else
            copy(garageArrivedAt = at, garageArrivalRecordedAt = at, garageArrivalSource = app)
        else -> {
            val visit = event.stopIds.ifEmpty { listOfNotNull(c.stopId) }
            copy(stops = stops?.map { stop -> if (stop.id in visit) stop.withTap(kind, at, c.temperature) else stop })
        }
    }
}

private fun TripStopDto.withTap(kind: TripEventKind, at: Instant, temperature: Double?): TripStopDto {
    val app = TripEventSource.DRIVER_APP
    return when (kind) {
        TripEventKind.ARRIVED -> if (arrivedAt != null) this else copy(
            arrivedAt = at, arrivalRecordedAt = at, arrivalSource = app,
            temperatureAtArrival = temperatureAtArrival ?: temperature
        )
        TripEventKind.HANDLING_STARTED -> if (handlingStartedAt != null || departedAt != null) this else
            copy(handlingStartedAt = at, handlingStartedRecordedAt = at, handlingStartedSource = app)
        TripEventKind.HANDLING_FINISHED -> if (handlingFinishedAt != null || departedAt != null) this else
            copy(handlingFinishedAt = at, handlingFinishedRecordedAt = at, handlingFinishedSource = app)
        TripEventKind.DEPARTED -> if (departedAt != null) this else copy(
            departedAt = at, departureRecordedAt = at, departureSource = app,
            temperatureAtDeparture = temperatureAtDeparture ?: temperature
        )
        else -> this
    }
}
