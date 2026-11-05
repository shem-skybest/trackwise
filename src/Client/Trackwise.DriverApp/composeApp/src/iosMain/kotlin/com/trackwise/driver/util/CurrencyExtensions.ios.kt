package com.trackwise.driver.util

import platform.Foundation.NSLocale
import platform.Foundation.NSNumber
import platform.Foundation.NSNumberFormatter
import platform.Foundation.NSNumberFormatterCurrencyStyle
import platform.Foundation.autoupdatingCurrentLocale

actual fun Double.formatCurrency(currencyCode: String): String {
    val formatter = NSNumberFormatter()
    val locale = NSLocale.autoupdatingCurrentLocale
    formatter.numberStyle = NSNumberFormatterCurrencyStyle
    formatter.locale = locale
    formatter.currencyCode = currencyCode
    return formatter.stringFromNumber(NSNumber(this)) ?: "$currencyCode $this"
}
