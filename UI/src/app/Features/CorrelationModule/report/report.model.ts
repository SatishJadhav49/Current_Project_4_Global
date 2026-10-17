import { CrParameter } from '../correlation.model';

export interface CrReadingPoint {
  Audit_Date: string | Date;
  Reading_No: number;
  Reading: number;
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
  Stats: CrReportStats;
  Points: CrReadingPoint[];
}

export interface CrReport {
  Parameter: CrParameter | null;
  From_Date: string | Date;
  To_Date: string | Date;
  Series: CrReportSeries[];
}

/** Date range shared by every chart so trends line up across applications. */
export interface CrDateRange {
  from: Date;
  to: Date;
}
