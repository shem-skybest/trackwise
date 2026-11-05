package com.trackwise.driver.navigation

import androidx.navigation3.runtime.NavEntry
import androidx.navigation3.runtime.NavKey
import androidx.navigation3.runtime.entryProvider
import com.trackwise.driver.api.models.DvirType
import com.trackwise.driver.api.models.InspectionType
import com.trackwise.driver.ui.screens.AboutScreen
import com.trackwise.driver.ui.screens.AccountScreen
import com.trackwise.driver.ui.screens.ConditionReportScreen
import com.trackwise.driver.ui.screens.ConversationScreen
import com.trackwise.driver.ui.screens.DashboardScreen
import com.trackwise.driver.ui.screens.DvirFormScreen
import com.trackwise.driver.ui.screens.EmployeeSelectScreen
import com.trackwise.driver.ui.screens.LoadDetailScreen
import com.trackwise.driver.ui.screens.LocationDisclosureScreen
import com.trackwise.driver.ui.screens.LoginScreen
import com.trackwise.driver.ui.screens.MessagesScreen
import com.trackwise.driver.ui.screens.MyLicensesScreen
import com.trackwise.driver.ui.screens.PastLoadsScreen
import com.trackwise.driver.ui.screens.PodCaptureScreen
import com.trackwise.driver.ui.screens.PrivacyScreen
import com.trackwise.driver.ui.screens.SettingsScreen
import com.trackwise.driver.ui.screens.StatsScreen
import com.trackwise.driver.ui.screens.TripDetailScreen
import com.trackwise.driver.ui.screens.TripsScreen
import com.trackwise.driver.viewmodel.ConditionReportViewModel
import com.trackwise.driver.viewmodel.DocumentCaptureType
import com.trackwise.driver.viewmodel.DvirFormViewModel
import com.trackwise.driver.viewmodel.LoadDetailViewModel
import com.trackwise.driver.viewmodel.PodCaptureViewModel
import com.trackwise.driver.viewmodel.TripDetailViewModel
import org.koin.compose.viewmodel.koinViewModel
import org.koin.core.parameter.parametersOf

/**
 * Creates the entry provider that maps routes to screen composables.
 */
fun createEntryProvider(
    navigator: Navigator,
    onOpenUrl: (String) -> Unit
): (NavKey) -> NavEntry<NavKey> = entryProvider {
    // Login Screen
    entry<LoginRoute> {
        LoginScreen(
            onLoginSuccess = { hasAcceptedDisclosure ->
                if (hasAcceptedDisclosure) {
                    navigator.navigateAndClear(DashboardRoute, LoginRoute)
                } else {
                    navigator.navigateAndClear(LocationDisclosureRoute, LoginRoute)
                }
            }
        )
    }

    // Location Disclosure Screen (shown once before first dashboard load)
    entry<LocationDisclosureRoute> {
        LocationDisclosureScreen(
            onAccepted = {
                navigator.navigateAndClear(DashboardRoute, LocationDisclosureRoute)
            },
            onDeclined = {
                navigator.clearAndNavigate(LoginRoute)
            }
        )
    }

    // Dashboard Screen (Start destination after login)
    entry<DashboardRoute> {
        DashboardScreen(
            onTripClick = { tripId ->
                navigator.navigate(TripDetailRoute(tripId))
            },
            onTruckCheck = { truckId, tripId, type ->
                navigator.navigate(DvirFormRoute(truckId = truckId, tripId = tripId, dvirType = type))
            },
            onStopPhoto = { block ->
                val type = if (block.isDrop) DocumentCaptureType.POD else DocumentCaptureType.BOL
                navigator.navigate(PodCaptureRoute(block.loadId, type, block.stopId))
            },
            onOpenUrl = onOpenUrl
        )
    }

    // Stats Screen
    entry<StatsRoute> {
        StatsScreen()
    }

    // Past Loads Screen
    entry<PastLoadsRoute> {
        PastLoadsScreen(
            onLoadClick = { loadId ->
                navigator.navigate(LoadDetailRoute(loadId))
            },
            onBack = { navigator.goBack() }
        )
    }

    // Trips Screen
    entry<TripsRoute> {
        TripsScreen(
            onTripClick = { tripId ->
                navigator.navigate(TripDetailRoute(tripId))
            },
            onBack = { navigator.goBack() }
        )
    }

    // Trip Detail Screen
    entry<TripDetailRoute> { key ->
        val viewModel: TripDetailViewModel = koinViewModel { parametersOf(key.tripId) }

        TripDetailScreen(
            onNavigateBack = { navigator.goBack() },
            onOpenMaps = onOpenUrl,
            onLoadClick = { loadId ->
                navigator.navigate(LoadDetailRoute(loadId))
            },
            onDvirClick = { tripId, truckId ->
                navigator.navigate(DvirFormRoute(truckId = truckId, tripId = tripId, dvirType = DvirType.POST_TRIP))
            },
            viewModel = viewModel
        )
    }

    // Load Detail Screen
    entry<LoadDetailRoute> { key ->
        val viewModel: LoadDetailViewModel = koinViewModel { parametersOf(key.loadId) }

        LoadDetailScreen(
            onNavigateBack = { navigator.goBack() },
            onOpenMaps = onOpenUrl,
            onCapturePod = { id ->
                navigator.navigate(PodCaptureRoute(id, DocumentCaptureType.POD))
            },
            onCaptureBol = { id ->
                navigator.navigate(PodCaptureRoute(id, DocumentCaptureType.BOL))
            },
            onPickupInspection = { id ->
                navigator.navigate(ConditionReportRoute(id, InspectionType.PICKUP))
            },
            onDeliveryInspection = { id ->
                navigator.navigate(ConditionReportRoute(id, InspectionType.DELIVERY))
            },
            viewModel = viewModel
        )
    }

    // Messages Screen
    entry<MessagesRoute> {
        MessagesScreen(
            onConversationClick = { conversationId ->
                navigator.navigate(ConversationRoute(conversationId))
            },
            onNewMessage = {
                navigator.navigate(EmployeeSelectRoute)
            },
            onBack = { navigator.goBack() }
        )
    }

    // Employee Select Screen (New Message)
    entry<EmployeeSelectRoute> {
        EmployeeSelectScreen(
            onConversationCreated = { conversationId ->
                // Navigate to the conversation, replacing EmployeeSelectRoute
                navigator.navigateAndClear(ConversationRoute(conversationId), EmployeeSelectRoute)
            },
            onBack = { navigator.goBack() }
        )
    }

    // Conversation Screen
    entry<ConversationRoute> { key ->
        ConversationScreen(
            conversationId = key.conversationId,
            onBack = { navigator.goBack() }
        )
    }

    // POD Capture Screen
    entry<PodCaptureRoute> { key ->
        val viewModel: PodCaptureViewModel =
            koinViewModel { parametersOf(key.loadId, key.tripStopId, key.captureType) }

        PodCaptureScreen(
            onNavigateBack = { navigator.goBack() },
            viewModel = viewModel
        )
    }

    // Condition Report Screen
    entry<ConditionReportRoute> { key ->
        val viewModel: ConditionReportViewModel =
            koinViewModel { parametersOf(key.loadId, key.inspectionType) }

        ConditionReportScreen(
            onNavigateBack = { navigator.goBack() },
            viewModel = viewModel
        )
    }

    // DVIR Form Screen
    entry<DvirFormRoute> { key ->
        val viewModel: DvirFormViewModel =
            koinViewModel { parametersOf(key.truckId, key.tripId, key.dvirType) }

        DvirFormScreen(
            onNavigateBack = { navigator.goBack() },
            viewModel = viewModel
        )
    }

    // Account Screen
    entry<AccountRoute> {
        AccountScreen(
            onNavigateToPastTrips = { navigator.navigate(TripsRoute) },
            onNavigateToPastLoads = { navigator.navigate(PastLoadsRoute) },
            onNavigateToSettings = { navigator.navigate(SettingsRoute) },
            onNavigateToPrivacy = { navigator.navigate(PrivacyRoute) },
            onNavigateToMyLicenses = { navigator.navigate(MyLicensesRoute) },
            onLogout = { navigator.clearAndNavigate(LoginRoute) }
        )
    }

    // Settings Screen
    entry<SettingsRoute> {
        SettingsScreen(onNavigateBack = { navigator.goBack() })
    }

    // About Screen
    entry<AboutRoute> {
        AboutScreen()
    }

    // Privacy Screen
    entry<PrivacyRoute> {
        PrivacyScreen(onNavigateBack = { navigator.goBack() })
    }

    // My Licenses Screen
    entry<MyLicensesRoute> {
        MyLicensesScreen(onNavigateBack = { navigator.goBack() })
    }
}
