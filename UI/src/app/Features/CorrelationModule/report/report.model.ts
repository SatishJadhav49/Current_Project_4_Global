import { CrParameter } from '../correlation.model';

export interface CrReadingPoint {
  Audit_Date: string | Date;
  Reading_No: number;
  /** Plotted value - average of Reading1..3 for multi-reading apps. */
  Reading: number;
  Reading1?: number | null;
  Reading2?: number | null;
  Reading3?: number | null;
  LSL?: number | null;
  USL?: number | null;
  Is_Out_Of_Spec: boolean;
}

export interface CrReportStats {
  Count: number;
  Mean?: number | null;
  Min?: number | null;
  Max?: number | null;
  Out_Of_Spec_Count: number;
  LSL_Start?: number | null;
  USL_Start?: number | null;
  LSL_End?: number | null;
  USL_End?: number | null;
}

/** Readings of one mapped item - one chart card. */
export interface CrReportSeries {
  Mapping_ID: number;
  App_ID: number;
  App_Name?: string;
  Leaf_Level_Name?: string;
  Source_Leaf_ID: number;
  Source_Leaf_Type?: string | null;
  Display_Text?: string;
  Error?: string | null;
  /** True when each point is the average of Reading1..3 (X-bar chart). */
  Is_Average?: boolean;
  Stats: CrReportStats;
  Points: CrReadingPoint[];
}

export interface CrReport {
  Parameter: CrParameter | null;
  /** Date mode - null in "last N audits" mode. */
  From_Date?: string | Date | null;
  To_Date?: string | Date | null;
  /** "Last N audits" mode - last N audits of each mapped item. */
  Last_N?: number | null;
  Series: CrReportSeries[];
}

/** Report filter sent to the API: a date range or the last N audits. */
export type CrReportFilter =
  | { fromDate: string; toDate: string }
  | { lastN: number };
