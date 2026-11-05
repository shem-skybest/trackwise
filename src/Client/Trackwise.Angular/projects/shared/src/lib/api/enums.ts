/**
 * Enum-like const objects for backwards compatibility.
 * The generated code uses string union types, but consuming code expects enum-like objects.
 * These shadow the type-only exports from generated/models with both type + value.
 */
import type { SelectOption } from "../models/select-option";
import type {
  AccidentType,
  BillIndicator,
  BillingBatchStatus,
  BillingChargeKind,
  BillingCycle,
  BookingLossReason,
  BookingStatus,
  BusinessPartnerRole,
  CancellationParty,
  CashExpenseCategory,
  CashFundStatus,
  ContainerMoveStatus,
  ContainerStatus,
  ContainerTypeCategory,
  CreditType,
  CrewDayStatus,
  CrewUnit,
  CustomerStatus,
  DocumentType,
  DriverLicenseStatus,
  DropReturnReason,
  EmployeeStatus,
  EmploymentType,
  ExpenseStatus,
  FleetCard,
  FreightMode,
  FuelAdjustmentMethod,
  GensetBillingType,
  GensetStatus,
  HazmatClass,
  IncidentKind,
  IncidentSeverity,
  IncidentStatus,
  InvoiceLineItemType,
  InvoiceStatus,
  LicenseClass,
  LicenseEndorsement,
  LoadExceptionType,
  LoadSource,
  LoadStatus,
  LoadType,
  LocationType,
  MaintenanceIntervalType,
  MaintenanceType,
  MarkupType,
  MasterDataApprovalState,
  OperatingMode,
  PaymentMethodType,
  PaymentStatus,
  PhilippineDlCode,
  QuotationStatus,
  RateAgreementStatus,
  ReasoningEffort,
  Region,
  ReportCategory,
  SalaryType,
  ServiceLevel,
  SubscriptionStatus,
  TerminalType,
  TimeEntryType,
  TrailerType,
  TripStatus,
  TruckDayStatus,
  TruckingVariant,
  TruckLegRole,
  TruckOwnership,
  TruckStatus,
  TruckTonnageClass,
  TruckType,
  VehiclePermitType,
  VehicleSystem,
  VesselType,
  WorkfileStatus,
  WorkfileType,
} from "./generated";

export const freightModeOptions: SelectOption<FreightMode>[] = [
  { label: "Domestic", value: "domestic" as FreightMode },
  { label: "Import", value: "import" as FreightMode },
  { label: "Export", value: "export" as FreightMode },
  { label: "Triangular", value: "triangular" as FreightMode },
];

export const serviceLevelOptions: SelectOption<ServiceLevel>[] = [
  { label: "Economy", value: "economy" as ServiceLevel },
  { label: "Standard", value: "standard" as ServiceLevel },
  { label: "Express", value: "express" as ServiceLevel },
];

export const markupTypeOptions: SelectOption<MarkupType>[] = [
  { label: "Percentage", value: "percentage" as MarkupType },
  { label: "Flat", value: "flat" as MarkupType },
];

export const quotationStatusOptions: SelectOption<QuotationStatus>[] = [
  { label: "Draft", value: "draft" as QuotationStatus },
  { label: "Approved", value: "approved" as QuotationStatus },
  { label: "Rejected", value: "rejected" as QuotationStatus },
];

export const bookingStatusOptions: SelectOption<BookingStatus>[] = [
  { label: "Pending", value: "pending" as BookingStatus },
  { label: "Confirmed", value: "confirmed" as BookingStatus },
  { label: "In Execution", value: "in_execution" as BookingStatus },
  { label: "Completed", value: "completed" as BookingStatus },
  { label: "Cancelled", value: "cancelled" as BookingStatus },
  { label: "Rescheduled", value: "resched" as BookingStatus },
  { label: "On Hold", value: "hold" as BookingStatus },
  { label: "Lost Opportunity", value: "lost_opportunity" as BookingStatus },
  { label: "No Booking", value: "no_booking" as BookingStatus },
];

/** The four operational outcomes settable via `setBookingOperationalStatus`. */
export const bookingOperationalStatusOptions: SelectOption<BookingStatus>[] = [
  { label: "Rescheduled", value: "resched" as BookingStatus },
  { label: "On Hold", value: "hold" as BookingStatus },
  { label: "Lost Opportunity", value: "lost_opportunity" as BookingStatus },
  { label: "No Booking", value: "no_booking" as BookingStatus },
];

export const workfileTypeOptions: SelectOption<WorkfileType>[] = [
  { label: "Regular (Ocean)", value: "regular" as WorkfileType },
  { label: "Reefer", value: "reefer" as WorkfileType },
  { label: "Trucking", value: "trucking" as WorkfileType },
];

export const workfileStatusOptions: SelectOption<WorkfileStatus>[] = [
  { label: "Open", value: "open" as WorkfileStatus },
  { label: "Manager Review", value: "manager_review" as WorkfileStatus },
  { label: "Accounting Review", value: "accounting_review" as WorkfileStatus },
  { label: "Closed", value: "closed" as WorkfileStatus },
  { label: "Cancelled", value: "cancelled" as WorkfileStatus },
];

export const truckingVariantOptions: SelectOption<TruckingVariant>[] = [
  { label: "Truck", value: "truck" as TruckingVariant },
  { label: "Trailer", value: "trailer" as TruckingVariant },
];

export const truckLegRoleOptions: SelectOption<TruckLegRole>[] = [
  { label: "Pickup (door to port)", value: "pickup" as TruckLegRole },
  { label: "Delivery (port to door)", value: "delivery" as TruckLegRole },
  { label: "Transfer (port, depot or yard)", value: "transfer" as TruckLegRole },
];

export const containerMoveStatusOptions: SelectOption<ContainerMoveStatus>[] = [
  { label: "Not started", value: "not_started" as ContainerMoveStatus },
  { label: "Loading", value: "loading" as ContainerMoveStatus },
  { label: "On the road", value: "on_the_road" as ContainerMoveStatus },
  { label: "Unloading", value: "unloading" as ContainerMoveStatus },
  { label: "At port", value: "at_port" as ContainerMoveStatus },
  { label: "On vessel", value: "on_vessel" as ContainerMoveStatus },
  { label: "At yard", value: "at_yard" as ContainerMoveStatus },
  { label: "Delivered", value: "delivered" as ContainerMoveStatus },
];

export const reportCategoryOptions: SelectOption<ReportCategory>[] = [
  { label: "PSB", value: "psb" as ReportCategory },
  { label: "Not PSB", value: "not_psb" as ReportCategory },
];

export const billIndicatorOptions: SelectOption<BillIndicator>[] = [
  { label: "MBL", value: "mbl" as BillIndicator },
  { label: "HBL", value: "hbl" as BillIndicator },
];

export const creditTypeOptions: SelectOption<CreditType>[] = [
  { label: "Cash", value: "cash" as CreditType },
  { label: "Charge", value: "charge" as CreditType },
  { label: "Prepaid", value: "prepaid" as CreditType },
  { label: "Collect", value: "collect" as CreditType },
];

export const masterDataApprovalStateOptions: SelectOption<MasterDataApprovalState>[] = [
  { label: "Draft", value: "draft" as MasterDataApprovalState },
  { label: "Awaiting approval", value: "pending" as MasterDataApprovalState },
  { label: "Approved", value: "approved" as MasterDataApprovalState },
];

export const businessPartnerRoleOptions: SelectOption<BusinessPartnerRole>[] = [
  { label: "Shipping Line", value: "shipping_line" as BusinessPartnerRole },
  { label: "Co-Loader", value: "co_loader" as BusinessPartnerRole },
  { label: "Forwarder", value: "forwarder" as BusinessPartnerRole },
  { label: "Trucker", value: "trucker" as BusinessPartnerRole },
  { label: "Broker", value: "broker" as BusinessPartnerRole },
  { label: "Agent", value: "agent" as BusinessPartnerRole },
  { label: "Shipper", value: "shipper" as BusinessPartnerRole },
  { label: "Consignee", value: "consignee" as BusinessPartnerRole },
  { label: "Depot", value: "depot" as BusinessPartnerRole },
];

// Options arrays for select dropdowns
export const reasoningEffortOptions: SelectOption<ReasoningEffort>[] = [
  { label: "Off", value: "none" },
  { label: "Low", value: "low" },
  { label: "Medium", value: "medium" },
  { label: "High", value: "high" },
  { label: "Extra high", value: "x_high" },
  { label: "Max", value: "max" },
];

export const paymentMethodTypeOptions: SelectOption<PaymentMethodType>[] = [
  { label: "Cash", value: "cash" },
  { label: "Check", value: "check" },
  { label: "Zelle", value: "zelle" },
];

export const salaryTypeOptions: SelectOption<SalaryType>[] = [
  { label: "None", value: "none" },
  { label: "Monthly", value: "monthly" },
  { label: "Weekly", value: "weekly" },
  { label: "Share of Gross", value: "share_of_gross" },
  { label: "Rate per Distance", value: "rate_per_distance" },
  { label: "Hourly", value: "hourly" },
];

export const employeeStatusOptions: SelectOption<EmployeeStatus>[] = [
  { label: "Active", value: "active" },
  { label: "On Leave", value: "on_leave" },
  { label: "Suspended", value: "suspended" },
  { label: "Terminated", value: "terminated" },
];

export const customerStatusOptions: SelectOption<CustomerStatus>[] = [
  { label: "Active", value: "active" },
  { label: "Inactive", value: "inactive" },
  { label: "Prospect", value: "prospect" },
];

export const documentTypeOptions: SelectOption<DocumentType>[] = [
  { label: "Bill of Lading", value: "bill_of_lading" },
  { label: "Proof of Delivery", value: "proof_of_delivery" },
  { label: "Invoice", value: "invoice" },
  { label: "Receipt", value: "receipt" },
  { label: "Contract", value: "contract" },
  { label: "Insurance Certificate", value: "insurance_certificate" },
  { label: "Photo", value: "photo" },
  { label: "Driver License", value: "driver_license" },
  { label: "Vehicle Registration", value: "vehicle_registration" },
  { label: "Identity Document", value: "identity_document" },
  { label: "Other", value: "other" },
];

export const loadTypeOptions: SelectOption<LoadType>[] = [
  { label: "General Freight", value: "general_freight" },
  { label: "Refrigerated Goods", value: "refrigerated_goods" },
  { label: "Hazardous Materials", value: "hazardous_materials" },
  { label: "Oversize/Heavy", value: "oversize_heavy" },
  { label: "Liquid", value: "liquid" },
  { label: "Bulk", value: "bulk" },
  { label: "Vehicle", value: "vehicle" },
  { label: "Livestock", value: "livestock" },
  { label: "Intermodal Container", value: "intermodal_container" },
  { label: "Tank Container", value: "tank_container" },
  { label: "Reefer Container", value: "reefer_container" },
  { label: "Break Bulk", value: "break_bulk" },
  { label: "High Value", value: "high_value" },
  { label: "Pharmaceutical", value: "pharmaceutical" },
  { label: "Project Cargo", value: "project_cargo" },
];

export const truckTypeOptions: SelectOption<TruckType>[] = [
  { label: "Flatbed", value: "flatbed" },
  { label: "Freight Truck", value: "freight_truck" },
  { label: "Reefer", value: "reefer" },
  { label: "Tanker", value: "tanker" },
  { label: "Box Truck", value: "box_truck" },
  { label: "Dump Truck", value: "dump_truck" },
  { label: "Tow Truck", value: "tow_truck" },
  { label: "Car Hauler", value: "car_hauler" },
  { label: "Container Truck", value: "container_truck" },
  { label: "Tautliner", value: "tautliner" },
  { label: "Low Loader", value: "low_loader" },
  { label: "Car Transporter", value: "car_transporter" },
  { label: "Swap Body", value: "swap_body" },
  { label: "Curtainsider", value: "curtainsider" },
  { label: "Wing Van", value: "wing_van" },
];

export const truckTonnageClassOptions: SelectOption<TruckTonnageClass>[] = [
  { label: "2 Tonner", value: "two_tonner" },
  { label: "3 Tonner", value: "three_tonner" },
  { label: "4 Tonner", value: "four_tonner" },
  { label: "10 Tonner", value: "ten_tonner" },
];

/** Why a Cold Chain booking didn't run - the reasons its Monday booking board records. */
export const bookingLossReasonOptions: SelectOption<BookingLossReason>[] = [
  { label: "Lack of personnel", value: "lack_of_personnel" },
  { label: "Lack of unit", value: "lack_of_unit" },
  { label: "Lack of documents", value: "lack_of_documents" },
  { label: "Crew in training or inspection", value: "crew_in_training_or_inspection" },
  { label: "Client backed out", value: "client_backed_out" },
  { label: "Client's customer cancelled", value: "end_customer_cancelled" },
  { label: "Client used their own unit", value: "client_used_own_unit" },
  { label: "Rate not agreed", value: "rate_not_agreed" },
  { label: "Weather or road condition", value: "weather_or_road_condition" },
  { label: "Other", value: "other" },
];

export const cancellationPartyOptions: SelectOption<CancellationParty>[] = [
  { label: "We cancelled", value: "company" },
  { label: "The client cancelled", value: "client" },
];

/** What a crew paid for out of an operational fund - the rows of Monday's liquidation board. */
export const cashExpenseCategoryOptions: SelectOption<CashExpenseCategory>[] = [
  { label: "Passway / entry fee", value: "passway" },
  { label: "Parking", value: "parking" },
  { label: "Toll", value: "toll" },
  { label: "Courier", value: "courier" },
  { label: "Other", value: "other" },
];

export const billingBatchStatusOptions: SelectOption<BillingBatchStatus>[] = [
  { label: "Draft", value: "draft" },
  { label: "Locked", value: "locked" },
  { label: "Forwarded", value: "forwarded" },
  { label: "Billed", value: "billed" },
  { label: "Cancelled", value: "cancelled" },
];

export const billingChargeKindOptions: SelectOption<BillingChargeKind>[] = [
  { label: "Demurrage", value: "demurrage" },
  { label: "Backload", value: "backload" },
  { label: "Different location", value: "different_location" },
  { label: "Tolls and parking", value: "tolls_and_parking" },
  { label: "Other", value: "other" },
];

/** How often a client is billed: its billing batch cut-off. */
export const billingCycleOptions: SelectOption<BillingCycle>[] = [
  { label: "Twice a month (1-15, 16-end)", value: "semi_monthly" },
  { label: "Weekly (Monday-Saturday)", value: "weekly" },
  { label: "Per trip", value: "per_trip" },
  { label: "Monthly", value: "monthly" },
];

/** What went wrong, in the order Cold Chain's weekly report logs them most. */
export const incidentKindOptions: SelectOption<IncidentKind>[] = [
  { label: "Accident", value: "accident" },
  { label: "Breakdown", value: "breakdown" },
  { label: "Backload (goods brought back)", value: "backload" },
  { label: "Delay (late, long waiting)", value: "delay" },
  { label: "Cancellation (booking or trip)", value: "cancellation" },
  { label: "No truck or crew", value: "resource_shortage" },
  { label: "Product damage or quality", value: "product_damage" },
  { label: "Loading error (short, wrong items, wrong serve)", value: "loading_error" },
  { label: "Wrong drop", value: "wrong_drop" },
  { label: "Documents (missing or wrong)", value: "documents" },
  { label: "Temperature excursion", value: "temperature_excursion" },
  { label: "Traffic violation", value: "traffic_violation" },
  { label: "Theft or missing goods", value: "theft" },
  { label: "Other", value: "other" },
];

export const incidentSeverityOptions: SelectOption<IncidentSeverity>[] = [
  { label: "Minor - no injuries, small loss", value: "minor" },
  { label: "Moderate - minor injuries or a real loss", value: "moderate" },
  { label: "Severe - serious injuries or a major loss", value: "severe" },
  { label: "Fatal", value: "fatal" },
];

export const incidentStatusOptions: SelectOption<IncidentStatus>[] = [
  { label: "Open", value: "open" },
  { label: "Under review", value: "under_review" },
  { label: "Resolved", value: "resolved" },
  { label: "Closed", value: "closed" },
];

export const accidentTypeOptions: SelectOption<AccidentType>[] = [
  { label: "Collision", value: "collision" },
  { label: "Rear-end", value: "rear_end" },
  { label: "Sideswipe", value: "sideswipe" },
  { label: "Head-on", value: "head_on" },
  { label: "Hit property (gate, post, CCTV)", value: "property_damage_only" },
  { label: "Hit and run", value: "hit_and_run" },
  { label: "Pedestrian involved", value: "pedestrian_involved" },
  { label: "Ran off the road", value: "run_off_road" },
  { label: "Rollover", value: "rollover" },
  { label: "Jackknife", value: "jackknife" },
  { label: "Cargo spill", value: "cargo_spill" },
  { label: "Other", value: "other" },
];

export const cashFundStatusOptions: SelectOption<CashFundStatus>[] = [
  { label: "Open", value: "open" },
  { label: "Liquidated", value: "liquidated" },
];

/** The fleet cards Skybest's trucks fill up on; each bills its own monthly statement. */
export const fleetCardOptions: SelectOption<FleetCard>[] = [
  { label: "Shell", value: "shell" },
  { label: "Petron", value: "petron" },
  { label: "Unioil", value: "unioil" },
];

/** A fill-up's review, in the words of Monday's Fleet Gas board. */
export const fuelReviewStatusOptions: SelectOption<ExpenseStatus>[] = [
  { label: "For review", value: "pending" },
  { label: "Verified", value: "approved" },
  { label: "Flagged", value: "rejected" },
];

export const truckOwnershipOptions: SelectOption<TruckOwnership>[] = [
  { label: "Owned", value: "owned" },
  { label: "Leased", value: "leased" },
  { label: "Subcontracted", value: "subcontracted" },
];

export const vehiclePermitTypeOptions: SelectOption<VehiclePermitType>[] = [
  { label: "LTO Registration", value: "lto_registration" },
  { label: "LTFRB Franchise", value: "ltfrb_franchise" },
  { label: "Insurance", value: "insurance" },
  { label: "NMIS Accreditation", value: "nmis_accreditation" },
  { label: "Other", value: "other" },
];

export const truckDayStatusOptions: SelectOption<TruckDayStatus>[] = [
  { label: "Available", value: "available" },
  { label: "Buffer unit", value: "buffer" },
  { label: "Understaffed", value: "understaffed" },
  { label: "For repair", value: "for_repair" },
  { label: "Under repair", value: "under_repair" },
  { label: "Maintenance (PMS, change oil)", value: "maintenance" },
  { label: "Stuck", value: "stuck" },
  { label: "For transfer", value: "for_transfer" },
  { label: "Other", value: "other" },
];

export const crewDayStatusOptions: SelectOption<CrewDayStatus>[] = [
  { label: "Available", value: "available" },
  { label: "Reserve", value: "reserve" },
  { label: "Absent", value: "absent" },
  { label: "On leave", value: "on_leave" },
  { label: "Suspended", value: "suspended" },
  { label: "Training / seminar", value: "training" },
  { label: "Lent out", value: "lent_out" },
];

export const employmentTypeOptions: SelectOption<EmploymentType>[] = [
  { label: "Regular", value: "regular" },
  { label: "On-call", value: "on_call" },
];

export const crewUnitOptions: SelectOption<CrewUnit>[] = [
  { label: "Cold Chain", value: "cold_chain" },
  { label: "Wing van", value: "wing_van" },
  { label: "Trucking", value: "trucking" },
];

export const truckStatusOptions: SelectOption<TruckStatus>[] = [
  { label: "Available", value: "available" },
  { label: "En Route", value: "en_route" },
  { label: "Loading", value: "loading" },
  { label: "Unloading", value: "unloading" },
  { label: "Maintenance", value: "maintenance" },
  { label: "Out of Service", value: "out_of_service" },
  { label: "Offline", value: "offline" },
];

export const loadStatusOptions: SelectOption<LoadStatus>[] = [
  { label: "Draft", value: "draft" },
  { label: "Dispatched", value: "dispatched" },
  { label: "Picked Up", value: "picked_up" },
  { label: "Delivered", value: "delivered" },
  { label: "Cancelled", value: "cancelled" },
  { label: "Foul Trip", value: "foul_trip" },
  { label: "Cancelled by Client", value: "cancelled_by_client" },
];

/** The outcomes a drop can depart with - the resolved statuses a dispatcher actually records. */
export const dropOutcomeOptions: SelectOption<LoadStatus>[] = [
  { label: "Delivered", value: "delivered" as LoadStatus },
  { label: "Foul Trip", value: "foul_trip" as LoadStatus },
  { label: "Cancelled by Client", value: "cancelled_by_client" as LoadStatus },
];

export const dropReturnReasonOptions: SelectOption<DropReturnReason>[] = [
  { label: "Expired", value: "expired" as DropReturnReason },
  { label: "Wrong PO / Barcode", value: "wrong_po_or_barcode" as DropReturnReason },
  { label: "Over Stock", value: "over_stock" as DropReturnReason },
  { label: "Full Storage", value: "full_storage" as DropReturnReason },
  { label: "Damaged", value: "damaged" as DropReturnReason },
  { label: "Wrong Address", value: "wrong_address" as DropReturnReason },
  { label: "Other", value: "other" as DropReturnReason },
];

export const tripStatusOptions: SelectOption<TripStatus>[] = [
  { label: "Draft", value: "draft" },
  { label: "Dispatched", value: "dispatched" },
  { label: "In Transit", value: "in_transit" },
  { label: "Completed", value: "completed" },
  { label: "Cancelled", value: "cancelled" },
];

export const invoiceStatusOptions: SelectOption<InvoiceStatus>[] = [
  { label: "Draft", value: "draft" },
  { label: "Pending Approval", value: "pending_approval" },
  { label: "Approved", value: "approved" },
  { label: "Rejected", value: "rejected" },
  { label: "Issued", value: "issued" },
  { label: "Partially Paid", value: "partially_paid" },
  { label: "Paid", value: "paid" },
  { label: "Cancelled", value: "cancelled" },
];

export const paymentStatusOptions: SelectOption<PaymentStatus>[] = [
  { label: "Pending", value: "pending" },
  { label: "Paid", value: "paid" },
  { label: "Failed", value: "failed" },
  { label: "Cancelled", value: "cancelled" },
  { label: "Refunded", value: "refunded" },
  { label: "Partially Refunded", value: "partially_refunded" },
];

export const subscriptionStatusOptions: SelectOption<SubscriptionStatus>[] = [
  { label: "Active", value: "active" },
  { label: "Incomplete", value: "incomplete" },
  { label: "Incomplete Expired", value: "incomplete_expired" },
  { label: "Trialing", value: "trialing" },
  { label: "Past Due", value: "past_due" },
  { label: "Cancelled", value: "cancelled" },
  { label: "Unpaid", value: "unpaid" },
  { label: "Paused", value: "paused" },
];

export const timeEntryTypeOptions: SelectOption<TimeEntryType>[] = [
  { label: "Regular", value: "regular" },
  { label: "Overtime", value: "overtime" },
  { label: "Double Time", value: "double_time" },
  { label: "Paid Time Off", value: "paid_time_off" },
  { label: "Holiday", value: "holiday" },
];

export const invoiceLineItemTypeOptions: SelectOption<InvoiceLineItemType>[] = [
  { label: "Base Rate", value: "base_rate" },
  { label: "Fuel Surcharge", value: "fuel_surcharge" },
  { label: "Detention", value: "detention" },
  { label: "Layover", value: "layover" },
  { label: "Lumper", value: "lumper" },
  { label: "Accessorial", value: "accessorial" },
  { label: "Discount", value: "discount" },
  { label: "Tax", value: "tax" },
  { label: "Other", value: "other" },
];

export const payrollLineItemTypeOptions: SelectOption<InvoiceLineItemType>[] = [
  { label: "Base Pay", value: "base_pay" },
  { label: "Bonus", value: "bonus" },
  { label: "Deduction", value: "deduction" },
  { label: "Reimbursement", value: "reimbursement" },
  { label: "Adjustment", value: "adjustment" },
];

export const loadExceptionTypeOptions: SelectOption<LoadExceptionType>[] = [
  { label: "Delay", value: "delay" },
  { label: "Accident", value: "accident" },
  { label: "Weather Delay", value: "weather_delay" },
  { label: "Mechanical Failure", value: "mechanical_failure" },
  { label: "Route Change", value: "route_change" },
  { label: "Customer Request", value: "customer_request" },
  { label: "Other", value: "other" },
];

export const loadSourceOptions: SelectOption<LoadSource>[] = [
  { label: "Manual", value: "manual" },
  { label: "Email", value: "email" },
  { label: "PDF", value: "pdf" },
  { label: "API", value: "api" },
  { label: "Load Board", value: "load_board" },
];

export const containerStatusOptions: SelectOption<ContainerStatus>[] = [
  { label: "Empty", value: "empty" },
  { label: "Loaded", value: "loaded" },
  { label: "At Port", value: "at_port" },
  { label: "In Transit", value: "in_transit" },
  { label: "Delivered", value: "delivered" },
  { label: "Returned", value: "returned" },
];

export const containerTypeCategoryOptions: SelectOption<ContainerTypeCategory>[] = [
  { label: "General Purpose", value: "general_purpose" },
  { label: "Reefer", value: "reefer" },
  { label: "Open Top", value: "open_top" },
  { label: "Flat Rack", value: "flat_rack" },
  { label: "Tank", value: "tank" },
  { label: "Bulk", value: "bulk" },
];

export const terminalTypeOptions: SelectOption<TerminalType>[] = [
  { label: "Sea Port", value: "sea_port" },
  { label: "Rail Terminal", value: "rail_terminal" },
  { label: "Inland Depot", value: "inland_depot" },
  { label: "Air Cargo", value: "air_cargo" },
  { label: "Border Crossing", value: "border_crossing" },
];

export const vesselTypeOptions: SelectOption<VesselType>[] = [
  { label: "Container Ship", value: "container_ship" },
  { label: "Bulk Carrier", value: "bulk_carrier" },
  { label: "Tanker", value: "tanker" },
  { label: "Ro-Ro", value: "ro_ro" },
  { label: "General Cargo", value: "general_cargo" },
  { label: "Barge", value: "barge" },
  { label: "Feeder", value: "feeder" },
  { label: "Reefer", value: "reefer" },
];

export const gensetStatusOptions: SelectOption<GensetStatus>[] = [
  { label: "Available", value: "available" },
  { label: "In Use", value: "in_use" },
  { label: "Under Maintenance", value: "under_maintenance" },
  { label: "Out of Service", value: "out_of_service" },
];

export const gensetBillingTypeOptions: SelectOption<GensetBillingType>[] = [
  { label: "Itemized (rate x hours)", value: "itemized" },
  { label: "Profit Share (flat amount)", value: "profit_share" },
];

export const trailerTypeOptions: SelectOption<TrailerType>[] = [
  { label: "Dry Van", value: "dry_van" },
  { label: "Reefer", value: "reefer" },
  { label: "Flatbed", value: "flatbed" },
  { label: "Low Boy", value: "low_boy" },
  { label: "Tanker", value: "tanker" },
  { label: "Container Chassis", value: "container_chassis" },
  { label: "Other", value: "other" },
];

export const locationTypeOptions: SelectOption<LocationType>[] = [
  { label: "Delivery Point", value: "delivery_point" },
  { label: "Zone", value: "zone" },
  { label: "Warehouse", value: "warehouse" },
  { label: "Container Yard", value: "container_yard" },
  { label: "Depot", value: "depot" },
  { label: "Plant", value: "plant" },
  { label: "Truck Yard", value: "truck_yard" },
];

export const regionOptions: SelectOption<Region>[] = [
  { label: "United States", value: "us" },
  { label: "Europe", value: "eu" },
  { label: "Philippines", value: "ph" },
];

export const operatingModeOptions: SelectOption<OperatingMode>[] = [
  { label: "Fleet", value: "fleet" },
  { label: "Owner-operator (solo)", value: "solo_operator" },
];

export const licenseClassOptions: SelectOption<LicenseClass>[] = [
  { label: "US CDL Class A", value: "us_class_a" },
  { label: "US CDL Class B", value: "us_class_b" },
  { label: "US CDL Class C", value: "us_class_c" },
  { label: "EU Category C", value: "euc" },
  { label: "EU Category C+E", value: "euce" },
  { label: "EU Category D", value: "eud" },
  { label: "EU Category D+E", value: "eude" },
  { label: "EU Category C1", value: "euc1" },
  { label: "EU Category C1+E", value: "euc1_e" },
  { label: "PH Professional", value: "ph_professional" },
  { label: "PH Non-Professional", value: "ph_non_professional" },
];

/** Which issuing countries a license class belongs to - mirrors `LicenseClass.IsValidForCountry`. */
export function licenseClassesForCountry(country: string): SelectOption<LicenseClass>[] {
  if (country === "US") {
    return licenseClassOptions.filter((o) => o.value.startsWith("us_"));
  }
  if (country === "PH") {
    return licenseClassOptions.filter((o) => o.value.startsWith("ph_"));
  }
  return licenseClassOptions.filter((o) => o.value.startsWith("eu"));
}

export const philippineDlCodeOptions: SelectOption<PhilippineDlCode>[] = [
  { label: "A - Motorcycle", value: "a" },
  { label: "A1 - Tricycle", value: "a1" },
  { label: "B - Car, up to 5,000 kg GVW, up to 8 seats", value: "b" },
  { label: "B1 - Van, up to 5,000 kg GVW, 9+ seats", value: "b1" },
  { label: "B2 - Goods vehicle up to 3,500 kg GVW", value: "b2" },
  { label: "C - Goods vehicle over 3,500 kg GVW", value: "c" },
  { label: "D - Bus over 5,000 kg GVW, 9+ seats", value: "d" },
  { label: "BE - B/B1/B2 with trailer over 750 kg", value: "be" },
  { label: "CE - C with trailer over 750 kg (articulated)", value: "ce" },
];

export const driverLicenseStatusOptions: SelectOption<DriverLicenseStatus>[] = [
  { label: "Active", value: "active" },
  { label: "Expired", value: "expired" },
  { label: "Suspended", value: "suspended" },
  { label: "Revoked", value: "revoked" },
];

export const hazmatClassOptions: SelectOption<HazmatClass>[] = [
  { label: "Class 1 - Explosives", value: "class1" },
  { label: "Class 2 - Gases", value: "class2" },
  { label: "Class 3 - Flammable Liquids", value: "class3" },
  { label: "Class 4 - Flammable Solids", value: "class4" },
  { label: "Class 5 - Oxidizers", value: "class5" },
  { label: "Class 6 - Toxic & Infectious", value: "class6" },
  { label: "Class 7 - Radioactive", value: "class7" },
  { label: "Class 8 - Corrosives", value: "class8" },
  { label: "Class 9 - Miscellaneous", value: "class9" },
];

export const licenseEndorsementOptions: {
  label: string;
  value: LicenseEndorsement;
  group: "us" | "eu";
}[] = [
  { label: "Hazmat (H)", value: "hazmat", group: "us" },
  { label: "Tanker (N)", value: "tanker", group: "us" },
  { label: "Doubles/Triples (T)", value: "doubles", group: "us" },
  { label: "Passenger (P)", value: "passenger", group: "us" },
  { label: "ADR (Basic)", value: "adr", group: "eu" },
  { label: "ADR Tanks", value: "adr_tanks", group: "eu" },
  { label: "ADR Class 1 (Explosives)", value: "adr_class1", group: "eu" },
  { label: "ADR Class 7 (Radioactive)", value: "adr_class7", group: "eu" },
];

export const fuelAdjustmentMethodOptions: SelectOption<FuelAdjustmentMethod>[] = [
  { label: "None (fuel included)", value: "none" },
  { label: "Surcharge per diesel price tier", value: "diesel_tier_surcharge" },
  { label: "Percentage of the rate", value: "percentage" },
];

export const rateAgreementStatusOptions: SelectOption<RateAgreementStatus>[] = [
  { label: "Draft", value: "draft" },
  { label: "Active", value: "active" },
  { label: "Superseded", value: "superseded" },
  { label: "Withdrawn", value: "withdrawn" },
];

/** Repair and Reefer PMS first: they're most of what Cold Chain logs. */
export const maintenanceTypeOptions: SelectOption<MaintenanceType>[] = [
  { label: "Repair", value: "repair" },
  { label: "Reefer PMS", value: "reefer_pms" },
  { label: "Preventive Maintenance", value: "preventive_maintenance" },
  { label: "Oil Change", value: "oil_change" },
  { label: "Battery", value: "battery" },
  { label: "Tire Replacement", value: "tire_replacement" },
  { label: "Tire Rotation", value: "tire_rotation" },
  { label: "Brake Inspection", value: "brake_inspection" },
  { label: "Brake Replacement", value: "brake_replacement" },
  { label: "Air Filter Replacement", value: "air_filter_replacement" },
  { label: "Fuel Filter Replacement", value: "fuel_filter_replacement" },
  { label: "Transmission Service", value: "transmission_service" },
  { label: "Coolant Flush", value: "coolant_flush" },
  { label: "Belt Inspection", value: "belt_inspection" },
  { label: "Engine Service", value: "engine_service" },
  { label: "Suspension Service", value: "suspension_service" },
  { label: "Electrical Repair", value: "electrical_repair" },
  { label: "Body Work", value: "body_work" },
  { label: "HVAC Service", value: "hvac_service" },
  { label: "Exhaust System", value: "exhaust_system" },
  { label: "Steering Repair", value: "steering_repair" },
  { label: "Annual DOT Inspection", value: "annual_dot_inspection" },
  { label: "Other", value: "other" },
];

/** The part of the truck a service was on - the "System" column of Motorpool's repair requisitions. */
export const vehicleSystemOptions: SelectOption<VehicleSystem>[] = [
  { label: "Engine", value: "engine" },
  { label: "Drivetrain (clutch, gearbox)", value: "drivetrain" },
  { label: "Brakes", value: "brakes" },
  { label: "Suspension", value: "suspension" },
  { label: "Steering", value: "steering" },
  { label: "Electrical", value: "electrical" },
  { label: "Cooling (radiator)", value: "cooling" },
  { label: "Reefer unit", value: "reefer" },
  { label: "Tires", value: "tires" },
  { label: "Body", value: "body" },
  { label: "Other", value: "other" },
];

export const maintenanceIntervalTypeOptions: SelectOption<MaintenanceIntervalType>[] = [
  { label: "Every so many days", value: "time_based" },
  { label: "Every so many km / mi", value: "mileage" },
  { label: "Every so many engine hours", value: "engine_hours" },
  { label: "Whichever comes first", value: "combined" },
];
