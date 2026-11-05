package com.trackwise.driver.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Group
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.Schedule
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.trackwise.driver.ui.components.settings.LanguagePicker
import com.trackwise.driver.ui.i18n.LocalStrings
import com.trackwise.driver.viewmodel.SettingsViewModel
import com.trackwise.driver.permission.RequestLocationPermissionFlow
import com.trackwise.driver.service.PreferencesManager
import com.trackwise.driver.ui.components.AppTopBar
import com.trackwise.driver.ui.components.CardContainer
import com.trackwise.driver.ui.components.capture.SubmitButton
import kotlinx.coroutines.launch
import org.koin.compose.koinInject
import org.koin.compose.viewmodel.koinViewModel

/**
 * Prominent disclosure screen shown once per logged-in account before any
 * location permission prompt. Required by Google Play policy for apps that
 * collect location in a foreground service. After the user accepts, the
 * runtime location permission is requested and the [HAS_ACCEPTED_LOCATION_DISCLOSURE]
 * flag is persisted so this screen does not show again.
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun LocationDisclosureScreen(
    onAccepted: () -> Unit,
    onDeclined: () -> Unit,
    settingsViewModel: SettingsViewModel = koinViewModel()
) {
    val s = LocalStrings.current
    val settings by settingsViewModel.settings.collectAsState()
    val preferencesManager: PreferencesManager = koinInject()
    val coroutineScope = rememberCoroutineScope()
    var triggerPermission by remember { mutableStateOf(false) }

    RequestLocationPermissionFlow(
        trigger = triggerPermission,
        onComplete = {
            triggerPermission = false
            coroutineScope.launch {
                preferencesManager.saveHasAcceptedLocationDisclosure(true)
                onAccepted()
            }
        }
    )

    Scaffold(
        topBar = { AppTopBar(title = s.disclosureTitle) },
        bottomBar = {
            Surface(
                tonalElevation = 3.dp,
                shadowElevation = 8.dp,
                color = MaterialTheme.colorScheme.surface
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp, vertical = 12.dp),
                    verticalArrangement = Arrangement.spacedBy(4.dp)
                ) {
                    SubmitButton(
                        text = s.disclosureContinue,
                        onClick = { triggerPermission = true },
                        isLoading = false,
                        enabled = true
                    )
                    TextButton(
                        onClick = onDeclined,
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(44.dp)
                    ) {
                        Text(s.cancel)
                    }
                }
            }
        }
    ) { paddingValues ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
                .verticalScroll(rememberScrollState())
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            LanguagePicker(selected = settings.language, onSelect = settingsViewModel::updateLanguage)

            HeroHeader()

            Spacer(modifier = Modifier.height(4.dp))

            InfoCard(
                icon = Icons.Filled.LocationOn,
                title = s.disclosureWhatTitle,
                body = s.disclosureWhatBody
            )

            InfoCard(
                icon = Icons.Filled.Schedule,
                title = s.disclosureWhenTitle,
                body = s.disclosureWhenBody
            )

            InfoCard(
                icon = Icons.Filled.Group,
                title = s.disclosureWhoTitle,
                body = s.disclosureWhoBody
            )

            InfoCard(
                icon = Icons.Filled.Notifications,
                title = s.disclosureHowTitle,
                body = s.disclosureHowBody
            )
        }
    }
}

@Composable
private fun HeroHeader() {
    Column(
        modifier = Modifier.fillMaxWidth(),
        verticalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        Text(
            text = LocalStrings.current.disclosureHeadline,
            style = MaterialTheme.typography.headlineSmall,
            fontWeight = FontWeight.Bold,
            modifier = Modifier.fillMaxWidth()
        )
        Text(
            text = LocalStrings.current.disclosureIntro,
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            modifier = Modifier.fillMaxWidth()
        )
    }
}

@Composable
private fun InfoCard(
    icon: ImageVector,
    title: String,
    body: String
) {
    CardContainer {
        Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.Top
        ) {
            Box(
                modifier = Modifier
                    .size(40.dp)
                    .clip(CircleShape)
                    .background(MaterialTheme.colorScheme.primaryContainer),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = icon,
                    contentDescription = null,
                    tint = MaterialTheme.colorScheme.onPrimaryContainer,
                    modifier = Modifier.size(22.dp)
                )
            }
            Spacer(modifier = Modifier.width(12.dp))
            Column(modifier = Modifier.fillMaxWidth()) {
                Text(
                    text = title,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold
                )
                Spacer(modifier = Modifier.height(4.dp))
                Text(
                    text = body,
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurface
                )
            }
        }
    }
}
