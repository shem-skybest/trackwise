package com.trackwise.driver.service.realtime

import com.trackwise.driver.service.PreferencesManager
import com.trackwise.driver.util.Logger
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.microsoft.signalr.HubConnectionState
import io.reactivex.rxjava3.core.Single
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.runBlocking
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.coroutines.withContext

actual class SignalRService(
    private val hubUrl: String,
    private val preferencesManager: PreferencesManager
) {
    private var hubConnection: HubConnection? = null
    private val connectLock = Mutex()
    private val _connectionState = MutableStateFlow(SignalRConnectionState.DISCONNECTED)

    actual val connectionState: StateFlow<SignalRConnectionState> = _connectionState.asStateFlow()

    actual suspend fun connect(): Unit = connectLock.withLock {
        if (hubConnection?.connectionState == HubConnectionState.CONNECTED) {
            Logger.d("SignalR already connected")
            return@withLock
        }

        _connectionState.value = SignalRConnectionState.CONNECTING

        withContext(Dispatchers.IO) {
            try {
                val tenantId = preferencesManager.getTenantId()

                // Read per (re)connect: a token captured once goes stale after a refresh.
                hubConnection = HubConnectionBuilder.create(hubUrl)
                    .withAccessTokenProvider(Single.fromCallable {
                        runBlocking { preferencesManager.getAccessToken() } ?: ""
                    })
                    .withHeader("X-Tenant", tenantId ?: "")
                    .build()

                setupConnectionCallbacks()

                hubConnection?.start()?.blockingAwait()
                _connectionState.value = SignalRConnectionState.CONNECTED
                Logger.d("SignalR connected successfully")
            } catch (e: Exception) {
                _connectionState.value = SignalRConnectionState.DISCONNECTED
                Logger.e("Failed to connect SignalR", e)
                throw e
            }
        }
    }

    private fun setupConnectionCallbacks() {
        hubConnection?.onClosed { exception ->
            _connectionState.value = SignalRConnectionState.DISCONNECTED
            if (exception != null) {
                Logger.e("SignalR connection closed with error", exception)
            } else {
                Logger.d("SignalR connection closed")
            }
        }
    }

    actual suspend fun disconnect() {
        withContext(Dispatchers.IO) {
            try {
                hubConnection?.stop()?.blockingAwait()
                hubConnection = null
                _connectionState.value = SignalRConnectionState.DISCONNECTED
                Logger.d("SignalR disconnected")
            } catch (e: Exception) {
                Logger.e("Error disconnecting SignalR", e)
            }
        }
    }

    actual suspend fun sendLocationUpdate(location: TruckGeolocation) {
        // The Java client never reconnects by itself, so a dropped signal or a server restart
        // would otherwise end live tracking until the app is restarted.
        if (hubConnection?.connectionState != HubConnectionState.CONNECTED) {
            try {
                connect()
            } catch (e: Exception) {
                Logger.w("Cannot send location update: SignalR reconnect failed")
                return
            }
        }

        withContext(Dispatchers.IO) {
            try {
                hubConnection?.send(
                    "SendGeolocationData",
                    location
                )
                Logger.d("Location sent: ${location.currentLocation.latitude}, ${location.currentLocation.longitude}")
            } catch (e: Exception) {
                Logger.e("Failed to send location update", e)
            }
        }
    }

    actual fun isConnected(): Boolean {
        return hubConnection?.connectionState == HubConnectionState.CONNECTED
    }
}
