package com.trackwise.driver.ui.i18n

import androidx.compose.runtime.staticCompositionLocalOf
import com.trackwise.driver.api.models.DefectSeverity
import com.trackwise.driver.api.models.DropReturnReason
import com.trackwise.driver.api.models.DvirType
import com.trackwise.driver.model.Language
import com.trackwise.driver.model.TruckPart

/**
 * Every word the crew sees on the trip flow, truck check and photos, in each language the app
 * speaks. Crews read these on the road, so keep them short and in the words they use themselves:
 * no DVIR, no POD, no "outcome".
 */
interface Strings {
    val ok: String
    val cancel: String
    val save: String
    val saving: String
    val back: String
    val refresh: String
    val notSaved: String
    val offlineTitle: String
    val offlineBody: String
    fun waitingTaps(count: Int): String
    val unsentTitle: String
    val unsentBody: String
    fun logoutWaiting(count: Int): String

    val navToday: String
    val navMessages: String
    val navMe: String

    val noTripTitle: String
    val noTripBody: String
    val finishedTitle: String
    val finishedBody: String
    fun callTime(time: String): String
    fun helper(names: String): String
    fun step(n: Int, total: Int): String
    val warehouse: String
    fun drop(n: Int): String
    fun boxes(n: Int): String
    fun bags(n: Int): String
    fun cutOff(time: String): String
    val beforeLeaving: String
    val truckCheckTitle: String
    val truckCheckHint: String
    val truckCheckButton: String
    val truckCheckOptional: String
    val truckCheckDone: String
    val readyTitle: String
    fun firstStop(place: String): String
    val leaveGarage: String
    val headingTo: String
    val openMaps: String
    val arrive: String
    /** The buzz when GPS sees the truck reach a stop; a blank [place] is a warehouse. */
    fun nudgeTitle(place: String): String
    val nudgeBody: String
    val nudgeYes: String
    val nudgeChannel: String
    fun atPlace(place: String): String
    val startLoading: String
    val doneLoading: String
    val startUnloading: String
    val doneUnloading: String
    val leaveStop: String
    val skipHandling: String
    val lastStep: String
    val goingHome: String
    val goingHomeHint: String
    val backAtGarage: String
    val postTripCheck: String
    val photoLoading: String
    val photoDelivery: String
    fun photoCount(n: Int): String
    val undo: String
    val otherStop: String
    val chooseStop: String

    // What's on record, in the past tense
    val leftGarage: String
    val arrived: String
    val startedLoading: String
    val finishedLoading: String
    val startedUnloading: String
    val finishedUnloading: String
    val departed: String
    val returnedToGarage: String

    val locationOffTitle: String
    val locationOffBody: String
    val locationOffButton: String
    val locationBlockedBody: String
    val locationSettingsButton: String

    val whenNow: String
    fun whenGps(time: String): String
    val whenEarlier: String
    fun tooOld(hours: Long): String
    val tempQuestion: String
    val tempHint: String
    val tempSkip: String
    val outcomeQuestion: String
    val outcomeDelivered: String
    val outcomeSomeReturned: String
    val outcomeFoul: String
    val outcomeCancelled: String
    val returnedBoxes: String
    val returnedBags: String
    val returnReason: String
    fun reason(reason: DropReturnReason): String
    val returnsMissing: String
    val noteLabel: String
    val saveButton: String

    val preTripTitle: String
    val postTripTitle: String
    val inspectionType: String
    val typePreTrip: String
    val typePostTrip: String
    val truck: String
    val selectTruck: String
    val odometerTitle: String
    val odometerLabel: String
    val defectsTitle: String
    val addDefect: String
    val noDefects: String
    val reportDefect: String
    val defectPart: String
    val defectDescription: String
    val defectSeverity: String
    val severityMinor: String
    val severityMajor: String
    val severityOutOfService: String
    val add: String
    val remove: String
    val submitCheck: String
    val nothingBroken: String
    val somethingBroken: String
    val tapEveryBrokenPart: String
    val canStillDrive: String
    val driveOk: String
    val driveNo: String
    val photoOfDefect: String
    val whatIsBroken: String
    val moreOptional: String
    fun part(part: TruckPart): String

    // Photos, signature, notes
    val photos: String
    val takePhoto: String
    val gallery: String
    val noPhotosYet: String
    val signature: String
    val signHere: String
    val clear: String
    val confirm: String
    val signatureCaptured: String
    val recipient: String
    val notes: String
    val submitPhotos: String

    val meTitle: String
    val language: String
    val pastTrips: String
    val pastLoads: String
    val myLicenses: String
    val settings: String
    val privacy: String
    val logout: String
    val logoutQuestion: String
    val logoutBody: String

    val disclosureTitle: String
    val disclosureHeadline: String
    val disclosureIntro: String
    val disclosureWhatTitle: String
    val disclosureWhatBody: String
    val disclosureWhenTitle: String
    val disclosureWhenBody: String
    val disclosureWhoTitle: String
    val disclosureWhoBody: String
    val disclosureHowTitle: String
    val disclosureHowBody: String
    val disclosureContinue: String
}

private fun plural(n: Int, one: String, many: String) = if (n == 1) one else many

object TaglishStrings : Strings {
    override val ok = "OK"
    override val cancel = "Huwag na"
    override val save = "I-save"
    override val saving = "Sine-save…"
    override val back = "Bumalik"
    override val refresh = "I-refresh"
    override val notSaved = "Hindi na-save. Tingnan ang signal at pindutin ulit."
    override val offlineTitle = "Walang signal"
    override val offlineBody = "Tuloy lang ang pindot. Ipapadala ng app pag may signal na."
    override fun waitingTaps(count: Int) = "$count pindot ang naka-save sa phone, hindi pa naipapadala."
    override val unsentTitle = "Hindi naipadala"
    override val unsentBody = "Sabihan ang office para sila ang maglagay nito:"
    override fun logoutWaiting(count: Int) =
        "May $count pindot pa na hindi naipapadala. Mawawala ito kapag nag-logout. Hintayin muna ang signal."

    override val navToday = "Ngayon"
    override val navMessages = "Mensahe"
    override val navMe = "Ako"

    override val noTripTitle = "Wala ka pang biyahe ngayon"
    override val noTripBody = "Lalabas dito pag naipadala na ng dispatch."
    override val finishedTitle = "Tapos na ang biyahe!"
    override val finishedBody = "Salamat. Lalabas dito ang susunod mong biyahe pag naipadala na ng dispatch."
    override fun callTime(time: String) = "Call time $time"
    override fun helper(names: String) = "Kasama: $names"
    override fun step(n: Int, total: Int) = "Hakbang $n sa $total"
    override val warehouse = "Bodega"
    override fun drop(n: Int) = "Drop $n"
    override fun boxes(n: Int) = "$n kahon"
    override fun bags(n: Int) = "$n bag"
    override fun cutOff(time: String) = "Hanggang $time lang"
    override val beforeLeaving = "Bago umalis"
    override val truckCheckTitle = "Check muna ang truck"
    override val truckCheckHint = "Gulong, ilaw, preno, reefer."
    override val truckCheckButton = "CHECK ANG TRUCK"
    override val truckCheckOptional = "Puwedeng laktawan"
    override val truckCheckDone = "OK ang truck"
    override val readyTitle = "Handa na? Aalis na sa garahe."
    override fun firstStop(place: String) = "Unang puntahan: $place"
    override val leaveGarage = "ALIS NA"
    override val headingTo = "Papunta sa"
    override val openMaps = "Buksan sa Maps"
    override val arrive = "NANDITO NA"
    override fun nudgeTitle(place: String) = "Nandito ka na ba sa ${place.ifBlank { "bodega" }}?"
    override val nudgeBody = "Pindutin ang Oo para i-record ang dating mo."
    override val nudgeYes = "Oo, nandito na"
    override val nudgeChannel = "Pagdating sa stop"
    override fun atPlace(place: String) = "Nasa $place"
    override val startLoading = "SIMULAN ANG PAGKARGA"
    override val doneLoading = "TAPOS NA ANG PAGKARGA"
    override val startUnloading = "SIMULAN ANG PAGBABA"
    override val doneUnloading = "TAPOS NA ANG PAGBABA"
    override val leaveStop = "AALIS NA"
    override val skipHandling = "Aalis na agad"
    override val lastStep = "Huling hakbang"
    override val goingHome = "Pauwi na sa garahe"
    override val goingHomeHint = "Pindutin pagdating sa garahe."
    override val backAtGarage = "NASA GARAHE NA"
    override val postTripCheck = "Check ng truck pag-uwi"
    override val photoLoading = "Picture ng karga"
    override val photoDelivery = "Picture ng pinirmahang DR"
    override fun photoCount(n: Int) = if (n == 0) "Wala pang picture" else "$n picture"
    override val undo = "Mali? I-undo"
    override val otherStop = "Iba ang pupuntahan?"
    override val chooseStop = "Saan ka pupunta?"

    override val leftGarage = "Umalis sa garahe"
    override val arrived = "Dumating"
    override val startedLoading = "Nagsimula magkarga"
    override val finishedLoading = "Tapos magkarga"
    override val startedUnloading = "Nagsimula magbaba"
    override val finishedUnloading = "Tapos magbaba"
    override val departed = "Umalis"
    override val returnedToGarage = "Nasa garahe na"

    override val locationOffTitle = "Naka-off ang location"
    override val locationOffBody = "Kailangan ito ng office para makita kung nasaan ang truck."
    override val locationOffButton = "BUKSAN ANG LOCATION"
    override val locationBlockedBody = "Naka-block ang location sa phone na ito. Pindutin ang button, tapos: Permissions → Location → Allow."
    override val locationSettingsButton = "BUKSAN ANG SETTINGS"

    override val whenNow = "Ngayon lang"
    override fun whenGps(time: String) = "GPS: $time"
    override val whenEarlier = "Kanina pa…"
    override fun tooOld(hours: Long) = "Lagpas na $hours oras. Sabihan ang office, sila na ang maglalagay."
    override val tempQuestion = "Ilang degrees ang reefer?"
    override val tempHint = "Pindutin ang − o +, o pumili sa baba."
    override val tempSkip = "Laktawan"
    override val outcomeQuestion = "Kumusta ang delivery?"
    override val outcomeDelivered = "Natanggap lahat"
    override val outcomeSomeReturned = "May ibinalik"
    override val outcomeFoul = "Hindi nakapagbaba (Foul)"
    override val outcomeCancelled = "Kinansela ng client"
    override val returnedBoxes = "Ilang kahon ang ibinalik?"
    override val returnedBags = "Ilang bag?"
    override val returnReason = "Bakit ibinalik?"
    override fun reason(reason: DropReturnReason) = when (reason) {
        DropReturnReason.EXPIRED -> "Expired na"
        DropReturnReason.WRONG_PO_OR_BARCODE -> "Mali ang PO / barcode"
        DropReturnReason.OVER_STOCK -> "Sobra na ang stock nila"
        DropReturnReason.FULL_STORAGE -> "Puno ang bodega"
        DropReturnReason.DAMAGED -> "Sira / damaged"
        DropReturnReason.WRONG_ADDRESS -> "Maling address"
        DropReturnReason.OTHER -> "Iba pa"
    }
    override val returnsMissing = "Ilagay kung ilan ang ibinalik at bakit."
    override val noteLabel = "May gustong idagdag?"
    override val saveButton = "I-SAVE"

    override val preTripTitle = "Check ng truck bago umalis"
    override val postTripTitle = "Check ng truck pag-uwi"
    override val inspectionType = "Kailan ang check?"
    override val typePreTrip = "Bago umalis"
    override val typePostTrip = "Pag-uwi"
    override val truck = "Truck"
    override val selectTruck = "Pumili ng truck"
    override val odometerTitle = "Odometer"
    override val odometerLabel = "Ilang km sa odometer?"
    override val defectsTitle = "May sira ba ang truck?"
    override val addDefect = "May sira, ilagay"
    override val noDefects = "Walang sira? Pumirma na lang sa baba."
    override val reportDefect = "Ano ang sira?"
    override val defectPart = "Saang parte?"
    override val defectDescription = "Ano ang nangyari?"
    override val defectSeverity = "Gaano kalala?"
    override val severityMinor = "Maliit lang"
    override val severityMajor = "Malala"
    override val severityOutOfService = "Huwag ibiyahe"
    override val add = "Idagdag"
    override val remove = "Alisin"
    override val submitCheck = "I-SAVE ANG CHECK"
    override val nothingBroken = "Wala"
    override val somethingBroken = "Meron"
    override val tapEveryBrokenPart = "Pindutin lahat ng may sira."
    override val canStillDrive = "Puwede pa bang ibiyahe?"
    override val driveOk = "Oo, puwede pa"
    override val driveNo = "Hindi, delikado"
    override val photoOfDefect = "Kunan ng litrato ang sira"
    override val whatIsBroken = "Ano ang sira? (puwedeng wala)"
    override val moreOptional = "Iba pa (puwedeng laktawan)"
    override fun part(part: TruckPart) = when (part) {
        TruckPart.TIRES -> "Gulong"
        TruckPart.BRAKES -> "Preno"
        TruckPart.HEADLIGHTS -> "Headlight"
        TruckPart.REAR_LIGHTS -> "Ilaw sa likod"
        TruckPart.ENGINE -> "Makina"
        TruckPart.OIL_WATER -> "Langis o tubig"
        TruckPart.BATTERY -> "Battery"
        TruckPart.STEERING -> "Manibela"
        TruckPart.MIRRORS -> "Side mirror"
        TruckPart.WINDSHIELD -> "Salamin sa harap"
        TruckPart.HORN -> "Busina"
        TruckPart.REEFER -> "Reefer"
        TruckPart.OTHER -> "Iba pa"
    }

    override val photos = "Mga picture"
    override val takePhoto = "Kumuha ng picture"
    override val gallery = "Gallery"
    override val noPhotosYet = "Wala pang picture"
    override val signature = "Pirma"
    override val signHere = "Pumirma dito"
    override val clear = "Burahin"
    override val confirm = "OK na"
    override val signatureCaptured = "Napirmahan na"
    override val recipient = "Sino ang tumanggap?"
    override val notes = "Notes (puwedeng wala)"
    override val submitPhotos = "I-SAVE"

    override val meTitle = "Ako"
    override val language = "Wika / Language"
    override val pastTrips = "Mga nakaraang biyahe"
    override val pastLoads = "Mga nakaraang load"
    override val myLicenses = "Mga lisensya ko"
    override val settings = "Settings"
    override val privacy = "Privacy"
    override val logout = "Mag-log out"
    override val logoutQuestion = "Mag-log out?"
    override val logoutBody = "Kailangan mong mag-login ulit para makita ang biyahe mo. Hihinto rin ang pag-share ng location."

    override val disclosureTitle = "Location ng truck"
    override val disclosureHeadline = "Nakikita ng company mo kung nasaan ang truck"
    override val disclosureIntro = "Basahin muna kung paano ginagamit ang location bago tumuloy."
    override val disclosureWhatTitle = "Ano ang nakikita"
    override val disclosureWhatBody = "Ang GPS ng truck at ang address kung nasaan ito. Wala nang iba pang kinukuha sa phone mo."
    override val disclosureWhenTitle = "Kailan"
    override val disclosureWhenBody = "Palagi habang naka-login ka, kahit nakasara ang app o naka-off ang screen. Hihinto lang pag nag-log out ka."
    override val disclosureWhoTitle = "Sino ang nakakakita"
    override val disclosureWhoBody = "Ang dispatch at mga manager ng company mo, at ang client ng biyahe mo ngayon. Hindi ito ibinibigay sa iba."
    override val disclosureHowTitle = "Paano mo malalaman"
    override val disclosureHowBody = "May notification sa itaas ng phone habang naka-share ang location. Hilahin pababa para makita."
    override val disclosureContinue = "Naiintindihan ko, tumuloy"
}

object EnglishStrings : Strings {
    override val ok = "OK"
    override val cancel = "Cancel"
    override val save = "Save"
    override val saving = "Saving…"
    override val back = "Back"
    override val refresh = "Refresh"
    override val notSaved = "That wasn't saved. Check your signal and tap again."
    override val offlineTitle = "No signal"
    override val offlineBody = "Keep tapping. The app sends them when there's signal again."
    override fun waitingTaps(count: Int) = "$count taps saved on the phone, not sent yet."
    override val unsentTitle = "Not sent"
    override val unsentBody = "Tell the office so they can enter these:"
    override fun logoutWaiting(count: Int) =
        "$count taps haven't been sent yet. Logging out deletes them. Wait for signal first."

    override val navToday = "Today"
    override val navMessages = "Messages"
    override val navMe = "Me"

    override val noTripTitle = "No trip yet today"
    override val noTripBody = "It shows here once dispatch sends it."
    override val finishedTitle = "Trip done!"
    override val finishedBody = "Thanks. Your next trip shows here once dispatch sends it."
    override fun callTime(time: String) = "Call time $time"
    override fun helper(names: String) = "With: $names"
    override fun step(n: Int, total: Int) = "Step $n of $total"
    override val warehouse = "Warehouse"
    override fun drop(n: Int) = "Drop $n"
    override fun boxes(n: Int) = "$n ${plural(n, "box", "boxes")}"
    override fun bags(n: Int) = "$n ${plural(n, "bag", "bags")}"
    override fun cutOff(time: String) = "Until $time only"
    override val beforeLeaving = "Before you leave"
    override val truckCheckTitle = "Check the truck first"
    override val truckCheckHint = "Tires, lights, brakes, reefer."
    override val truckCheckButton = "CHECK THE TRUCK"
    override val truckCheckOptional = "You can skip this"
    override val truckCheckDone = "Truck OK"
    override val readyTitle = "Ready? Leave the garage."
    override fun firstStop(place: String) = "First stop: $place"
    override val leaveGarage = "LEAVE NOW"
    override val headingTo = "Heading to"
    override val openMaps = "Open in Maps"
    override val arrive = "I'M HERE"
    override fun nudgeTitle(place: String) = "Are you at ${place.ifBlank { "the warehouse" }}?"
    override val nudgeBody = "Tap Yes to record your arrival."
    override val nudgeYes = "Yes, I'm here"
    override val nudgeChannel = "Arriving at a stop"
    override fun atPlace(place: String) = "At $place"
    override val startLoading = "START LOADING"
    override val doneLoading = "DONE LOADING"
    override val startUnloading = "START UNLOADING"
    override val doneUnloading = "DONE UNLOADING"
    override val leaveStop = "LEAVING NOW"
    override val skipHandling = "Leaving right away"
    override val lastStep = "Last step"
    override val goingHome = "Going back to the garage"
    override val goingHomeHint = "Tap when you reach the garage."
    override val backAtGarage = "BACK AT GARAGE"
    override val postTripCheck = "Truck check after the trip"
    override val photoLoading = "Photo of the load"
    override val photoDelivery = "Photo of the signed DR"
    override fun photoCount(n: Int) = if (n == 0) "No photo yet" else "$n ${plural(n, "photo", "photos")}"
    override val undo = "Wrong? Undo"
    override val otherStop = "Going somewhere else?"
    override val chooseStop = "Where are you going?"

    override val leftGarage = "Left the garage"
    override val arrived = "Arrived"
    override val startedLoading = "Started loading"
    override val finishedLoading = "Done loading"
    override val startedUnloading = "Started unloading"
    override val finishedUnloading = "Done unloading"
    override val departed = "Left"
    override val returnedToGarage = "Back at garage"

    override val locationOffTitle = "Location is off"
    override val locationOffBody = "The office needs it to see where the truck is."
    override val locationOffButton = "TURN ON LOCATION"
    override val locationBlockedBody = "Location is blocked on this phone. Tap the button, then: Permissions → Location → Allow."
    override val locationSettingsButton = "OPEN SETTINGS"

    override val whenNow = "Just now"
    override fun whenGps(time: String) = "GPS: $time"
    override val whenEarlier = "Earlier…"
    override fun tooOld(hours: Long) = "That's more than $hours hours ago. Tell the office, they'll enter it."
    override val tempQuestion = "What does the reefer read?"
    override val tempHint = "Tap − or +, or pick one below."
    override val tempSkip = "Skip"
    override val outcomeQuestion = "How did the delivery go?"
    override val outcomeDelivered = "All received"
    override val outcomeSomeReturned = "Some came back"
    override val outcomeFoul = "Couldn't unload (Foul)"
    override val outcomeCancelled = "Client cancelled"
    override val returnedBoxes = "How many boxes came back?"
    override val returnedBags = "How many bags?"
    override val returnReason = "Why did it come back?"
    override fun reason(reason: DropReturnReason) = when (reason) {
        DropReturnReason.EXPIRED -> "Expired"
        DropReturnReason.WRONG_PO_OR_BARCODE -> "Wrong PO / barcode"
        DropReturnReason.OVER_STOCK -> "Over stock"
        DropReturnReason.FULL_STORAGE -> "Storage full"
        DropReturnReason.DAMAGED -> "Damaged"
        DropReturnReason.WRONG_ADDRESS -> "Wrong address"
        DropReturnReason.OTHER -> "Other"
    }
    override val returnsMissing = "Enter how many came back and why."
    override val noteLabel = "Anything to add?"
    override val saveButton = "SAVE"

    override val preTripTitle = "Truck check before leaving"
    override val postTripTitle = "Truck check after the trip"
    override val inspectionType = "When is this check?"
    override val typePreTrip = "Before leaving"
    override val typePostTrip = "After the trip"
    override val truck = "Truck"
    override val selectTruck = "Pick a truck"
    override val odometerTitle = "Odometer"
    override val odometerLabel = "How many km on the odometer?"
    override val defectsTitle = "Anything broken on the truck?"
    override val addDefect = "Something's broken, add it"
    override val noDefects = "Nothing broken? Just sign below."
    override val reportDefect = "What's broken?"
    override val defectPart = "Which part?"
    override val defectDescription = "What happened?"
    override val defectSeverity = "How bad is it?"
    override val severityMinor = "Small"
    override val severityMajor = "Serious"
    override val severityOutOfService = "Don't drive it"
    override val add = "Add"
    override val remove = "Remove"
    override val submitCheck = "SAVE THE CHECK"
    override val nothingBroken = "No"
    override val somethingBroken = "Yes"
    override val tapEveryBrokenPart = "Tap every part that's broken."
    override val canStillDrive = "Can it still go out?"
    override val driveOk = "Yes, still OK"
    override val driveNo = "No, it's unsafe"
    override val photoOfDefect = "Take a photo of it"
    override val whatIsBroken = "What's broken? (optional)"
    override val moreOptional = "More (optional)"
    override fun part(part: TruckPart) = when (part) {
        TruckPart.TIRES -> "Tires"
        TruckPart.BRAKES -> "Brakes"
        TruckPart.HEADLIGHTS -> "Headlights"
        TruckPart.REAR_LIGHTS -> "Rear lights"
        TruckPart.ENGINE -> "Engine"
        TruckPart.OIL_WATER -> "Oil or water"
        TruckPart.BATTERY -> "Battery"
        TruckPart.STEERING -> "Steering"
        TruckPart.MIRRORS -> "Side mirrors"
        TruckPart.WINDSHIELD -> "Front glass"
        TruckPart.HORN -> "Horn"
        TruckPart.REEFER -> "Reefer"
        TruckPart.OTHER -> "Something else"
    }

    override val photos = "Photos"
    override val takePhoto = "Take a photo"
    override val gallery = "Gallery"
    override val noPhotosYet = "No photo yet"
    override val signature = "Signature"
    override val signHere = "Sign here"
    override val clear = "Clear"
    override val confirm = "Done"
    override val signatureCaptured = "Signed"
    override val recipient = "Who received it?"
    override val notes = "Notes (optional)"
    override val submitPhotos = "SAVE"

    override val meTitle = "Me"
    override val language = "Wika / Language"
    override val pastTrips = "Past trips"
    override val pastLoads = "Past loads"
    override val myLicenses = "My licenses"
    override val settings = "Settings"
    override val privacy = "Privacy"
    override val logout = "Log out"
    override val logoutQuestion = "Log out?"
    override val logoutBody = "You'll need to log in again to see your trips. Location sharing stops too."

    override val disclosureTitle = "Truck location"
    override val disclosureHeadline = "Your company can see where the truck is"
    override val disclosureIntro = "Please read how location is used before you continue."
    override val disclosureWhatTitle = "What is shared"
    override val disclosureWhatBody = "The truck's GPS position and the address it's at. Nothing else from your phone is collected."
    override val disclosureWhenTitle = "When"
    override val disclosureWhenBody = "All the time while you're logged in, even with the app closed or the screen off. It stops only when you log out."
    override val disclosureWhoTitle = "Who sees it"
    override val disclosureWhoBody = "Dispatch and managers at your company, and the client of your current trip. It isn't given to anyone else."
    override val disclosureHowTitle = "How you know"
    override val disclosureHowBody = "A notification stays at the top of your phone while location is shared. Pull it down to check."
    override val disclosureContinue = "I understand, continue"
}

object BisayaStrings : Strings {
    override val ok = "OK"
    override val cancel = "Ayaw na"
    override val save = "I-save"
    override val saving = "Gi-save…"
    override val back = "Balik"
    override val refresh = "I-refresh"
    override val notSaved = "Wala na-save. Tan-awa ang signal ug pindota pag-usab."
    override val offlineTitle = "Walay signal"
    override val offlineBody = "Padayon lang sa pagpindot. Ipadala sa app inig naay signal na."
    override fun waitingTaps(count: Int) = "$count ka pindot ang na-save sa phone, wala pa naipadala."
    override val unsentTitle = "Wala naipadala"
    override val unsentBody = "Sultihi ang opisina aron sila ang mobutang ani:"
    override fun logoutWaiting(count: Int) =
        "Naa pay $count ka pindot nga wala pa naipadala. Mawala kini kung mag-logout. Hulata una ang signal."

    override val navToday = "Karon"
    override val navMessages = "Mensahe"
    override val navMe = "Ako"

    override val noTripTitle = "Wala pa kay biyahe karon"
    override val noTripBody = "Mogawas diri inig padala sa dispatch."
    override val finishedTitle = "Human na ang biyahe!"
    override val finishedBody = "Salamat. Mogawas diri ang sunod nimong biyahe inig padala sa dispatch."
    override fun callTime(time: String) = "Call time $time"
    override fun helper(names: String) = "Kauban: $names"
    override fun step(n: Int, total: Int) = "Lakang $n sa $total"
    override val warehouse = "Bodega"
    override fun drop(n: Int) = "Drop $n"
    override fun boxes(n: Int) = "$n ka kahon"
    override fun bags(n: Int) = "$n ka bag"
    override fun cutOff(time: String) = "Hangtod $time ra"
    override val beforeLeaving = "Sa dili pa mobiya"
    override val truckCheckTitle = "Susiha una ang trak"
    override val truckCheckHint = "Ligid, suga, preno, reefer."
    override val truckCheckButton = "SUSIHA ANG TRAK"
    override val truckCheckOptional = "Puwede laktawan"
    override val truckCheckDone = "OK ang trak"
    override val readyTitle = "Andam na? Mobiya na sa garahe."
    override fun firstStop(place: String) = "Unang adtoan: $place"
    override val leaveGarage = "MOBIYA NA"
    override val headingTo = "Padulong sa"
    override val openMaps = "Ablihi sa Maps"
    override val arrive = "NIABOT NA"
    override fun nudgeTitle(place: String) = "Naa na ka sa ${place.ifBlank { "bodega" }}?"
    override val nudgeBody = "Pindota ang Oo para ma-record ang imong pag-abot."
    override val nudgeYes = "Oo, naa na ko"
    override val nudgeChannel = "Pag-abot sa stop"
    override fun atPlace(place: String) = "Naa sa $place"
    override val startLoading = "SUGDI ANG PAGKARGA"
    override val doneLoading = "HUMAN NA ANG PAGKARGA"
    override val startUnloading = "SUGDI ANG PAGDISKARGA"
    override val doneUnloading = "HUMAN NA ANG PAGDISKARGA"
    override val leaveStop = "MOBIYA NA"
    override val skipHandling = "Mobiya na dayon"
    override val lastStep = "Katapusang lakang"
    override val goingHome = "Pauli na sa garahe"
    override val goingHomeHint = "Pindota inig abot sa garahe."
    override val backAtGarage = "NAA NA SA GARAHE"
    override val postTripCheck = "Susiha ang trak pag-uli"
    override val photoLoading = "Litrato sa karga"
    override val photoDelivery = "Litrato sa napirmahang DR"
    override fun photoCount(n: Int) = if (n == 0) "Wala pay litrato" else "$n ka litrato"
    override val undo = "Sayop? I-undo"
    override val otherStop = "Lahi ang adtoan?"
    override val chooseStop = "Asa ka padulong?"

    override val leftGarage = "Mibiya sa garahe"
    override val arrived = "Niabot"
    override val startedLoading = "Nagsugod pagkarga"
    override val finishedLoading = "Human pagkarga"
    override val startedUnloading = "Nagsugod pagdiskarga"
    override val finishedUnloading = "Human pagdiskarga"
    override val departed = "Mibiya"
    override val returnedToGarage = "Naa na sa garahe"

    override val locationOffTitle = "Naka-off ang location"
    override val locationOffBody = "Kinahanglan kini sa opisina aron makita asa ang trak."
    override val locationOffButton = "ABLIHI ANG LOCATION"
    override val locationBlockedBody = "Naka-block ang location niining phone. Pindota ang button, dayon: Permissions → Location → Allow."
    override val locationSettingsButton = "ABLIHI ANG SETTINGS"

    override val whenNow = "Karon ra"
    override fun whenGps(time: String) = "GPS: $time"
    override val whenEarlier = "Ganina pa…"
    override fun tooOld(hours: Long) = "Lapas na sa $hours ka oras. Sultihi ang opisina, sila na ang mosulod ani."
    override val tempQuestion = "Pila ka degrees ang reefer?"
    override val tempHint = "Pindota ang − o +, o pili sa ubos."
    override val tempSkip = "Laktawan"
    override val outcomeQuestion = "Kumusta ang delivery?"
    override val outcomeDelivered = "Nadawat tanan"
    override val outcomeSomeReturned = "Naay gibalik"
    override val outcomeFoul = "Wala nakadiskarga (Foul)"
    override val outcomeCancelled = "Gikansela sa client"
    override val returnedBoxes = "Pila ka kahon ang gibalik?"
    override val returnedBags = "Pila ka bag?"
    override val returnReason = "Ngano gibalik?"
    override fun reason(reason: DropReturnReason) = when (reason) {
        DropReturnReason.EXPIRED -> "Expired na"
        DropReturnReason.WRONG_PO_OR_BARCODE -> "Sayop ang PO / barcode"
        DropReturnReason.OVER_STOCK -> "Sobra na ang stock nila"
        DropReturnReason.FULL_STORAGE -> "Puno ang bodega"
        DropReturnReason.DAMAGED -> "Guba / damaged"
        DropReturnReason.WRONG_ADDRESS -> "Sayop nga address"
        DropReturnReason.OTHER -> "Uban pa"
    }
    override val returnsMissing = "Ibutang pila ang gibalik ug ngano."
    override val noteLabel = "Naa kay idugang?"
    override val saveButton = "I-SAVE"

    override val preTripTitle = "Susi sa trak sa dili pa mobiya"
    override val postTripTitle = "Susi sa trak pag-uli"
    override val inspectionType = "Kanus-a kini nga susi?"
    override val typePreTrip = "Sa dili pa mobiya"
    override val typePostTrip = "Pag-uli"
    override val truck = "Trak"
    override val selectTruck = "Pili og trak"
    override val odometerTitle = "Odometer"
    override val odometerLabel = "Pila ka km sa odometer?"
    override val defectsTitle = "Naa bay guba ang trak?"
    override val addDefect = "Naay guba, ibutang"
    override val noDefects = "Walay guba? Pirma na lang sa ubos."
    override val reportDefect = "Unsa ang guba?"
    override val defectPart = "Asa nga parte?"
    override val defectDescription = "Unsa ang nahitabo?"
    override val defectSeverity = "Unsa ka grabe?"
    override val severityMinor = "Gamay ra"
    override val severityMajor = "Grabe"
    override val severityOutOfService = "Ayaw ibiyahe"
    override val add = "Idugang"
    override val remove = "Tangtangon"
    override val submitCheck = "I-SAVE ANG SUSI"
    override val nothingBroken = "Wala"
    override val somethingBroken = "Naa"
    override val tapEveryBrokenPart = "Pinduta tanan nga naay guba."
    override val canStillDrive = "Puwede pa ba ibiyahe?"
    override val driveOk = "Oo, puwede pa"
    override val driveNo = "Dili, delikado"
    override val photoOfDefect = "Kuhai og litrato ang guba"
    override val whatIsBroken = "Unsa ang guba? (puwede wala)"
    override val moreOptional = "Uban pa (puwede laktawan)"
    override fun part(part: TruckPart) = when (part) {
        TruckPart.TIRES -> "Ligid"
        TruckPart.BRAKES -> "Preno"
        TruckPart.HEADLIGHTS -> "Headlight"
        TruckPart.REAR_LIGHTS -> "Suga sa luyo"
        TruckPart.ENGINE -> "Makina"
        TruckPart.OIL_WATER -> "Lana o tubig"
        TruckPart.BATTERY -> "Battery"
        TruckPart.STEERING -> "Manibela"
        TruckPart.MIRRORS -> "Side mirror"
        TruckPart.WINDSHIELD -> "Samin sa atubangan"
        TruckPart.HORN -> "Busina"
        TruckPart.REEFER -> "Reefer"
        TruckPart.OTHER -> "Uban pa"
    }

    override val photos = "Mga litrato"
    override val takePhoto = "Kuha og litrato"
    override val gallery = "Gallery"
    override val noPhotosYet = "Wala pay litrato"
    override val signature = "Pirma"
    override val signHere = "Pirma diri"
    override val clear = "Papasa"
    override val confirm = "OK na"
    override val signatureCaptured = "Napirmahan na"
    override val recipient = "Kinsa ang nidawat?"
    override val notes = "Notes (puwede wala)"
    override val submitPhotos = "I-SAVE"

    override val meTitle = "Ako"
    override val language = "Wika / Language"
    override val pastTrips = "Mga nangagi nga biyahe"
    override val pastLoads = "Mga nangagi nga load"
    override val myLicenses = "Akong mga lisensya"
    override val settings = "Settings"
    override val privacy = "Privacy"
    override val logout = "Mag-log out"
    override val logoutQuestion = "Mag-log out?"
    override val logoutBody = "Kinahanglan ka mo-login pag-usab aron makita ang imong biyahe. Mohunong pud ang pag-share sa location."

    override val disclosureTitle = "Location sa trak"
    override val disclosureHeadline = "Makita sa imong kompanya asa ang trak"
    override val disclosureIntro = "Basaha una kung giunsa paggamit ang location sa dili pa mopadayon."
    override val disclosureWhatTitle = "Unsa ang makita"
    override val disclosureWhatBody = "Ang GPS sa trak ug ang address kung asa kini. Wala nay lain nga kuhaon sa imong phone."
    override val disclosureWhenTitle = "Kanus-a"
    override val disclosureWhenBody = "Kanunay samtang naka-login ka, bisan sirado ang app o naka-off ang screen. Mohunong ra kung mag-log out ka."
    override val disclosureWhoTitle = "Kinsa ang makakita"
    override val disclosureWhoBody = "Ang dispatch ug mga manager sa imong kompanya, ug ang client sa imong biyahe karon. Dili kini ihatag sa uban."
    override val disclosureHowTitle = "Unsaon nimo pagkahibalo"
    override val disclosureHowBody = "Naay notification sa ibabaw sa phone samtang gi-share ang location. Biraha paubos aron makita."
    override val disclosureContinue = "Nakasabot ko, padayon"
}

fun Language.strings(): Strings = when (this) {
    Language.TAGLISH -> TaglishStrings
    Language.ENGLISH -> EnglishStrings
    Language.BISAYA -> BisayaStrings
}

/** The crew's chosen language; provided at the app root from their saved setting. */
val LocalStrings = staticCompositionLocalOf<Strings> { TaglishStrings }

fun Strings.dvirType(type: DvirType): String = when (type) {
    DvirType.PRE_TRIP -> typePreTrip
    DvirType.POST_TRIP -> typePostTrip
}

fun Strings.severity(severity: DefectSeverity): String = when (severity) {
    DefectSeverity.MINOR -> severityMinor
    DefectSeverity.MAJOR -> severityMajor
    DefectSeverity.OUT_OF_SERVICE -> severityOutOfService
}
