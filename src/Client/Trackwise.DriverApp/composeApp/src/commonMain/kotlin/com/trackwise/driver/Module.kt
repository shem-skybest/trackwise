package com.trackwise.driver

import com.trackwise.driver.api.ApiFactory
import com.trackwise.driver.api.CustomerApi
import com.trackwise.driver.config.AppConfig
import com.trackwise.driver.api.DocumentApi
import com.trackwise.driver.api.DriverApi
import com.trackwise.driver.api.DvirApi
import com.trackwise.driver.api.EmployeeApi
import com.trackwise.driver.api.InspectionsApi
import com.trackwise.driver.api.LoadApi
import com.trackwise.driver.api.MessageApi
import com.trackwise.driver.api.PrivacyApi
import com.trackwise.driver.api.ReportApi
import com.trackwise.driver.api.StatApi
import com.trackwise.driver.api.TripApi
import com.trackwise.driver.api.TruckApi
import com.trackwise.driver.api.UserApi
import com.trackwise.driver.api.VinsApi
import com.trackwise.driver.api.models.DvirType
import com.trackwise.driver.api.models.InspectionType
import com.trackwise.driver.service.LocationSharing
import com.trackwise.driver.service.LoadProximityWatcher
import com.trackwise.driver.service.TripEventOutbox
import com.trackwise.driver.service.TripEventRecorder
import com.trackwise.driver.service.PreferencesManager
import com.trackwise.driver.service.messaging.ConversationStateManager
import com.trackwise.driver.viewmodel.AccountViewModel
import com.trackwise.driver.viewmodel.ChatViewModel
import com.trackwise.driver.viewmodel.ConditionReportViewModel
import com.trackwise.driver.viewmodel.ConversationListViewModel
import com.trackwise.driver.viewmodel.DashboardViewModel
import com.trackwise.driver.viewmodel.DocumentCaptureType
import com.trackwise.driver.viewmodel.DvirFormViewModel
import com.trackwise.driver.viewmodel.EmployeeSelectViewModel
import com.trackwise.driver.viewmodel.LoadDetailViewModel
import com.trackwise.driver.viewmodel.LoginViewModel
import com.trackwise.driver.viewmodel.MyLicensesViewModel
import com.trackwise.driver.viewmodel.PastLoadsViewModel
import com.trackwise.driver.viewmodel.PodCaptureViewModel
import com.trackwise.driver.viewmodel.PrivacyViewModel
import com.trackwise.driver.viewmodel.SettingsViewModel
import com.trackwise.driver.viewmodel.StatsViewModel
import com.trackwise.driver.viewmodel.TripDetailViewModel
import com.trackwise.driver.viewmodel.TripsViewModel
import io.ktor.client.HttpClient
import org.koin.core.module.dsl.singleOf
import org.koin.core.module.dsl.viewModel
import org.koin.core.module.dsl.viewModelOf
import org.koin.dsl.module

fun commonModule() = module {
    singleOf(::PreferencesManager)

    // Register ApiFactory as a singleton
    single { ApiFactory(AppConfig.apiBaseUrl, get(), get()) }

    // HttpClient for file upload operations
    single<HttpClient> { get<ApiFactory>().httpClient }

    // Generated API instances from ApiFactory
    single<CustomerApi> { get<ApiFactory>().customerApi }
    single<DocumentApi> { get<ApiFactory>().documentApi }
    single<DriverApi> { get<ApiFactory>().driverApi }
    single<DvirApi> { get<ApiFactory>().dvirApi }
    single<EmployeeApi> { get<ApiFactory>().employeeApi }
    single<InspectionsApi> { get<ApiFactory>().inspectionsApi }
    single<LoadApi> { get<ApiFactory>().loadApi }
    single<MessageApi> { get<ApiFactory>().messageApi }
    single<PrivacyApi> { get<ApiFactory>().privacyApi }
    single<ReportApi> { get<ApiFactory>().reportApi }
    single<StatApi> { get<ApiFactory>().statApi }
    single<TripApi> { get<ApiFactory>().tripApi }
    single<TruckApi> { get<ApiFactory>().truckApi }
    single<UserApi> { get<ApiFactory>().userApi }
    single<VinsApi> { get<ApiFactory>().vinsApi }

    // ConversationStateManager service for shared messaging state
    singleOf(::ConversationStateManager)

    // Duty status + proximity watcher (drives location tracking lifecycle)
    singleOf(::LoadProximityWatcher)
    singleOf(::TripEventOutbox)
    singleOf(::TripEventRecorder)
    singleOf(::LocationSharing)

    viewModelOf(::DashboardViewModel)
    viewModelOf(::AccountViewModel)
    viewModelOf(::LoadDetailViewModel)
    viewModelOf(::PastLoadsViewModel)
    viewModelOf(::MyLicensesViewModel)
    viewModelOf(::StatsViewModel)
    viewModelOf(::LoginViewModel)
    viewModelOf(::SettingsViewModel)
    viewModelOf(::ConversationListViewModel)
    viewModelOf(::EmployeeSelectViewModel)
    viewModelOf(::TripsViewModel)
    viewModelOf(::TripDetailViewModel)
    viewModelOf(::ChatViewModel)
    // viewModelOf resolves parameters by type, so two String ids would both get the first one
    viewModel { params -> PodCaptureViewModel(get(), get(), params[0], params[1], params[2]) }
    viewModelOf(::ConditionReportViewModel)
    viewModel { params -> DvirFormViewModel(get(), get(), get(), get(), params[0], params[1], params[2]) }
    viewModelOf(::PrivacyViewModel)
}
