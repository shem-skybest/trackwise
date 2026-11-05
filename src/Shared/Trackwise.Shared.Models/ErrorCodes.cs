namespace Trackwise.Shared.Models;

/// <summary>
///     Well-known error codes for programmatic error handling.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    ///     A master-data write closely matches an existing record and the steward has not declared
    ///     it distinct. Retryable by re-issuing with the confirmed-distinct flag and a reason, or by
    ///     opening the existing record instead. Candidates come from the duplicate-probe endpoint.
    /// </summary>
    public const string DuplicateMasterDataRecord = "DUPLICATE_MASTER_DATA_RECORD";

    /// <summary>
    ///     A steward tried to deactivate or delete a master record that other records still point
    ///     at. Retryable by re-issuing with the confirmed-despite-dependents flag and a reason, or
    ///     by reassigning the dependents first. Counts come from the dependency-probe endpoint.
    /// </summary>
    public const string MasterDataRecordInUse = "MASTER_DATA_RECORD_IN_USE";

    /// <summary>
    ///     An approver tried to decide their own submission. Not retryable by the caller at all -
    ///     the point of maker-checker is that somebody else has to look at it - so the client shows
    ///     the record as still pending rather than offering a way through.
    /// </summary>
    public const string MasterDataSelfApproval = "MASTER_DATA_SELF_APPROVAL";

    /// <summary>
    ///     The requested feature is not included in the tenant's current subscription plan.
    /// </summary>
    public const string FeatureNotInPlan = "FEATURE_NOT_IN_PLAN";

    /// <summary>
    ///     The feature has been disabled by a platform administrator.
    /// </summary>
    public const string FeatureDisabledByAdmin = "FEATURE_DISABLED_BY_ADMIN";

    /// <summary>
    ///     A resource limit defined by the subscription plan has been reached (e.g., max trucks).
    /// </summary>
    public const string ResourceLimitReached = "RESOURCE_LIMIT_REACHED";

    /// <summary>
    ///     The broker's credit is below the tenant's configured threshold, or its FMCSA operating
    ///     authority is inactive. Retryable by re-issuing the booking with the override flag set.
    /// </summary>
    public const string BrokerCreditBelowThreshold = "BROKER_CREDIT_BELOW_THRESHOLD";

    /// <summary>
    ///     No rate floor covers the listing's lane and the tenant has no default floor, so a
    ///     counter-offer cannot be checked against anything. Fixed by adding a lane floor.
    /// </summary>
    public const string NegotiationFloorMissing = "NEGOTIATION_FLOOR_MISSING";

    /// <summary>
    ///     The proposed counter-offer is below the tenant's rate floor for that lane.
    /// </summary>
    public const string NegotiationBelowFloor = "NEGOTIATION_BELOW_FLOOR";

    /// <summary>
    ///     The webhook request itself is unusable - a bad signature or a body we cannot read. The
    ///     endpoint answers 4xx on this so the provider stops retrying; every other failure is
    ///     transient and must answer 5xx to keep the retry.
    /// </summary>
    public const string WebhookRejected = "WEBHOOK_REJECTED";

    /// <summary>
    ///     Weekly AI budget spent and the owner opted to pause AI instead of billing overage
    ///     (TenantSettings.BlockAIOverage). Self-imposed - deliberately not an upgrade-prompt code.
    /// </summary>
    public const string AIBudgetReached = "AI_BUDGET_REACHED";

    /// <summary>
    ///     The one wording for <see cref="AIBudgetReached"/>, shared by the copilot send result, the
    ///     copilot transcript notice and the blocked dispatch session so all three name the same
    ///     setting and the same place to change it.
    /// </summary>
    public const string AIBudgetReachedMessage =
        "Your company's weekly AI budget is used up and AI is paused until it resets. " +
        "An owner can allow overage billing in Company Settings.";


    // ── General (map to HTTP status in ToActionResult) ───────────────────────
    public const string Validation = "validation_failed";          // 400
    public const string InvalidRequest = "invalid_request";        // 400
    public const string Unauthorized = "unauthorized";             // 401
    public const string Forbidden = "forbidden";                   // 403
    public const string NotFound = "not_found";                    // 404
    public const string Conflict = "conflict";                     // 409
    public const string Duplicate = "duplicate";                   // 409
    public const string InvalidState = "invalid_state";            // 409
    public const string ConcurrencyConflict = "concurrency_conflict"; // 409
    public const string Gone = "gone";                             // 410
    public const string PreconditionFailed = "precondition_failed"; // 412
    public const string Unprocessable = "unprocessable";           // 422
    public const string RateLimited = "rate_limited";              // 429
    public const string Unexpected = "unexpected_error";           // 500
    public const string ExternalServiceFailure = "external_service_failure"; // 502
    public const string ServiceUnavailable = "service_unavailable"; // 503

    // ── Customer ─────────────────────────────────────────────────────────────
    public const string CustomerNotFound = "customer_not_found";
    public const string CustomerInactive = "customer_inactive";

    // ── Location ─────────────────────────────────────────────────────────────
    public const string LocationNotFound = "location_not_found";
    public const string LocationInactive = "location_inactive";
    public const string LocationMerged = "location_merged";
    public const string LocationIsZone = "location_is_zone";
    public const string LocationNotOwnedByCustomer = "location_not_owned_by_customer";
    public const string LocationMissingCoordinates = "location_missing_coordinates";

    // ── Route / Lane ─────────────────────────────────────────────────────────
    public const string RouteTooFewStops = "route_too_few_stops";
    public const string RouteInvalidSequence = "route_invalid_sequence";
    public const string DistanceNotCalculable = "distance_not_calculable";

    // ── Quotation ────────────────────────────────────────────────────────────
    public const string QuotationNotFound = "quotation_not_found";
    public const string QuotationNotDraft = "quotation_not_draft";
    public const string QuotationInvalidStatus = "quotation_invalid_status";
    public const string QuotationExpired = "quotation_expired";
    public const string QuotationHasNoLines = "quotation_has_no_lines";
    public const string QuotationMissingBaseCharge = "quotation_missing_base_charge";
    public const string QuotationMissingTelematicsLine = "quotation_missing_telematics_line";
    public const string QuotationMissingPreCoolingLine = "quotation_missing_precooling_line";
    public const string QuotationMissingCustomsLine = "quotation_missing_customs_line";
    public const string QuotationRequiresApproval = "quotation_requires_approval";
    public const string QuotationCurrencyChangeBlocked = "quotation_currency_change_blocked";
    public const string ReferenceNumberTaken = "reference_number_taken";

    // ── Cargo / Currency / FX ────────────────────────────────────────────────
    public const string InvalidCurrency = "invalid_currency";
    public const string FxRateUnavailable = "fx_rate_unavailable";
    public const string CargoValueCurrencyMismatch = "cargo_value_currency_mismatch";
    public const string InvalidCargoSpecification = "invalid_cargo_specification";
    public const string InvalidTemperatureSpecification = "invalid_temperature_specification";

    // ── Pricing / Rating engine ──────────────────────────────────────────────
    public const string TariffRateNotFound = "tariff_rate_not_found";
    public const string ChargeCodeNotFound = "charge_code_not_found";
    public const string ChargeCategoryMismatch = "charge_category_mismatch";
    public const string PricingBaseUnresolvable = "pricing_base_unresolvable";
    public const string RatingFailed = "rating_failed";

    /// <summary>
    ///     The request hit a database error. The message is generic on purpose, so a client should
    ///     show its own "not saved, try again" text instead of treating it as a reason to act on.
    /// </summary>
    public const string ServerFault = "SERVER_FAULT";

    /// <summary>
    ///     A crew's trip event (arrived, departed, ...) is already on record, from an earlier send of
    ///     the same tap or from the office. The driver app treats it as saved, not as a refusal.
    /// </summary>
    public const string TripEventAlreadyRecorded = "TRIP_EVENT_ALREADY_RECORDED";
}
