using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_PQ_Global_Reporting.Models.DTOs
{
    public class DocumentsMasterCreateDtos
    {
        public IFormFile? File { get; set; }
        public decimal Document_ID { get; set; }
        public string Document_Title { get; set; } = string.Empty;
        public string Document_Path { get; set; } = string.Empty;
        public string Employee_Name { get; set; } = string.Empty;
        public DateTime? Inserted_Date { get; set; }
        public string? Inserted_Host { get; set; } = string.Empty;
        public decimal? Inserted_User_ID { get; set; }
        public string? Plant_Code { get; set; } = string.Empty;

    }

    public class DocumentsMasterListDtos
    {
        public decimal Document_ID { get; set; }
        public string Document_Title { get; set; } = string.Empty;
        public string Document_Path { get; set; } = string.Empty;
        public string Employee_Name { get; set; } = string.Empty;
        public DateTime Inserted_Date { get; set; }
        public string? Inserted_Host { get; set; } = string.Empty;
        public decimal? Inserted_User_ID { get; set; }
        public string? Plant_Code { get; set; } = string.Empty;

    }
    
   
}