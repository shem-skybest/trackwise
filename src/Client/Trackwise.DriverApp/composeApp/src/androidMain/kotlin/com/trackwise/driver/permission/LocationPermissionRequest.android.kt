package com.trackwise.driver.permission

import android.content.Context
import android.content.Intent
import android.net.Uri
import android.provider.Settings
import androidx.compose.runtime.Composable
import org.koin.mp.KoinPlatform

@Composable
actual fun RequestLocationPermissionFlow(
    trigger: Boolean,
    onComplete: (granted: Boolean) -> Unit
) {
    if (!trigger) return
    RequestPermissions(
        permissions = AppPermission.locationPermissions,
        onAllResults = { results ->
            onComplete(results.any { it.isGranted })
        }
    )
}

actual fun openAppSettings() {
    val context = KoinPlatform.getKoin().get<Context>()
    val intent = Intent(Settings.ACTION_APPLICATION_DETAILS_SETTINGS, Uri.fromParts("package", context.packageName, null))
        .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
    context.startActivity(intent)
}

actual fun isLocationPermissionGranted(): Boolean {
    val context = KoinPlatform.getKoin().get<Context>()
    return AppPermission.locationPermissions.any { context.isPermissionGranted(it) }
}
