namespace API_PQ_Global_Reporting.CorrelationModule.Models
{
    public class MM_CR_ReportDto
    {
        public MM_CR_ParameterDto? Parameter { get; set; }
        public DateTime From_Date { get; set; }
        public DateTime To_Date { get; set; }
        public List<MM_CR_ReportSeriesDto> Series { get; set; } = new();
    }

    /// <summary>Readings of one mapped item (one chart card).</summary>
    public class MM_CR_ReportSeriesDto
    {
        public int Mapping_ID { get; set; }
        public int App_ID { get; set; }
        public string? App_Name { get; set; }
        public string? Leaf_Level_Name { get; set; }
        public decimal Source_Leaf_ID { get; set; }
        public string? Source_Leaf_Type { get; set; }
        public string? Display_Text { get; set; }

        /// <summary>Set when readings could not be loaded for this application.</summary>
        public string? Error { get; set; }

        public MM_CR_ReportStatsDto Stats { get; set; } = new();
        public List<MM_CR_ReadingPointDto> Points { get; set; } = new();
    }

    public class MM_CR_ReadingPointDto
    {
        public DateTime Audit_Date { get; set; }
        public int Reading_No { get; set; }
        public decimal Reading { get; set; }
        public decimal? LSL { get; set; }
        public decimal? USL { get; set; }
        public bool Is_Out_Of_Spec { get; set; }
    }

    public class MM_CR_ReportStatsDto
    {
        public int Count { get; set; }
        public decimal? Mean { get; set; }
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
        public int Out_Of_Spec_Count { get; set; }

        /// <summary>Specification at the first and last reading of the range.</summary>
        public decimal? LSL_Start { get; set; }
        public decimal? USL_Start { get; set; }
        public decimal? LSL_End { get; set; }
        public decimal? USL_End { get; set; }
    }
}
