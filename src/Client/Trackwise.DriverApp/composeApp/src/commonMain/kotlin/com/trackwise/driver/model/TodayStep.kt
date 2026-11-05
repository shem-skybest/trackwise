package com.trackwise.driver.model

import com.trackwise.driver.api.models.TripDto
import com.trackwise.driver.api.models.TripEventKind
import kotlin.time.Instant

/** The one thing Today asks the crew to do next on a trip. */
sealed interface TodayStep {
    data object LeaveGarage : TodayStep

    /** [next] is the tap the big button records at [block]. */
    data class AtBlock(val block: StopBlock, val next: TripEventKind) : TodayStep

    data object ReturnToGarage : TodayStep

    data object Finished : TodayStep
}

/**
 * Where the crew is in the trip. The truck is at one stop at a time, so a stop that's arrived and not
 * yet left is always the step. Otherwise it's [chosenStopId] if the crew picked where they're going,
 * or the next stop in trip order. Drops another truck took over are skipped.
 */
fun TripDto.todayStep(blocks: List<StopBlock>, chosenStopId: String? = null): TodayStep {
    val own = blocks.filterNot { it.rescued }
    val anyArrived = own.any { it.arrivedAt != null }
    if (garageDepartedAt == null && !anyArrived) return TodayStep.LeaveGarage

    own.firstOrNull { it.arrivedAt != null && it.departedAt == null }?.let { open ->
        return TodayStep.AtBlock(open, open.nextEvent() ?: TripEventKind.DEPARTED)
    }

    val ahead = own.filter { it.departedAt == null }
    val next = ahead.firstOrNull { it.stopId == chosenStopId } ?: ahead.firstOrNull()
    if (next != null && !allDropsLeft) return TodayStep.AtBlock(next, TripEventKind.ARRIVED)

    return if (garageArrivedAt == null) TodayStep.ReturnToGarage else TodayStep.Finished
}

/** The stops still ahead that the crew could head to instead of the suggested one. */
fun List<StopBlock>.notYetVisited(): List<StopBlock> = filter { !it.rescued && it.arrivedAt == null }

/** One dot per part of the trip: leaving the garage, each stop, and getting back. */
enum class StepState { DONE, NOW, LATER }

fun TripDto.progress(blocks: List<StopBlock>, step: TodayStep): List<StepState> {
    val current = (step as? TodayStep.AtBlock)?.block?.stopId
    val own = blocks.filterNot { it.rescued }
    return buildList {
        add(if (garageDepartedAt != null || step !is TodayStep.LeaveGarage) StepState.DONE else StepState.NOW)
        own.forEach { block ->
            add(
                when {
                    block.departedAt != null -> StepState.DONE
                    block.stopId == current -> StepState.NOW
                    else -> StepState.LATER
                }
            )
        }
        add(
            when (step) {
                TodayStep.Finished -> StepState.DONE
                TodayStep.ReturnToGarage -> StepState.NOW
                else -> StepState.LATER
            }
        )
    }
}

/** When the [event] the crew can still take back happened, to show next to the undo. */
fun TripDto.timeOf(event: UndoableEvent, blocks: List<StopBlock>): Instant? {
    val block = event.stopId?.let { id -> blocks.firstOrNull { id in it.stopIds } }
    return when (event.kind) {
        TripEventKind.GARAGE_DEPARTED -> garageDepartedAt
        TripEventKind.GARAGE_ARRIVED -> garageArrivedAt
        TripEventKind.ARRIVED -> block?.arrivedAt
        TripEventKind.HANDLING_STARTED -> block?.handlingStartedAt
        TripEventKind.HANDLING_FINISHED -> block?.handlingFinishedAt
        TripEventKind.DEPARTED -> block?.departedAt
    }
}

/** The reefer's last reading on this trip, so the crew starts from a number close to today's. */
val TripDto.lastTemperature: Double?
    get() = stops.orEmpty()
        .sortedBy { it.order ?: 0 }
        .flatMap { listOfNotNull(it.temperatureAtArrival, it.temperatureAtDeparture) }
        .lastOrNull()
