namespace API_PQ_Global_Reporting.Mail
{
    // DTO for mail recipients
    public class MailRecipientDto
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    // DTO for mail recipients list
    public class MailRecipientsDto
    {
        public List<MailRecipientDto> ToRecipients { get; set; } = new List<MailRecipientDto>();
        public List<MailRecipientDto> CcRecipients { get; set; } = new List<MailRecipientDto>();
    }

    // DTO for concern data in assigned emails
    public class ConcernMailDataDto
    {
        public decimal Concern_ID { get; set; }
        public string Employee_Name { get; set; } = string.Empty;
        public string Employee_No { get; set; } = string.Empty;
        public string Manager_Name { get; set; } = string.Empty;
        public string Manager_No { get; set; } = string.Empty;
        public decimal Manager_ID { get; set; }
        public decimal MCAR_ID { get; set; }
        public string MCAR_No { get; set; } = string.Empty;
        public string Source_Name { get; set; } = string.Empty;
        public string Shop_Name { get; set; } = string.Empty;
        public string Stage_Name { get; set; } = string.Empty;
        public string Reported_Date { get; set; } = string.Empty;
        public string Severity_Name { get; set; } = string.Empty;
        public string Problem_Definition { get; set; } = string.Empty;
        public string Part_Name { get; set; } = string.Empty;
        public string Is_Repeated { get; set; } = string.Empty;
        public string Plant_Code { get; set; } = string.Empty;
        public string Auditor_Name { get; set; } = string.Empty;
        public decimal Auditor_ID { get; set; }
        public string Auditor_No { get; set; } = string.Empty;
        public decimal Shop_ID { get; set; }
        public decimal Assigned_To { get; set; }
    }

    // DTO for MFG approval mail data
    public class MfgApprovalMailDataDto
    {
        public decimal Concern_ID { get; set; }
        public string Employee_Name { get; set; } = string.Empty;
        public string Employee_No { get; set; } = string.Empty;
        public string Manager_Name { get; set; } = string.Empty;
        public string Manager_No { get; set; } = string.Empty;
        public decimal Manager_ID { get; set; }
        public decimal MCAR_ID { get; set; }
        public string MCAR_No { get; set; } = string.Empty;
        public string Source_Name { get; set; } = string.Empty;
        public string Shop_Name { get; set; } = string.Empty;
        public string Stage_Name { get; set; } = string.Empty;
        public string Reported_Date { get; set; } = string.Empty;
        public string Severity_Name { get; set; } = string.Empty;
        public string Problem_Definition { get; set; } = string.Empty;
        public string Part_Name { get; set; } = string.Empty;
        public string Is_Repeated { get; set; } = string.Empty;
        public string PCA_Submission_Date { get; set; } = string.Empty;
        public string Plant_Code { get; set; } = string.Empty;
        public string Auditor_Name { get; set; } = string.Empty;
        public decimal Auditor_ID { get; set; }
        public string Auditor_No { get; set; } = string.Empty;
        public decimal Shop_ID { get; set; }
        public decimal Assigned_To { get; set; }
    }

    // DTO for effectiveness monitoring mail data
    public class EffectivenessMonitoringMailDataDto
    {
        public decimal Concern_ID { get; set; }
        public string Employee_Name { get; set; } = string.Empty;
        public string Employee_No { get; set; } = string.Empty;
        public string Manager_Name { get; set; } = string.Empty;
        public string Manager_No { get; set; } = string.Empty;
        public decimal Manager_ID { get; set; }
        public decimal MCAR_ID { get; set; }
        public string MCAR_No { get; set; } = string.Empty;
        public string Source_Name { get; set; } = string.Empty;
        public string Shop_Name { get; set; } = string.Empty;
        public string Stage_Name { get; set; } = string.Empty;
        public string Reported_Date { get; set; } = string.Empty;
        public string Severity_Name { get; set; } = string.Empty;
        public string Problem_Definition { get; set; } = string.Empty;
        public string Part_Name { get; set; } = string.Empty;
        public string Is_Repeated { get; set; } = string.Empty;
        public string MFG_Approval_Date { get; set; } = string.Empty;
        public string PCA_Completion_Date { get; set; } = string.Empty;
        public string Plant_Code { get; set; } = string.Empty;
        public string Auditor_Name { get; set; } = string.Empty;
        public decimal Auditor_ID { get; set; }
        public string Auditor_No { get; set; } = string.Empty;
        public decimal Shop_ID { get; set; }
        public decimal Assigned_To { get; set; }
    }

    // DTO for quality manager approval mail data
    public class QualityManagerApprovalMailDataDto
    {
        public decimal Concern_ID { get; set; }
        public string Employee_Name { get; set; } = string.Empty;
        public string Employee_No { get; set; } = string.Empty;
        public string Manager_Name { get; set; } = string.Empty;
        public string Manager_No { get; set; } = string.Empty;
        public decimal Manager_ID { get; set; }
        public decimal Inserted_User_ID { get; set; }
        public string Quality_Manager_Name { get; set; } = string.Empty;
        public string Quality_Manager_No { get; set; } = string.Empty;
        public decimal Quality_Manager_ID { get; set; }
        public decimal MCAR_ID { get; set; }
        public string MCAR_No { get; set; } = string.Empty;
        public string Source_Name { get; set; } = string.Empty;
        public string Shop_Name { get; set; } = string.Empty;
        public string Stage_Name { get; set; } = string.Empty;
        public string Reported_Date { get; set; } = string.Empty;
        public string Severity_Name { get; set; } = string.Empty;
        public string Problem_Definition { get; set; } = string.Empty;
        public string Part_Name { get; set; } = string.Empty;
        public string Is_Repeated { get; set; } = string.Empty;
        public string MFG_Approval_Date { get; set; } = string.Empty;
        public string PCA_Completion_Date { get; set; } = string.Empty;
        public string Effectiveness_Submission_Date { get; set; } = string.Empty;
        public string Plant_Code { get; set; } = string.Empty;
        public string Auditor_Name { get; set; } = string.Empty;
        public decimal Auditor_ID { get; set; }
        public string Auditor_No { get; set; } = string.Empty;
        public decimal Shop_ID { get; set; }
        public decimal Assigned_To { get; set; }
    }
}
