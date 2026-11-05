package com.trackwise.driver.viewmodel

import com.trackwise.driver.api.TripApi
import com.trackwise.driver.api.bodyOrThrow
import com.trackwise.driver.api.models.TripDto
import com.trackwise.driver.service.PreferencesManager
import com.trackwise.driver.viewmodel.base.BaseViewModel
import com.trackwise.driver.viewmodel.base.UiState
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

class TripsViewModel(
    private val tripApi: TripApi,
    private val preferencesManager: PreferencesManager
) : BaseViewModel() {

    private val _uiState = MutableStateFlow<UiState<List<TripDto>>>(UiState.Loading)
    val uiState: StateFlow<UiState<List<TripDto>>> = _uiState.asStateFlow()

    init {
        loadTrips()
    }

    private fun loadTrips() {
        launchWithState(_uiState) {
            val driverId = preferencesManager.getUserId()
            val response = tripApi.getTrips(driverId = driverId, orderBy = "-CreatedAt").bodyOrThrow()
            response.items ?: emptyList()
        }
    }

    fun refresh() {
        loadTrips()
    }
}
