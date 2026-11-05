package com.trackwise.driver.viewmodel

import com.trackwise.driver.api.ApiException
import com.trackwise.driver.api.DriverApi
import com.trackwise.driver.api.LoadApi
import com.trackwise.driver.api.TripApi
import com.trackwise.driver.api.bodyOrThrow
import com.trackwise.driver.api.models.ConfirmLoadStatusCommand
import com.trackwise.driver.api.models.LoadDto
import com.trackwise.driver.api.models.LoadStatus
import com.trackwise.driver.api.models.TripEventKind
import com.trackwise.driver.model.TripEventDraft
import com.trackwise.driver.model.TripEventInput
import com.trackwise.driver.model.toStopBlocks
import com.trackwise.driver.service.LoadProximityWatcher
import com.trackwise.driver.service.PreferencesManager
import com.trackwise.driver.service.TripEventRecorder
import com.trackwise.driver.util.Logger
import com.trackwise.driver.viewmodel.base.BaseViewModel
import com.trackwise.driver.viewmodel.base.UiState
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

class LoadDetailViewModel(
    private val loadApi: LoadApi,
    private val driverApi: DriverApi,
    private val tripApi: TripApi,
    private val preferencesManager: PreferencesManager,
    private val proximityWatcher: LoadProximityWatcher,
    private val tripEventRecorder: TripEventRecorder,
    private val loadId: String
) : BaseViewModel() {

    private val _uiState = MutableStateFlow<UiState<LoadDto>>(UiState.Loading)
    val uiState: StateFlow<UiState<LoadDto>> = _uiState.asStateFlow()

    private val _actionError = MutableStateFlow<String?>(null)
    val actionError: StateFlow<String?> = _actionError.asStateFlow()

    private val _eventDraft = MutableStateFlow<TripEventDraft?>(null)
    val eventDraft: StateFlow<TripEventDraft?> = _eventDraft.asStateFlow()

    private val _eventSaving = MutableStateFlow(false)
    val eventSaving: StateFlow<Boolean> = _eventSaving.asStateFlow()

    private val _eventError = MutableStateFlow<String?>(null)
    val eventError: StateFlow<String?> = _eventError.asStateFlow()

    init {
        loadDetails()
    }

    private fun loadDetails() {
        launchWithState(_uiState) {
            loadApi.getLoadById(loadId).bodyOrThrow()
        }
    }

    fun confirmPickup() = confirm(LoadStatus.PICKED_UP)

    fun confirmDelivery() = confirm(LoadStatus.DELIVERED)

    /**
     * On a trip, confirming is the stop's arrival, so it asks what Today's Arrived asks: the time
     * (GPS's when it saw the truck arrive) and the reefer temperature.
     */
    private fun confirm(status: LoadStatus) {
        _actionError.value = null
        val tripId = (uiState.value as? UiState.Success)?.data?.tripId
        launchSafely(onError = { e ->
            Logger.e("Failed to confirm $status for load $loadId: ${e.message}", e)
            _actionError.value = e.message ?: "Couldn't confirm. Try again."
        }) {
            if (tripId != null) {
                startArrival(tripId, isDrop = status == LoadStatus.DELIVERED)
                return@launchSafely
            }

            val driverId = preferencesManager.getUserId()
                ?: throw IllegalStateException("Please log in again.")
            val response = driverApi.confirmLoadStatus(
                ConfirmLoadStatusCommand(driverId = driverId, loadId = loadId, loadStatus = status)
            )
            if (!response.success) throw ApiException(response.status)
            proximityWatcher.onLoadStatusChanged(loadId)
            loadDetails()
        }
    }

    private suspend fun startArrival(tripId: String, isDrop: Boolean) {
        val trip = tripApi.getTripById(tripId).bodyOrThrow()
        val block = trip.toStopBlocks().firstOrNull { loadId in it.loadIds && it.isDrop == isDrop }
            ?: error("This stop isn't on the trip any more. Refresh and try again.")
        _eventError.value = null
        _eventDraft.value = tripEventRecorder.draft(trip, block, TripEventKind.ARRIVED)
    }

    fun submitEvent(input: TripEventInput) {
        val draft = _eventDraft.value ?: return
        _eventError.value = null
        _eventSaving.value = true
        launchSafely(onError = { e ->
            Logger.e("Trip event failed", e)
            _eventError.value = e.message ?: "That wasn't saved. Try again."
            _eventSaving.value = false
        }) {
            tripEventRecorder.record(draft, input)
            draft.block?.loadIds?.forEach(proximityWatcher::onLoadStatusChanged)
            _eventDraft.value = null
            _eventSaving.value = false
            loadDetails()
        }
    }

    fun dismissEvent() {
        _eventDraft.value = null
        _eventError.value = null
    }

    fun refresh() {
        loadDetails()
    }
}
