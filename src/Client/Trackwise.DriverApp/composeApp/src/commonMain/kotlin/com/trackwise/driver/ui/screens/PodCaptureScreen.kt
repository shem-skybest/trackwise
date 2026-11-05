package com.trackwise.driver.ui.screens

import com.trackwise.driver.ui.i18n.LocalStrings
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.trackwise.driver.ui.components.capture.CaptureScreenScaffold
import com.trackwise.driver.ui.components.capture.LocationDisplay
import com.trackwise.driver.ui.components.capture.NotesTextField
import com.trackwise.driver.ui.components.capture.PhotoCaptureSection
import com.trackwise.driver.ui.components.capture.SignatureCaptureField
import com.trackwise.driver.ui.components.capture.SubmitButton
import com.trackwise.driver.ui.components.capture.rememberCameraCapture
import com.trackwise.driver.ui.components.capture.rememberGalleryPick
import com.trackwise.driver.viewmodel.DocumentCaptureType
import com.trackwise.driver.viewmodel.PodCaptureViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PodCaptureScreen(
    onNavigateBack: () -> Unit,
    viewModel: PodCaptureViewModel
) {
    val s = LocalStrings.current
    val onCapturePhoto = rememberCameraCapture(onPhotoCaptured = viewModel::addPhoto)
    val onPickPhoto = rememberGalleryPick(onPhotoCaptured = viewModel::addPhoto)
    val uiState by viewModel.uiState.collectAsState()

    val title = when (uiState.captureType) {
        DocumentCaptureType.POD -> s.photoDelivery
        DocumentCaptureType.BOL -> s.photoLoading
    }

    CaptureScreenScaffold(
        title = title,
        error = uiState.error,
        isSuccess = uiState.isSuccess,
        onClearError = viewModel::clearError,
        onNavigateBack = onNavigateBack
    ) {
        item {
            PhotoCaptureSection(
                photos = uiState.photos,
                onAddPhoto = onCapturePhoto,
                onPickPhoto = onPickPhoto,
                onRemovePhoto = viewModel::removePhoto,
                emptyMessage = s.noPhotosYet
            )
        }

        item {
            SignatureCaptureField(
                onCaptured = viewModel::setSignature,
                onClear = viewModel::clearSignature
            )
            if (uiState.signaturePaths != null) {
                Text(
                    text = s.signatureCaptured,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.primary,
                    modifier = Modifier.padding(top = 4.dp)
                )
            }
        }

        item {
            OutlinedTextField(
                value = uiState.recipientName,
                onValueChange = viewModel::setRecipientName,
                label = { Text(s.recipient) },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true
            )
        }

        item {
            NotesTextField(
                value = uiState.notes,
                onValueChange = viewModel::setNotes
            )
        }

        if (uiState.latitude != null && uiState.longitude != null) {
            item {
                LocationDisplay(
                    latitude = uiState.latitude!!,
                    longitude = uiState.longitude!!
                )
            }
        }

        item {
            SubmitButton(
                text = s.submitPhotos,
                onClick = viewModel::submit,
                isLoading = uiState.isSubmitting,
                enabled = viewModel.canSubmit()
            )
        }
    }
}
