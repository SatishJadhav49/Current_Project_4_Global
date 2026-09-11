namespace API_PQ_Global_Reporting.Models
{
    public class SmtpSettings
    {
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; }
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }

    public class SystemSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ReportURL { get; set; } = string.Empty;
    }
}
