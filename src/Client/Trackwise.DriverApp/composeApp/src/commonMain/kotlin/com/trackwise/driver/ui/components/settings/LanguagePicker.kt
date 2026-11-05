package com.trackwise.driver.ui.components.settings

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.trackwise.driver.model.Language
import com.trackwise.driver.ui.components.CardContainer
import com.trackwise.driver.ui.i18n.LocalStrings

/** All three languages side by side, so a crew who can't read the current one can still find theirs. */
@Composable
fun LanguagePicker(selected: Language, onSelect: (Language) -> Unit, modifier: Modifier = Modifier) {
    CardContainer(modifier = modifier) {
        Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
            Text(LocalStrings.current.language, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                Language.entries.forEach { language ->
                    val shape = RoundedCornerShape(12.dp)
                    val buttonModifier = Modifier.weight(1f).heightIn(min = 52.dp)
                    if (language == selected) {
                        Button(
                            onClick = { onSelect(language) },
                            shape = shape,
                            colors = ButtonDefaults.buttonColors(),
                            modifier = buttonModifier
                        ) { Text(language.displayName, fontWeight = FontWeight.Bold) }
                    } else {
                        OutlinedButton(onClick = { onSelect(language) }, shape = shape, modifier = buttonModifier) {
                            Text(language.displayName, fontWeight = FontWeight.Bold)
                        }
                    }
                }
            }
        }
    }
}
