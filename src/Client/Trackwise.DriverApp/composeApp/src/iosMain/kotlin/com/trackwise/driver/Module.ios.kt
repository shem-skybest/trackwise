package com.trackwise.driver

import com.trackwise.driver.config.AppConfig
import com.trackwise.driver.config.messagingHubUrl
import com.trackwise.driver.config.signalRHubUrl
import com.trackwise.driver.service.IosLocaleManager
import com.trackwise.driver.service.LocaleManager
import com.trackwise.driver.service.LocationService
import com.trackwise.driver.service.IosNetworkMonitor
import com.trackwise.driver.service.NetworkMonitor
import com.trackwise.driver.service.auth.AuthService
import com.trackwise.driver.service.createIosDataStore
import com.trackwise.driver.service.messaging.MessagingService
import com.trackwise.driver.service.realtime.SignalRService
import org.koin.core.context.startKoin
import org.koin.core.module.dsl.singleOf
import org.koin.dsl.module

private var koinInitialized = false

fun initKoin() {
    if (koinInitialized) {
        return
    }

    startKoin {
        modules(
            iosModule,
            commonModule()
        )
    }
    koinInitialized = true
}

/**
 * Koin module for iOS-specific dependencies
 */
val iosModule = module {
    single { createIosDataStore() }
    single { AuthService(AppConfig.identityServerUrl, get()) }
    single<SignalRService> { SignalRService(AppConfig.signalRHubUrl, get()) }
    single { MessagingService(AppConfig.messagingHubUrl, get()) }
    singleOf(::LocationService)
    single<NetworkMonitor> { IosNetworkMonitor() }
    single<LocaleManager> { IosLocaleManager() }
}
