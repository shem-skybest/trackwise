package com.trackwise.driver.util

import java.text.NumberFormat
import java.util.Currency
import java.util.Locale

actual fun Double.formatCurrency(currencyCode: String): String {
    val format = NumberFormat.getCurrencyInstance(Locale.getDefault())
    format.currency = Currency.getInstance(currencyCode)
    return format.format(this)
}
