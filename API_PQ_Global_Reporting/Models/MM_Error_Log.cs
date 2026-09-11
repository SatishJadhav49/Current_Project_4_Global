namespace API_PQ_Global_Reporting.Models
{
    public class MM_Error_Log
    {

        public decimal EL_ID { get; set; } 
        public decimal? Plant_ID { get; set; }
        public decimal? Shop_ID { get; set; }
        public decimal? Line_ID { get; set; }
        public decimal? BuyoffCode { get; set; }
        public string? Controller_Name { get; set; }
        public string? Action_Name { get; set; }
        public string? Meta_Class_Name { get; set; }
        public string? Method_Name { get; set; }
        public string? Exception_Detail { get; set; }
        public string? Inner_Exception { get; set; }
        public string? Message { get; set; }
        public string? Exception_Data { get; set; }
        public string? Target_Site { get; set; }
        public string? Stack_Trace { get; set; }
        public string? Source { get; set; }
        public string? H_Result { get; set; }
        public bool? Is_Transferred { get; set; }
        public bool? Is_Purgeable { get; set; }
        public string? Inserted_Host { get; set; }
        public decimal? Inserted_User_ID { get; set; }
        public DateTime Inserted_Date { get; set; } // Not nullable

    }

}