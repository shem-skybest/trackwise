package com.trackwise.driver.permission

import androidx.compose.runtime.Composable

/**
 * Cross-platform location-permission requester used by the disclosure screen.
 *
 * On Android this fires the OS runtime prompt for `ACCESS_FINE_LOCATION` and
 * `ACCESS_COARSE_LOCATION` and invokes [onComplete] with whether either was
 * granted.
 *
 * On iOS this is a no-op that immediately invokes `onComplete(true)`; the
 * actual `CLLocationManager` authorization prompt is shown later, when
 * `LocationSharing.ensureStarted()` starts the tracker.
 *
 * @param trigger Set to true to launch the permission request once. After the
 *   request completes the caller should reset this back to false.
 */
@Composable
expect fun RequestLocationPermissionFlow(
    trigger: Boolean,
    onComplete: (granted: Boolean) -> Unit
)

/** Whether the OS currently lets the app read location. iOS asks when tracking starts, so it says yes. */
expect fun isLocationPermissionGranted(): Boolean

/**
 * Opens this app's page in the phone's settings. Once a crew member denies location twice, Android
 * stops showing the prompt, so this is the only way left to turn it back on.
 */
expect fun openAppSettings()
