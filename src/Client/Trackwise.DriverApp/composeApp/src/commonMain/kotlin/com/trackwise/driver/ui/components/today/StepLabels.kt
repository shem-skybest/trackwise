package com.trackwise.driver.ui.components.today

import com.trackwise.driver.api.models.TripEventKind
import com.trackwise.driver.model.StopBlock
import com.trackwise.driver.ui.i18n.Strings

/** "Drop 2" or "Warehouse", as the crew calls the stop. */
fun Strings.blockName(block: StopBlock): String = when {
    block.isDrop -> drop(block.number)
    block.number == 0 -> warehouse
    else -> "$warehouse ${block.number}"
}

/** The stop's own name when the booking has one, else what kind of stop it is. */
fun Strings.blockPlace(block: StopBlock): String = block.place.ifBlank { blockName(block) }

/** What the button says for the tap that records [kind]. */
fun Strings.action(kind: TripEventKind, isDrop: Boolean): String = when (kind) {
    TripEventKind.GARAGE_DEPARTED -> leaveGarage
    TripEventKind.ARRIVED -> arrive
    TripEventKind.HANDLING_STARTED -> if (isDrop) startUnloading else startLoading
    TripEventKind.HANDLING_FINISHED -> if (isDrop) doneUnloading else doneLoading
    TripEventKind.DEPARTED -> leaveStop
    TripEventKind.GARAGE_ARRIVED -> backAtGarage
}

/** What's on record once [kind] is tapped. */
fun Strings.recorded(kind: TripEventKind, isDrop: Boolean): String = when (kind) {
    TripEventKind.GARAGE_DEPARTED -> leftGarage
    TripEventKind.ARRIVED -> arrived
    TripEventKind.HANDLING_STARTED -> if (isDrop) startedUnloading else startedLoading
    TripEventKind.HANDLING_FINISHED -> if (isDrop) finishedUnloading else finishedLoading
    TripEventKind.DEPARTED -> departed
    TripEventKind.GARAGE_ARRIVED -> returnedToGarage
}

fun Strings.cargo(block: StopBlock): String? = listOfNotNull(
    block.boxes.takeIf { it > 0 }?.let(::boxes),
    block.bags.takeIf { it > 0 }?.let(::bags)
).joinToString(" + ").ifBlank { null }
