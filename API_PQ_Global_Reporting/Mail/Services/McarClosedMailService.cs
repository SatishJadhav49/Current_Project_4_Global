using API_PQ_Global_Reporting.Mail;
using API_PQ_Global_Reporting.Models;
using API_PQ_Global_Reporting.Utils;
using Microsoft.Data.SqlClient;
using System.Net.Mail;
using System.Net;

namespace API_PQ_Global_Reporting.Mail.Services
{
    public class McarClosedMailService : BaseMailService
    {
        public McarClosedMailService(
            string connectionString,
            SmtpSettings smtpSettings,
            SystemSettings systemSettings) 
            : base(connectionString, smtpSettings, systemSettings)
        {
        }

        /// <summary>
        /// Sends mail notification when Quality Manager provides final approval and MCAR is closed
        /// </summary>
        public async Task<bool> SendMailAsync(decimal concernId)
        {
            try
            {
                // Get concern details from database
                var concernDetails = await GetConcernDetailsForMcarClosedAsync(concernId);
                if (concernDetails == null)
                {
                    throw new Exception($"Concern with ID {concernId} not found");
                }

                // Get mail recipients (To and CC lists)
                var recipients = await GetMailRecipientsForMcarClosedAsync(concernDetails);

                // Prepare mail body
                string mailBody = await PrepareMailBodyForMcarClosedAsync(concernDetails);

                // Send email with multiple recipients
                await SendEmailWithRecipientsAsync(recipients, 
                    $"MCAR Successfully Closed - {concernDetails.Source_Name} - {concernDetails.Shop_Name} - {concernDetails.MCAR_No}", 
                    mailBody);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending MCAR closed mail for concern {concernId}: {ex.Message}", ex);
            }
        }

        private async Task<string> PrepareMailBodyForMcarClosedAsync(McarClosedMailDataDto concernDetails)
        {
            try
            {
                // Read mail template
                string mailTemplate = await ReadMailTemplateAsync("mcar-closed-template.html");

                // Replace placeholders with actual data
                string mailBody = ReplacePlaceholdersInTemplate(mailTemplate, GetReplacementDictionaryForMcarClosed(concernDetails));

                return mailBody;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error preparing mail body for MCAR closed concern {concernDetails.Concern_ID}: {ex.Message}", ex);
            }
        }

        private Dictionary<string, string> GetReplacementDictionaryForMcarClosed(McarClosedMailDataDto data)
        {
            string plantName = GetPlantName(data.Plant_Code);

            // Calculate total duration from concern reported date to closure date
            var totalDuration = (data.Closure_Date.Date - data.Reported_Date.Date).Days;

            // Generate dynamic system URL with concern and MCAR IDs
            string systemUrl = $"{_systemSettings.BaseUrl}/mcar/actionhome?concernId={data.Concern_ID}&mcarId={data.MCAR_ID}";
            string downloadUrl = $"{_systemSettings.ReportURL}{data.Concern_ID}";

            return new Dictionary<string, string>
            {
                { "[MCAR_NO]", data.MCAR_No },
                { "[CLOSURE_DATE]", data.Closure_Date.ToString("dd-MMM-yyyy hh:mm tt") },
                { "[QUALITY_MANAGER_NAME]", data.Quality_Manager_Name },
                { "[EMPLOYEE_NAME]", data.Employee_Name },
                { "[EMPLOYEE_ID]", data.Employee_No },
                { "[ASSIGNED_EMPLOYEE_NAME]", data.Assigned_Employee_Name },
                { "[ASSIGNED_EMPLOYEE_ID]", data.Assigned_Employee_No },
                { "[MANAGER_NAME]", data.Manager_Name },
                { "[AUDITOR_NAME]", data.Auditor_Name },
                { "[REPORTED_DATE]", data.Reported_Date.ToString("dd-MMM-yyyy") },
                { "[MFG_APPROVAL_DATE]", data.MFG_Approval_Date.ToString("dd-MMM-yyyy hh:mm tt") },
                { "[ACTION_PLAN_SUBMISSION_DATE]", data.Action_Plan_Submission_Date.ToString("dd-MMM-yyyy hh:mm tt") },
                { "[EFFECTIVENESS_COMPLETION_DATE]", data.Effectiveness_Completion_Date.ToString("dd-MMM-yyyy hh:mm tt") },
                { "[SOURCE_NAME]", data.Source_Name },
                { "[STAGE_NAME]", data.Stage_Name },
                { "[PART_NAME]", data.Part_Name },
                { "[SEVERITY_NAME]", data.Severity_Name },
                { "[IS_REPEATED]", data.Is_Repeated ? "Yes" : "No" },
                { "[PROBLEM_DEFINITION]", data.Problem_Definition },
                { "[TOTAL_DURATION]", totalDuration.ToString() },
                { "[PLANT_NAME]", plantName },
                { "[SHOP_NAME]", data.Shop_Name },
                { "[CURRENT_DATE]", DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt") },
                { "[SYSTEM_URL]", systemUrl },
                { "[DOWNLOAD_URL]", downloadUrl }
            };
        }

        private async Task<McarClosedMailDataDto?> GetConcernDetailsForMcarClosedAsync(decimal concernId)
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
                        
                        -- Employee Info (Person who reported concern)
                        ISNULL(ins_emp.Employee_Name, '') as Employee_Name,
                        ISNULL(ins_emp.Employee_No, '') as Employee_No,
                        ISNULL(ins_emp.Email_Address, '') as Employee_Email,
                        
                        -- Assigned Employee Info (Person who worked on action plan)
                        ISNULL(assigned_emp.Employee_Name, '') as Assigned_Employee_Name,
                        ISNULL(assigned_emp.Employee_No, '') as Assigned_Employee_No,
                        ISNULL(assigned_emp.Email_Address, '') as Assigned_Employee_Email,
                        
                        -- Manager Info (User who provided MFG approval)
                        ISNULL(mgr.Employee_Name, '') as Manager_Name,
                        ISNULL(mgr.Employee_No, '') as Manager_No,
                        ISNULL(mgr.Email_Address, '') as Manager_Email,
                        ISNULL(mgr.Employee_ID, 0) as Manager_ID,
                        
                        -- Quality Manager Info (User who provided Quality approval)
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
                        ISNULL(at_mfg.Inserted_Date, GETDATE()) as MFG_Approval_Date,
                        ISNULL(at_action_plan.Inserted_Date, GETDATE()) as Action_Plan_Submission_Date,
                        ISNULL(at_effectiveness.Inserted_Date, GETDATE()) as Effectiveness_Completion_Date,
                        ISNULL(at_quality_closure.Inserted_Date, GETDATE()) as Closure_Date
                        
                    FROM MM_Concern_Data cd
                    
                    -- Get person who reported concern
                    LEFT JOIN MM_Employee ins_emp ON cd.Inserted_User_ID = ins_emp.Employee_ID
                    
                    -- Get assigned employee (who worked on action plan)
                    LEFT JOIN MM_Employee assigned_emp ON cd.Assigned_To = assigned_emp.Employee_ID
                    
                    -- Get manufacturing manager (user who provided MFG approval)
                    LEFT JOIN MM_Approval_Tracking at_mfg_user ON cd.Concern_ID = at_mfg_user.Concern_ID 
                        AND at_mfg_user.Is_MFG = 1 AND at_mfg_user.Is_Approved = 1
                    LEFT JOIN MM_Employee mgr ON at_mfg_user.Inserted_User_ID = mgr.Employee_ID
                    
                    -- Get Quality Manager (user who provided Quality approval)
                    LEFT JOIN MM_Approval_Tracking at_quality_user ON cd.Concern_ID = at_quality_user.Concern_ID 
                        AND at_quality_user.Is_Quality = 1 AND at_quality_user.Is_Approved = 1
                    LEFT JOIN MM_Employee qm ON at_quality_user.Inserted_User_ID = qm.Employee_ID
                    
                    -- Get MCAR registration info
                    LEFT JOIN MM_MCAR_Registration mr ON cd.MCAR_ID = mr.MCAR_ID
                    
                    -- Get master data
                    LEFT JOIN MM_Source_Master sm ON cd.Source_ID = sm.Source_ID
                    LEFT JOIN MM_Shop shop ON cd.Shop_ID = shop.Shop_ID
                    LEFT JOIN MM_Stage_Master st ON cd.Stage_ID = st.Stage_ID
                    LEFT JOIN MM_Severity_Master sev ON cd.Severity_ID = sev.Severity_ID
                    LEFT JOIN MM_Employee aud ON cd.Auditor_ID = aud.Employee_ID
                    
                    -- Get MFG Approval Date
                    LEFT JOIN MM_Approval_Tracking at_mfg ON cd.Concern_ID = at_mfg.Concern_ID 
                        AND at_mfg.Is_MFG = 1 AND at_mfg.Is_Approved = 1
                    
                    -- Get Action Plan Submission Date (first action plan submitted)
                    LEFT JOIN (
                        SELECT 
                            Concern_ID,
                            MIN(Inserted_Date) as Inserted_Date
                        FROM MM_Action_Plan_PCA 
                        WHERE (Is_Deleted IS NULL OR Is_Deleted = 0)
                        GROUP BY Concern_ID
                    ) at_action_plan ON cd.Concern_ID = at_action_plan.Concern_ID
                    
                    -- Get Effectiveness Completion Date
                    LEFT JOIN MM_Approval_Tracking at_effectiveness ON cd.Concern_ID = at_effectiveness.Concern_ID 
                        AND at_effectiveness.Is_Effectivness = 1 AND at_effectiveness.Is_Approved = 1
                    
                    -- Get Quality Manager Closure Date (final approval)
                    LEFT JOIN MM_Approval_Tracking at_quality_closure ON cd.Concern_ID = at_quality_closure.Concern_ID 
                        AND at_quality_closure.Is_Quality = 1 AND at_quality_closure.Is_Approved = 1
                        
                    WHERE cd.Concern_ID = @ConcernId AND (cd.Is_Deleted IS NULL OR cd.Is_Deleted = 0)";

                using var connection = await CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);
                command.Parameters.Add(new SqlParameter("@ConcernId", concernId));

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var mfgApprovalDate = reader.IsDBNull(reader.GetOrdinal("MFG_Approval_Date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("MFG_Approval_Date"));
                    var actionPlanSubmissionDate = reader.IsDBNull(reader.GetOrdinal("Action_Plan_Submission_Date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("Action_Plan_Submission_Date"));
                    var effectivenessCompletionDate = reader.IsDBNull(reader.GetOrdinal("Effectiveness_Completion_Date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("Effectiveness_Completion_Date"));
                    var closureDate = reader.IsDBNull(reader.GetOrdinal("Closure_Date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("Closure_Date"));
                    var reportedDate = reader.GetDateTime(reader.GetOrdinal("Reported_Date"));
                    var isRepeated = !reader.IsDBNull(reader.GetOrdinal("Is_Repeated")) && reader.GetBoolean(reader.GetOrdinal("Is_Repeated"));

                    return new McarClosedMailDataDto
                    {
                        Concern_ID = reader.GetDecimal(reader.GetOrdinal("Concern_ID")),
                        Employee_Name = reader.GetString(reader.GetOrdinal("Employee_Name")),
                        Employee_No = reader.GetString(reader.GetOrdinal("Employee_No")),
                        Employee_Email = reader.GetString(reader.GetOrdinal("Employee_Email")),
                        Assigned_Employee_Name = reader.GetString(reader.GetOrdinal("Assigned_Employee_Name")),
                        Assigned_Employee_No = reader.GetString(reader.GetOrdinal("Assigned_Employee_No")),
                        Assigned_Employee_Email = reader.GetString(reader.GetOrdinal("Assigned_Employee_Email")),
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
                        Action_Plan_Submission_Date = actionPlanSubmissionDate,
                        Effectiveness_Completion_Date = effectivenessCompletionDate,
                        Closure_Date = closureDate,
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
                throw new Exception($"Error retrieving concern details for MCAR closed mail: {ex.Message}", ex);
            }

            return null;
        }

        private async Task<MailRecipientsDto> GetMailRecipientsForMcarClosedAsync(McarClosedMailDataDto concernData)
        {
            var recipients = new MailRecipientsDto();

            try
            {
                using var connection = await CreateConnectionAsync();

                // Get TO recipients: All Quality & Manufacturing officers & managers, plus PU head
                await GetToRecipientsForMcarClosedAsync(connection, concernData, recipients);

                return recipients;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting mail recipients for MCAR closed concern {concernData.Concern_ID}: {ex.Message}", ex);
            }
        }

        private async Task GetToRecipientsForMcarClosedAsync(SqlConnection connection, McarClosedMailDataDto concernData, MailRecipientsDto recipients)
        {
            var addedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Add all Quality & Manufacturing officers, managers, and PU heads for the shop
            const string allStakeholdersQuery = @"
                SELECT DISTINCT
                    e.Employee_Name,
                    e.Employee_No,
                    e.Email_Address,
                    d.Designation_Name
                FROM MM_Employee e
                INNER JOIN MM_User_Shop_Model usm ON e.Employee_ID = usm.Employee_ID
                INNER JOIN MM_Designation d ON e.Designation_ID = d.Designation_ID
                WHERE e.Designation_ID IN (@PuHeadDesignationId, @ManufacturingManagerDesignationId, @QualityManagerDesignationId, @ManufacturingOfficerDesignationId, @QualityOfficerDesignationId)
                    AND usm.Shop_ID = @ShopId
                    AND (e.Is_Deleted IS NULL OR e.Is_Deleted = 0)
                ORDER BY e.Employee_Name";

            using var command = new SqlCommand(allStakeholdersQuery, connection);
            command.Parameters.Add(new SqlParameter("@PuHeadDesignationId", Constants.PU_HEAD));
            command.Parameters.Add(new SqlParameter("@ManufacturingManagerDesignationId", Constants.MANUFACTURING_MANAGER));
            command.Parameters.Add(new SqlParameter("@QualityManagerDesignationId", Constants.QUALITY_MANAGER));
            command.Parameters.Add(new SqlParameter("@ManufacturingOfficerDesignationId", Constants.MANUFACTURING_OFFICER));
            command.Parameters.Add(new SqlParameter("@QualityOfficerDesignationId", Constants.QUALITY_OFFICER));
            command.Parameters.Add(new SqlParameter("@ShopId", concernData.Shop_ID));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var email = reader.GetString(reader.GetOrdinal("Email_Address")).Trim();
                
                if (addedEmails.Add(email))
                {
                    recipients.ToRecipients.Add(new MailRecipientDto
                    {
                        Email = email,
                        Name = reader.GetString(reader.GetOrdinal("Employee_Name"))
                    });
                }
            }
            reader.Close();

            // Also add the specific manager and quality manager who provided approvals
            if (!string.IsNullOrEmpty(concernData.Manager_Email))
            {
                var mgrEmail = concernData.Manager_Email.Trim();
                if (addedEmails.Add(mgrEmail))
                {
                    recipients.ToRecipients.Add(new MailRecipientDto
                    {
                        Email = mgrEmail,
                        Name = concernData.Manager_Name
                    });
                }
            }

            if (!string.IsNullOrEmpty(concernData.Quality_Manager_Email))
            {
                var qmEmail = concernData.Quality_Manager_Email.Trim();
                if (addedEmails.Add(qmEmail))
                {
                    recipients.ToRecipients.Add(new MailRecipientDto
                    {
                        Email = qmEmail,
                        Name = concernData.Quality_Manager_Name
                    });
                }
            }

            // Add person who reported the concern
            if (!string.IsNullOrEmpty(concernData.Employee_Email))
            {
                var empEmail = concernData.Employee_Email.Trim();
                if (addedEmails.Add(empEmail))
                {
                    recipients.ToRecipients.Add(new MailRecipientDto
                    {
                        Email = empEmail,
                        Name = concernData.Employee_Name
                    });
                }
            }

            // Add assigned employee (who worked on action plan)
            if (!string.IsNullOrEmpty(concernData.Assigned_Employee_Email))
            {
                var assignedEmail = concernData.Assigned_Employee_Email.Trim();
                if (addedEmails.Add(assignedEmail))
                {
                    recipients.ToRecipients.Add(new MailRecipientDto
                    {
                        Email = assignedEmail,
                        Name = concernData.Assigned_Employee_Name
                    });
                }
            }

            // Add auditor who completed effectiveness monitoring
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
        }
    }

    // DTO for MCAR closed mail data
    public class McarClosedMailDataDto
    {
        public decimal Concern_ID { get; set; }
        public decimal Inserted_User_ID { get; set; }
        public string Employee_Name { get; set; } = string.Empty;
        public string Employee_No { get; set; } = string.Empty;
        public string Employee_Email { get; set; } = string.Empty;
        public string Assigned_Employee_Name { get; set; } = string.Empty;
        public string Assigned_Employee_No { get; set; } = string.Empty;
        public string Assigned_Employee_Email { get; set; } = string.Empty;
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
        public DateTime Action_Plan_Submission_Date { get; set; }
        public DateTime Effectiveness_Completion_Date { get; set; }
        public DateTime Closure_Date { get; set; }
        public string Plant_Code { get; set; } = string.Empty;
        public string Auditor_Name { get; set; } = string.Empty;
        public decimal Auditor_ID { get; set; }
        public string Auditor_No { get; set; } = string.Empty;
        public string Auditor_Email { get; set; } = string.Empty;
        public decimal Shop_ID { get; set; }
        public decimal Assigned_To { get; set; }
    }
}
