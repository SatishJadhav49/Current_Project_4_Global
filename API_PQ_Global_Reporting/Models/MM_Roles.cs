namespace API_PQ_Global_Reporting.Models
{
    public class MM_Roles
    {
        public decimal Role_ID { get; set; }
        public string Role_Name { get; set; } = String.Empty;
        public string Role_Description { get; set; } = String.Empty;
        public Nullable<bool> Is_Transfered { get; set; }
        public Nullable<bool> Is_Purgeable { get; set; }
        public Nullable<bool> Is_Edited { get; set; }
        public decimal Inserted_User_ID { get; set; }
        public System.DateTime Inserted_Date { get; set; }
        public Nullable<decimal> Updated_User_ID { get; set; }
        public Nullable<System.DateTime> Updated_Date { get; set; }
        public Nullable<bool> Is_Show_Documentations { get; set; }
        public string Technical_Documentations { get; set; } = String.Empty;
        public string Functional_Documentations { get; set; } = String.Empty;
        public decimal Plant_ID { get; set; }
        public Nullable<bool> Is_Active { get; set; }
        public Nullable<decimal> Audit_Type_Id { get; set; }
    }
}