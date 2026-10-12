/**
 * Every path the app calls, in one place. Feature code refers to these keys and never writes a path.
 * `clientConfig` is served from the site root; the rest are relative to the API base URL from env.
 */
export const ENDPOINTS = {
  clientConfig: '/config.json',
  me: '/auth/me',
  permissions: '/auth/me/permissions',
  menu: '/auth/me/menu',
  executiveSummary: '/executive-summary',
  vasApplications: '/vas/applications',
  vasOutcomes: '/vas/outcomes',
  vasProcessing: '/vas/processing',
  vasRevenue: '/vas/revenue',
  vasVisaTypes: '/vas/visa-types',
  vasCentres: '/vas/centres',
  lookupCountries: '/lookups/countries',
  lookupCentres: '/lookups/centres',
  lookupVisaTypes: '/lookups/visa-types',
} as const;

export type EndpointKey = Exclude<keyof typeof ENDPOINTS, 'clientConfig'>;
