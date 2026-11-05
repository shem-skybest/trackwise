package com.trackwise.driver.ui.components.dvir

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Block
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.trackwise.driver.model.TruckPart
import com.trackwise.driver.ui.components.CardContainer
import com.trackwise.driver.ui.components.capture.PhotoCaptureSection
import com.trackwise.driver.ui.components.today.ChoiceTile
import com.trackwise.driver.ui.i18n.LocalStrings
import com.trackwise.driver.ui.theme.StepDone
import com.trackwise.driver.ui.theme.StepProblem
import com.trackwise.driver.viewmodel.DvirDefect

/** One broken part: can the truck still go out, and a photo of it so nobody has to describe it. */
@Composable
fun BrokenPartCard(
    defect: DvirDefect,
    onCanStillDrive: (Boolean) -> Unit,
    onTakePhoto: () -> Unit,
    onPickPhoto: () -> Unit,
    onRemovePhoto: (String) -> Unit,
    onNoteChange: (String) -> Unit,
    modifier: Modifier = Modifier
) {
    val s = LocalStrings.current
    CardContainer(modifier = modifier) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(defect.part.icon, contentDescription = null, tint = StepProblem, modifier = Modifier.size(28.dp))
                Spacer(Modifier.width(10.dp))
                Text(s.part(defect.part), style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.Bold)
            }

            Text(s.canStillDrive, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
            Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                ChoiceTile(
                    text = s.driveOk,
                    icon = Icons.Default.CheckCircle,
                    color = StepDone,
                    selected = defect.canStillDrive == true,
                    onClick = { onCanStillDrive(true) },
                    modifier = Modifier.weight(1f)
                )
                ChoiceTile(
                    text = s.driveNo,
                    icon = Icons.Default.Block,
                    color = StepProblem,
                    selected = defect.canStillDrive == false,
                    onClick = { onCanStillDrive(false) },
                    modifier = Modifier.weight(1f)
                )
            }

            PhotoCaptureSection(
                photos = defect.photos,
                onAddPhoto = onTakePhoto,
                onPickPhoto = onPickPhoto,
                onRemovePhoto = onRemovePhoto,
                title = s.photoOfDefect
            )

            if (defect.part == TruckPart.OTHER) {
                OutlinedTextField(
                    value = defect.note,
                    onValueChange = onNoteChange,
                    label = { Text(s.whatIsBroken) },
                    modifier = Modifier.fillMaxWidth()
                )
            }
        }
    }
}
