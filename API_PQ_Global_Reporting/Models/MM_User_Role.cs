namespace API_PQ_Global_Reporting.Models
{
    public class MM_User_Roles
    {
        public decimal User_Role_Key { get; set; }
        public decimal Employee_ID { get; set; }
        public decimal Role_ID { get; set; }
        public string Description { get; set; } = String.Empty;
        public Nullable<bool> Is_Create { get; set; }
        public Nullable<bool> Is_Edit { get; set; }
        public Nullable<bool> Is_Delete { get; set; }
        public Nullable<bool> Is_Deleted { get; set; }
        public Nullable<bool> Is_Transfered { get; set; }
        public Nullable<bool> Is_Purgeable { get; set; }
        public Nullable<bool> Is_Edited { get; set; }
        public Nullable<decimal> Inserted_User_ID { get; set; }
        public Nullable<System.DateTime> Inserted_Date { get; set; }
        public string Inserted_Host { get; set; } = String.Empty;
        public Nullable<decimal> Updated_User_ID { get; set; }
        public Nullable<System.DateTime> Updated_Date { get; set; }
        public string Updated_Host { get; set; } = String.Empty;
        public decimal Plant_ID { get; set; }
        public Nullable<decimal> Audit_Type_Id { get; set; }
    }

    


}