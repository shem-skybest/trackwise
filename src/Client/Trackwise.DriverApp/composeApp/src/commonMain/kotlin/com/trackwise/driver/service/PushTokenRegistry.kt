package com.trackwise.driver.service

/**
 * The phone's push token, handed over by the platform's push service (FCM on Android). Firebase
 * issues it once per install, so whoever logs in next must register it again - see
 * DashboardViewModel.
 */
object PushTokenRegistry {
    var currentToken: String? = null

    /** The user and token last registered with the server, to skip re-sending the same pair. */
    var registeredFor: Pair<String, String>? = null
}
