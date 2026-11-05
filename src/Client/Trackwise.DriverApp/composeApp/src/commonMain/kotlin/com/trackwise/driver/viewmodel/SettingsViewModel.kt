package com.trackwise.driver.viewmodel

import com.trackwise.driver.model.DistanceUnit
import com.trackwise.driver.model.Language
import com.trackwise.driver.model.UserSettings
import com.trackwise.driver.service.LocaleManager
import com.trackwise.driver.service.PreferencesManager
import com.trackwise.driver.viewmodel.base.ActionState
import com.trackwise.driver.viewmodel.base.BaseViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

class SettingsViewModel(
    private val preferencesManager: PreferencesManager,
    private val localeManager: LocaleManager
) : BaseViewModel() {

    private val _settings = MutableStateFlow(UserSettings())
    val settings: StateFlow<UserSettings> = _settings.asStateFlow()

    private val _saveState = MutableStateFlow<ActionState<Unit>>(ActionState.Idle)
    val saveState: StateFlow<ActionState<Unit>> = _saveState.asStateFlow()

    init {
        loadSettings()
    }

    private fun loadSettings() {
        launchSafely {
            preferencesManager.getUserSettingsFlow().collect { userSettings ->
                _settings.value = userSettings
            }
        }
    }

    fun updateDistanceUnit(unit: DistanceUnit) {
        launchSafely(onError = { e ->
            _saveState.value = ActionState.Error(e.message ?: "Failed to save setting")
        }) {
            preferencesManager.saveDistanceUnit(unit)
            _settings.value = _settings.value.copy(distanceUnit = unit)
            _saveState.value = ActionState.Success(Unit)
        }
    }

    fun updateLanguage(language: Language) {
        launchSafely(onError = { e ->
            _saveState.value = ActionState.Error(e.message ?: "Failed to save setting")
        }) {
            preferencesManager.saveLanguage(language)
            _settings.value = _settings.value.copy(language = language)
            localeManager.apply(language.code)
            _saveState.value = ActionState.Success(Unit)
        }
    }

    fun resetSaveState() {
        _saveState.value = ActionState.Idle
    }
}
