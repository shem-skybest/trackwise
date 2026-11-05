package com.trackwise.driver

import android.content.Intent
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.ui.platform.LocalContext
import androidx.core.net.toUri
import com.trackwise.driver.permission.RequestStartupPermissions
import com.trackwise.driver.config.AppConfig
import com.google.firebase.messaging.FirebaseMessaging
import com.trackwise.driver.service.ArrivalNudgeNotifier
import com.trackwise.driver.service.PushTokenRegistry
import com.trackwise.driver.ui.DriverApp

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        AppConfig.initialize(
            apiBaseUrl = BuildConfig.API_BASE_URL,
            identityServerUrl = BuildConfig.IDENTITY_SERVER_URL,
            isProduction = !BuildConfig.DEBUG
        )
        initKoin(this)
        FirebaseMessaging.getInstance().token.addOnSuccessListener { PushTokenRegistry.currentToken = it }
        enableEdgeToEdge()
        ArrivalNudgeNotifier.handle(this, intent)

        setContent {
            // Request non-location startup permissions (camera, notifications).
            // Location permission is requested from the disclosure screen.
            RequestStartupPermissions()

            val context = LocalContext.current
            DriverApp(
                onOpenUrl = { url ->
                    val intent = Intent(Intent.ACTION_VIEW, url.toUri())
                    context.startActivity(intent)
                }
            )
        }
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        ArrivalNudgeNotifier.handle(this, intent)
    }
}
