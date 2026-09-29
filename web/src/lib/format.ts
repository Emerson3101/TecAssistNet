export function formatRelativeTime(iso: string): string {
  const diffSeconds = Math.round((new Date(iso).getTime() - Date.now()) / 1000);
  const formatter = new Intl.RelativeTimeFormat("en", { numeric: "auto" });
  const absolute = Math.abs(diffSeconds);

  if (absolute < 60) {
    return formatter.format(Math.round(diffSeconds), "second");
  }
  if (absolute < 3600) {
    return formatter.format(Math.round(diffSeconds / 60), "minute");
  }
  if (absolute < 86400) {
    return formatter.format(Math.round(diffSeconds / 3600), "hour");
  }
  return formatter.format(Math.round(diffSeconds / 86400), "day");
}
