using API_PQ_Global_Reporting.Mail;
using API_PQ_Global_Reporting.Models;
using API_PQ_Global_Reporting.Utils;
using Microsoft.Data.SqlClient;
using System.Net.Mail;
using System.Net;

namespace API_PQ_Global_Reporting.Mail.Services
{
    public class EffectivenessRejectionMailService : BaseMailService
    {
        public EffectivenessRejectionMailService(
            string connectionString,
            SmtpSettings smtpSettings,
            SystemSettings systemSettings) 
            : base(connectionString, smtpSettings, systemSettings)
        {
        }

        /// <summary>
        /// Sends mail notification when quality officer rejects effectiveness monitoring
        /// </summary>
        public async Task<bool> SendMailAsync(decimal concernId)
        {
            try
            {
                // Get concern details from database
                var concernDetails = await GetConcernDetailsForEffectivenessRejectionMailAsync(concernId);
                if (concernDetails == null)
                {
                    throw new Exception($"Concern with ID {concernId} not found");
                }

                if (string.IsNullOrEmpty(concernDetails.Employee_No))
                {
                    throw new Exception($"Employee information not found for concern {concernId}");
                }

                // Get mail recipients (To and CC lists)
                var recipients = await GetMailRecipientsForEffectivenessRejectionAsync(concernDetails);

                // Prepare mail body
                string mailBody = await PrepareMailBodyForEffectivenessRejectionAsync(concernDetails);

                // Send email with multiple recipients
                await SendEmailWithRecipientsAsync(recipients, 
                    $"MCAR Effectiveness Rejected - New Action Plan Required - {concernDetails.Source_Name} - {concernDetails.Shop_Name} - {concernDetails.Stage_Name}", 
                    mailBody);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending effectiveness rejection mail for concern {concernId}: {ex.Message}", ex);
            }
        }

        private async Task<string> PrepareMailBodyForEffectivenessRejectionAsync(EffectivenessRejectionMailDataDto concernDetails)
        {
            try
            {
                // Read mail template
                string mailTemplate = await ReadMailTemplateAsync("effectiveness-rejection-template.html");

                // Replace placeholders with actual data
                string mailBody = ReplacePlaceholdersInTemplate(mailTemplate, GetReplacementDictionaryForEffectivenessRejection(concernDetails));

                return mailBody;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error preparing mail body for effectiveness rejection concern {concernDetails.Concern_ID}: {ex.Message}", ex);
            }
        }

        private Dictionary<string, string> GetReplacementDictionaryForEffectivenessRejection(EffectivenessRejectionMailDataDto data)
        {
            string plantName = GetPlantName(data.Plant_Code);

            // Calculate monitoring dates based on PCA completion date
            var monitoringStartDate = data.PCA_Completion_Date;
            var monitoringEndDate = data.PCA_Completion_Date.AddDays(15); // 15 days monitoring period

            // Generate dynamic system URL with concern and MCAR IDs
            string systemUrl = $"{_systemSettings.BaseUrl}/mcar/actionhome?concernId={data.Concern_ID}&mcarId={data.MCAR_ID}";

            return new Dictionary<string, string>
            {
                { "[EMPLOYEE_NAME]", data.Employee_Name },
                { "[EMPLOYEE_ID]", data.Employee_No },
                { "[QUALITY_OFFICER_NAME]", data.Quality_Officer_Name },
                { "[MANAGER_NAME]", data.Manager_Name },
                { "[MCAR_NO]", data.MCAR_No },
                { "[SOURCE_NAME]", data.Source_Name },
                { "[STAGE_NAME]", data.Stage_Name },
                { "[PART_NAME]", data.Part_Name },
                { "[SEVERITY_NAME]", data.Severity_Name },
                { "[PROBLEM_DEFINITION]", data.Problem_Definition },
                { "[MFG_APPROVAL_DATE]", data.MFG_Approval_Date.ToString("dd-MMM-yyyy hh:mm tt") },
                { "[PCA_COMPLETION_DATE]", data.PCA_Completion_Date.ToString("dd-MMM-yyyy") },
                { "[MONITORING_START_DATE]", monitoringStartDate.ToString("dd-MMM-yyyy") },
                { "[MONITORING_END_DATE]", monitoringEndDate.ToString("dd-MMM-yyyy") },
                { "[REPORTED_DATE]", data.Reported_Date.ToString("dd-MMM-yyyy") },
                { "[REJECTION_DATE]", data.Rejection_Date.ToString("dd-MMM-yyyy hh:mm tt") },
                { "[EFFECTIVENESS_REMARK]", data.Effectiveness_Remark },
                { "[PLANT_NAME]", plantName },
                { "[CURRENT_DATE]", DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt") },
                { "[AUDITOR_NAME]", data.Auditor_Name },
                { "[AUDITOR_ID]", data.Auditor_No },
                { "[IS_REPEATED]", data.Is_Repeated },
                { "[SYSTEM_URL]", systemUrl }
            };
        }

        private async Task<EffectivenessRejectionMailDataDto?> GetConcernDetailsForEffectivenessRejectionMailAsync(decimal concernId)
        {
            try
            {
                const string query = @"
                    SELECT 
                        cd.Concern_ID,
                        cd.Date_Of_Complaint as Reported_Date,
                        ISNULL(at_effectiveness.Inserted_Date, GETDATE()) as Rejection_Date,
                        ISNULL(at_mfg.Inserted_Date, GETDATE()) as MFG_Approval_Date,
                        ISNULL(pca_latest.Target_Date, GETDATE()) as PCA_Completion_Date,
                        ISNULL(cd.Problem_Defination, '') as Problem_Definition,
                        cd.Plant_Code,
                        cd.Shop_ID,
                        ISNULL(cd.Assigned_To, 0) as Assigned_To,
                        
                        -- Employee Info (Assigned To)
                        ISNULL(emp.Employee_Name, '') as Employee_Name,
                        ISNULL(emp.Employee_No, '') as Employee_No,
                        ISNULL(emp.Email_Address, '') as Employee_Email,
                        
                        -- Manager Info (Who approved manufacturing)
                        ISNULL(mgr.Employee_Name, '') as Manager_Name,
                        ISNULL(mgr.Employee_No, '') as Manager_No,
                        ISNULL(mgr.Email_Address, '') as Manager_Email,
                        ISNULL(mgr.Employee_ID, 0) as Manager_ID,
                        
                        -- Quality Officer Info (Who rejected effectiveness)
                        ISNULL(qo.Employee_Name, '') as Quality_Officer_Name,
                        ISNULL(qo.Employee_No, '') as Quality_Officer_No,
                        ISNULL(qo.Email_Address, '') as Quality_Officer_Email,
                        ISNULL(qo.Employee_ID, 0) as Quality_Officer_ID,
                        
                        -- MCAR Info
                        ISNULL(cd.MCAR_ID, 0) as MCAR_ID,
                        ISNULL(mr.MCAR_No, '') as MCAR_No,
                        
                        -- Source Info
                        ISNULL(sm.Source_Name, '') as Source_Name,
                        
                        -- Shop Info
                        ISNULL(shop.Shop_Name, '') as Shop_Name,
                        
                        -- Stage Info
                        ISNULL(st.Stage_Name, '') as Stage_Name,
                        
                        -- Severity Info
                        ISNULL(sev.Severity_Name, '') as Severity_Name,
                        
                        -- Part Name
                        ISNULL(cd.Part_Name, '') as Part_Name,
                        
                        -- Auditor Info
                        ISNULL(aud.Employee_Name, '') as Auditor_Name,
                        ISNULL(aud.Employee_ID, 0) as Auditor_ID,
                        ISNULL(aud.Employee_No, '') as Auditor_No,
                        ISNULL(aud.Email_Address, '') as Auditor_Email,
                        CASE WHEN ISNULL(cd.Is_Repeated, 0) = 1 THEN 'YES' ELSE 'NO' END as Is_Repeated,
                        
                        -- Effectiveness Tracking Info (Rejection Remark)
                        ISNULL(at_effectiveness.Approval_Remark, 'No specific remarks provided') as Effectiveness_Remark
                        
                    FROM MM_Concern_Data cd
                    LEFT JOIN MM_Employee emp ON cd.Assigned_To = emp.Employee_ID
                    LEFT JOIN MM_Employee mgr ON emp.Reporting_Manager_ID = mgr.Employee_ID
                    LEFT JOIN MM_MCAR_Registration mr ON cd.MCAR_ID = mr.MCAR_ID
                    LEFT JOIN MM_Source_Master sm ON cd.Source_ID = sm.Source_ID
                    LEFT JOIN MM_Shop shop ON cd.Shop_ID = shop.Shop_ID
                    LEFT JOIN MM_Stage_Master st ON cd.Stage_ID = st.Stage_ID
                    LEFT JOIN MM_Severity_Master sev ON cd.Severity_ID = sev.Severity_ID
                    LEFT JOIN MM_Employee aud ON cd.Auditor_ID = aud.Employee_ID
                    LEFT JOIN MM_Approval_Tracking at_mfg ON cd.Concern_ID = at_mfg.Concern_ID AND at_mfg.Is_MFG = 1 AND at_mfg.Is_Approved = 1
                    LEFT JOIN MM_Approval_Tracking at_effectiveness ON cd.Concern_ID = at_effectiveness.Concern_ID AND at_effectiveness.Is_Effectivness = 1 AND at_effectiveness.Is_Rejected = 1
                    LEFT JOIN MM_Employee qo ON at_effectiveness.Inserted_User_ID = qo.Employee_ID
                    LEFT JOIN (
                        SELECT 
                            Concern_ID,
                            MAX(Target_Date) as Target_Date
                        FROM MM_Action_Plan_PCA 
                        WHERE (Is_Deleted IS NULL OR Is_Deleted = 0)
                        GROUP BY Concern_ID
                    ) pca_latest ON cd.Concern_ID = pca_latest.Concern_ID
                    WHERE cd.Concern_ID = @ConcernId AND (cd.Is_Deleted IS NULL OR cd.Is_Deleted = 0)";

                using var connection = await CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);
                command.Parameters.Add(new SqlParameter("@ConcernId", concernId));

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var rejectionDate = reader.IsDBNull(reader.GetOrdinal("Rejection_Date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("Rejection_Date"));
                    var mfgApprovalDate = reader.IsDBNull(reader.GetOrdinal("MFG_Approval_Date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("MFG_Approval_Date"));
                    var pcaCompletionDate = reader.IsDBNull(reader.GetOrdinal("PCA_Completion_Date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("PCA_Completion_Date"));
                    var reportedDate = reader.GetDateTime(reader.GetOrdinal("Reported_Date"));

                    return new EffectivenessRejectionMailDataDto
                    {
                        Concern_ID = reader.GetDecimal(reader.GetOrdinal("Concern_ID")),
                        Employee_Name = reader.GetString(reader.GetOrdinal("Employee_Name")),
                        Employee_No = reader.GetString(reader.GetOrdinal("Employee_No")),
                        Employee_Email = reader.GetString(reader.GetOrdinal("Employee_Email")),
                        Manager_Name = reader.GetString(reader.GetOrdinal("Manager_Name")),
                        Manager_No = reader.GetString(reader.GetOrdinal("Manager_No")),
                        Manager_Email = reader.GetString(reader.GetOrdinal("Manager_Email")),
                        Manager_ID = reader.GetDecimal(reader.GetOrdinal("Manager_ID")),
                        Quality_Officer_Name = reader.GetString(reader.GetOrdinal("Quality_Officer_Name")),
                        Quality_Officer_No = reader.GetString(reader.GetOrdinal("Quality_Officer_No")),
                        Quality_Officer_Email = reader.GetString(reader.GetOrdinal("Quality_Officer_Email")),
                        Quality_Officer_ID = reader.GetDecimal(reader.GetOrdinal("Quality_Officer_ID")),
                        MCAR_ID = reader.GetDecimal(reader.GetOrdinal("MCAR_ID")),
                        MCAR_No = reader.GetString(reader.GetOrdinal("MCAR_No")),
                        Source_Name = reader.GetString(reader.GetOrdinal("Source_Name")),
                        Shop_Name = reader.GetString(reader.GetOrdinal("Shop_Name")),
                        Stage_Name = reader.GetString(reader.GetOrdinal("Stage_Name")),
                        Reported_Date = reportedDate,
                        Severity_Name = reader.GetString(reader.GetOrdinal("Severity_Name")),
                        Problem_Definition = reader.GetString(reader.GetOrdinal("Problem_Definition")),
                        Part_Name = reader.GetString(reader.GetOrdinal("Part_Name")),
                        Rejection_Date = rejectionDate,
                        MFG_Approval_Date = mfgApprovalDate,
                        PCA_Completion_Date = pcaCompletionDate,
                        Plant_Code = reader.GetString(reader.GetOrdinal("Plant_Code")),
                        Auditor_Name = reader.GetString(reader.GetOrdinal("Auditor_Name")),
                        Auditor_ID = reader.GetDecimal(reader.GetOrdinal("Auditor_ID")),
                        Auditor_No = reader.GetString(reader.GetOrdinal("Auditor_No")),
                        Auditor_Email = reader.GetString(reader.GetOrdinal("Auditor_Email")),
                        Is_Repeated = reader.GetString(reader.GetOrdinal("Is_Repeated")),
                        Shop_ID = reader.GetDecimal(reader.GetOrdinal("Shop_ID")),
                        Assigned_To = reader.GetDecimal(reader.GetOrdinal("Assigned_To")),
                        Effectiveness_Remark = reader.GetString(reader.GetOrdinal("Effectiveness_Remark"))
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving concern details for effectiveness rejection mail: {ex.Message}", ex);
            }

            return null;
        }

        private async Task<MailRecipientsDto> GetMailRecipientsForEffectivenessRejectionAsync(EffectivenessRejectionMailDataDto concernData)
        {
            var recipients = new MailRecipientsDto();

            try
            {
                using var connection = await CreateConnectionAsync();

                // Get TO recipients: Only the assigned user
                GetToRecipientsForEffectivenessRejectionAsync(connection, concernData, recipients);

                // Get CC recipients: Manufacturing officers, PU Head & Manufacturing managers (same as MFG rejection)
                await GetCcRecipientsForEffectivenessRejectionAsync(connection, concernData, recipients);

                return recipients;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting mail recipients for effectiveness rejection concern {concernData.Concern_ID}: {ex.Message}", ex);
            }
        }

        private void GetToRecipientsForEffectivenessRejectionAsync(SqlConnection connection, EffectivenessRejectionMailDataDto concernData, MailRecipientsDto recipients)
        {
            // Add only the assigned user who needs to create a new action plan
            if (!string.IsNullOrEmpty(concernData.Employee_Email))
            {
                recipients.ToRecipients.Add(new MailRecipientDto
                {
                    Email = concernData.Employee_Email.Trim(),
                    Name = concernData.Employee_Name
                });
            }
        }

        private async Task GetCcRecipientsForEffectivenessRejectionAsync(SqlConnection connection, EffectivenessRejectionMailDataDto concernData, MailRecipientsDto recipients)
        {
            var addedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Add auditor for visibility
            if (!string.IsNullOrEmpty(concernData.Auditor_Email))
            {
                var auditorEmail = concernData.Auditor_Email.Trim();
                if (addedEmails.Add(auditorEmail))
                {
                    recipients.CcRecipients.Add(new MailRecipientDto
                    {
                        Email = auditorEmail,
                        Name = concernData.Auditor_Name
                    });
                }
            }

            // Add quality officer who rejected for visibility
            if (!string.IsNullOrEmpty(concernData.Quality_Officer_Email))
            {
                var qualityOfficerEmail = concernData.Quality_Officer_Email.Trim();
                if (addedEmails.Add(qualityOfficerEmail))
                {
                    recipients.CcRecipients.Add(new MailRecipientDto
                    {
                        Email = qualityOfficerEmail,
                        Name = concernData.Quality_Officer_Name
                    });
                }
            }

            // Get Manufacturing Officers, PU Head & Manufacturing Managers for the shop
            const string ccRecipientsQuery = @"
                SELECT DISTINCT
                    e.Employee_Name,
                    e.Employee_No,
                    e.Email_Address
                FROM MM_Employee e
                INNER JOIN MM_User_Shop_Model usm ON e.Employee_ID = usm.Employee_ID
                WHERE e.Designation_ID IN (@PuHeadDesignationId, @ManufacturingManagerId, @ManufacturingOfficerId)
                    AND usm.Shop_ID = @ShopId
                    AND (e.Is_Deleted IS NULL OR e.Is_Deleted = 0)";

            using var sqlCommand = new SqlCommand(ccRecipientsQuery, connection);
            sqlCommand.Parameters.Add(new SqlParameter("@PuHeadDesignationId", Constants.PU_HEAD));
            sqlCommand.Parameters.Add(new SqlParameter("@ManufacturingManagerId", Constants.MANUFACTURING_MANAGER));
            sqlCommand.Parameters.Add(new SqlParameter("@ManufacturingOfficerId", Constants.MANUFACTURING_OFFICER));
            sqlCommand.Parameters.Add(new SqlParameter("@ShopId", concernData.Shop_ID));

            using var ccReader = await sqlCommand.ExecuteReaderAsync();
            while (await ccReader.ReadAsync())
            {
                var ccEmail = ccReader.GetString(ccReader.GetOrdinal("Email_Address")).Trim();
                
                if (addedEmails.Add(ccEmail))
                {
                    recipients.CcRecipients.Add(new MailRecipientDto
                    {
                        Email = ccEmail,
                        Name = ccReader.GetString(ccReader.GetOrdinal("Employee_Name"))
                    });
                }
            }
        }
    }

    // DTO for effectiveness rejection mail data
    public class EffectivenessRejectionMailDataDto
    {
        public decimal Concern_ID { get; set; }
        public string Employee_Name { get; set; } = string.Empty;
        public string Employee_No { get; set; } = string.Empty;
        public string Employee_Email { get; set; } = string.Empty;
        public string Manager_Name { get; set; } = string.Empty;
        public string Manager_No { get; set; } = string.Empty;
        public string Manager_Email { get; set; } = string.Empty;
        public decimal Manager_ID { get; set; }
        public string Quality_Officer_Name { get; set; } = string.Empty;
        public string Quality_Officer_No { get; set; } = string.Empty;
        public string Quality_Officer_Email { get; set; } = string.Empty;
        public decimal Quality_Officer_ID { get; set; }
        public decimal MCAR_ID { get; set; }
        public string MCAR_No { get; set; } = string.Empty;
        public string Source_Name { get; set; } = string.Empty;
        public string Shop_Name { get; set; } = string.Empty;
        public string Stage_Name { get; set; } = string.Empty;
        public DateTime Reported_Date { get; set; }
        public string Severity_Name { get; set; } = string.Empty;
        public string Problem_Definition { get; set; } = string.Empty;
        public string Part_Name { get; set; } = string.Empty;
        public DateTime Rejection_Date { get; set; }
        public DateTime MFG_Approval_Date { get; set; }
        public DateTime PCA_Completion_Date { get; set; }
        public string Plant_Code { get; set; } = string.Empty;
        public string Auditor_Name { get; set; } = string.Empty;
        public decimal Auditor_ID { get; set; }
        public string Auditor_No { get; set; } = string.Empty;
        public string Auditor_Email { get; set; } = string.Empty;
        public string Is_Repeated { get; set; } = string.Empty;
        public decimal Shop_ID { get; set; }
        public decimal Assigned_To { get; set; }
        public string Effectiveness_Remark { get; set; } = string.Empty;
    }
}
