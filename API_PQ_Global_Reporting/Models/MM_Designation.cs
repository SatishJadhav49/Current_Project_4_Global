
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_PQ_Global_Reporting.Models
{
    public class MM_Designation
    {
        [Key]
        public decimal Designation_ID { get; set; }
        public string Designation_Name { get; set; } = String.Empty;
        public decimal Audit_Type_Id { get; set; }
        public decimal Plant_ID { get; set; }
        public bool? Is_Transfered { get; set; }
        public bool? Is_Purgeable { get; set; }
        public bool? Is_Edited { get; set; }
        public bool? Is_Deleted { get; set; }
        public string? Inserted_Host { get; set; }
        public decimal? Inserted_User_ID { get; set; }
        public DateTime? Inserted_Date { get; set; }
        public string? Updated_Host { get; set; }
        public decimal? Updated_User_ID { get; set; }
        public DateTime? Updated_Date { get; set; }
        public string Plant_Code { get; set; } = String.Empty;
    }
}