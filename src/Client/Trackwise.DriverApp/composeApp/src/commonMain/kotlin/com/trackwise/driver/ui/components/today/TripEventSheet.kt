package com.trackwise.driver.ui.components.today

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.AssignmentReturn
import androidx.compose.material.icons.filled.Block
import androidx.compose.material.icons.filled.Cancel
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TimeInput
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.material3.rememberTimePickerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.trackwise.driver.api.models.DropReturnReason
import com.trackwise.driver.api.models.TripEventKind
import com.trackwise.driver.model.CrewBackdateWindow
import com.trackwise.driver.model.DropOutcomeChoice
import com.trackwise.driver.model.TripEventDraft
import com.trackwise.driver.model.TripEventInput
import com.trackwise.driver.model.lastTemperature
import com.trackwise.driver.ui.i18n.LocalStrings
import com.trackwise.driver.ui.theme.StepAtStop
import com.trackwise.driver.ui.theme.StepDone
import com.trackwise.driver.ui.theme.StepGo
import com.trackwise.driver.ui.theme.StepNeutral
import com.trackwise.driver.ui.theme.StepProblem
import com.trackwise.driver.util.formatClock
import kotlin.math.roundToInt
import kotlin.time.Clock
import kotlin.time.Duration.Companion.days
import kotlin.time.Instant
import kotlinx.datetime.LocalDateTime
import kotlinx.datetime.LocalTime
import kotlinx.datetime.TimeZone
import kotlinx.datetime.toInstant
import kotlinx.datetime.toLocalDateTime

private enum class TimeChoice { NOW, GPS, EARLIER }

/**
 * Management wants crews to record events as they happen, so the typed "earlier" time is off.
 * The GPS arrival time stays. Flip this to bring the earlier-time picker back.
 */
private const val OfferEarlierTime = false

/** Readings crews see most: frozen goods around -18 to -20, chilled around 0 to 4. */
private val CommonTemperatures = listOf(-20, -18, 0, 2, 4)

private const val FallbackTemperature = -18

/**
 * Confirms one tap: when it happened (now, the time GPS saw the truck arrive, or a little earlier),
 * the reefer temperature on arrival and departure, and a drop's outcome on departure. Everything is
 * a tap; only the optional note uses the keyboard.
 */
@OptIn(ExperimentalMaterial3Api::class, ExperimentalLayoutApi::class)
@Composable
fun TripEventSheet(
    draft: TripEventDraft,
    saving: Boolean,
    error: String?,
    onDismiss: () -> Unit,
    onSubmit: (TripEventInput) -> Unit
) {
    val s = LocalStrings.current
    val block = draft.block
    val isDrop = block?.isDrop == true
    val gps = draft.gpsArrivedAt
    val asksTemperature = draft.kind == TripEventKind.ARRIVED || draft.kind == TripEventKind.DEPARTED
    val asksOutcome = draft.kind == TripEventKind.DEPARTED && isDrop

    var timeChoice by remember { mutableStateOf(if (gps != null) TimeChoice.GPS else TimeChoice.NOW) }
    val now = remember { Clock.System.now() }
    val local = remember { now.toLocalDateTime(TimeZone.currentSystemDefault()) }
    val timeState = rememberTimePickerState(local.hour, local.minute, is24Hour = true)
    var temperature by remember { mutableStateOf(draft.trip.lastTemperature?.roundToInt()) }
    var outcome by remember { mutableStateOf(DropOutcomeChoice.DELIVERED) }
    var boxes by remember { mutableStateOf(0) }
    var bags by remember { mutableStateOf(0) }
    var reason by remember { mutableStateOf<DropReturnReason?>(null) }
    var note by remember { mutableStateOf("") }

    val at: Instant? = when (timeChoice) {
        TimeChoice.NOW -> null
        TimeChoice.GPS -> gps
        TimeChoice.EARLIER -> earlierToday(timeState.hour, timeState.minute, now)
    }
    val tooOld = at != null && Clock.System.now() - at > CrewBackdateWindow
    val returned = asksOutcome && outcome == DropOutcomeChoice.SOME_RETURNED
    val returnsMissing = returned && (boxes + bags <= 0 || reason == null)
    val canSave = !saving && !tooOld && !returnsMissing

    ModalBottomSheet(onDismissRequest = onDismiss, sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true)) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 16.dp)
                .padding(bottom = 16.dp)
                .navigationBarsPadding(),
            verticalArrangement = Arrangement.spacedBy(14.dp)
        ) {
            Text(
                text = listOfNotNull(s.action(draft.kind, isDrop), block?.let(s::blockPlace)).joinToString(" · "),
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold
            )

            FlowRow(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                BigChip(s.whenNow, timeChoice == TimeChoice.NOW) { timeChoice = TimeChoice.NOW }
                gps?.let { BigChip(s.whenGps(it.formatClock()), timeChoice == TimeChoice.GPS) { timeChoice = TimeChoice.GPS } }
                if (OfferEarlierTime) {
                    BigChip(s.whenEarlier, timeChoice == TimeChoice.EARLIER) { timeChoice = TimeChoice.EARLIER }
                }
            }
            if (timeChoice == TimeChoice.EARLIER) {
                TimeInput(state = timeState)
            }
            if (tooOld) {
                Text(s.tooOld(CrewBackdateWindow.inWholeHours), color = MaterialTheme.colorScheme.error)
            }

            if (asksTemperature) {
                Text(s.tempQuestion, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
                NumberStepper(
                    value = temperature?.let { "$it°C" } ?: "—",
                    onMinus = { temperature = (temperature ?: FallbackTemperature) - 1 },
                    onPlus = { temperature = (temperature ?: FallbackTemperature) + 1 }
                )
                Text(s.tempHint, style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                FlowRow(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    CommonTemperatures.forEach { t ->
                        BigChip(if (t > 0) "+$t°" else "$t°", temperature == t) { temperature = t }
                    }
                    BigChip(s.tempSkip, temperature == null) { temperature = null }
                }
            }

            if (asksOutcome) {
                Text(s.outcomeQuestion, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
                OutcomeTiles(outcome) { outcome = it }

                if (returned) {
                    Text(s.returnedBoxes, style = MaterialTheme.typography.titleSmall, fontWeight = FontWeight.Bold)
                    NumberStepper("$boxes", { boxes = (boxes - 1).coerceAtLeast(0) }, { boxes += 1 })
                    Text(s.returnedBags, style = MaterialTheme.typography.titleSmall, fontWeight = FontWeight.Bold)
                    NumberStepper("$bags", { bags = (bags - 1).coerceAtLeast(0) }, { bags += 1 })
                    Text(s.returnReason, style = MaterialTheme.typography.titleSmall, fontWeight = FontWeight.Bold)
                    FlowRow(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        DropReturnReason.entries.forEach { r ->
                            BigChip(s.reason(r), reason == r) { reason = r }
                        }
                    }
                    if (returnsMissing) {
                        Text(s.returnsMissing, color = MaterialTheme.colorScheme.error)
                    }
                }

                OutlinedTextField(
                    value = note,
                    onValueChange = { note = it.take(500) },
                    label = { Text(s.noteLabel) },
                    modifier = Modifier.fillMaxWidth()
                )
            }

            error?.let {
                Text(text = it, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodyLarge)
            }

            BigActionButton(
                text = if (saving) s.saving else s.saveButton,
                icon = Icons.Default.CheckCircle,
                color = when (draft.kind) {
                    TripEventKind.GARAGE_DEPARTED -> StepGo
                    TripEventKind.GARAGE_ARRIVED -> StepDone
                    else -> StepAtStop
                },
                enabled = canSave,
                onClick = {
                    onSubmit(
                        TripEventInput(
                            at = at,
                            temperature = temperature?.toDouble()?.takeIf { asksTemperature },
                            outcome = if (asksOutcome) outcome.status else null,
                            returnedBoxes = if (returned) boxes else null,
                            returnedBags = if (returned) bags else null,
                            returnReason = if (returned) reason else null,
                            note = note.trim().takeIf { asksOutcome && it.isNotEmpty() }
                        )
                    )
                }
            )
        }
    }
}

@Composable
private fun OutcomeTiles(selected: DropOutcomeChoice, onSelect: (DropOutcomeChoice) -> Unit) {
    val s = LocalStrings.current
    val tiles = listOf(
        Triple(DropOutcomeChoice.DELIVERED, s.outcomeDelivered, Icons.Default.CheckCircle) to StepDone,
        Triple(DropOutcomeChoice.SOME_RETURNED, s.outcomeSomeReturned, Icons.AutoMirrored.Filled.AssignmentReturn) to StepAtStop,
        Triple(DropOutcomeChoice.FOUL_TRIP, s.outcomeFoul, Icons.Default.Block) to StepNeutral,
        Triple(DropOutcomeChoice.CANCELLED_BY_CLIENT, s.outcomeCancelled, Icons.Default.Cancel) to StepProblem
    )
    Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
        tiles.chunked(2).forEach { row ->
            Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                row.forEach { (tile, color) ->
                    val (choice, label, icon) = tile
                    ChoiceTile(
                        text = label,
                        icon = icon,
                        color = color,
                        selected = selected == choice,
                        onClick = { onSelect(choice) },
                        modifier = Modifier.weight(1f)
                    )
                }
            }
        }
    }
}

@Composable
private fun BigChip(text: String, selected: Boolean, onClick: () -> Unit) {
    FilterChip(
        selected = selected,
        onClick = onClick,
        label = { Text(text, fontSize = 16.sp, fontWeight = FontWeight.Bold) },
        modifier = Modifier.heightIn(min = 48.dp)
    )
}

/** A time picked on the clock: today, or yesterday when it would otherwise be in the future (just past midnight). */
private fun earlierToday(hour: Int, minute: Int, now: Instant): Instant {
    val zone = TimeZone.currentSystemDefault()
    val picked = LocalDateTime(now.toLocalDateTime(zone).date, LocalTime(hour, minute)).toInstant(zone)
    return if (picked > now) picked - 1.days else picked
}
