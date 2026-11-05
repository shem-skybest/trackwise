package com.trackwise.driver

import android.content.Context
import androidx.activity.ComponentActivity
import com.trackwise.driver.config.AppConfig
import com.trackwise.driver.config.messagingHubUrl
import com.trackwise.driver.config.signalRHubUrl
import com.trackwise.driver.service.AndroidLocaleManager
import com.trackwise.driver.service.LocaleManager
import com.trackwise.driver.service.LocationService
import com.trackwise.driver.service.AndroidNetworkMonitor
import com.trackwise.driver.service.NetworkMonitor
import com.trackwise.driver.service.auth.AuthService
import com.trackwise.driver.service.createAndroidDataStore
import com.trackwise.driver.service.messaging.MessagingService
import com.trackwise.driver.service.realtime.SignalRService
import com.trackwise.driver.util.BarcodeScannerLauncher
import com.trackwise.driver.util.CameraLauncher
import org.koin.android.ext.koin.androidContext
import org.koin.android.ext.koin.androidLogger
import org.koin.core.context.loadKoinModules
import org.koin.core.context.startKoin
import org.koin.core.module.dsl.singleOf
import org.koin.dsl.module

private var koinInitialized = false

fun initKoin(activity: ComponentActivity) {
    // The launchers register against this activity (in onCreate, before setContent). Koin outlives
    // it, so a recreated activity - opened from a push, rotated, reclaimed - must replace them, or
    // the photo and scanner buttons launch the dead activity's unregistered launchers.
    val launchers = launcherModule(CameraLauncher(activity), BarcodeScannerLauncher(activity))
    if (koinInitialized) {
        loadKoinModules(launchers)
        return
    }

    startKoin {
        androidLogger()
        androidContext(activity)
        modules(
            // Android-specific module (must be loaded first to provide PreferencesManager)
            androidModule(),
            launchers,
            commonModule()
        )
    }

    koinInitialized = true
}

private fun launcherModule(
    cameraLauncher: CameraLauncher,
    barcodeScannerLauncher: BarcodeScannerLauncher
) = module {
    single { cameraLauncher }
    single { barcodeScannerLauncher }
}

private fun androidModule() = module {
    single { createAndroidDataStore(get<Context>()) }
    single { AuthService(AppConfig.identityServerUrl, get()) }
    single { SignalRService(AppConfig.signalRHubUrl, get()) }
    single { MessagingService(AppConfig.messagingHubUrl, get()) }
    singleOf(::LocationService)
    single<NetworkMonitor> { AndroidNetworkMonitor(get()) }
    single<LocaleManager> { AndroidLocaleManager() }
}
