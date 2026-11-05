package com.trackwise.driver.ui.components

import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.trackwise.driver.service.TripEventOutbox
import org.koin.compose.koinInject
import com.trackwise.driver.ui.i18n.LocalStrings

@Composable
fun LogoutDialog(
    onConfirm: () -> Unit,
    onDismiss: () -> Unit
) {
    val s = LocalStrings.current
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(s.logoutQuestion) },
        text = {
            val waiting by koinInject<TripEventOutbox>().pending.collectAsState()
            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                Text(s.logoutBody)
                if (waiting.isNotEmpty()) {
                    Text(s.logoutWaiting(waiting.size), color = MaterialTheme.colorScheme.error, fontWeight = FontWeight.Bold)
                }
            }
        },
        confirmButton = {
            Button(onClick = onConfirm) {
                Text(s.logout)
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text(s.cancel)
            }
        }
    )
}
