package com.trackwise.driver.model

import androidx.compose.runtime.compositionLocalOf

/**
 * CompositionLocal for accessing user settings throughout the app.
 */
val LocalUserSettings = compositionLocalOf { UserSettings() }

/**
 * Distance unit for displaying distances in the app.
 */
enum class DistanceUnit(val code: String, val displayName: String, val abbreviation: String) {
    MILES("mi", "Miles", "mi"),
    KILOMETERS("km", "Kilometers", "km");

    companion object {
        fun fromCode(code: String): DistanceUnit {
            return entries.find { it.code == code } ?: KILOMETERS
        }
    }
}

/**
 * Supported languages for the app.
 */
enum class Language(val code: String, val displayName: String) {
    TAGLISH("tl", "Taglish"),
    ENGLISH("en", "English"),
    BISAYA("ceb", "Bisaya");

    companion object {
        /** Crews read Taglish unless they pick otherwise. */
        val DEFAULT = TAGLISH

        fun fromCode(code: String?): Language = entries.find { it.code == code } ?: DEFAULT
    }
}

/**
 * User settings data class.
 */
data class UserSettings(
    val distanceUnit: DistanceUnit = DistanceUnit.KILOMETERS,
    val language: Language = Language.DEFAULT
)
