using API_PQ_Global_Reporting.Models;
using API_PQ_Global_Reporting.Models.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net;
using System.Net.Mail;

namespace API_PQ_Global_Reporting.Data
{
    public class MM_CommonDataService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public MM_CommonDataService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async Task<List<MM_DesignationDTO>> GetDesignationList()
        {
            try
            {

                const string query = @"
                    SELECT 
                    Designation_ID,
                    Designation_Name
                    FROM MM_Designation ";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                var designationList = new List<MM_DesignationDTO>();

                while (await reader.ReadAsync())
                {
                    designationList.Add(new MM_DesignationDTO
                    {
                        Designation_ID = reader.GetDecimal(reader.GetOrdinal("Designation_ID")),
                        Designation_Name = reader.GetString(reader.GetOrdinal("Designation_Name"))
                    });
                }
                return designationList;

            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<MM_ShopGetDTO>> GetShopList()
        {
            try
            {
                const string query = @"
                    SELECT 
                    Shop_ID,
                    Shop_Name
                    FROM MM_Shop";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                var shopList = new List<MM_ShopGetDTO>();

                while (await reader.ReadAsync())
                {
                    shopList.Add(new MM_ShopGetDTO
                    {
                        Shop_ID = reader.GetDecimal(reader.GetOrdinal("Shop_ID")),
                        Shop_Name = reader.GetString(reader.GetOrdinal("Shop_Name"))
                    });
                }
                return shopList;
            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task SendTestMail()
        {
            // Teams channel email address
            string teamsChannelEmail = "d636d9bf.mahindraonline.onmicrosoft.com@apac.teams.ms";

            // SMTP configuration (from provided details)
            string smtpHost = "10.218.124.232";
            int smtpPort = 25;
            string smtpUser = "drona@mahindra.com";
            string smtpPass = "";
            bool enableSsl = false;
            string fromEmail = "mcar@mahindra.com";
            string fromName = "MCAR System";

            // Create the email message
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(fromEmail, fromName);
            mail.To.Add(teamsChannelEmail);
            mail.To.Add("50005817@mahindra.com");
            mail.Subject = "Test Message to Teams Channel";
            mail.IsBodyHtml = true;
            mail.Body = $@"
                Hello Team, this is a test message sent via SMTP from .NET.<br/><br/>
                SMTP Settings:<br/>
                
            ";

            using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
            {
                smtpClient.Credentials = new NetworkCredential(smtpUser, smtpPass);
                smtpClient.EnableSsl = enableSsl;

                try
                {
                    await smtpClient.SendMailAsync(mail);
                    Console.WriteLine("Email sent successfully to Teams channel.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error sending email: " + ex.Message);
                }
            }
        }

    }

}
