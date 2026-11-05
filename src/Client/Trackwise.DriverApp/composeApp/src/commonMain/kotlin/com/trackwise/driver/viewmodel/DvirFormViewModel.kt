package com.trackwise.driver.viewmodel

import com.trackwise.driver.api.DvirApi
import com.trackwise.driver.api.TruckApi
import com.trackwise.driver.api.models.CreateDvirDefectRequest
import com.trackwise.driver.api.models.CreateDvirReportCommand
import com.trackwise.driver.api.models.DefectSeverity
import com.trackwise.driver.api.models.DvirType
import com.trackwise.driver.api.models.SubmitDvirReportCommand
import com.trackwise.driver.api.models.TruckDto
import com.trackwise.driver.model.FileUploadData
import com.trackwise.driver.model.TruckPart
import com.trackwise.driver.model.toFormParts
import com.trackwise.driver.service.LocationService
import com.trackwise.driver.service.PreferencesManager
import com.trackwise.driver.ui.components.PathData
import com.trackwise.driver.viewmodel.base.BaseViewModel
import com.trackwise.driver.viewmodel.base.CaptureFormState
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update

/** One broken part: can the truck still go out with it, its photos, and (for "Other") what it is. */
data class DvirDefect(
    val part: TruckPart,
    val canStillDrive: Boolean? = null,
    val photos: List<CapturedPhoto> = emptyList(),
    val note: String = ""
) {
    val severity: DefectSeverity
        get() = if (canStillDrive == false) DefectSeverity.OUT_OF_SERVICE else DefectSeverity.MINOR

    val description: String
        get() = listOf(part.officeLabel, note.trim()).filter { it.isNotEmpty() }.joinToString(": ")
}

data class DvirFormUiState(
    val truckId: String? = null,
    val tripId: String? = null,
    val selectedTruck: TruckDto? = null,
    val availableTrucks: List<TruckDto> = emptyList(),
    val isLoadingTrucks: Boolean = false,
    val dvirType: DvirType = DvirType.PRE_TRIP,
    /** Today opens the check for a known step, so it doesn't ask "before or after?" again. */
    val isTypeFixed: Boolean = false,
    /** Null until the crew answers "May sira ba?". */
    val hasDefects: Boolean? = null,
    val defects: List<DvirDefect> = emptyList(),
    /** The part whose photo the camera or gallery is taking right now. */
    val photoFor: TruckPart? = null,
    val odometerReading: String = "",
    /** Set once the draft is on the server, so a retry after a failed photo upload doesn't file it twice. */
    val savedReportId: String? = null,
    val photosSent: Boolean = false,
    override val signaturePaths: List<PathData>? = null,
    override val signatureBase64: String? = null,
    override val notes: String = "",
    override val latitude: Double? = null,
    override val longitude: Double? = null,
    override val isSubmitting: Boolean = false,
    override val error: String? = null,
    override val isSuccess: Boolean = false
) : CaptureFormState {
    override val photos: List<CapturedPhoto>
        get() = defects.flatMap { it.photos }
}

class DvirFormViewModel(
    private val truckApi: TruckApi,
    private val dvirApi: DvirApi,
    private val locationService: LocationService,
    private val preferencesManager: PreferencesManager,
    private val truckId: String?,
    tripId: String?,
    initialDvirType: DvirType? = null
) : BaseViewModel() {

    private val _formState = MutableStateFlow(
        DvirFormUiState(
            truckId = truckId,
            tripId = tripId,
            dvirType = initialDvirType ?: DvirType.PRE_TRIP,
            isTypeFixed = initialDvirType != null
        )
    )
    val uiState: StateFlow<DvirFormUiState> = _formState.asStateFlow()

    init {
        loadTrucks()
        fetchCurrentLocation()
    }

    private fun fetchCurrentLocation() {
        launchSafely {
            locationService.getCurrentLocation()?.let { loc ->
                _formState.update { it.copy(latitude = loc.latitude, longitude = loc.longitude) }
            }
        }
    }

    fun setSignature(paths: List<PathData>, base64: String) =
        edit { it.copy(signaturePaths = paths, signatureBase64 = base64) }

    fun clearSignature() = edit { it.copy(signaturePaths = null, signatureBase64 = null) }
    fun setNotes(notes: String) = edit { it.copy(notes = notes) }
    fun clearError() = _formState.update { it.copy(error = null) }

    private fun loadTrucks() {
        launchSafely(onError = { e ->
            _formState.update {
                it.copy(isLoadingTrucks = false, error = "Failed to load trucks: ${e.message}")
            }
        }) {
            _formState.update { it.copy(isLoadingTrucks = true) }
            val response = truckApi.getTrucks(pageSize = 100)
            val trucks = response.body().items ?: emptyList()
            _formState.update { state ->
                state.copy(
                    availableTrucks = trucks,
                    isLoadingTrucks = false,
                    selectedTruck = if (truckId != null) {
                        trucks.find { it.id == truckId }
                    } else {
                        state.selectedTruck
                    }
                )
            }
        }
    }

    fun selectTruck(truck: TruckDto) = edit { it.copy(selectedTruck = truck) }

    fun setDvirType(type: DvirType) = edit { it.copy(dvirType = type) }

    fun setOdometerReading(value: String) {
        if (value.isEmpty() || value.all { it.isDigit() }) {
            edit { it.copy(odometerReading = value) }
        }
    }

    fun setHasDefects(hasDefects: Boolean) = edit { it.copy(hasDefects = hasDefects) }

    /** Tapping a part tile adds it to the list; tapping it again takes it off. */
    fun togglePart(part: TruckPart) = edit { state ->
        val defects = if (state.defects.any { it.part == part }) {
            state.defects.filterNot { it.part == part }
        } else {
            state.defects + DvirDefect(part)
        }
        state.copy(defects = defects)
    }

    fun setCanStillDrive(part: TruckPart, canDrive: Boolean) = editDefect(part) { it.copy(canStillDrive = canDrive) }

    fun setDefectNote(part: TruckPart, note: String) = editDefect(part) { it.copy(note = note) }

    /** Called before opening the camera or gallery, so the photo lands on the right part. */
    fun takePhotoFor(part: TruckPart) = _formState.update { it.copy(photoFor = part) }

    fun addPhoto(photo: CapturedPhoto) {
        val part = _formState.value.photoFor ?: return
        editDefect(part) { it.copy(photos = it.photos + photo) }
    }

    fun removePhoto(part: TruckPart, photoId: String) =
        editDefect(part) { it.copy(photos = it.photos.filterNot { p -> p.id == photoId }) }

    private fun editDefect(part: TruckPart, change: (DvirDefect) -> DvirDefect) = edit { state ->
        state.copy(defects = state.defects.map { if (it.part == part) change(it) else it })
    }

    /** Any change after a half-finished save starts a fresh draft; the old one never leaves draft. */
    private fun edit(change: (DvirFormUiState) -> DvirFormUiState) = _formState.update {
        change(it).copy(savedReportId = null, photosSent = false)
    }

    fun canSubmit(): Boolean {
        val state = _formState.value
        val defectsAnswered = when (state.hasDefects) {
            null -> false
            false -> true
            true -> state.defects.isNotEmpty() && state.defects.all { it.canStillDrive != null }
        }
        return !state.isSubmitting &&
            state.selectedTruck != null &&
            defectsAnswered &&
            state.signatureBase64 != null
    }

    fun submit() = submitForm(
        state = _formState,
        canSubmit = canSubmit(),
        setSubmitting = { isSubmitting, error, isSuccess ->
            copy(isSubmitting = isSubmitting, error = error, isSuccess = isSuccess)
        },
        perform = { performSubmit() }
    )

    private suspend fun performSubmit() {
        val state = _formState.value
        val defects = if (state.hasDefects == true) state.defects else emptyList()
        val reportId = state.savedReportId ?: createDraft(state, defects)

        // Photos only go on while the report is a draft, so they're sent before submitting
        val photos = defects.flatMap { it.photos }
        if (photos.isNotEmpty() && !state.photosSent) {
            val parts = photos.map { FileUploadData(it.bytes, it.fileName, it.contentType) }.toFormParts("Photos")
            if (!dvirApi.addDvirPhotos(reportId, parts).success) {
                throw RuntimeException("Truck check photos weren't sent")
            }
            _formState.update { it.copy(photosSent = true) }
        }

        // Create leaves the report a draft, which the office's review queue never shows
        val submitted = dvirApi.submitDvirReport(
            reportId,
            SubmitDvirReportCommand(reportId = reportId, driverSignature = state.signatureBase64)
        )
        if (!submitted.success) {
            throw RuntimeException("DVIR saved as a draft but not submitted")
        }
    }

    private suspend fun createDraft(state: DvirFormUiState, defects: List<DvirDefect>): String {
        val driverId = preferencesManager.getUserId()
            ?: throw IllegalStateException("Driver ID not found. Please log in again.")

        val command = CreateDvirReportCommand(
            truckId = state.selectedTruck!!.id!!,
            driverId = driverId,
            type = state.dvirType,
            latitude = state.latitude,
            longitude = state.longitude,
            odometerReading = state.odometerReading.toIntOrNull(),
            driverNotes = state.notes.takeIf { it.isNotBlank() },
            driverSignature = state.signatureBase64,
            tripId = state.tripId,
            defects = defects.takeIf { it.isNotEmpty() }?.map { defect ->
                CreateDvirDefectRequest(
                    category = defect.part.category,
                    description = defect.description,
                    severity = defect.severity
                )
            }
        )

        val response = dvirApi.createDvirReport(command)
        if (!response.success) {
            throw RuntimeException("Failed to submit DVIR")
        }
        val reportId = response.body().id ?: throw RuntimeException("Failed to submit DVIR")
        _formState.update { it.copy(savedReportId = reportId) }
        return reportId
    }
}
