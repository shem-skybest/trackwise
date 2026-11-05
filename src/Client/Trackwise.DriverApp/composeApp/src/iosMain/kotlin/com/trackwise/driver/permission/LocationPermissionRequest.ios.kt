package com.trackwise.driver.permission

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import platform.Foundation.NSURL
import platform.UIKit.UIApplication
import platform.UIKit.UIApplicationOpenSettingsURLString

@Composable
actual fun RequestLocationPermissionFlow(
    trigger: Boolean,
    onComplete: (granted: Boolean) -> Unit
) {
    // iOS shows the CLLocationManager prompt the first time location updates
    // start, not from a separate composable. The disclosure screen still
    // gates progression so the prompt is preceded by an explanation.
    LaunchedEffect(trigger) {
        if (trigger) {
            onComplete(true)
        }
    }
}

actual fun isLocationPermissionGranted(): Boolean = true

actual fun openAppSettings() {
    val url = NSURL.URLWithString(UIApplicationOpenSettingsURLString) ?: return
    UIApplication.sharedApplication.openURL(url, emptyMap<Any?, Any>(), null)
}
