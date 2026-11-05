package com.trackwise.driver.ui.components.dvir

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AcUnit
import androidx.compose.material.icons.filled.BatteryAlert
import androidx.compose.material.icons.filled.Campaign
import androidx.compose.material.icons.filled.CarRepair
import androidx.compose.material.icons.filled.Flip
import androidx.compose.material.icons.filled.Lightbulb
import androidx.compose.material.icons.filled.MoreHoriz
import androidx.compose.material.icons.filled.OilBarrel
import androidx.compose.material.icons.filled.PanTool
import androidx.compose.material.icons.filled.TireRepair
import androidx.compose.material.icons.filled.TripOrigin
import androidx.compose.material.icons.filled.WbIncandescent
import androidx.compose.material.icons.filled.Window
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.unit.dp
import com.trackwise.driver.model.TruckPart
import com.trackwise.driver.ui.components.today.ChoiceTile
import com.trackwise.driver.ui.i18n.LocalStrings
import com.trackwise.driver.ui.theme.StepProblem

private const val COLUMNS = 3

/** Every part as a big tile; tap the broken ones (they light up), tap again to take one off. */
@Composable
fun TruckPartGrid(
    selected: Set<TruckPart>,
    onToggle: (TruckPart) -> Unit,
    modifier: Modifier = Modifier
) {
    val s = LocalStrings.current
    Column(modifier = modifier, verticalArrangement = Arrangement.spacedBy(10.dp)) {
        TruckPart.entries.chunked(COLUMNS).forEach { row ->
            Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                row.forEach { part ->
                    ChoiceTile(
                        text = s.part(part),
                        icon = part.icon,
                        color = StepProblem,
                        selected = part in selected,
                        onClick = { onToggle(part) },
                        modifier = Modifier.weight(1f)
                    )
                }
                repeat(COLUMNS - row.size) { Spacer(Modifier.weight(1f)) }
            }
        }
    }
}

val TruckPart.icon: ImageVector
    get() = when (this) {
        TruckPart.TIRES -> Icons.Default.TireRepair
        TruckPart.BRAKES -> Icons.Default.PanTool
        TruckPart.HEADLIGHTS -> Icons.Default.Lightbulb
        TruckPart.REAR_LIGHTS -> Icons.Default.WbIncandescent
        TruckPart.ENGINE -> Icons.Default.CarRepair
        TruckPart.OIL_WATER -> Icons.Default.OilBarrel
        TruckPart.BATTERY -> Icons.Default.BatteryAlert
        TruckPart.STEERING -> Icons.Default.TripOrigin
        TruckPart.MIRRORS -> Icons.Default.Flip
        TruckPart.WINDSHIELD -> Icons.Default.Window
        TruckPart.HORN -> Icons.Default.Campaign
        TruckPart.REEFER -> Icons.Default.AcUnit
        TruckPart.OTHER -> Icons.Default.MoreHoriz
    }
