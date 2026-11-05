package com.trackwise.driver.service

import com.trackwise.driver.api.DriverApi
import com.trackwise.driver.api.LoadApi
import com.trackwise.driver.api.models.LoadDto
import com.trackwise.driver.api.models.LoadStatus
import com.trackwise.driver.api.models.UpdateLoadProximityCommand
import com.trackwise.driver.util.Logger
import com.trackwise.driver.util.distanceMeters
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.async
import kotlinx.coroutines.awaitAll
import kotlinx.coroutines.cancel
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.flow.launchIn
import kotlinx.coroutines.flow.onEach
import kotlin.time.Clock
import kotlin.time.Duration.Companion.minutes
import kotlin.time.Instant

/**
 * Watches the [LocationUpdateBus] for new fixes while location is shared
 * and pings the backend with [UpdateLoadProximityCommand] on state transitions
 * (truck enters / leaves a 500m radius around the load's next checkpoint: the
 * origin while Dispatched, the destination once Picked Up). Repeated ticks
 * inside the radius are not re-posted.
 *
 * Lifecycle is owned by [LocationSharing], which starts and stops it with tracking.
 * The active-loads list is cached for 5 minutes to avoid
 * an API call on every location update.
 */
class LoadProximityWatcher(
    private val loadApi: LoadApi,
    private val driverApi: DriverApi,
    private val preferencesManager: PreferencesManager
) {
    private companion object {
        const val PROXIMITY_THRESHOLD_METERS = 500.0
        const val LOAD_CACHE_DURATION_MS = 5 * 60 * 1000L
        val NUDGE_COOLDOWN = 30.minutes
    }

    private val nudgedAt = mutableMapOf<String, Instant>()

    private var scope: CoroutineScope? = null
    private var collectJob: Job? = null
    private var cachedLoads: List<LoadDto> = emptyList()
    private var lastLoadFetchTime: Long = 0
    private val loadsInProximity = mutableSetOf<String>()

    /** When the truck last came within range of each load's checkpoint, by [checkpointKey]. */
    private val enteredAt = mutableMapOf<String, Instant>()

    /**
     * When GPS last saw the truck reach any of [loadIds]' warehouse ([atPickup]) or drop - the time
     * the app offers for an arrival the crew records late. Null if it wasn't seen this session.
     */
    fun arrivalSeenAt(loadIds: Collection<String>, atPickup: Boolean): Instant? {
        val status = if (atPickup) LoadStatus.DISPATCHED else LoadStatus.PICKED_UP
        return loadIds.mapNotNull { enteredAt[checkpointKey(it, status)] }.maxOrNull()
    }

    fun start() {
        if (scope != null) {
            return
        }
        val newScope = CoroutineScope(Dispatchers.Default + SupervisorJob())
        scope = newScope
        collectJob = LocationUpdateBus.updates
            .onEach { fix -> checkLoadProximity(fix) }
            .launchIn(newScope)
        Logger.d("LoadProximityWatcher: started")
    }

    fun stop() {
        scope?.cancel()
        collectJob = null
        scope = null
        cachedLoads = emptyList()
        lastLoadFetchTime = 0
        loadsInProximity.clear()
        Logger.d("LoadProximityWatcher: stopped")
    }

    private suspend fun checkLoadProximity(fix: LocationFix) {
        try {
            val loads = getActiveLoads()
            val transitions = loads.mapNotNull { load -> proximityTransition(load, fix) }
            if (transitions.isEmpty()) return
            nudgeArrival(transitions, loads)

            coroutineScope {
                transitions.map { transition ->
                    async {
                        driverApi.updateLoadProximity(
                            UpdateLoadProximityCommand(
                                loadId = transition.loadId,
                                isInProximity = transition.isInProximity
                            )
                        )
                        Logger.d("Load ${transition.loadId} proximity -> ${transition.isInProximity}")
                    }
                }.awaitAll()
            }
        } catch (e: Exception) {
            Logger.e("LoadProximityWatcher: error checking proximity", e)
        }
    }

    /**
     * Forget what was last posted for [loadId] and refetch statuses on the next fix. Call after a
     * confirm: the server clears proximity on a status change and the next checkpoint differs.
     */
    fun onLoadStatusChanged(loadId: String) {
        loadsInProximity.remove(loadId)
        lastLoadFetchTime = 0
    }

    private fun proximityTransition(load: LoadDto, fix: LocationFix): ProximityTransition? {
        val loadId = load.id ?: return null
        val checkpoint = when (load.status) {
            LoadStatus.DISPATCHED -> load.originLocation
            LoadStatus.PICKED_UP -> load.destinationLocation
            else -> return null
        }
        val lat = checkpoint.latitude ?: return null
        val lon = checkpoint.longitude ?: return null
        val nowNear = distanceMeters(fix.latitude, fix.longitude, lat, lon) <= PROXIMITY_THRESHOLD_METERS
        val wasNear = loadId in loadsInProximity
        val key = checkpointKey(loadId, load.status)

        // [onLoadStatusChanged] forgets which zones the truck is in after every tap, so the next fix
        // "enters" again. Only a fix outside the zone ends the visit - until then keep the first entry,
        // or the GPS arrival time drifts to whenever the crew last tapped.
        if (!nowNear) {
            enteredAt.remove(key)
        }

        return when {
            nowNear && !wasNear -> {
                loadsInProximity.add(loadId)
                val newVisit = key !in enteredAt
                enteredAt.getOrPut(key) { Clock.System.now() }
                ProximityTransition(loadId, isInProximity = true, newVisit = newVisit)
            }
            !nowNear && wasNear -> {
                loadsInProximity.remove(loadId)
                ProximityTransition(loadId, isInProximity = false)
            }
            else -> null
        }
    }

    private suspend fun getActiveLoads(): List<LoadDto> {
        val now = Clock.System.now().toEpochMilliseconds()
        if (now - lastLoadFetchTime > LOAD_CACHE_DURATION_MS || cachedLoads.isEmpty()) {
            val userId = preferencesManager.getUserId() ?: return emptyList()
            val result = loadApi.getLoads(onlyActiveLoads = true, userId = userId).body()
            cachedLoads = result.items ?: emptyList()
            lastLoadFetchTime = now
        }
        return cachedLoads
    }

    private fun checkpointKey(loadId: String, status: LoadStatus?) = "$loadId:$status"

    /**
     * One buzz per stop: a warehouse's loads all enter on the same fix, and taps re-enter the zone
     * without leaving it ([newVisit] is false then). The cooldown stops GPS wobbling across the edge
     * of the zone from buzzing again and again.
     */
    private fun nudgeArrival(transitions: List<ProximityTransition>, loads: List<LoadDto>) {
        val now = Clock.System.now()
        val load = transitions
            .filter { it.isInProximity && it.newVisit }
            .firstNotNullOfOrNull { t -> loads.firstOrNull { it.id == t.loadId } }
            ?: return
        val loadId = load.id ?: return
        val key = checkpointKey(loadId, load.status)
        if (nudgedAt[key]?.let { now - it < NUDGE_COOLDOWN } == true) return
        nudgedAt[key] = now

        val atPickup = load.status == LoadStatus.DISPATCHED
        // Booked names are "Client - Outlet" or just the outlet's code; a blank place means a warehouse.
        val place = if (atPickup) {
            ""
        } else {
            load.name?.substringAfterLast(" - ")?.trim()?.ifBlank { null }
                ?: load.destinationAddress.streetAddress?.substringBefore(",")?.trim().orEmpty()
        }
        ArrivalNudges.show(NearbyStop(loadId, atPickup, place))
    }

    private data class ProximityTransition(
        val loadId: String,
        val isInProximity: Boolean,
        val newVisit: Boolean = false
    )
}
