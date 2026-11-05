package com.trackwise.driver.service.realtime

import com.trackwise.driver.api.models.Address
import com.trackwise.driver.api.models.GeoPoint
import kotlinx.serialization.Serializable

/**
 * Location data to send to the server.
 */
@Serializable
data class TruckGeolocation(
    val truckId: String,
    val tenantId: String,
    val currentLocation: GeoPoint,
    val currentAddress: Address? = null,
    val truckNumber: String? = null,
    val driversName: String? = null
)
