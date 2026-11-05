package com.trackwise.driver.model

import com.trackwise.driver.api.models.DvirInspectionCategory

/**
 * The parts a crew picks from on the truck check: what goes wrong on Skybest's reefer trucks,
 * in the crew's words, instead of the US inspection list (air lines, fifth wheel, coupling).
 * Each part files under one of the server's categories; [officeLabel] goes in the defect's
 * description so the office can tell apart parts that share a category (Reefer and Other).
 */
enum class TruckPart(val category: DvirInspectionCategory, val officeLabel: String) {
    TIRES(DvirInspectionCategory.TIRES, "Tires"),
    BRAKES(DvirInspectionCategory.BRAKES_SERVICE, "Brakes"),
    HEADLIGHTS(DvirInspectionCategory.LIGHTS_HEAD, "Headlights"),
    REAR_LIGHTS(DvirInspectionCategory.LIGHTS_TAIL, "Rear lights (tail, brake, signal)"),
    ENGINE(DvirInspectionCategory.ENGINE, "Engine"),
    OIL_WATER(DvirInspectionCategory.FLUID_LEVELS, "Oil or water"),
    BATTERY(DvirInspectionCategory.BATTERY, "Battery"),
    STEERING(DvirInspectionCategory.STEERING, "Steering"),
    MIRRORS(DvirInspectionCategory.MIRRORS, "Side mirrors"),
    WINDSHIELD(DvirInspectionCategory.WINDSHIELD, "Windshield or wipers"),
    HORN(DvirInspectionCategory.HORN, "Horn"),
    REEFER(DvirInspectionCategory.OTHER, "Reefer unit"),
    OTHER(DvirInspectionCategory.OTHER, "Other")
}
