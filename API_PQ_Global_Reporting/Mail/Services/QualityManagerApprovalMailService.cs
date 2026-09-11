using API_PQ_Global_Reporting.Mail;
using API_PQ_Global_Reporting.Models;
using API_PQ_Global_Reporting.Utils;
using Microsoft.Data.SqlClient;
using System.Net.Mail;
using System.Net;

namespace API_PQ_Global_Reporting.Mail.Services
{
    public class QualityManagerApprovalMailService : BaseMailService
    {
        public QualityManagerApprovalMailService(
            string connectionString,
            SmtpSettings smtpSettings,
            SystemSettings systemSettings) 
            : base(connectionString, smtpSettings, systemSettings)
        {
        }

        /// <summary>
        /// Sends mail notification when auditor submits effectiveness monitoring and Quality Manager needs final approval to close MCAR
        /// </summary>
        public async Task<bool> SendMailAsync(decimal concernId)
        {
            try
            {
                // Get concern details from database
                var concernDetails = await GetConcernDetailsForQualityManagerApprovalAsync(concernId);
                if (concernDetails == null)
                {
                    throw new Exception($"Concern with ID {concernId} not found");
                }

                // Validate that we have a quality manager to notify
                if (string.IsNullOrEmpty(concernDetails.Quality_Manager_No))
                {
                    throw new Exception($"Quality Manager information not found for concern {concernId}");
                }

                // Get mail recipients (To and CC lists)
                var recipients = await GetMailRecipientsForQualityManagerApprovalAsync(concernDetails);

                // Prepare mail body
                string mailBody = await PrepareMailBodyForQualityManagerApprovalAsync(concernDetails);

                // Send email with multiple recipients
                await SendEmailWithRecipientsAsync(recipients, 
                    $"MCAR Final Approval Required - {concernDetails.Source_Name} - {concernDetails.Shop_Name} - {concernDetails.MCAR_No}", 
                    mailBody);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending quality manager approval mail for concern {concernId}: {ex.Message}", ex);
            }
        }

        private async Task<string> PrepareMailBodyForQualityManagerApprovalAsync(QualityManagerApprovalMailDataDto concernDetails)
        {
            try
            {
                // Read mail template
                string mailTemplate = await ReadMailTemplateAsync("quality-manager-approval-template.html");

                // Replace placeholders with actual data
                string mailBody = ReplacePlaceholdersInTemplate(mailTemplate, GetReplacementDictionaryForQualityManagerApproval(concernDetails));

                return mailBody;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error preparing mail body for quality manager approval concern {concernDetails.Concern_ID}: {ex.Message}", ex);
            }
        }

        private Dictionary<string, string> GetReplacementDictionaryForQualityManagerApproval(QualityManagerApprovalMailDataDto data)
        {
            string plantName = GetPlantName(data.Plant_Code);

            // Calculate 2-day deadline from effectiveness submission date
            var approvalDueDate = data.Effectiveness_Submission_Date.AddDays(2);
            
            // Calculate days remaining for approval
            var daysRemaining = Math.Max(0, (approvalDueDate.Date - DateTime.Now.Date).Days);

            // Generate dynamic system URL with concern and MCAR IDs
            string systemUrl = $"{_systemSettings.BaseUrl}/mcar/actionhome?concernId={data.Concern_ID}&mcarId={data.MCAR_ID}";

            return new Dictionary<string, string>
            {
                { "[QUALITY_MANAGER_NAME]", data.Quality_Manager_Name },
                { "[MCAR_NO]", data.MCAR_No },
                { "[EMPLOYEE_NAME]", data.Employee_Name },
                { "[EMPLOYEE_ID]", data.Employee_No },
                { "[MANAGER_NAME]", data.Manager_Name },
                { "[AUDITOR_NAME]", data.Auditor_Name },
                { "[REPORTED_DATE]", data.Reported_Date.ToString("dd-MMM-yyyy") },
                { "[SOURCE_NAME]", data.Source_Name },
                { "[STAGE_NAME]", data.Stage_Name },
                { "[PART_NAME]", data.Part_Name },
                { "[SEVERITY_NAME]", data.Severity_Name },
                { "[IS_REPEATED]", data.Is_Repeated ? "Yes" : "No" },
                { "[PROBLEM_DEFINITION]", data.Problem_Definition },
                { "[MFG_APPROVAL_DATE]", data.MFG_Approval_Date.ToString("dd-MMM-yyyy hh:mm tt") },
                { "[PCA_COMPLETION_DATE]", data.PCA_Completion_Date.ToString("dd-MMM-yyyy") },
                { "[EFFECTIVENESS_SUBMISSION_DATE]", data.Effectiveness_Submission_Date.ToString("dd-MMM-yyyy hh:mm tt") },
                { "[APPROVAL_DUE_DATE]", approvalDueDate.ToString("dd-MMM-yyyy") },
                { "[DAYS_REMAINING]", daysRemaining.ToString() },
                { "[PLANT_NAME]", plantName },
                { "[CURRENT_DATE]", DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt") },
                { "[SYSTEM_URL]", systemUrl }
            };
        }

        private async Task<QualityManagerApprovalMailDataDto?> GetConcernDetailsForQualityManagerApprovalAsync(decimal concernId)
        {
            try
            {
                const string query = @"
                    SELECT 
                        cd.Concern_ID,
                        cd.Date_Of_Complaint as Reported_Date,
                        cd.Is_Repeated,
                        ISNULL(cd.Problem_Defination, '') as Problem_Definition,
                        cd.Plant_Code,
                        cd.Shop_ID,
                        ISNULL(cd.Assigned_To, 0) as Assigned_To,
                        ISNULL(cd.Inserted_User_ID, 0) as Inserted_User_ID,
                        
                        -- Employee Info (Assigned To)
                        ISNULL(emp.Employee_Name, '') as Employee_Name,
                        ISNULL(emp.Employee_No, '') as Employee_No,
                        ISNULL(emp.Email_Address, '') as Employee_Email,
                        
                        -- Manager Info (Manufacturing Manager who approved action plan)
                        ISNULL(mgr.Employee_Name, '') as Manager_Name,
                        ISNULL(mgr.Employee_No, '') as Manager_No,
                        ISNULL(mgr.Email_Address, '') as Manager_Email,
                        ISNULL(mgr.Employee_ID, 0) as Manager_ID,
                        
                        -- Quality Manager Info (Reporting manager of person who inserted concern)
                        ISNULL(qm.Employee_Name, '') as Quality_Manager_Name,
                        ISNULL(qm.Employee_No, '') as Quality_Manager_No,
                        ISNULL(qm.Email_Address, '') as Quality_Manager_Email,
                        ISNULL(qm.Employee_ID, 0) as Quality_Manager_ID,
                        
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
                        
                        -- Auditor Info (Who completed effectiveness monitoring)
                        ISNULL(aud.Employee_Name, '') as Auditor_Name,
                        ISNULL(aud.Employee_ID, 0) as Auditor_ID,
                        ISNULL(aud.Employee_No, '') as Auditor_No,
                        ISNULL(aud.Email_Address, '') as Auditor_Email,
                        
                        -- Process Dates
                        ISNULL(pl_mfg_approval.Inserted_Date, GETDATE()) as MFG_Approval_Date,
                        ISNULL(pca_latest.Target_Date, GETDATE()) as PCA_Completion_Date,
                        ISNULL(pl_effectiveness.Inserted_Date, GETDATE()) as Effectiveness_Submission_Date
                        
                    FROM MM_Concern_Data cd
                    LEFT JOIN MM_Employee emp ON cd.Assigned_To = emp.Employee_ID
                    LEFT JOIN MM_Employee mgr ON emp.Reporting_Manager_ID = mgr.Employee_ID
                    LEFT JOIN MM_MCAR_Registration mr ON cd.MCAR_ID = mr.MCAR_ID
                    LEFT JOIN MM_Source_Master sm ON cd.Source_ID = sm.Source_ID
                    LEFT JOIN MM_Shop shop ON cd.Shop_ID = shop.Shop_ID
                    LEFT JOIN MM_Stage_Master st ON cd.Stage_ID = st.Stage_ID
                    LEFT JOIN MM_Severity_Master sev ON cd.Severity_ID = sev.Severity_ID
                    LEFT JOIN MM_Employee aud ON cd.Auditor_ID = aud.Employee_ID
                    
                    -- Get Quality Manager (Reporting manager of concern inserted user)
                    LEFT JOIN MM_Employee ins_emp ON cd.Inserted_User_ID = ins_emp.Employee_ID
                    LEFT JOIN MM_Employee qm ON ins_emp.Reporting_Manager_ID = qm.Employee_ID
                    
                    -- Get MFG Approval Date (from MM_Approval_Tracking where Is_MFG = true and Is_Approved = true)
                    LEFT JOIN MM_Approval_Tracking pl_mfg_approval ON cd.Concern_ID = pl_mfg_approval.Concern_ID 
                        AND pl_mfg_approval.Is_MFG = 1 AND pl_mfg_approval.Is_Approved = 1
                    
                    -- Get PCA Completion Date (latest target date)
                    LEFT JOIN (
                        SELECT 
                            Concern_ID,
                            MAX(Target_Date) as Target_Date
                        FROM MM_Action_Plan_PCA 
                        WHERE (Is_Deleted IS NULL OR Is_Deleted = 0)
                        GROUP BY Concern_ID
                    ) pca_latest ON cd.Concern_ID = pca_latest.Concern_ID
                    
                    -- Get Effectiveness Submission Date (from MM_Approval_Tracking where Is_Effectivness = true and Is_Approved = true)
                    LEFT JOIN MM_Approval_Tracking pl_effectiveness ON cd.Concern_ID = pl_effectiveness.Concern_ID 
                        AND pl_effectiveness.Is_Effectivness = 1 AND pl_effectiveness.Is_Approved = 1
                        
                    WHERE cd.Concern_ID = @ConcernId AND (cd.Is_Deleted IS NULL OR cd.Is_Deleted = 0)";

                using var connection = await CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);
                command.Parameters.Add(new SqlParameter("@ConcernId", concernId));

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var mfgApprovalDate = reader.IsDBNull(reader.GetOrdinal("MFG_Approval_Date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("MFG_Approval_Date"));
                    var pcaCompletionDate = reader.IsDBNull(reader.GetOrdinal("PCA_Completion_Date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("PCA_Completion_Date"));
                    var effectivenessSubmissionDate = reader.IsDBNull(reader.GetOrdinal("Effectiveness_Submission_Date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("Effectiveness_Submission_Date"));
                    var reportedDate = reader.GetDateTime(reader.GetOrdinal("Reported_Date"));
                    var isRepeated = !reader.IsDBNull(reader.GetOrdinal("Is_Repeated")) && reader.GetBoolean(reader.GetOrdinal("Is_Repeated"));

                    return new QualityManagerApprovalMailDataDto
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
                        Quality_Manager_Name = reader.GetString(reader.GetOrdinal("Quality_Manager_Name")),
                        Quality_Manager_No = reader.GetString(reader.GetOrdinal("Quality_Manager_No")),
                        Quality_Manager_Email = reader.GetString(reader.GetOrdinal("Quality_Manager_Email")),
                        Quality_Manager_ID = reader.GetDecimal(reader.GetOrdinal("Quality_Manager_ID")),
                        MCAR_ID = reader.GetDecimal(reader.GetOrdinal("MCAR_ID")),
                        MCAR_No = reader.GetString(reader.GetOrdinal("MCAR_No")),
                        Source_Name = reader.GetString(reader.GetOrdinal("Source_Name")),
                        Shop_Name = reader.GetString(reader.GetOrdinal("Shop_Name")),
                        Stage_Name = reader.GetString(reader.GetOrdinal("Stage_Name")),
                        Reported_Date = reportedDate,
                        Severity_Name = reader.GetString(reader.GetOrdinal("Severity_Name")),
                        Problem_Definition = reader.GetString(reader.GetOrdinal("Problem_Definition")),
                        Part_Name = reader.GetString(reader.GetOrdinal("Part_Name")),
                        Is_Repeated = isRepeated,
                        MFG_Approval_Date = mfgApprovalDate,
                        PCA_Completion_Date = pcaCompletionDate,
                        Effectiveness_Submission_Date = effectivenessSubmissionDate,
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
                throw new Exception($"Error retrieving concern details for quality manager approval mail: {ex.Message}", ex);
            }

            return null;
        }

        private async Task<MailRecipientsDto> GetMailRecipientsForQualityManagerApprovalAsync(QualityManagerApprovalMailDataDto concernData)
        {
            var recipients = new MailRecipientsDto();

            try
            {
                using var connection = await CreateConnectionAsync();

                // Get TO recipients: Quality Manager for final approval
                GetToRecipientsForQualityManagerApprovalAsync(connection, concernData, recipients);

                // Get CC recipients: Manufacturing manager, assigned employee, auditor, and PU Head for visibility
                await GetCcRecipientsForQualityManagerApprovalAsync(connection, concernData, recipients);

                return recipients;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting mail recipients for quality manager approval concern {concernData.Concern_ID}: {ex.Message}", ex);
            }
        }

        private void GetToRecipientsForQualityManagerApprovalAsync(SqlConnection connection, QualityManagerApprovalMailDataDto concernData, MailRecipientsDto recipients)
        {
            // Add only the Quality Manager who needs to provide final approval
            if (!string.IsNullOrEmpty(concernData.Quality_Manager_Email))
            {
                recipients.ToRecipients.Add(new MailRecipientDto
                {
                    Email = concernData.Quality_Manager_Email.Trim(),
                    Name = concernData.Quality_Manager_Name
                });
            }
        }

        private async Task GetCcRecipientsForQualityManagerApprovalAsync(SqlConnection connection, QualityManagerApprovalMailDataDto concernData, MailRecipientsDto recipients)
        {
            var addedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Add the manufacturing manager who approved the action plan
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

            // Add the assigned employee for visibility
            if (!string.IsNullOrEmpty(concernData.Employee_Email))
            {
                var employeeEmail = concernData.Employee_Email.Trim();
                if (addedEmails.Add(employeeEmail))
                {
                    recipients.CcRecipients.Add(new MailRecipientDto
                    {
                        Email = employeeEmail,
                        Name = concernData.Employee_Name
                    });
                }
            }

            // Add the auditor who completed effectiveness monitoring
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

            // Add PU Head for the shop (for visibility on final closure process)
            const string ccRecipientsQuery = @"
                SELECT DISTINCT
                    e.Employee_Name,
                    e.Employee_No,
                    e.Email_Address
                FROM MM_Employee e
                INNER JOIN MM_User_Shop_Model usm ON e.Employee_ID = usm.Employee_ID
                WHERE e.Designation_ID = @PuHeadDesignationId
                    AND usm.Shop_ID = @ShopId
                    AND (e.Is_Deleted IS NULL OR e.Is_Deleted = 0)";

            using var sqlCommand = new SqlCommand(ccRecipientsQuery, connection);
            sqlCommand.Parameters.Add(new SqlParameter("@PuHeadDesignationId", Constants.PU_HEAD));
            sqlCommand.Parameters.Add(new SqlParameter("@ShopId", concernData.Shop_ID));

            using var ccReader = await sqlCommand.ExecuteReaderAsync();
            while (await ccReader.ReadAsync())
            {
                var puHeadEmail = ccReader.GetString(ccReader.GetOrdinal("Email_Address")).Trim();
                
                if (addedEmails.Add(puHeadEmail))
                {
                    recipients.CcRecipients.Add(new MailRecipientDto
                    {
                        Email = puHeadEmail,
                        Name = ccReader.GetString(ccReader.GetOrdinal("Employee_Name"))
                    });
                }
            }
        }
    }

    // DTO for quality manager approval mail data
    public class QualityManagerApprovalMailDataDto
    {
        public decimal Concern_ID { get; set; }
        public decimal Inserted_User_ID { get; set; }
        public string Employee_Name { get; set; } = string.Empty;
        public string Employee_No { get; set; } = string.Empty;
        public string Employee_Email { get; set; } = string.Empty;
        public string Manager_Name { get; set; } = string.Empty;
        public string Manager_No { get; set; } = string.Empty;
        public string Manager_Email { get; set; } = string.Empty;
        public decimal Manager_ID { get; set; }
        public string Quality_Manager_Name { get; set; } = string.Empty;
        public string Quality_Manager_No { get; set; } = string.Empty;
        public string Quality_Manager_Email { get; set; } = string.Empty;
        public decimal Quality_Manager_ID { get; set; }
        public decimal MCAR_ID { get; set; }
        public string MCAR_No { get; set; } = string.Empty;
        public string Source_Name { get; set; } = string.Empty;
        public string Shop_Name { get; set; } = string.Empty;
        public string Stage_Name { get; set; } = string.Empty;
        public DateTime Reported_Date { get; set; }
        public string Severity_Name { get; set; } = string.Empty;
        public string Problem_Definition { get; set; } = string.Empty;
        public string Part_Name { get; set; } = string.Empty;
        public bool Is_Repeated { get; set; }
        public DateTime MFG_Approval_Date { get; set; }
        public DateTime PCA_Completion_Date { get; set; }
        public DateTime Effectiveness_Submission_Date { get; set; }
        public string Plant_Code { get; set; } = string.Empty;
        public string Auditor_Name { get; set; } = string.Empty;
        public decimal Auditor_ID { get; set; }
        public string Auditor_No { get; set; } = string.Empty;
        public string Auditor_Email { get; set; } = string.Empty;
        public decimal Shop_ID { get; set; }
        public decimal Assigned_To { get; set; }
    }
}
