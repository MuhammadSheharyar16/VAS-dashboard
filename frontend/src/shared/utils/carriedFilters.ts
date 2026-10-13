/** The filters stay with the user as they move between the home screen and the reports. */
const CARRIED_PARAMS = ['period', 'from', 'to', 'country', 'centre', 'visaType', 'compare'];

/** The part of a query string that travels to the next page: the report filters and nothing else. */
export function carriedSearch(search: string): string {
  const carried = new URLSearchParams();

  new URLSearchParams(search).forEach((value, key) => {
    if (CARRIED_PARAMS.includes(key)) {
      carried.set(key, value);
    }
  });

  return carried.toString();
}
