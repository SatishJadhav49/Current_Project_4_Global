namespace API_PQ_Global_Reporting.Models
{
    public class MM_Menu_Role
    {
        public decimal Menu_Role_ID { get; set; }
        public decimal Menu_ID { get; set; }
        public decimal Sub_Menu_ID { get; set; }
        public decimal Role_ID { get; set; }
        public string Inserted_Host { get; set; } = String.Empty;
        public Nullable<decimal> Inserted_User_ID { get; set; }
        public Nullable<System.DateTime> Inserted_Date { get; set; }
        public string Updated_Host { get; set; } = String.Empty;
        public Nullable<decimal> Updated_User_ID { get; set; }
        public Nullable<System.DateTime> Updated_Date { get; set; }
        public Nullable<bool> Is_Transfered { get; set; }
        public Nullable<bool> Is_Purgeable { get; set; }
        public Nullable<bool> Is_Edited { get; set; }
        public decimal Plant_ID { get; set; }
        public Nullable<decimal> Audit_Type_Id { get; set; }
    }
}