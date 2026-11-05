package com.trackwise.driver.ui.components.today

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Undo
import androidx.compose.material.icons.filled.Build
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.Inventory2
import androidx.compose.material.icons.filled.LocalShipping
import androidx.compose.material.icons.filled.Map
import androidx.compose.material.icons.filled.PhotoCamera
import androidx.compose.material.icons.filled.Place
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.trackwise.driver.api.models.DvirType
import com.trackwise.driver.api.models.TripEventKind
import com.trackwise.driver.model.StepState
import com.trackwise.driver.model.StopBlock
import com.trackwise.driver.model.TodayStep
import com.trackwise.driver.model.UndoableEvent
import com.trackwise.driver.model.callTime
import com.trackwise.driver.model.clientName
import com.trackwise.driver.model.helperNames
import com.trackwise.driver.model.notYetVisited
import com.trackwise.driver.model.progress
import com.trackwise.driver.model.timeOf
import com.trackwise.driver.model.todayStep
import com.trackwise.driver.model.undoableEvent
import com.trackwise.driver.ui.components.CardContainer
import com.trackwise.driver.ui.i18n.LocalStrings
import com.trackwise.driver.ui.i18n.Strings
import com.trackwise.driver.ui.theme.StepAtStop
import com.trackwise.driver.ui.theme.StepDone
import com.trackwise.driver.ui.theme.StepGo
import com.trackwise.driver.util.buildLocationUrl
import com.trackwise.driver.util.formatClock
import com.trackwise.driver.viewmodel.TodayTrip
import kotlin.time.Clock

/** One trip as the crew runs it: who and which truck, then only the next thing to do. */
@Composable
fun TodayStepCard(
    today: TodayTrip,
    chosenStopId: String?,
    saving: Boolean,
    onChooseStop: (String) -> Unit,
    onEvent: (StopBlock?, TripEventKind) -> Unit,
    onUndo: (UndoableEvent) -> Unit,
    onPhoto: (StopBlock) -> Unit,
    onTruckCheck: (DvirType) -> Unit,
    onOpenMaps: (String) -> Unit,
    onTripClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    val s = LocalStrings.current
    val trip = today.trip
    val step = trip.todayStep(today.blocks, chosenStopId)
    val dots = trip.progress(today.blocks, step)
    val undoable = remember(trip) { trip.undoableEvent(Clock.System.now()) }

    CardContainer(modifier = modifier) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(14.dp)
        ) {
            Column(modifier = Modifier.fillMaxWidth().clickable(onClick = onTripClick)) {
                Text(
                    text = trip.truckNumber ?: trip.name ?: "#${trip.number}",
                    style = MaterialTheme.typography.headlineSmall,
                    fontWeight = FontWeight.Bold
                )
                Text(
                    text = listOfNotNull(trip.clientName, trip.callTime?.let { s.callTime(it.formatClock()) })
                        .joinToString(" · "),
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                trip.helperNames.takeIf { it.isNotBlank() }?.let {
                    Text(s.helper(it), style = MaterialTheme.typography.bodyMedium)
                }
            }

            val now = dots.indexOfFirst { it == StepState.NOW }.takeIf { it >= 0 } ?: dots.lastIndex
            ProgressDots(dots, s.step(now + 1, dots.size))

            when (step) {
                TodayStep.LeaveGarage -> LeaveGarageStep(today, saving, onEvent, onTruckCheck)
                is TodayStep.AtBlock -> BlockStep(
                    step = step,
                    canChoose = step.next == TripEventKind.ARRIVED && today.blocks.notYetVisited().size > 1,
                    choices = today.blocks.notYetVisited(),
                    saving = saving,
                    onChooseStop = onChooseStop,
                    onEvent = onEvent,
                    onPhoto = onPhoto,
                    onOpenMaps = onOpenMaps
                )
                TodayStep.ReturnToGarage -> ReturnToGarageStep(saving, onEvent, onTruckCheck)
                TodayStep.Finished -> FinishedTrip()
            }

            undoable?.let { event ->
                val block = event.stopId?.let { id -> today.blocks.firstOrNull { id in it.stopIds } }
                UndoStrip(
                    text = listOfNotNull(
                        s.recorded(event.kind, block?.isDrop == true),
                        block?.let(s::blockName),
                        trip.timeOf(event, today.blocks)?.formatClock()
                    ).joinToString(" · "),
                    enabled = !saving,
                    onUndo = { onUndo(event) }
                )
            }
        }
    }
}

@Composable
private fun LeaveGarageStep(
    today: TodayTrip,
    saving: Boolean,
    onEvent: (StopBlock?, TripEventKind) -> Unit,
    onTruckCheck: (DvirType) -> Unit
) {
    val s = LocalStrings.current
    if (today.preTripDvirDone) {
        DoneLine(s.truckCheckDone)
    } else {
        Eyebrow(s.beforeLeaving, StepGo)
        Text(s.truckCheckTitle, style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.Bold)
        Text(s.truckCheckHint, color = MaterialTheme.colorScheme.onSurfaceVariant)
        BigOutlinedButton(s.truckCheckButton, Icons.Default.Build, onClick = { onTruckCheck(DvirType.PRE_TRIP) })
        Text(
            s.truckCheckOptional,
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
    }

    StepTitle(s.readyTitle)
    today.blocks.firstOrNull { !it.rescued }?.let {
        Text(s.firstStop(s.blockPlace(it)), color = MaterialTheme.colorScheme.onSurfaceVariant)
    }
    BigActionButton(
        text = s.leaveGarage,
        icon = Icons.Default.LocalShipping,
        color = StepGo,
        enabled = !saving,
        onClick = { onEvent(null, TripEventKind.GARAGE_DEPARTED) }
    )
}

@Composable
private fun BlockStep(
    step: TodayStep.AtBlock,
    canChoose: Boolean,
    choices: List<StopBlock>,
    saving: Boolean,
    onChooseStop: (String) -> Unit,
    onEvent: (StopBlock?, TripEventKind) -> Unit,
    onPhoto: (StopBlock) -> Unit,
    onOpenMaps: (String) -> Unit
) {
    val s = LocalStrings.current
    val block = step.block
    var choosing by remember { mutableStateOf(false) }

    if (step.next == TripEventKind.ARRIVED) {
        Eyebrow(s.headingTo, StepGo)
    } else {
        Eyebrow(s.atPlace(s.blockName(block)), StepAtStop)
    }
    StepTitle(s.blockPlace(block))
    StopDetails(s, block)

    if (step.next == TripEventKind.ARRIVED) {
        val lat = block.latitude
        val lng = block.longitude
        if (lat != null && lng != null) {
            BigOutlinedButton(s.openMaps, Icons.Default.Map, onClick = { onOpenMaps(buildLocationUrl(lat, lng)) })
        }
    }

    BigActionButton(
        text = s.action(step.next, block.isDrop),
        icon = when (step.next) {
            TripEventKind.ARRIVED -> Icons.Default.Place
            TripEventKind.HANDLING_STARTED, TripEventKind.HANDLING_FINISHED -> Icons.Default.Inventory2
            else -> Icons.Default.LocalShipping
        },
        color = StepAtStop,
        enabled = !saving,
        onClick = { onEvent(block, step.next) }
    )

    if (step.next == TripEventKind.HANDLING_STARTED) {
        TextButton(onClick = { onEvent(block, TripEventKind.DEPARTED) }, enabled = !saving) {
            Text(s.skipHandling)
        }
    }

    if (step.next != TripEventKind.ARRIVED) {
        BigOutlinedButton(
            text = "${if (block.isDrop) s.photoDelivery else s.photoLoading} (${s.photoCount(block.photoCount)})",
            icon = Icons.Default.PhotoCamera,
            onClick = { onPhoto(block) }
        )
    }

    if (canChoose) {
        TextButton(onClick = { choosing = true }) { Text(s.otherStop) }
    }

    if (choosing) {
        AlertDialog(
            onDismissRequest = { choosing = false },
            title = { Text(s.chooseStop) },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    choices.forEach { choice ->
                        BigOutlinedButton(
                            text = listOf(s.blockName(choice), choice.place).filter { it.isNotBlank() }.distinct()
                                .joinToString(" · "),
                            icon = Icons.Default.Place,
                            onClick = {
                                onChooseStop(choice.stopId)
                                choosing = false
                            }
                        )
                    }
                }
            },
            confirmButton = { TextButton(onClick = { choosing = false }) { Text(s.cancel) } }
        )
    }
}

@Composable
private fun StopDetails(s: Strings, block: StopBlock) {
    val lines = listOfNotNull(
        block.address.takeIf { it.isNotBlank() },
        listOfNotNull(s.cargo(block), block.cutOff?.let { s.cutOff(it.formatClock()) })
            .joinToString(" · ").ifBlank { null }
    )
    lines.forEach { Text(it, color = MaterialTheme.colorScheme.onSurfaceVariant) }
}

@Composable
private fun ReturnToGarageStep(
    saving: Boolean,
    onEvent: (StopBlock?, TripEventKind) -> Unit,
    onTruckCheck: (DvirType) -> Unit
) {
    val s = LocalStrings.current
    Eyebrow(s.lastStep, StepGo)
    StepTitle(s.goingHome)
    Text(s.goingHomeHint, color = MaterialTheme.colorScheme.onSurfaceVariant)
    BigActionButton(
        text = s.backAtGarage,
        icon = Icons.Default.Home,
        color = StepDone,
        enabled = !saving,
        onClick = { onEvent(null, TripEventKind.GARAGE_ARRIVED) }
    )
    BigOutlinedButton(s.postTripCheck, Icons.Default.Build, onClick = { onTruckCheck(DvirType.POST_TRIP) })
}

/** The big green finish, so nobody wonders whether a step was missed. */
@Composable
fun FinishedTrip(modifier: Modifier = Modifier) {
    val s = LocalStrings.current
    Column(
        modifier = modifier.fillMaxWidth().padding(vertical = 16.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Icon(Icons.Default.CheckCircle, contentDescription = null, tint = StepDone, modifier = Modifier.size(96.dp))
        Text(s.finishedTitle, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
        Text(s.finishedBody, color = MaterialTheme.colorScheme.onSurfaceVariant)
    }
}

@Composable
private fun UndoStrip(text: String, enabled: Boolean, onUndo: () -> Unit) {
    Surface(
        color = MaterialTheme.colorScheme.surfaceVariant,
        shape = RoundedCornerShape(12.dp),
        modifier = Modifier.fillMaxWidth()
    ) {
        Row(
            modifier = Modifier.padding(start = 12.dp, end = 4.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(text, style = MaterialTheme.typography.bodyMedium, modifier = Modifier.weight(1f))
            TextButton(onClick = onUndo, enabled = enabled) {
                Icon(Icons.AutoMirrored.Filled.Undo, contentDescription = null, modifier = Modifier.size(18.dp))
                Text(LocalStrings.current.undo, modifier = Modifier.padding(start = 4.dp), fontWeight = FontWeight.Bold)
            }
        }
    }
}

@Composable
private fun Eyebrow(text: String, color: Color) {
    Text(text.uppercase(), style = MaterialTheme.typography.labelLarge, fontWeight = FontWeight.Bold, color = color)
}

@Composable
private fun StepTitle(text: String) {
    Text(text, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
}

@Composable
private fun DoneLine(text: String) {
    Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
        Icon(Icons.Default.CheckCircle, contentDescription = null, tint = StepDone)
        Text(text, fontWeight = FontWeight.Bold, color = StepDone)
    }
}
