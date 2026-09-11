using API_PQ_Global_Reporting.Mail;
using API_PQ_Global_Reporting.Models;
using API_PQ_Global_Reporting.Utils;
using Microsoft.Data.SqlClient;
using System.Net.Mail;
using System.Net;

namespace API_PQ_Global_Reporting.Mail.Services
{
    public class EffectivenessMonitoringMailService : BaseMailService
    {
        public EffectivenessMonitoringMailService(
            string connectionString,
            SmtpSettings smtpSettings,
            SystemSettings systemSettings) 
            : base(connectionString, smtpSettings, systemSettings)
        {
        }

        /// <summary>
        /// Sends mail notification when manufacturing manager approves action plan and auditor needs to monitor effectiveness
        /// </summary>
        public async Task<bool> SendMailAsync(decimal concernId)
        {
            try
            {
                // Get concern details from database
                var concernDetails = await GetConcernDetailsForEffectivenessMailAsync(concernId);
                if (concernDetails == null)
                {
                    throw new Exception($"Concern with ID {concernId} not found");
                }

                if (string.IsNullOrEmpty(concernDetails.Auditor_No))
                {
                    throw new Exception($"Auditor information not found for concern {concernId}");
                }

                // Get mail recipients (To and CC lists)
                var recipients = await GetMailRecipientsForEffectivenessAsync(concernDetails);

                // Prepare mail body
                string mailBody = await PrepareMailBodyForEffectivenessAsync(concernDetails);

                // Send email with multiple recipients
                await SendEmailWithRecipientsAsync(recipients, 
                    $"MCAR Effectiveness Verification Required - {concernDetails.Source_Name} - {concernDetails.Shop_Name} - {concernDetails.Stage_Name}", 
                    mailBody);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending effectiveness monitoring mail for concern {concernId}: {ex.Message}", ex);
            }
        }

        private async Task<string> PrepareMailBodyForEffectivenessAsync(EffectivenessMonitoringMailDataDto concernDetails)
        {
            try
            {
                // Read mail template
                string mailTemplate = await ReadMailTemplateAsync("effectiveness-monitoring-template.html");

                // Replace placeholders with actual data
                string mailBody = ReplacePlaceholdersInTemplate(mailTemplate, GetReplacementDictionaryForEffectiveness(concernDetails));

                return mailBody;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error preparing mail body for effectiveness monitoring concern {concernDetails.Concern_ID}: {ex.Message}", ex);
            }
        }

        private Dictionary<string, string> GetReplacementDictionaryForEffectiveness(EffectivenessMonitoringMailDataDto data)
        {
            string plantName = GetPlantName(data.Plant_Code);

            // Calculate monitoring dates based on PCA completion date
            var monitoringEndDate = data.PCA_Completion_Date.AddDays(Constants.EFFECTIVNESS_DONE_AFTER_DAYS); // n days monitoring period
            var assessmentDueDate = monitoringEndDate.AddDays(2); // 2 days after monitoring ends
            
            // Calculate days remaining for assessment
            var daysRemaining = Math.Max(0, (assessmentDueDate.Date - DateTime.Now.Date).Days);

            // Generate dynamic system URL with concern and MCAR IDs
            string systemUrl = $"{_systemSettings.BaseUrl}/mcar/actionhome?concernId={data.Concern_ID}&mcarId={data.MCAR_ID}";

            return new Dictionary<string, string>
            {
                { "[AUDITOR_NAME]", data.Auditor_Name },
                { "[MCAR_NO]", data.MCAR_No },
                { "[EMPLOYEE_NAME]", data.Employee_Name },
                { "[EMPLOYEE_ID]", data.Employee_No },
                { "[MANAGER_NAME]", data.Manager_Name },
                { "[APPROVAL_DATE]", data.Approval_Date.ToString("dd-MMM-yyyy hh:mm tt") },
                { "[SOURCE_NAME]", data.Source_Name },
                { "[STAGE_NAME]", data.Stage_Name },
                { "[PART_NAME]", data.Part_Name },
                { "[SEVERITY_NAME]", data.Severity_Name },
                { "[PCA_COMPLETION_DATE]", data.PCA_Completion_Date.ToString("dd-MMM-yyyy") },
                { "[MONITORING_END_DATE]", monitoringEndDate.ToString("dd-MMM-yyyy") },
                { "[ASSESSMENT_DUE_DATE]", assessmentDueDate.ToString("dd-MMM-yyyy") },
                { "[DAYS_REMAINING]", daysRemaining.ToString() },
                { "[PROBLEM_DEFINITION]", data.Problem_Definition },
                { "[PLANT_NAME]", plantName },
                { "[CURRENT_DATE]", DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt") },
                { "[SYSTEM_URL]", systemUrl }
            };
        }

        private async Task<EffectivenessMonitoringMailDataDto?> GetConcernDetailsForEffectivenessMailAsync(decimal concernId)
        {
            try
            {
                const string query = @"
                    SELECT 
                        cd.Concern_ID,
                        cd.Date_Of_Complaint as Reported_Date,
                        ISNULL(pl_approval.Inserted_Date, GETDATE()) as Approval_Date,
                        ISNULL(pca_latest.Target_Date, GETDATE()) as PCA_Completion_Date,
                        ISNULL(cd.Problem_Defination, '') as Problem_Definition,
                        cd.Plant_Code,
                        cd.Shop_ID,
                        ISNULL(cd.Assigned_To, 0) as Assigned_To,
                        ISNULL(cd.Inserted_User_ID, 0) as Inserted_User_ID,
                        
                        -- Employee Info (Assigned To)
                        ISNULL(emp.Employee_Name, '') as Employee_Name,
                        ISNULL(emp.Employee_No, '') as Employee_No,
                        ISNULL(emp.Email_Address, '') as Employee_Email,
                        
                        -- Manager Info (Who approved - Manufacturing Manager)
                        ISNULL(mgr.Employee_Name, '') as Manager_Name,
                        ISNULL(mgr.Employee_No, '') as Manager_No,
                        ISNULL(mgr.Email_Address, '') as Manager_Email,
                        ISNULL(mgr.Employee_ID, 0) as Manager_ID,
                        
                        -- Inserted User Info (Who created the concern)
                        ISNULL(ins_emp.Employee_Name, '') as Inserted_User_Name,
                        ISNULL(ins_emp.Employee_No, '') as Inserted_User_No,
                        ISNULL(ins_emp.Email_Address, '') as Inserted_User_Email,
                        
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
                        
                        -- Auditor Info (Who will monitor effectiveness)
                        ISNULL(aud.Employee_Name, '') as Auditor_Name,
                        ISNULL(aud.Employee_ID, 0) as Auditor_ID,
                        ISNULL(aud.Employee_No, '') as Auditor_No,
                        ISNULL(aud.Email_Address, '') as Auditor_Email
                        
                    FROM MM_Concern_Data cd
                    LEFT JOIN MM_Employee emp ON cd.Assigned_To = emp.Employee_ID
                    LEFT JOIN MM_Employee mgr ON emp.Reporting_Manager_ID = mgr.Employee_ID
                    LEFT JOIN MM_Employee ins_emp ON cd.Inserted_User_ID = ins_emp.Employee_ID
                    LEFT JOIN MM_MCAR_Registration mr ON cd.MCAR_ID = mr.MCAR_ID
                    LEFT JOIN MM_Source_Master sm ON cd.Source_ID = sm.Source_ID
                    LEFT JOIN MM_Shop shop ON cd.Shop_ID = shop.Shop_ID
                    LEFT JOIN MM_Stage_Master st ON cd.Stage_ID = st.Stage_ID
                    LEFT JOIN MM_Severity_Master sev ON cd.Severity_ID = sev.Severity_ID
                    LEFT JOIN MM_Employee aud ON cd.Auditor_ID = aud.Employee_ID
                    LEFT JOIN MM_Process_Logs pl_approval ON cd.Concern_ID = pl_approval.Concern_ID AND pl_approval.Process_ID = @MfgApprovalProcessId
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
                command.Parameters.Add(new SqlParameter("@MfgApprovalProcessId", Constants.MfgApproval));

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var approvalDate = reader.IsDBNull(reader.GetOrdinal("Approval_Date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("Approval_Date"));
                    var pcaCompletionDate = reader.IsDBNull(reader.GetOrdinal("PCA_Completion_Date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("PCA_Completion_Date"));
                    var reportedDate = reader.GetDateTime(reader.GetOrdinal("Reported_Date"));

                    return new EffectivenessMonitoringMailDataDto
                    {
                        Concern_ID = reader.GetDecimal(reader.GetOrdinal("Concern_ID")),
                        Employee_Name = reader.GetString(reader.GetOrdinal("Employee_Name")),
                        Employee_No = reader.GetString(reader.GetOrdinal("Employee_No")),
                        Employee_Email = reader.GetString(reader.GetOrdinal("Employee_Email")),
                        Manager_Name = reader.GetString(reader.GetOrdinal("Manager_Name")),
                        Manager_No = reader.GetString(reader.GetOrdinal("Manager_No")),
                        Manager_Email = reader.GetString(reader.GetOrdinal("Manager_Email")),
                        Manager_ID = reader.GetDecimal(reader.GetOrdinal("Manager_ID")),
                        Inserted_User_ID = reader.GetDecimal(reader.GetOrdinal("Inserted_User_ID")),
                        Inserted_User_Name = reader.GetString(reader.GetOrdinal("Inserted_User_Name")),
                        Inserted_User_No = reader.GetString(reader.GetOrdinal("Inserted_User_No")),
                        Inserted_User_Email = reader.GetString(reader.GetOrdinal("Inserted_User_Email")),
                        MCAR_ID = reader.GetDecimal(reader.GetOrdinal("MCAR_ID")),
                        MCAR_No = reader.GetString(reader.GetOrdinal("MCAR_No")),
                        Source_Name = reader.GetString(reader.GetOrdinal("Source_Name")),
                        Shop_Name = reader.GetString(reader.GetOrdinal("Shop_Name")),
                        Stage_Name = reader.GetString(reader.GetOrdinal("Stage_Name")),
                        Reported_Date = reportedDate,
                        Severity_Name = reader.GetString(reader.GetOrdinal("Severity_Name")),
                        Problem_Definition = reader.GetString(reader.GetOrdinal("Problem_Definition")),
                        Part_Name = reader.GetString(reader.GetOrdinal("Part_Name")),
                        Approval_Date = approvalDate,
                        PCA_Completion_Date = pcaCompletionDate,
                        Plant_Code = reader.GetString(reader.GetOrdinal("Plant_Code")),
                        Auditor_Name = reader.GetString(reader.GetOrdinal("Auditor_Name")),
                        Auditor_ID = reader.GetDecimal(reader.GetOrdinal("Auditor_ID")),
                        Auditor_No = reader.GetString(reader.GetOrdinal("Auditor_No")),
                        Auditor_Email = reader.GetString(reader.GetOrdinal("Auditor_Email")),
                        Shop_ID = reader.GetDecimal(reader.GetOrdinal("Shop_ID")),
                        Assigned_To = reader.GetDecimal(reader.GetOrdinal("Assigned_To"))
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving concern details for effectiveness monitoring mail: {ex.Message}", ex);
            }

            return null;
        }

        private async Task<MailRecipientsDto> GetMailRecipientsForEffectivenessAsync(EffectivenessMonitoringMailDataDto concernData)
        {
            var recipients = new MailRecipientsDto();

            try
            {
                using var connection = await CreateConnectionAsync();

                // Get TO recipients: Quality officers, auditor, and person who inserted concern
                await GetToRecipientsForEffectivenessAsync(connection, concernData, recipients);

                // Get CC recipients: Manufacturing manager who approved + assigned employee for visibility
                await GetCcRecipientsForEffectivenessAsync(connection, concernData, recipients);

                return recipients;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting mail recipients for effectiveness monitoring concern {concernData.Concern_ID}: {ex.Message}", ex);
            }
        }

        private async Task GetToRecipientsForEffectivenessAsync(SqlConnection connection, EffectivenessMonitoringMailDataDto concernData, MailRecipientsDto recipients)
        {
            var addedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Add the auditor who needs to monitor effectiveness
            if (!string.IsNullOrEmpty(concernData.Auditor_Email))
            {
                var auditorEmail = concernData.Auditor_Email.Trim();
                if (addedEmails.Add(auditorEmail))
                {
                    recipients.ToRecipients.Add(new MailRecipientDto
                    {
                        Email = auditorEmail,
                        Name = concernData.Auditor_Name
                    });
                }
            }

            // Add the person who inserted/created the concern
            if (!string.IsNullOrEmpty(concernData.Inserted_User_Email))
            {
                var insertedUserEmail = concernData.Inserted_User_Email.Trim();
                if (addedEmails.Add(insertedUserEmail))
                {
                    recipients.ToRecipients.Add(new MailRecipientDto
                    {
                        Email = insertedUserEmail,
                        Name = concernData.Inserted_User_Name
                    });
                }
            }

            // Add all quality officers for the shop
            const string qualityOfficersQuery = @"
                SELECT DISTINCT
                    e.Employee_Name,
                    e.Employee_No,
                    e.Email_Address
                FROM MM_Employee e
                INNER JOIN MM_User_Shop_Model usm ON e.Employee_ID = usm.Employee_ID
                WHERE e.Designation_ID = @QualityOfficerDesignationId
                    AND usm.Shop_ID = @ShopId
                    AND (e.Is_Deleted IS NULL OR e.Is_Deleted = 0)";

            using var qualityOfficersCommand = new SqlCommand(qualityOfficersQuery, connection);
            qualityOfficersCommand.Parameters.Add(new SqlParameter("@QualityOfficerDesignationId", Constants.QUALITY_OFFICER));
            qualityOfficersCommand.Parameters.Add(new SqlParameter("@ShopId", concernData.Shop_ID));

            using var qualityReader = await qualityOfficersCommand.ExecuteReaderAsync();
            while (await qualityReader.ReadAsync())
            {
                var qualityOfficerEmail = qualityReader.GetString(qualityReader.GetOrdinal("Email_Address")).Trim();
                
                if (addedEmails.Add(qualityOfficerEmail))
                {
                    recipients.ToRecipients.Add(new MailRecipientDto
                    {
                        Email = qualityOfficerEmail,
                        Name = qualityReader.GetString(qualityReader.GetOrdinal("Employee_Name"))
                    });
                }
            }
        }

        private async Task GetCcRecipientsForEffectivenessAsync(SqlConnection connection, EffectivenessMonitoringMailDataDto concernData, MailRecipientsDto recipients)
        {
            var addedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Add the manufacturing manager who approved
            if (!string.IsNullOrEmpty(concernData.Manager_Email))
            {
                var managerEmail = concernData.Manager_Email.Trim();
                if (addedEmails.Add(managerEmail))
                {
                    recipients.CcRecipients.Add(new MailRecipientDto
                    {
                        Email = managerEmail,
                        Name = concernData.Manager_Name
                    });
                }
            }
            // Add PU Head, Quality & manufacturing Manager for the shop (for visibility on monitoring process)
            const string ccRecipientsQuery = @"
                SELECT DISTINCT
                    e.Employee_Name,
                    e.Employee_No,
                    e.Email_Address
                FROM MM_Employee e
                INNER JOIN MM_User_Shop_Model usm ON e.Employee_ID = usm.Employee_ID
                WHERE e.Designation_ID IN (@MfgManagerDesignationId, @QualityManagerId)
                    AND usm.Shop_ID = @ShopId";

            using var sqlCommand = new SqlCommand(ccRecipientsQuery, connection);
            sqlCommand.Parameters.Add(new SqlParameter("@MfgManagerDesignationId", Constants.MANUFACTURING_MANAGER));
            sqlCommand.Parameters.Add(new SqlParameter("@QualityManagerId", Constants.QUALITY_MANAGER));
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

    // DTO for effectiveness monitoring mail data
    public class EffectivenessMonitoringMailDataDto
    {
        public decimal Concern_ID { get; set; }
        public string Employee_Name { get; set; } = string.Empty;
        public string Employee_No { get; set; } = string.Empty;
        public string Employee_Email { get; set; } = string.Empty;
        public string Manager_Name { get; set; } = string.Empty;
        public string Manager_No { get; set; } = string.Empty;
        public string Manager_Email { get; set; } = string.Empty;
        public decimal Manager_ID { get; set; }
        public decimal Inserted_User_ID { get; set; }
        public string Inserted_User_Name { get; set; } = string.Empty;
        public string Inserted_User_No { get; set; } = string.Empty;
        public string Inserted_User_Email { get; set; } = string.Empty;
        public decimal MCAR_ID { get; set; }
        public string MCAR_No { get; set; } = string.Empty;
        public string Source_Name { get; set; } = string.Empty;
        public string Shop_Name { get; set; } = string.Empty;
        public string Stage_Name { get; set; } = string.Empty;
        public DateTime Reported_Date { get; set; }
        public string Severity_Name { get; set; } = string.Empty;
        public string Problem_Definition { get; set; } = string.Empty;
        public string Part_Name { get; set; } = string.Empty;
        public DateTime Approval_Date { get; set; }
        public DateTime PCA_Completion_Date { get; set; }
        public string Plant_Code { get; set; } = string.Empty;
        public string Auditor_Name { get; set; } = string.Empty;
        public decimal Auditor_ID { get; set; }
        public string Auditor_No { get; set; } = string.Empty;
        public string Auditor_Email { get; set; } = string.Empty;
        public decimal Shop_ID { get; set; }
        public decimal Assigned_To { get; set; }
    }
}
