package com.trackwise.driver.ui.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Warning
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import com.trackwise.driver.api.models.DvirType
import com.trackwise.driver.ui.components.SectionCard
import com.trackwise.driver.ui.components.capture.CaptureScreenScaffold
import com.trackwise.driver.ui.components.capture.NotesTextField
import com.trackwise.driver.ui.components.capture.SignatureCaptureField
import com.trackwise.driver.ui.components.capture.SubmitButton
import com.trackwise.driver.ui.components.capture.rememberCameraCapture
import com.trackwise.driver.ui.components.capture.rememberGalleryPick
import com.trackwise.driver.ui.components.dvir.BrokenPartCard
import com.trackwise.driver.ui.components.dvir.DvirInspectionTypeSelector
import com.trackwise.driver.ui.components.dvir.DvirTruckSelector
import com.trackwise.driver.ui.components.dvir.TruckPartGrid
import com.trackwise.driver.ui.components.today.ChoiceTile
import com.trackwise.driver.ui.i18n.LocalStrings
import com.trackwise.driver.ui.theme.StepDone
import com.trackwise.driver.ui.theme.StepProblem
import com.trackwise.driver.viewmodel.DvirFormViewModel

/**
 * The truck check, asked the way a crew answers it: "May sira ba?" first, then a tap on each
 * broken part and a photo of it, instead of a category list and a typed description.
 */
@Composable
fun DvirFormScreen(
    onNavigateBack: () -> Unit,
    viewModel: DvirFormViewModel
) {
    val s = LocalStrings.current
    val onCapturePhoto = rememberCameraCapture(onPhotoCaptured = viewModel::addPhoto)
    val onPickPhoto = rememberGalleryPick(onPhotoCaptured = viewModel::addPhoto)
    val uiState by viewModel.uiState.collectAsState()

    val title = when (uiState.dvirType) {
        DvirType.PRE_TRIP -> s.preTripTitle
        DvirType.POST_TRIP -> s.postTripTitle
    }

    CaptureScreenScaffold(
        title = title,
        error = uiState.error?.let { s.notSaved },
        isSuccess = uiState.isSuccess,
        onClearError = viewModel::clearError,
        onNavigateBack = onNavigateBack
    ) {
        if (!uiState.isTypeFixed) {
            item {
                DvirInspectionTypeSelector(
                    selectedType = uiState.dvirType,
                    onTypeSelected = viewModel::setDvirType
                )
            }
        }

        item {
            DvirTruckSelector(
                selectedTruck = uiState.selectedTruck,
                availableTrucks = uiState.availableTrucks,
                isLoading = uiState.isLoadingTrucks,
                onTruckSelected = viewModel::selectTruck,
                enabled = uiState.truckId == null
            )
        }

        item {
            Text(s.defectsTitle, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
            Spacer(Modifier.height(12.dp))
            Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                ChoiceTile(
                    text = s.nothingBroken,
                    icon = Icons.Default.CheckCircle,
                    color = StepDone,
                    selected = uiState.hasDefects == false,
                    onClick = { viewModel.setHasDefects(false) },
                    modifier = Modifier.weight(1f)
                )
                ChoiceTile(
                    text = s.somethingBroken,
                    icon = Icons.Default.Warning,
                    color = StepProblem,
                    selected = uiState.hasDefects == true,
                    onClick = { viewModel.setHasDefects(true) },
                    modifier = Modifier.weight(1f)
                )
            }
        }

        if (uiState.hasDefects == true) {
            item {
                Text(s.defectPart, style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.Bold)
                Text(
                    s.tapEveryBrokenPart,
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Spacer(Modifier.height(12.dp))
                TruckPartGrid(
                    selected = uiState.defects.map { it.part }.toSet(),
                    onToggle = viewModel::togglePart
                )
            }

            items(uiState.defects.size, key = { uiState.defects[it].part.name }) { index ->
                val defect = uiState.defects[index]
                BrokenPartCard(
                    defect = defect,
                    onCanStillDrive = { viewModel.setCanStillDrive(defect.part, it) },
                    onTakePhoto = {
                        viewModel.takePhotoFor(defect.part)
                        onCapturePhoto()
                    },
                    onPickPhoto = {
                        viewModel.takePhotoFor(defect.part)
                        onPickPhoto()
                    },
                    onRemovePhoto = { viewModel.removePhoto(defect.part, it) },
                    onNoteChange = { viewModel.setDefectNote(defect.part, it) }
                )
            }
        }

        item {
            SignatureCaptureField(
                onCaptured = viewModel::setSignature,
                onClear = viewModel::clearSignature
            )
        }

        item {
            SectionCard(title = s.moreOptional) {
                OutlinedTextField(
                    value = uiState.odometerReading,
                    onValueChange = viewModel::setOdometerReading,
                    label = { Text(s.odometerLabel) },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                    modifier = Modifier.fillMaxWidth(),
                    suffix = { Text("km") }
                )
                Spacer(Modifier.height(12.dp))
                NotesTextField(
                    value = uiState.notes,
                    onValueChange = viewModel::setNotes
                )
            }
        }

        item {
            SubmitButton(
                text = s.submitCheck,
                onClick = viewModel::submit,
                isLoading = uiState.isSubmitting,
                enabled = viewModel.canSubmit()
            )
            Spacer(modifier = Modifier.height(16.dp))
        }
    }
}
