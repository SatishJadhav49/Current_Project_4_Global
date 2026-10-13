export interface CrModel {
  Model_ID: number;
  Model_Name?: string;
}

export interface CrApplicationColumn {
  Column_Key: string;
  Column_Header: string;
  Sort_Order: number;
  Is_Filterable: boolean;
}

export interface CrApplication {
  App_ID: number;
  App_Code?: string;
  App_Name?: string;
  Leaf_Level_Name?: string;
  Sort_Order: number;
  Columns: CrApplicationColumn[];
}

export interface CrAppMappedCount {
  App_ID: number;
  App_Name?: string;
  Mapped_Count: number;
}

export interface CrParameter {
  CR_Parameter_ID: number;
  Model_ID: number;
  Model_Name?: string;
  Parameter_Name?: string;
  Inserted_Date?: string | Date | null;
  Updated_Date?: string | Date | null;
  Total_Mapped: number;
  App_Counts: CrAppMappedCount[];
}

export interface CrParameterSave {
  CR_Parameter_ID: number;
  Model_ID: number;
  Parameter_Name: string;
  User_ID?: number | null;
  Host?: string;
}

export interface CrParameterDelete {
  CR_Parameter_ID: number;
  User_ID?: number | null;
  Host?: string;
}

export interface CrMapping {
  Mapping_ID: number;
  CR_Parameter_ID: number;
  App_ID: number;
  App_Name?: string;
  Leaf_Level_Name?: string;
  Source_Model_ID?: number | null;
  Source_Leaf_ID: number;
  Source_Display_Text?: string;
  Inserted_Date?: string | Date | null;
  Mapped_By?: string;
}

export interface CrParameterMappings {
  Parameter: CrParameter | null;
  Mappings: CrMapping[];
}

/**
 * One master-data row of a source application. Always carries Leaf_ID,
 * Leaf_Name, Source_Model_ID, Display_Text, Is_Mapped, Other_Parameters plus
 * the application specific columns described by CrApplication.Columns.
 */
export type CrMasterRow = Record<string, unknown>;

export interface CrMasterData {
  App_ID: number;
  Rows: CrMasterRow[];
}

export interface CrLeafItem {
  Leaf_ID: number;
  Source_Model_ID?: number | null;
  Display_Text?: string;
}

export interface CrMappingSave {
  CR_Parameter_ID: number;
  App_ID: number;
  Items: CrLeafItem[];
  User_ID?: number | null;
  Host?: string;
}

export interface CrUnmap {
  Mapping_IDs: number[];
  User_ID?: number | null;
  Host?: string;
}

/** Result_Code: 1 Success, 0 Validation, 2 Duplicate, 3 Not found */
export interface CrResult {
  Result_Code: number;
  Message?: string;
  ID: number;
  Inserted_Count: number;
  Skipped_Count: number;
  Is_Success: boolean;
}

/** Mapped items of one application - for the "Mapped" panel. */
export interface CrMappingGroup {
  appId: number;
  appName: string;
  leafLevelName: string;
  mappings: CrMapping[];
}
