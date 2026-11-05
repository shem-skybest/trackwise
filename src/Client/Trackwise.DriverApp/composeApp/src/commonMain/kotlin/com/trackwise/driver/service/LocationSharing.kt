package com.trackwise.driver.service

import com.trackwise.driver.permission.isLocationPermissionGranted
import com.trackwise.driver.util.Logger
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

/**
 * The truck's location is shared the whole time a crew member is logged in; there is no off switch
 * (management's call, 2026-11-03). Only a missing OS permission or logging out stops it.
 */
class LocationSharing(private val loadProximityWatcher: LoadProximityWatcher) {
    private val _isSharing = MutableStateFlow(false)
    val isSharing: StateFlow<Boolean> = _isSharing.asStateFlow()

    /**
     * Starts sharing if the OS allows it. Starting the foreground service without the permission
     * crashes the app on Android 14+, so this checks first and reports false instead.
     */
    fun ensureStarted(): Boolean {
        if (!isLocationPermissionGranted()) {
            _isSharing.value = false
            return false
        }
        if (!LocationTracker.isRunning()) {
            LocationTracker.start()
            loadProximityWatcher.start()
            Logger.d("LocationSharing: started")
        }
        _isSharing.value = true
        return true
    }

    fun stop() {
        loadProximityWatcher.stop()
        LocationTracker.stop()
        _isSharing.value = false
        Logger.d("LocationSharing: stopped")
    }
}
