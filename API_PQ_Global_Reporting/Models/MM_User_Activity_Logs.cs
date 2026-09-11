using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_PQ_Global_Reporting.Models
{
    public partial class MM_User_Activity_Logs
    {
        [Key]
        public decimal Log_ID { get; set; }
        public string User_Name { get; set; } = string.Empty;
        public string User_Token_No { get; set; } = string.Empty;
        public DateTime Logged_In_Time { get; set; }
        public string Audit_Type { get; set; } = string.Empty;
        public string Shop_Name { get; set; } = string.Empty;
    }
}
