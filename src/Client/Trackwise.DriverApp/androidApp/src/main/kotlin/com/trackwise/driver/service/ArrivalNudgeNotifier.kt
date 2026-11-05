package com.trackwise.driver.service

import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import androidx.core.app.NotificationCompat
import com.trackwise.driver.MainActivity
import com.trackwise.driver.R
import com.trackwise.driver.ui.i18n.strings
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.launch

/** Buzzes the phone with "Nandito ka na ba?" while the location service runs. */
class ArrivalNudgeNotifier(
    private val context: Context,
    private val preferencesManager: PreferencesManager
) {
    companion object {
        private const val CHANNEL_ID = "arrival_nudge_channel"
        private const val NOTIFICATION_ID = 3001
        const val EXTRA_LOAD_ID = "arrivalLoadId"
        const val EXTRA_AT_PICKUP = "arrivalAtPickup"
        const val EXTRA_PLACE = "arrivalPlace"

        /** The crew tapped "Oo": Today opens the arrival sheet for this stop. */
        fun handle(context: Context, intent: Intent?) {
            val loadId = intent?.getStringExtra(EXTRA_LOAD_ID) ?: return
            ArrivalNudges.request(
                NearbyStop(
                    loadId = loadId,
                    atPickup = intent.getBooleanExtra(EXTRA_AT_PICKUP, false),
                    place = intent.getStringExtra(EXTRA_PLACE).orEmpty()
                )
            )
            intent.removeExtra(EXTRA_LOAD_ID)
            context.getSystemService(NotificationManager::class.java).cancel(NOTIFICATION_ID)
        }
    }

    private val manager = context.getSystemService(NotificationManager::class.java)

    fun start(scope: CoroutineScope) {
        scope.launch {
            ArrivalNudges.nudges.collect { nudge ->
                when (nudge) {
                    is ArrivalNudge.Show -> show(nudge.stop)
                    ArrivalNudge.Clear -> manager.cancel(NOTIFICATION_ID)
                }
            }
        }
    }

    private suspend fun show(stop: NearbyStop) {
        val s = preferencesManager.getUserSettingsFlow().first().language.strings()
        manager.createNotificationChannel(
            NotificationChannel(CHANNEL_ID, s.nudgeChannel, NotificationManager.IMPORTANCE_HIGH).apply {
                enableVibration(true)
                vibrationPattern = longArrayOf(0, 400, 200, 400)
            }
        )

        val openToday = PendingIntent.getActivity(
            context,
            0,
            Intent(context, MainActivity::class.java)
                .addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP),
            PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT
        )
        val openArrival = PendingIntent.getActivity(
            context,
            1,
            Intent(context, MainActivity::class.java)
                .addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP)
                .putExtra(EXTRA_LOAD_ID, stop.loadId)
                .putExtra(EXTRA_AT_PICKUP, stop.atPickup)
                .putExtra(EXTRA_PLACE, stop.place),
            PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT
        )

        val notification = NotificationCompat.Builder(context, CHANNEL_ID)
            .setSmallIcon(R.drawable.ic_notification)
            .setContentTitle(s.nudgeTitle(stop.place))
            .setContentText(s.nudgeBody)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setCategory(NotificationCompat.CATEGORY_REMINDER)
            .setAutoCancel(true)
            // A stop the truck drove past an hour ago isn't worth answering any more.
            .setTimeoutAfter(60 * 60 * 1000L)
            .setContentIntent(openToday)
            .addAction(0, s.nudgeYes, openArrival)
            .build()
        manager.notify(NOTIFICATION_ID, notification)
    }
}
