package com.trackwise.driver.viewmodel

import androidx.lifecycle.viewModelScope
import com.trackwise.driver.api.ApiException
import com.trackwise.driver.api.DriverApi
import com.trackwise.driver.api.DvirApi
import com.trackwise.driver.api.TripApi
import com.trackwise.driver.api.TruckApi
import com.trackwise.driver.api.bodyOrThrow
import com.trackwise.driver.api.models.DvirStatus
import com.trackwise.driver.api.models.DvirType
import com.trackwise.driver.api.models.SetDriverDeviceTokenCommand
import com.trackwise.driver.api.models.TripDto
import com.trackwise.driver.api.models.TripEventKind
import com.trackwise.driver.api.models.TripStatus
import com.trackwise.driver.api.models.TripStopPhotoDto
import com.trackwise.driver.api.models.TruckDto
import com.trackwise.driver.api.models.UndoDriverTripEventCommand
import com.trackwise.driver.api.throwIfFailed
import com.trackwise.driver.model.PendingTripEvent
import com.trackwise.driver.model.StopBlock
import com.trackwise.driver.model.TripEventDraft
import com.trackwise.driver.model.TripEventInput
import com.trackwise.driver.model.UndoableEvent
import com.trackwise.driver.model.matching
import com.trackwise.driver.model.toStopBlocks
import com.trackwise.driver.model.withPending
import com.trackwise.driver.service.ArrivalNudges
import com.trackwise.driver.service.LoadProximityWatcher
import com.trackwise.driver.service.NearbyStop
import com.trackwise.driver.service.LocationSharing
import com.trackwise.driver.service.PreferencesManager
import com.trackwise.driver.service.PushTokenRegistry
import com.trackwise.driver.service.TripEventOutbox
import com.trackwise.driver.service.TripEventRecorder
import com.trackwise.driver.service.UnsentTripEvent
import com.trackwise.driver.util.Logger
import com.trackwise.driver.viewmodel.base.BaseViewModel
import com.trackwise.driver.viewmodel.base.UiState
import kotlin.time.Clock
import kotlin.time.Duration.Companion.hours
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock

data class TodayTrip(
    val trip: TripDto,
    val blocks: List<StopBlock>,
    val preTripDvirDone: Boolean = false
)

data class DashboardData(
    val truck: TruckDto?,
    val trips: List<TodayTrip> = emptyList(),
    val currentUserId: String? = null,
    /** No signal: Today shows the trip as last loaded, plus the crew's taps since. */
    val offline: Boolean = false,
    /** Taps kept on the phone, waiting for signal. */
    val waitingTaps: Int = 0
)

/** A trip as the API last sent it, before the taps waiting on the phone are added. */
private data class LoadedTrip(val trip: TripDto, val photos: List<TripStopPhotoDto>, val preTripDvirDone: Boolean)

private data class Loaded(
    val truck: TruckDto?,
    val trips: List<LoadedTrip>,
    val userId: String,
    val offline: Boolean = false
) {
    fun withTaps(pending: List<PendingTripEvent>) = DashboardData(
        truck = truck,
        trips = trips.map {
            val trip = it.trip.withPending(pending)
            TodayTrip(trip, trip.toStopBlocks(it.photos), it.preTripDvirDone)
        },
        currentUserId = userId,
        offline = offline,
        waitingTaps = pending.size
    )
}

class DashboardViewModel(
    private val truckApi: TruckApi,
    private val tripApi: TripApi,
    private val driverApi: DriverApi,
    private val dvirApi: DvirApi,
    private val preferencesManager: PreferencesManager,
    private val locationSharing: LocationSharing,
    private val proximityWatcher: LoadProximityWatcher,
    private val tripEventRecorder: TripEventRecorder,
    private val outbox: TripEventOutbox
) : BaseViewModel() {

    private val loaded = MutableStateFlow<UiState<Loaded>>(UiState.Loading)
    private val reloading = Mutex()

    private val _refreshing = MutableStateFlow(false)
    val refreshing: StateFlow<Boolean> = _refreshing.asStateFlow()

    val uiState: StateFlow<UiState<DashboardData>> = combine(loaded, outbox.pending) { state, pending ->
        when (state) {
            is UiState.Success -> UiState.Success(state.data.withTaps(pending))
            is UiState.Error -> state
            UiState.Loading -> UiState.Loading
        }
    }.stateIn(viewModelScope, SharingStarted.Eagerly, UiState.Loading)

    /** Saved taps the API refused once signal came back, for the crew to tell the office about. */
    val unsentTaps: StateFlow<List<UnsentTripEvent>> = outbox.unsent

    val isSharingLocation: StateFlow<Boolean> = locationSharing.isSharing

    private val _eventDraft = MutableStateFlow<TripEventDraft?>(null)
    val eventDraft: StateFlow<TripEventDraft?> = _eventDraft.asStateFlow()

    private val _eventSaving = MutableStateFlow(false)
    val eventSaving: StateFlow<Boolean> = _eventSaving.asStateFlow()

    /** Why the last tap or undo was refused, in the API's words; blank when it never reached the API. */
    private val _eventError = MutableStateFlow<String?>(null)
    val eventError: StateFlow<String?> = _eventError.asStateFlow()

    /** Which stop the crew said they are heading to, by trip, when it isn't the next one in order. */
    private val _chosenStops = MutableStateFlow<Map<String, String>>(emptyMap())
    val chosenStops: StateFlow<Map<String, String>> = _chosenStops.asStateFlow()

    /** The crew just got back to the garage, so Today shows the finish instead of an empty screen. */
    private val _justFinished = MutableStateFlow(false)
    val justFinished: StateFlow<Boolean> = _justFinished.asStateFlow()

    init {
        outbox.keepRetrying()
        loadDashboard()
        // Without this, Today would drop the sent taps and fall back to the trip as last loaded,
        // from before them. A reload already running fetches after its own send, so skip then.
        viewModelScope.launch {
            outbox.delivered.collect { if (!reloading.isLocked) reload() }
        }
    }

    /**
     * The crew answered "Oo" to the buzz, so open its arrival sheet on fresh data. Nothing opens if the
     * arrival is already on record by then (the office or another tap got there first).
     */
    fun openRequestedArrival() {
        val stop = ArrivalNudges.consumeRequest() ?: return
        viewModelScope.launch {
            reload()
            openArrival(stop)
        }
    }

    private fun openArrival(stop: NearbyStop) {
        val trips = (uiState.value as? UiState.Success)?.data?.trips.orEmpty()
        for (today in trips) {
            val block = today.blocks.firstOrNull {
                stop.loadId in it.loadIds && it.isDrop != stop.atPickup && !it.rescued
            } ?: continue
            if (block.arrivedAt == null) startEvent(today.trip, block, TripEventKind.ARRIVED)
            return
        }
    }

    /** A refresh the crew asked for; taps on refresh while one is running are ignored. */
    fun loadDashboard() {
        if (reloading.isLocked) return
        viewModelScope.launch {
            _refreshing.value = true
            try {
                reload()
            } finally {
                _refreshing.value = false
            }
        }
    }

    /**
     * Sends what's waiting first, so the trip comes back with it on record. Once Today has shown a
     * trip, a failed load never takes it away: without signal (or when the API is busy) the crew
     * keeps the trip as last loaded and can carry on tapping.
     */
    private suspend fun reload() = reloading.withLock {
        val userId = preferencesManager.getUserId()
        val last = (loaded.value as? UiState.Success)?.data?.takeIf { it.userId == userId }
        if (last == null) loaded.value = UiState.Loading
        outbox.send()
        loaded.value = try {
            UiState.Success(fetchDashboard())
        } catch (e: CancellationException) {
            throw e
        } catch (e: Exception) {
            Logger.e("Today couldn't load", e)
            when {
                last == null -> UiState.Error(e.message ?: "Unknown error")
                e is ApiException -> UiState.Success(last)
                else -> UiState.Success(last.copy(offline = true))
            }
        }
    }

    private suspend fun fetchDashboard(): Loaded {
        val userId = preferencesManager.getUserId()
        if (userId.isNullOrEmpty()) {
            error("Please log in to view your dashboard")
        }
        // Here rather than in init: Today is the start screen, so init also runs before login.
        locationSharing.ensureStarted()
        registerPushToken(userId)

        val driver = driverApi.getDriverByUserId(userId).bodyOrThrow()
        val driverId = driver.id ?: ""

        val inspectedTripIds = preTripInspectedTripIds(userId)
        val active = tripApi.getTrips(
            driverId = driverId,
            onlyActiveTrips = true,
            orderBy = "-CreatedAt"
        ).bodyOrThrow().items.orEmpty()
            .filter { it.status in listOf(TripStatus.DISPATCHED, TripStatus.IN_TRANSIT) }
        val trips = (active + awaitingGarage(driverId))
            .map { trip -> LoadedTrip(trip, photosOf(trip), trip.id in inspectedTripIds) }

        // Crews rotate across trucks daily, so today's truck is the trip's, not a standing one.
        val truckId = trips.firstNotNullOfOrNull { it.trip.truckId } ?: driverId
        val truck = runCatching { truckApi.getTruckById(truckId).bodyOrThrow() }.getOrNull()

        preferencesManager.saveTruckId(truck?.id ?: "")
        preferencesManager.saveDriverName(driver.fullName ?: "")
        preferencesManager.saveTruckNumber(truck?.number ?: "")

        return Loaded(truck, trips, userId)
    }

    /**
     * A trip completes when its last drop is left, but the truck still has to get back. It stays on
     * Today until the garage arrival is recorded, for half a day at most.
     */
    private suspend fun awaitingGarage(driverId: String): List<TripDto> {
        val since = Clock.System.now() - 12.hours
        return runCatching {
            tripApi.getTrips(driverId = driverId, status = TripStatus.COMPLETED, orderBy = "-CompletedAt", pageSize = 3)
                .bodyOrThrow().items.orEmpty()
        }.getOrDefault(emptyList())
            .filter { it.garageArrivedAt == null && (it.completedAt ?: since) > since }
    }

    private suspend fun photosOf(trip: TripDto) =
        trip.id?.let { id -> runCatching { tripApi.getTripStopPhotos(id).bodyOrThrow() }.getOrNull() }
            .orEmpty()

    private suspend fun preTripInspectedTripIds(userId: String): Set<String> =
        runCatching {
            dvirApi.getDvirReports(driverId = userId, type = DvirType.PRE_TRIP, pageSize = 50)
                .bodyOrThrow().items.orEmpty()
                .filter { it.status != DvirStatus.DRAFT }
                .mapNotNull { it.tripId }
                .toSet()
        }.getOrDefault(emptySet())

    private suspend fun registerPushToken(userId: String) {
        val token = PushTokenRegistry.currentToken ?: return
        if (PushTokenRegistry.registeredFor == userId to token) return
        runCatching { driverApi.setDriverDeviceToken(userId, SetDriverDeviceTokenCommand(userId, token)) }
            .onSuccess { if (it.success) PushTokenRegistry.registeredFor = userId to token }
            .onFailure { Logger.e("Failed to register the push token", it) }
    }

    fun startEvent(trip: TripDto, block: StopBlock?, kind: TripEventKind) {
        _eventError.value = null
        _eventDraft.value = tripEventRecorder.draft(trip, block, kind)
    }

    fun dismissEvent() {
        _eventDraft.value = null
        _eventError.value = null
    }

    fun submitEvent(input: TripEventInput) {
        val draft = _eventDraft.value ?: return
        runEvent(draft.block, finishing = draft.kind == TripEventKind.GARAGE_ARRIVED) {
            tripEventRecorder.record(draft, input)
            _eventDraft.value = null
            if (draft.kind == TripEventKind.ARRIVED) ArrivalNudges.clear()
        }
    }

    /** A tap still waiting on the phone is simply dropped; one on record is taken back at the API. */
    fun undo(trip: TripDto, event: UndoableEvent) {
        val tripId = trip.id ?: return
        val block = event.stopId?.let { id -> blocksOf(trip).firstOrNull { id in it.stopIds } }
        runEvent(block) {
            val waiting = outbox.current().matching(tripId, event)
            if (waiting != null) {
                outbox.remove(waiting.id)
            } else {
                driverApi.undoDriverTripEvent(tripId, UndoDriverTripEventCommand(tripId, event.stopId, event.kind))
                    .throwIfFailed()
            }
        }
    }

    fun dismissUnsentTaps() {
        outbox.dismissUnsent()
    }

    fun clearEventError() {
        _eventError.value = null
    }

    private fun blocksOf(trip: TripDto): List<StopBlock> =
        (uiState.value as? UiState.Success)?.data?.trips?.firstOrNull { it.trip.id == trip.id }?.blocks.orEmpty()

    /** Arrivals move the loads on, so the watcher re-reads them; then Today shows what's on record. */
    private fun runEvent(block: StopBlock?, finishing: Boolean = false, call: suspend () -> Unit) {
        _eventError.value = null
        _eventSaving.value = true
        launchSafely(onError = { e ->
            Logger.e("Trip event failed", e)
            // The API's refusals (400) are written for the crew; anything else (no signal, timeouts,
            // server faults) gets the app's own "not saved" text in the crew's language.
            _eventError.value = (e as? ApiException)?.takeIf { it.statusCode == 400 }?.message.orEmpty()
            _eventSaving.value = false
        }) {
            call()
            block?.loadIds?.forEach(proximityWatcher::onLoadStatusChanged)
            reload()
            if (finishing) _justFinished.value = true
            _eventSaving.value = false
        }
    }

    fun chooseStop(tripId: String, stopId: String) {
        _chosenStops.value += tripId to stopId
    }

    /** Called after the OS location prompt, or when Today opens again, since there is no off switch. */
    fun startLocationSharing() {
        locationSharing.ensureStarted()
    }

    fun refresh() {
        loadDashboard()
    }
}
