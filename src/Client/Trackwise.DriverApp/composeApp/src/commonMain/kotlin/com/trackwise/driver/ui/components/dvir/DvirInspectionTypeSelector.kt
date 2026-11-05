package com.trackwise.driver.ui.components.dvir

import com.trackwise.driver.ui.i18n.dvirType
import com.trackwise.driver.ui.i18n.LocalStrings
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.trackwise.driver.api.models.DvirType
import com.trackwise.driver.ui.components.SectionCard

@OptIn(ExperimentalLayoutApi::class)
@Composable
fun DvirInspectionTypeSelector(
    selectedType: DvirType,
    onTypeSelected: (DvirType) -> Unit,
    modifier: Modifier = Modifier
) {
    SectionCard(
        title = LocalStrings.current.inspectionType,
        modifier = modifier
    ) {
        FlowRow(
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            DvirType.entries.forEach { type ->
                FilterChip(
                    selected = selectedType == type,
                    onClick = { onTypeSelected(type) },
                    label = { Text(LocalStrings.current.dvirType(type)) },
                    leadingIcon = if (selectedType == type) {
                        {
                            Icon(
                                Icons.Filled.Check,
                                contentDescription = null,
                                Modifier.size(18.dp)
                            )
                        }
                    } else null
                )
            }
        }
    }
}
