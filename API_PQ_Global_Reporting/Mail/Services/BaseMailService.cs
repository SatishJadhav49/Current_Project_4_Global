using API_PQ_Global_Reporting.Data;
using API_PQ_Global_Reporting.Mail;
using API_PQ_Global_Reporting.Models;
using API_PQ_Global_Reporting.Utils;
using Microsoft.Data.SqlClient;
using System.Net.Mail;
using System.Net;

namespace API_PQ_Global_Reporting.Mail.Services
{
    public class BaseMailService
    {
        protected readonly string _connectionString;
        protected readonly SmtpSettings _smtpSettings;
        protected readonly SystemSettings _systemSettings;

        protected BaseMailService(
            string connectionString,
            SmtpSettings smtpSettings,
            SystemSettings systemSettings)
        {
            _connectionString = connectionString;
            _smtpSettings = smtpSettings;
            _systemSettings = systemSettings;
        }

        /// <summary>
        /// Creates a database connection
        /// </summary>
        protected async Task<SqlConnection> CreateConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        /// <summary>
        /// Sends email with recipients
        /// </summary>
        protected async Task SendEmailWithRecipientsAsync(MailRecipientsDto recipients, string subject, string htmlBody)
        {
            try
            {
                if (recipients.ToRecipients.Count == 0)
                {
                    throw new Exception("No TO recipients specified for email");
                }

                // Validate all email addresses
                foreach (var recipient in recipients.ToRecipients.Concat(recipients.CcRecipients))
                {
                    if (!IsValidEmail(recipient.Email))
                    {
                        throw new ArgumentException($"Invalid email address: {recipient.Email} for {recipient.Name}");
                    }
                }

                using var mail = new MailMessage();
                mail.From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.FromName);

                // For production, uncomment these lines and comment the test line
                foreach (var toRecipient in recipients.ToRecipients)
                {
                    mail.To.Add(new MailAddress(toRecipient.Email, toRecipient.Name));
                    // Console.WriteLine($"Added TO recipient: {toRecipient.Name} <{toRecipient.Email}>");
                }
                //
                foreach (var ccRecipient in recipients.CcRecipients)
                {
                    mail.CC.Add(new MailAddress(ccRecipient.Email, ccRecipient.Name));
                    // Console.WriteLine($"Added CC recipient: {ccRecipient.Name} <{ccRecipient.Email}>");

                }

                // For monitoring - add admin to CC for all emails
                mail.CC.Add("50005817@mahindra.com");

                mail.Subject = subject;
                mail.Body = htmlBody;
                mail.IsBodyHtml = true;
                Console.WriteLine("Sending Mails");
                using var smtp_server = new SmtpClient();
                smtp_server.UseDefaultCredentials = false;
                smtp_server.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
                smtp_server.Host = _smtpSettings.Server;
                smtp_server.Port = _smtpSettings.Port;
                smtp_server.EnableSsl = _smtpSettings.EnableSsl;

                await smtp_server.SendMailAsync(mail);
                // Log success
                var toEmails = string.Join(", ", recipients.ToRecipients.Select(r => r.Email));
                var ccEmails = string.Join(", ", recipients.CcRecipients.Select(r => r.Email));
                await LogMailEventAsync(toEmails, ccEmails, subject, "Sent");
            }
            catch (Exception ex)
            {
                var toEmails = string.Join(", ", recipients.ToRecipients.Select(r => r.Email));
                var ccEmails = string.Join(", ", recipients.CcRecipients.Select(r => r.Email));
                await LogMailEventAsync(toEmails, ccEmails, subject, "Failed", ex.Message);
                throw new Exception($"Failed to send email to TO: [{toEmails}], CC: [{ccEmails}]. Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reads mail template from file
        /// </summary>
        protected async Task<string> ReadMailTemplateAsync(string templateFileName)
        {
            try
            {
                // Templates are now in wwwroot/Templates/
                // Try multiple possible paths for different deployment scenarios
                var possiblePaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Templates", templateFileName),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "Templates", templateFileName),
                };

                foreach (var templatePath in possiblePaths)
                {
                    if (File.Exists(templatePath))
                    {
                        return await File.ReadAllTextAsync(templatePath);
                    }
                }

                // If none found, throw exception with all attempted paths
                var attemptedPaths = string.Join("\n", possiblePaths);
                throw new FileNotFoundException($"Mail template '{templateFileName}' not found. Attempted paths:\n{attemptedPaths}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading mail template {templateFileName}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Replaces placeholders in template with actual data
        /// </summary>
        protected string ReplacePlaceholdersInTemplate(string template, Dictionary<string, string> replacements)
        {
            string result = template;
            foreach (var replacement in replacements)
            {
                result = result.Replace(replacement.Key, replacement.Value);
            }
            return result;
        }

        /// <summary>
        /// Validates email address format
        /// </summary>
        protected bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var mailAddress = new MailAddress(email);
                return mailAddress.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets plant name from plant code
        /// </summary>
        protected string GetPlantName(string plantCode)
        {
            var plantNameMap = new Dictionary<string, string>
            {
                { "A003", "Nashik" },
                { "A002", "Kandivali" },
                { "CK01", "Chakan" },
                { "A010", "Haridwar" }
            };

            return plantNameMap.GetValueOrDefault(plantCode, plantCode);
        }

        /// <summary>
        /// Log each mail send/error event in MailLog table
        /// </summary>
        public async Task LogMailEventAsync(string toEmails, string ccEmails, string subject, string status, string? errorMessage = null)
        {
            try
            {
                using var connection = await CreateConnectionAsync();
                var query = @"INSERT INTO MM_MailLogs (ToEmails, CcEmails, Subject, Status, ErrorMessage, SentAt)
                              VALUES (@ToEmails, @CcEmails, @Subject,  @Status, @ErrorMessage, @SentAt)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ToEmails", toEmails);
                command.Parameters.AddWithValue("@CcEmails", ccEmails);
                command.Parameters.AddWithValue("@Subject", subject);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@ErrorMessage", (object?)errorMessage ?? DBNull.Value);
                command.Parameters.AddWithValue("@SentAt", DateTime.UtcNow);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // If logging fails, write to console as last resort
                Console.WriteLine($"Failed to log mail event: {ex.Message}");
            }
        }
    }
}
