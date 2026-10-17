export type DatePresetKey =
  | 'last7'
  | 'thisMonth'
  | 'lastMonth'
  | 'last3Months'
  | 'custom'
  | 'lastN';

export interface DatePreset {
  key: DatePresetKey;
  label: string;
}

export const DATE_PRESETS: DatePreset[] = [
  { key: 'last7', label: 'Last 7 days' },
  { key: 'thisMonth', label: 'This month' },
  { key: 'lastMonth', label: 'Last month' },
  { key: 'last3Months', label: 'Last 3 months' },
  { key: 'custom', label: 'Custom range' },
  { key: 'lastN', label: 'Last N audits' },
];

/** Longest range the API accepts. */
export const MAX_RANGE_DAYS = 366;

/** "Last N audits" limits (the API accepts up to 1000). */
export const DEFAULT_LAST_N = 10;
export const MAX_LAST_N = 1000;

/** Presets that resolve to fixed dates. */
export type FixedPresetKey = Exclude<DatePresetKey, 'custom' | 'lastN'>;

/** Resolves a preset to a from / to date (local dates, time stripped). */
export function resolvePreset(key: FixedPresetKey, today = new Date()): { from: Date; to: Date } {
  const t = startOfDay(today);

  switch (key) {
    case 'last7':
      return { from: addDays(t, -6), to: t };
    case 'thisMonth':
      return { from: new Date(t.getFullYear(), t.getMonth(), 1), to: t };
    case 'lastMonth':
      return {
        from: new Date(t.getFullYear(), t.getMonth() - 1, 1),
        to: new Date(t.getFullYear(), t.getMonth(), 0),
      };
    case 'last3Months':
      return { from: addDays(new Date(t.getFullYear(), t.getMonth() - 3, t.getDate()), 1), to: t };
  }
}

export function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

export function endOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate(), 23, 59, 59, 999);
}

export function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}

/** yyyy-MM-dd in local time - what the API and date inputs expect. */
export function toIsoDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

/** Parses yyyy-MM-dd as a local date. */
export function fromIsoDate(value: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value ?? '');
  if (!match) return null;
  return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
}

export function daysBetween(from: Date, to: Date): number {
  return Math.round((startOfDay(to).getTime() - startOfDay(from).getTime()) / 86400000);
}
