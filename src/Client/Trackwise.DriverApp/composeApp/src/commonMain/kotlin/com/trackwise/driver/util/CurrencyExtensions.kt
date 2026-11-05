package com.trackwise.driver.util

/** Amounts come from the API in the tenant's currency - pesos for Skybest - not the phone's locale. */
const val DEFAULT_CURRENCY_CODE = "PHP"

/**
 * Formats a Double as a currency string in [currencyCode].
 * The actual implementation is platform-specific.
 * @return The formatted currency string.
 */
expect fun Double.formatCurrency(currencyCode: String = DEFAULT_CURRENCY_CODE): String
