package com.trackwise.driver.model.location

import android.location.Address
import android.location.Location
import com.trackwise.driver.api.models.GeoPoint
import com.trackwise.driver.api.models.Address as AddressDto

fun Address?.toAddressDto(): AddressDto = AddressDto(
    streetAddress = listOfNotNull(this?.subThoroughfare, this?.thoroughfare).joinToString(" "),
    barangay = this?.subLocality,
    cityOrMunicipality = this?.locality ?: "",
    province = this?.subAdminArea ?: this?.adminArea ?: "",
    region = this?.adminArea ?: "",
    zipcode = this?.postalCode ?: "",
    // ISO-3166-1 alpha-2 code (e.g. "PH"), not the display name - the backend
    // requires a 2-letter country code.
    country = this?.countryCode ?: "",
)

fun Location.toGeoPoint(): GeoPoint = GeoPoint(
    latitude = this.latitude,
    longitude = this.longitude
)
