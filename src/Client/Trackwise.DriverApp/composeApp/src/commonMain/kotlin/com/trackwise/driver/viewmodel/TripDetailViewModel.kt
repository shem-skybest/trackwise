package com.trackwise.driver.viewmodel

import com.trackwise.driver.api.TripApi
import com.trackwise.driver.api.bodyOrThrow
import com.trackwise.driver.api.models.TripDto
import com.trackwise.driver.api.models.TripStopDto
import com.trackwise.driver.model.toDisplayString
import com.trackwise.driver.util.buildDirectionsUrl
import com.trackwise.driver.util.buildLocationUrl
import com.trackwise.driver.viewmodel.base.BaseViewModel
import com.trackwise.driver.viewmodel.base.UiState
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

class TripDetailViewModel(
    private val tripApi: TripApi,
    private val tripId: String
) : BaseViewModel() {

    private val _uiState = MutableStateFlow<UiState<TripDto>>(UiState.Loading)
    val uiState: StateFlow<UiState<TripDto>> = _uiState.asStateFlow()

    init {
        loadTripDetails()
    }

    private fun loadTripDetails() {
        launchWithState(_uiState) {
            tripApi.getTripById(tripId).bodyOrThrow()
        }
    }

    fun refresh() {
        loadTripDetails()
    }

    fun getMapsUrl(trip: TripDto): String {
        val origin = trip.originAddress
        val destination = trip.destinationAddress
        val stops = trip.stops?.sortedBy { it.order } ?: emptyList()

        val originStr = origin.toDisplayString().encodeUrl()
        val destinationStr = destination.toDisplayString().encodeUrl()

        val waypoints = if (stops.size > 2) {
            stops.drop(1).dropLast(1).joinToString("%7C") { stop ->
                stop.address.toDisplayString().encodeUrl()
            }
        } else null

        return buildDirectionsUrl(originStr, destinationStr, waypoints)
    }

    fun getStopMapsUrl(stop: TripStopDto): String {
        val location = stop.location
        return buildLocationUrl(location.latitude ?: 0.0, location.longitude ?: 0.0)
    }

    private fun String.encodeUrl(): String {
        return this.replace(" ", "%20")
            .replace(",", "%2C")
            .replace("|", "%7C")
    }
}
