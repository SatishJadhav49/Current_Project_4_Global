export interface VehicleInfo {
  VIN_Number?: string;
  BIW_No?: string;
  Model_Description?: string;
  Colour_Desc?: string;
  Fuel?: string;
  Engine_No?: string;
  RollDown_Date?: string | Date | null;
  CAIOut_Date?: string | Date | null;
  Model_Name?: string;
  Country?: string;
  Drive_Type?: string;
  LSP_RFD_Date?: string | Date | null;
  Dock_Audit_Date?: string | Date | null;
}

export interface DefectsData {
  Audit_Category?: string;
  Audit_Type?: string;
  Auditor_Name?: string;
  Problem_Desc?: string;
  Severity_Name?: string;
  Attribution_Name?: string;
  Shop_Name?: string;
  Reported_Date?: string | Date | null;
}

export interface DefectsCategoryGroup {
  category: string;
  defects: DefectsData[];
  expanded: boolean;
}
