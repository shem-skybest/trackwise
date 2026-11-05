import { provideHttpClient, withInterceptors, type HttpInterceptorFn } from "@angular/common/http";
import { InjectionToken, makeEnvironmentProviders, type EnvironmentProviders } from "@angular/core";
import { ApiConfiguration } from "./generated/api-configuration";
import { errorHandlerInterceptor } from "./interceptors";
import { cacheInterceptor } from "./interceptors/cache.interceptor";
import { tokenAuthInterceptor } from "./interceptors/token-auth.interceptor";

/**
 * Configuration interface for the API provider.
 */
export interface ApiConfig {
  /**
   * Base path for the API.
   */
  baseUrl: string;
  /**
   * Cookie credentials flag.
   * If true, cookies will be sent with requests.
   */
  withCredentials?: boolean;
  /**
   * Function that returns the current access token (or null).
   */
  tokenGetter?: () => string | null;

  /**
   * Interceptors to be applied to the HTTP requests.
   */
  interceptors?: HttpInterceptorFn[];
}

/**
 * Injection token for the API configuration.
 * Used by interceptors to access the API settings.
 */
export const API_CONFIG = new InjectionToken<ApiConfig>("API_CONFIG");

/**
 * Provides the API configuration and HTTP client with interceptors.
 * @param config The API configuration options.
 * @returns The environment providers for the API.
 */
export function provideApi(config: ApiConfig): EnvironmentProviders {
  const interceptors: HttpInterceptorFn[] = [
    // Portal-supplied interceptors (e.g. tenantInterceptor) run first, so cacheInterceptor's
    // key-building sees any headers they set (X-Tenant) rather than caching under an empty prefix.
    ...(config.interceptors ?? []),
    cacheInterceptor,
    tokenAuthInterceptor,
    errorHandlerInterceptor,
  ];

  return makeEnvironmentProviders([
    { provide: API_CONFIG, useValue: config },
    {
      provide: ApiConfiguration,
      useFactory: () => {
        const apiConfig = new ApiConfiguration();
        apiConfig.rootUrl = config.baseUrl;
        return apiConfig;
      },
    },
    provideHttpClient(withInterceptors(interceptors)),
  ]);
}
