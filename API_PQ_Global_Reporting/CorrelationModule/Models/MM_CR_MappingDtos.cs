namespace API_PQ_Global_Reporting.CorrelationModule.Models
{
    public class MM_CR_ModelDto
    {
        public decimal Model_ID { get; set; }
        public string? Model_Name { get; set; }
    }

    public class MM_CR_ApplicationColumnDto
    {
        public string Column_Key { get; set; } = string.Empty;
        public string Column_Header { get; set; } = string.Empty;
        public int Sort_Order { get; set; }
        public bool Is_Filterable { get; set; }
    }

    public class MM_CR_ApplicationDto
    {
        public int App_ID { get; set; }
        public string? App_Code { get; set; }
        public string? App_Name { get; set; }
        public string? Leaf_Level_Name { get; set; }
        public int Sort_Order { get; set; }
        public List<MM_CR_ApplicationColumnDto> Columns { get; set; } = new();
    }

    public class MM_CR_AppMappedCountDto
    {
        public int App_ID { get; set; }
        public string? App_Name { get; set; }
        public int Mapped_Count { get; set; }
    }

    public class MM_CR_ParameterDto
    {
        public int CR_Parameter_ID { get; set; }
        public decimal Model_ID { get; set; }
        public string? Model_Name { get; set; }
        public string? Parameter_Name { get; set; }
        public DateTime? Inserted_Date { get; set; }
        public DateTime? Updated_Date { get; set; }
        public int Total_Mapped { get; set; }
        public List<MM_CR_AppMappedCountDto> App_Counts { get; set; } = new();
    }

    public class MM_CR_ParameterSaveDto
    {
        public int CR_Parameter_ID { get; set; }
        public decimal Model_ID { get; set; }
        public string Parameter_Name { get; set; } = string.Empty;
        public decimal? User_ID { get; set; }
        public string? Host { get; set; }
    }

    public class MM_CR_ParameterDeleteDto
    {
        public int CR_Parameter_ID { get; set; }
        public decimal? User_ID { get; set; }
        public string? Host { get; set; }
    }

    public class MM_CR_MappingDto
    {
        public int Mapping_ID { get; set; }
        public int CR_Parameter_ID { get; set; }
        public int App_ID { get; set; }
        public string? App_Name { get; set; }
        public string? Leaf_Level_Name { get; set; }
        public decimal? Source_Model_ID { get; set; }
        public decimal Source_Leaf_ID { get; set; }
        public string? Source_Leaf_Type { get; set; }
        public string? Source_Display_Text { get; set; }
        public DateTime? Inserted_Date { get; set; }
        public string? Mapped_By { get; set; }
    }

    public class MM_CR_ParameterMappingsDto
    {
        public MM_CR_ParameterDto? Parameter { get; set; }
        public List<MM_CR_MappingDto> Mappings { get; set; } = new();
    }

    /// <summary>
    /// Master data of one application. Rows are passed through as-is from the
    /// application's master SP so new applications need no DTO change.
    /// </summary>
    public class MM_CR_MasterDataDto
    {
        public int App_ID { get; set; }
        public List<Dictionary<string, object?>> Rows { get; set; } = new();
    }

    public class MM_CR_LeafItemDto
    {
        public decimal Leaf_ID { get; set; }
        /// <summary>Optional sub-type of the leaf, e.g. Gap / Flush for 1D.</summary>
        public string? Leaf_Type { get; set; }
        public decimal? Source_Model_ID { get; set; }
        public string? Display_Text { get; set; }
    }

    public class MM_CR_MappingSaveDto
    {
        public int CR_Parameter_ID { get; set; }
        public int App_ID { get; set; }
        public List<MM_CR_LeafItemDto> Items { get; set; } = new();
        public decimal? User_ID { get; set; }
        public string? Host { get; set; }
    }

    public class MM_CR_UnmapDto
    {
        public List<int> Mapping_IDs { get; set; } = new();
        public decimal? User_ID { get; set; }
        public string? Host { get; set; }
    }

    /// <summary>
    /// Outcome of a save / delete SP. Result_Code: 1 Success, 0 Validation, 2 Duplicate, 3 Not found.
    /// </summary>
    public class MM_CR_ResultDto
    {
        public int Result_Code { get; set; }
        public string? Message { get; set; }
        public int ID { get; set; }
        public int Inserted_Count { get; set; }
        public int Skipped_Count { get; set; }
        public bool Is_Success => Result_Code == 1;
    }
}
