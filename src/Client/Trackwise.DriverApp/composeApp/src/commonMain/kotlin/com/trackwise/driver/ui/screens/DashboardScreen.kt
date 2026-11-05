package com.trackwise.driver.ui.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.LifecycleResumeEffect
import com.trackwise.driver.api.models.DvirType
import com.trackwise.driver.model.StopBlock
import com.trackwise.driver.permission.RequestLocationPermissionFlow
import com.trackwise.driver.permission.openAppSettings
import com.trackwise.driver.service.ArrivalNudges
import com.trackwise.driver.service.UnsentTripEvent
import com.trackwise.driver.ui.components.AppTopBar
import com.trackwise.driver.ui.components.CardContainer
import com.trackwise.driver.ui.components.UiStateContent
import com.trackwise.driver.ui.components.today.BigActionButton
import com.trackwise.driver.ui.components.today.FinishedTrip
import com.trackwise.driver.ui.components.today.TodayStepCard
import com.trackwise.driver.ui.components.today.TripEventSheet
import com.trackwise.driver.ui.components.today.action
import com.trackwise.driver.ui.i18n.LocalStrings
import com.trackwise.driver.ui.theme.StepProblem
import com.trackwise.driver.util.formatClock
import com.trackwise.driver.viewmodel.DashboardViewModel
import com.trackwise.driver.viewmodel.base.UiState
import org.koin.compose.viewmodel.koinViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DashboardScreen(
    onTripClick: (String) -> Unit,
    onTruckCheck: (truckId: String?, tripId: String?, type: DvirType) -> Unit,
    onStopPhoto: (block: StopBlock) -> Unit,
    onOpenUrl: (String) -> Unit,
    viewModel: DashboardViewModel = koinViewModel()
) {
    val s = LocalStrings.current
    val uiState by viewModel.uiState.collectAsState()
    val isSharing by viewModel.isSharingLocation.collectAsState()
    val eventDraft by viewModel.eventDraft.collectAsState()
    val eventSaving by viewModel.eventSaving.collectAsState()
    val eventError by viewModel.eventError.collectAsState()
    val chosenStops by viewModel.chosenStops.collectAsState()
    val justFinished by viewModel.justFinished.collectAsState()
    val unsentTaps by viewModel.unsentTaps.collectAsState()
    val refreshing by viewModel.refreshing.collectAsState()
    var askLocation by remember { mutableStateOf(false) }
    // Android answers "denied" straight away once the crew has said no twice; from then on only
    // the phone's settings can turn location back on.
    var locationBlocked by rememberSaveable { mutableStateOf(false) }

    // Coming back from the phone's settings doesn't recompose Today, so check again on every resume.
    LifecycleResumeEffect(Unit) {
        viewModel.startLocationSharing()
        onPauseOrDispose { }
    }

    // The view model outlives this screen, so coming back from a truck check or photo would otherwise
    // show the state from before it. The first visit is already loading from init.
    var visited by rememberSaveable { mutableStateOf(false) }
    LaunchedEffect(Unit) {
        if (visited) viewModel.refresh() else visited = true
    }

    // Taken here, not in the view model, so only a Today that's on screen answers the buzz.
    val arrivalRequest by ArrivalNudges.requested.collectAsState()
    LaunchedEffect(arrivalRequest) {
        if (arrivalRequest != null) viewModel.openRequestedArrival()
    }

    RequestLocationPermissionFlow(trigger = askLocation) { granted ->
        askLocation = false
        locationBlocked = !granted
        viewModel.startLocationSharing()
    }

    Scaffold(
        topBar = {
            AppTopBar(
                title = s.navToday,
                actions = {
                    IconButton(onClick = { viewModel.refresh() }) {
                        Icon(Icons.Default.Refresh, s.refresh)
                    }
                }
            )
        }
    ) { paddingValues ->
        PullToRefreshBox(
            isRefreshing = uiState is UiState.Loading || refreshing,
            onRefresh = { viewModel.refresh() },
            modifier = Modifier.padding(paddingValues)
        ) {
            UiStateContent(uiState, viewModel::refresh) { data ->
                LazyColumn(
                    modifier = Modifier.fillMaxSize(),
                    contentPadding = PaddingValues(16.dp),
                    verticalArrangement = Arrangement.spacedBy(16.dp)
                ) {
                    if (!isSharing) {
                        item {
                            LocationOffCard(
                                blocked = locationBlocked,
                                onTurnOn = { if (locationBlocked) openAppSettings() else askLocation = true }
                            )
                        }
                    }

                    if (data.offline || data.waitingTaps > 0) {
                        item { NoSignalCard(offline = data.offline, waitingTaps = data.waitingTaps) }
                    }

                    if (data.trips.isEmpty()) {
                        item {
                            CardContainer {
                                if (justFinished) {
                                    FinishedTrip(Modifier.padding(16.dp))
                                } else {
                                    NoTripYet()
                                }
                            }
                        }
                    }

                    items(data.trips, key = { it.trip.id ?: "" }) { today ->
                        val tripId = today.trip.id.orEmpty()
                        TodayStepCard(
                            today = today,
                            chosenStopId = chosenStops[tripId],
                            saving = eventSaving,
                            onChooseStop = { viewModel.chooseStop(tripId, it) },
                            onEvent = { block, kind -> viewModel.startEvent(today.trip, block, kind) },
                            onUndo = { event -> viewModel.undo(today.trip, event) },
                            onPhoto = onStopPhoto,
                            onTruckCheck = { type -> onTruckCheck(today.trip.truckId, today.trip.id, type) },
                            onOpenMaps = onOpenUrl,
                            onTripClick = { today.trip.id?.let(onTripClick) }
                        )
                    }
                }
            }
        }
    }

    if (unsentTaps.isNotEmpty()) {
        UnsentTapsDialog(unsentTaps, onDismiss = viewModel::dismissUnsentTaps)
    }

    eventDraft?.let { draft ->
        TripEventSheet(
            draft = draft,
            saving = eventSaving,
            error = eventError?.ifBlank { s.notSaved },
            onDismiss = viewModel::dismissEvent,
            onSubmit = viewModel::submitEvent
        )
    }

    // An undo has no sheet to show its refusal in.
    if (eventDraft == null) {
        eventError?.let { message ->
            AlertDialog(
                onDismissRequest = viewModel::clearEventError,
                text = { Text(message.ifBlank { s.notSaved }) },
                confirmButton = { TextButton(onClick = viewModel::clearEventError) { Text(s.ok) } }
            )
        }
    }
}

@Composable
private fun NoTripYet() {
    val s = LocalStrings.current
    Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
        Text(s.noTripTitle, style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.Bold)
        Text(s.noTripBody, color = MaterialTheme.colorScheme.onSurfaceVariant)
    }
}

/** Taps made without signal are kept on the phone; this says so, so nobody taps twice. */
@Composable
private fun NoSignalCard(offline: Boolean, waitingTaps: Int) {
    val s = LocalStrings.current
    CardContainer {
        Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
            if (offline) {
                Text(s.offlineTitle, style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.Bold)
                Text(s.offlineBody)
            }
            if (waitingTaps > 0) {
                Text(s.waitingTaps(waitingTaps), fontWeight = FontWeight.Bold)
            }
        }
    }
}

/** Saved taps the API refused once they went out, usually because they were too old to enter. */
@Composable
private fun UnsentTapsDialog(taps: List<UnsentTripEvent>, onDismiss: () -> Unit) {
    val s = LocalStrings.current
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(s.unsentTitle) },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                Text(s.unsentBody)
                taps.forEach { tap ->
                    val c = tap.event.command
                    val what = listOfNotNull(
                        c.kind?.let { s.action(it, tap.event.isDrop) },
                        tap.event.place,
                        c.at?.formatClock()
                    ).joinToString(" · ")
                    Text(what, fontWeight = FontWeight.Bold)
                    Text(tap.reason, color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
            }
        },
        confirmButton = { TextButton(onClick = onDismiss) { Text(s.ok) } }
    )
}

/** Location can only be off because the phone blocks it; the crew can't switch it off themselves. */
@Composable
private fun LocationOffCard(blocked: Boolean, onTurnOn: () -> Unit) {
    val s = LocalStrings.current
    CardContainer {
        Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
            Text(s.locationOffTitle, style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.Bold, color = StepProblem)
            Text(s.locationOffBody)
            if (blocked) Text(s.locationBlockedBody, fontWeight = FontWeight.Bold)
            BigActionButton(
                text = if (blocked) s.locationSettingsButton else s.locationOffButton,
                icon = if (blocked) Icons.Default.Settings else Icons.Default.LocationOn,
                color = StepProblem,
                onClick = onTurnOn
            )
        }
    }
}
