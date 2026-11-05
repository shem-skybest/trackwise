package com.trackwise.driver.model

import com.trackwise.driver.api.models.TripDto
import com.trackwise.driver.api.models.TripLoadDto
import com.trackwise.driver.api.models.TripStopDto
import com.trackwise.driver.api.models.TripStopPhotoDto
import com.trackwise.driver.api.models.TripStopType
import kotlinx.datetime.LocalTime
import kotlinx.datetime.TimeZone
import kotlinx.datetime.toLocalDateTime
import kotlin.math.abs
import kotlin.time.Instant

/** Pickups closer than this (in degrees, ~100 m) are one warehouse visit - mirrors the API's rule. */
private const val SAME_VISIT_DEGREES = 0.001

/** One warehouse visit or one drop - the same blocks the office's crew report shows. */
data class StopBlock(
    val stopId: String,
    val stopIds: List<String>,
    val loadId: String,
    /** Every load of the block: a warehouse visit's loads, or the drop's one. */
    val loadIds: List<String>,
    val isDrop: Boolean,
    /** The drop's or warehouse's place in the trip, from 1; 0 for a trip's only warehouse. */
    val number: Int,
    val place: String,
    val address: String,
    val latitude: Double?,
    val longitude: Double?,
    val cutOff: Instant?,
    val boxes: Int,
    val bags: Int,
    val arrivedAt: Instant?,
    val handlingStartedAt: Instant?,
    val handlingFinishedAt: Instant?,
    val departedAt: Instant?,
    val photoCount: Int,
    /** Another truck took this drop over after a breakdown, so this crew no longer runs it. */
    val rescued: Boolean = false
)

/** The trip's warehouse visits and drops in trip order. */
fun TripDto.toStopBlocks(photos: List<TripStopPhotoDto> = emptyList()): List<StopBlock> {
    val ordered = stops.orEmpty().sortedBy { it.order ?: 0 }
    val loadsById = loads.orEmpty().associateBy { it.id }
    val covered = mutableSetOf<String>()
    val blocks = mutableListOf<StopBlock>()
    var warehouses = 0
    var drops = 0

    for (stop in ordered) {
        val id = stop.id ?: continue
        if (id in covered) continue

        val visit = sameVisit(stop, ordered).filter { it.id != null && it.id !in covered }
        visit.forEach { covered += it.id!! }
        val visitLoads = visit.mapNotNull { loadsById[it.loadId] }
        val isDrop = stop.type == TripStopType.DROP_OFF
        val load = loadsById[stop.loadId]
        val stopIds = visit.mapNotNull { it.id }

        blocks += StopBlock(
            stopId = id,
            stopIds = stopIds,
            loadId = stop.loadId.orEmpty(),
            loadIds = visit.mapNotNull { it.loadId },
            isDrop = isDrop,
            number = if (isDrop) ++drops else ++warehouses,
            place = if (isDrop) load?.outletName().orEmpty() else "",
            address = listOfNotNull(stop.address.streetAddress, stop.address.cityOrMunicipality)
                .filter { it.isNotBlank() }
                .joinToString(", "),
            latitude = stop.location.latitude,
            longitude = stop.location.longitude,
            cutOff = if (isDrop) stop.cutOff else null,
            boxes = visitLoads.sumOf { it.plannedBoxes ?: 0 },
            bags = visitLoads.sumOf { it.plannedBags ?: 0 },
            arrivedAt = stop.arrivedAt,
            handlingStartedAt = stop.handlingStartedAt,
            handlingFinishedAt = stop.handlingFinishedAt,
            departedAt = stop.departedAt,
            photoCount = photos.count { photo -> photo.stopIds.orEmpty().any { it in stopIds } },
            rescued = stop.rescueId != null
        )
    }

    return if (warehouses == 1) {
        blocks.map { if (it.isDrop) it else it.copy(number = 0) }
    } else {
        blocks
    }
}

/**
 * When the crew is due at the warehouse - the earliest requested pickup on the trip. A booking
 * entered with a date but no call time is stored at local midnight, so that reads as "not set".
 */
val TripDto.callTime: Instant?
    get() = loads.orEmpty().mapNotNull { it.requestedPickupDate }.minOrNull()
        ?.takeUnless { it.toLocalDateTime(TimeZone.currentSystemDefault()).time == LocalTime(0, 0) }

val TripDto.helperNames: String
    get() = listOfNotNull(helperName, secondHelperName).joinToString(", ")

/**
 * A Cold Chain drop's load is named "<booking ref> - <outlet>", so the outlet is the part after
 * the last separator. Other loads have no outlet in their name.
 */
private fun TripLoadDto.outletName(): String =
    name?.substringAfterLast(" - ", missingDelimiterValue = "").orEmpty()

private fun sameVisit(stop: TripStopDto, stops: List<TripStopDto>): List<TripStopDto> {
    if (stop.type != TripStopType.PICK_UP) return listOf(stop)

    val lat = stop.location.latitude ?: 0.0
    val lng = stop.location.longitude ?: 0.0
    return stops.filter {
        it.type == TripStopType.PICK_UP &&
            abs((it.location.latitude ?: 0.0) - lat) <= SAME_VISIT_DEGREES &&
            abs((it.location.longitude ?: 0.0) - lng) <= SAME_VISIT_DEGREES
    }
}

/** Who the trip is for, as the crew knows them. */
val TripDto.clientName: String?
    get() = loads.orEmpty().firstNotNullOfOrNull { it.customer?.name?.takeIf(String::isNotBlank) }
