namespace API_PQ_Global_Reporting.Models.DTOs
{
    public class VehicleInfoDto
    {
        public string? VIN_Number { get; set; }
        public string? BIW_No { get; set; }
        public string? Model_Description { get; set; }
        public string? Colour_Desc { get; set; }
        public string? Fuel { get; set; }
        public string? Engine_No { get; set; }
        public DateTime? RollDown_Date { get; set; }
        public DateTime? CAIOut_Date { get; set; }
        public string? Model_Name { get; set; }
        public string? Country { get; set; }
        public string? Drive_Type { get; set; }
        public DateTime? LSP_RFD_Date { get; set; }
        public DateTime? Dock_Audit_Date { get; set; }
    }

    public class DefectsDataDto
    {
        public string? Audit_Category { get; set; }
        public string? Audit_Type { get; set; }
        public string? Auditor_Name { get; set; }
        public string? Problem_Desc { get; set; }
        public string? Severity_Name { get; set; }
        public string? Attribution_Name { get; set; }
        public string? Shop_Name { get; set; }
        public DateTime? Reported_Date { get; set; }
    }
}
