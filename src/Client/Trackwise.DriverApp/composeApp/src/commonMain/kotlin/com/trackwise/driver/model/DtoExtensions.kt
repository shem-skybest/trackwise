package com.trackwise.driver.model

import com.trackwise.driver.api.models.Address
import com.trackwise.driver.api.models.DailyGrossDto
import com.trackwise.driver.api.models.EmployeeDto
import com.trackwise.driver.api.models.LoadDto
import com.trackwise.driver.api.models.MonthlyGrossDto
import com.trackwise.driver.api.models.TruckDto
import com.trackwise.driver.util.buildDirectionsUrl
import kotlin.time.Instant

// Address extensions
fun Address.toDisplayString(): String {
    val parts = listOfNotNull(streetAddress, barangay, cityOrMunicipality, province, zipcode).filter { it.isNotBlank() }
    return parts.joinToString(", ")
}

fun LoadDto.getMapsUrl(): String {
    val origin = "${originLocation.latitude},${originLocation.longitude}"
    val destination = "${destinationLocation.latitude},${destinationLocation.longitude}"
    return buildDirectionsUrl(origin, destination)
}

/**
 * The load's own price. A trip's price is the truck-trip rate spread over its drops (the first
 * drop carries all of it), so a single trip load has no price of its own to show.
 */
val LoadDto.ownPrice: Double?
    get() = deliveryCost.takeIf { tripId == null }

// TruckDto extensions
val TruckDto.driversList: List<EmployeeDto>
    get() = listOfNotNull(mainDriver, secondaryDriver)


// EmployeeDto extensions
fun EmployeeDto.fullName(): String {
    return listOfNotNull(firstName, lastName).joinToString(" ")
}

// Chart data extensions for DailyGrossDto and MonthlyGrossDto
fun DailyGrossDto.toChartData(): ChartData = ChartData(
    label = this.date?.toString()?.substring(0, 10) ?: "",
    gross = this.gross ?: 0.0,
    driverShare = this.driverShare ?: 0.0,
    date = date
)

fun MonthlyGrossDto.toChartData(): ChartData = ChartData(
    label = this.date?.toString()?.take(7) ?: "",
    gross = this.gross ?: 0.0,
    driverShare = this.driverShare ?: 0.0,
    date = date
)

data class ChartData(
    val label: String,
    val gross: Double,
    val driverShare: Double,
    val date: Instant? = null
)

data class DateRange(
    val name: String,
    val startDate: Instant,
    val endDate: Instant,
    val useMonthly: Boolean = false
)
