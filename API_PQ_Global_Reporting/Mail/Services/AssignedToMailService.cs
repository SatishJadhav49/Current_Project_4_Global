using API_PQ_Global_Reporting.Mail;
using API_PQ_Global_Reporting.Models;
using API_PQ_Global_Reporting.Utils;
using Microsoft.Data.SqlClient;
using System.Net.Mail;
using System.Net;

namespace API_PQ_Global_Reporting.Mail.Services
{
    public class AssignedToMailService : BaseMailService
    {
        public AssignedToMailService(
            string connectionString,
            SmtpSettings smtpSettings,
            SystemSettings systemSettings) 
            : base(connectionString, smtpSettings, systemSettings)
        {
        }

        /// <summary>
        /// Sends mail notification when a concern is assigned to an employee
        /// </summary>
        public async Task<bool> SendMailAsync(decimal concernId)
        {
            try
            {
                // Get concern details from database
                var concernDetails = await GetConcernDetailsForMailAsync(concernId);
                if (concernDetails == null)
                {
                    throw new Exception($"Concern with ID {concernId} not found");
                }

                if (string.IsNullOrEmpty(concernDetails.Employee_No))
                {
                    throw new Exception($"Employee number not found for concern {concernId}");
                }

                // Get mail recipients (To and CC lists)
                var recipients = await GetMailRecipientsForConcernAsync(concernDetails);

                // Prepare mail body
                string mailBody = await PrepareMailBodyForConcernAsync(concernDetails);

                // Send email with multiple recipients
                await SendEmailWithRecipientsAsync(recipients, 
                    $"Corrective Action Report Assigned for audit Non-Conformity - {concernDetails.Source_Name} - {concernDetails.Shop_Name} - {concernDetails.Stage_Name} - {concernDetails.Severity_Name}", 
                    mailBody);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending mail for concern {concernId}: {ex.Message}", ex);
            }
        }

        private async Task<string> PrepareMailBodyForConcernAsync(ConcernMailDataDto concernDetails)
        {
            try
            {
                // Read mail template
                string mailTemplate = await ReadMailTemplateAsync("assigned-concern-template.html");

                // Replace placeholders with actual data
                string mailBody = ReplacePlaceholdersInTemplate(mailTemplate, GetReplacementDictionary(concernDetails));

                return mailBody;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error preparing mail body for concern {concernDetails.Concern_ID}: {ex.Message}", ex);
            }
        }

        private Dictionary<string, string> GetReplacementDictionary(ConcernMailDataDto data)
        {
            string plantName = GetPlantName(data.Plant_Code);

            // Calculate target dates based on assigned date
            var icaTargetDate = data.Assigned_Date.AddDays(Constants.ICA_Days);
            var pcaTargetDate = data.Assigned_Date.AddDays(Constants.PCA_Days);
            var submissionTargetDate = data.Assigned_Date.AddDays(Constants.Standardization_Days);

            // Calculate days remaining
            var daysRemaining = Math.Max(0, (submissionTargetDate.Date - DateTime.Now.Date).Days);

            // Generate dynamic system URL with concern and MCAR IDs
            string systemUrl = $"{_systemSettings.BaseUrl}/mcar/actionhome?concernId={data.Concern_ID}&mcarId={data.MCAR_ID}";

            return new Dictionary<string, string>
            {
                { "[EMPLOYEE_NAME]", data.Employee_Name },
                { "[MCAR_NO]", data.MCAR_No },
                { "[SOURCE_NAME]", data.Source_Name },
                { "[REPORTED_DATE]", data.Reported_Date.ToString("dd-MMM-yyyy") },
                { "[STAGE_NAME]", data.Stage_Name },
                { "[SEVERITY_NAME]", data.Severity_Name },
                { "[PROBLEM_DEFINITION]", data.Problem_Definition },
                { "[CALCULTED_TIME_ICA_24_H]", icaTargetDate.ToString("dd-MMM-yyyy hh:mm tt") },
                { "[CALCULTED_TIME_PCA_7D]", pcaTargetDate.ToString("dd-MMM-yyyy hh:mm tt") },
                { "[SUBMISSION_DUE_DATE]", submissionTargetDate.ToString("dd-MMM-yyyy hh:mm tt") },
                { "[ASSIGNED_DATE]", data.Assigned_Date.ToString("dd-MMM-yyyy") },
                { "[EXPECTED_DATE]", submissionTargetDate.ToString("dd-MMM-yyyy") },
                { "[PLANT_NAME]", plantName },
                { "[CURRENT_DATE]", DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt") },
                { "[DAYS_REMAINING]", daysRemaining.ToString() },
                { "[AUDITOR_NAME]", data.Auditor_Name },
                { "[AUDITOR_ID]", data.Auditor_No },
                { "[IS_REPEATED]", data.Is_Repeated },
                { "[SYSTEM_URL]", systemUrl }
            };
        }

        private async Task<ConcernMailDataDto?> GetConcernDetailsForMailAsync(decimal concernId)
        {
            try
            {
                const string query = @"
                    SELECT 
                        cd.Concern_ID,
                        cd.Date_Of_Complaint as Reported_Date,
                        cd.Inserted_Date as Assigned_Date,
                        ISNULL(cd.Problem_Defination, '') as Problem_Definition,
                        cd.Plant_Code,
                        cd.Shop_ID,
                        ISNULL(cd.Assigned_To, 0) as Assigned_To,
                        
                        -- Employee Info (Assigned To)
                        ISNULL(emp.Employee_Name, '') as Employee_Name,
                        ISNULL(emp.Employee_No, '') as Employee_No,
                        ISNULL(emp.Email_Address, '') as Employee_Email,
                        
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
                        ISNULL(aud.Employee_ID, '') as Auditor_ID,
                        ISNULL(aud.Employee_No, '') as Auditor_No,
                        ISNULL(aud.Email_Address, '') as Auditor_Email,
                        CASE WHEN ISNULL(cd.Is_Repeated, 0) = 1 THEN 'YES' ELSE 'NO' END as Is_Repeated
                        
                    FROM MM_Concern_Data cd
                    LEFT JOIN MM_Employee emp ON cd.Assigned_To = emp.Employee_ID
                    LEFT JOIN MM_MCAR_Registration mr ON cd.MCAR_ID = mr.MCAR_ID
                    LEFT JOIN MM_Source_Master sm ON cd.Source_ID = sm.Source_ID
                    LEFT JOIN MM_Shop shop ON cd.Shop_ID = shop.Shop_ID
                    LEFT JOIN MM_Stage_Master st ON cd.Stage_ID = st.Stage_ID
                    LEFT JOIN MM_Severity_Master sev ON cd.Severity_ID = sev.Severity_ID
                    LEFT JOIN MM_Employee aud ON cd.Auditor_ID = aud.Employee_ID
                    WHERE cd.Concern_ID = @ConcernId AND (cd.Is_Deleted IS NULL OR cd.Is_Deleted = 0)";

                using var connection = await CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);
                command.Parameters.Add(new SqlParameter("@ConcernId", concernId));

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var assignedDate = reader.IsDBNull(reader.GetOrdinal("Assigned_Date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("Assigned_Date"));
                    var reportedDate = reader.GetDateTime(reader.GetOrdinal("Reported_Date"));

                    return new ConcernMailDataDto
                    {
                        Concern_ID = reader.GetDecimal(reader.GetOrdinal("Concern_ID")),
                        Employee_Name = reader.GetString(reader.GetOrdinal("Employee_Name")),
                        Employee_No = reader.GetString(reader.GetOrdinal("Employee_No")),
                        Employee_Email = reader.GetString(reader.GetOrdinal("Employee_Email")),
                        MCAR_ID = reader.GetDecimal(reader.GetOrdinal("MCAR_ID")),
                        MCAR_No = reader.GetString(reader.GetOrdinal("MCAR_No")),
                        Source_Name = reader.GetString(reader.GetOrdinal("Source_Name")),
                        Shop_Name = reader.GetString(reader.GetOrdinal("Shop_Name")),
                        Stage_Name = reader.GetString(reader.GetOrdinal("Stage_Name")),
                        Reported_Date = reportedDate,
                        Severity_Name = reader.GetString(reader.GetOrdinal("Severity_Name")),
                        Problem_Definition = reader.GetString(reader.GetOrdinal("Problem_Definition")),
                        Part_Name = reader.GetString(reader.GetOrdinal("Part_Name")),
                        Assigned_Date = assignedDate,
                        Plant_Code = reader.GetString(reader.GetOrdinal("Plant_Code")),
                        Auditor_Name = reader.GetString(reader.GetOrdinal("Auditor_Name")),
                        Auditor_ID = reader.GetDecimal(reader.GetOrdinal("Auditor_ID")).ToString(),
                        Auditor_No = reader.GetString(reader.GetOrdinal("Auditor_No")),
                        Auditor_Email = reader.GetString(reader.GetOrdinal("Auditor_Email")),
                        Is_Repeated = reader.GetString(reader.GetOrdinal("Is_Repeated")),
                        Shop_ID = reader.GetDecimal(reader.GetOrdinal("Shop_ID")),
                        Assigned_To = reader.GetDecimal(reader.GetOrdinal("Assigned_To"))
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving concern details for mail: {ex.Message}", ex);
            }

            return null;
        }

        private async Task<MailRecipientsDto> GetMailRecipientsForConcernAsync(ConcernMailDataDto concernData)
        {
            var recipients = new MailRecipientsDto();

            try
            {
                using var connection = await CreateConnectionAsync();

                // Get TO recipients: Assigned user and their reporting manager
                await GetToRecipientsAsync(connection, concernData, recipients);

                // Get CC recipients: PU Head, Quality & Manufacturing managers, Quality officers
                await GetCcRecipientsAsync(connection, concernData, recipients);

                return recipients;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting mail recipients for concern {concernData.Concern_ID}: {ex.Message}", ex);
            }
        }

        private async Task GetToRecipientsAsync(SqlConnection connection, ConcernMailDataDto concernData, MailRecipientsDto recipients)
        {
            // 1. Add assigned user
            if (!string.IsNullOrEmpty(concernData.Employee_Email))
            {
                recipients.ToRecipients.Add(new MailRecipientDto
                {
                    Email = concernData.Employee_Email.Trim(),
                    Name = concernData.Employee_Name
                });
            }

            // 2. Add reporting manager of assigned user
            const string reportingManagerQuery = @"
                SELECT 
                    rm.Employee_Name,
                    rm.Employee_No,
                    rm.Email_Address
                FROM MM_Employee e
                INNER JOIN MM_Employee rm ON e.Reporting_Manager_ID = rm.Employee_ID
                WHERE e.Employee_ID = @AssignedTo";

            using var rmCommand = new SqlCommand(reportingManagerQuery, connection);
            rmCommand.Parameters.Add(new SqlParameter("@AssignedTo", concernData.Assigned_To));

            using var rmReader = await rmCommand.ExecuteReaderAsync();
            if (await rmReader.ReadAsync())
            {
                var rmEmailAddress = rmReader.GetString(rmReader.GetOrdinal("Email_Address"));
                recipients.ToRecipients.Add(new MailRecipientDto
                {
                    Email = rmEmailAddress.Trim(),
                    Name = rmReader.GetString(rmReader.GetOrdinal("Employee_Name"))
                });
            }
        }

        private async Task GetCcRecipientsAsync(SqlConnection connection, ConcernMailDataDto concernData, MailRecipientsDto recipients)
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

            // Get PU Head, Quality Manager, Manufacturing Manager, Quality Officer for the shop
            const string ccRecipientsQuery = @"
                SELECT DISTINCT
                    e.Employee_Name,
                    e.Employee_No,
                    e.Email_Address
                FROM MM_Employee e
                INNER JOIN MM_User_Shop_Model usm ON e.Employee_ID = usm.Employee_ID
                WHERE e.Designation_ID IN (@PuHeadDesignationId,@QualityManagerId,@ManufacturingManagerId,@QualityOfficerId)
                    AND usm.Shop_ID = @ShopId";

            using var sqlCommand = new SqlCommand(ccRecipientsQuery, connection);
            sqlCommand.Parameters.Add(new SqlParameter("@PuHeadDesignationId", Constants.PU_HEAD));
            sqlCommand.Parameters.Add(new SqlParameter("@QualityManagerId", Constants.QUALITY_MANAGER));
            sqlCommand.Parameters.Add(new SqlParameter("@ManufacturingManagerId", Constants.MANUFACTURING_MANAGER));
            sqlCommand.Parameters.Add(new SqlParameter("@QualityOfficerId", Constants.QUALITY_OFFICER));
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

    // DTO for concern mail data
    public class ConcernMailDataDto
    {
        public decimal Concern_ID { get; set; }
        public string Employee_Name { get; set; } = string.Empty;
        public string Employee_No { get; set; } = string.Empty;
        public string Employee_Email { get; set; } = string.Empty;
        public decimal MCAR_ID { get; set; }
        public string MCAR_No { get; set; } = string.Empty;
        public string Source_Name { get; set; } = string.Empty;
        public string Shop_Name { get; set; } = string.Empty;
        public string Stage_Name { get; set; } = string.Empty;
        public DateTime Reported_Date { get; set; }
        public string Severity_Name { get; set; } = string.Empty;
        public string Problem_Definition { get; set; } = string.Empty;
        public string Part_Name { get; set; } = string.Empty;
        public DateTime Assigned_Date { get; set; }
        public string Plant_Code { get; set; } = string.Empty;
        public string Auditor_Name { get; set; } = string.Empty;
        public string Auditor_ID { get; set; } = string.Empty;
        public string Auditor_No { get; set; } = string.Empty;
        public string Auditor_Email { get; set; } = string.Empty;
        public string Is_Repeated { get; set; } = string.Empty;
        public decimal Shop_ID { get; set; }
        public decimal Assigned_To { get; set; }
    }
}
