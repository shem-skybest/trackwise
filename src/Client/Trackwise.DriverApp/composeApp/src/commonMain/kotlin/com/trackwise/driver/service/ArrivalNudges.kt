package com.trackwise.driver.service

import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow

/** The truck just came within range of a stop it hasn't arrived at; [place] is blank for a warehouse. */
data class NearbyStop(val loadId: String, val atPickup: Boolean, val place: String)

sealed interface ArrivalNudge {
    data class Show(val stop: NearbyStop) : ArrivalNudge
    data object Clear : ArrivalNudge
}

/**
 * "Nandito ka na ba?": [LoadProximityWatcher] posts here when the truck reaches a stop, the phone
 * buzzes, and the crew's "Oo" comes back as [requested] for Today to open the arrival sheet.
 */
object ArrivalNudges {
    private val _nudges = MutableSharedFlow<ArrivalNudge>(
        extraBufferCapacity = 8,
        onBufferOverflow = BufferOverflow.DROP_OLDEST
    )
    val nudges: SharedFlow<ArrivalNudge> = _nudges.asSharedFlow()

    private val _requested = MutableStateFlow<NearbyStop?>(null)
    val requested: StateFlow<NearbyStop?> = _requested.asStateFlow()

    fun show(stop: NearbyStop) {
        _nudges.tryEmit(ArrivalNudge.Show(stop))
    }

    /** The arrival is on record (or no longer wanted), so the buzz shouldn't linger in the shade. */
    fun clear() {
        _nudges.tryEmit(ArrivalNudge.Clear)
    }

    fun request(stop: NearbyStop) {
        _requested.value = stop
    }

    fun consumeRequest(): NearbyStop? = _requested.value.also { _requested.value = null }
}
