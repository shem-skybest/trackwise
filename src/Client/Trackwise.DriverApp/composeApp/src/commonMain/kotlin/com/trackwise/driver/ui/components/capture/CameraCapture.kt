package com.trackwise.driver.ui.components.capture

import androidx.compose.runtime.Composable
import com.trackwise.driver.util.CameraLauncher
import com.trackwise.driver.util.CaptureResult
import com.trackwise.driver.viewmodel.CapturedPhoto
import org.koin.compose.getKoin
import kotlin.uuid.ExperimentalUuidApi
import kotlin.uuid.Uuid

/**
 * Provides a camera capture callback that launches the camera and converts the result
 * into a [CapturedPhoto]. Returns a no-op callback if no [CameraLauncher] is available,
 * so callers can use the result directly without a null check.
 */
@Composable
fun rememberCameraCapture(onPhotoCaptured: (CapturedPhoto) -> Unit): () -> Unit =
    rememberPhotoSource(onPhotoCaptured) { launcher, onResult -> launcher.launchCamera(onResult) }

/** Like [rememberCameraCapture], but picks a photo already on the phone. */
@Composable
fun rememberGalleryPick(onPhotoCaptured: (CapturedPhoto) -> Unit): () -> Unit =
    rememberPhotoSource(onPhotoCaptured) { launcher, onResult -> launcher.launchGallery(onResult) }

@OptIn(ExperimentalUuidApi::class)
@Composable
private fun rememberPhotoSource(
    onPhotoCaptured: (CapturedPhoto) -> Unit,
    launch: (CameraLauncher, (CaptureResult?) -> Unit) -> Unit
): () -> Unit {
    val cameraLauncher: CameraLauncher = getKoin().getOrNull() ?: return {}
    return {
        launch(cameraLauncher) { result ->
            result?.let {
                onPhotoCaptured(
                    CapturedPhoto(
                        id = Uuid.random().toString(),
                        bytes = it.bytes,
                        fileName = it.fileName,
                        contentType = it.contentType
                    )
                )
            }
        }
    }
}
