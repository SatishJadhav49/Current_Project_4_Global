namespace API_PQ_Global_Reporting.Models
{
    public class MM_Menus
    {
        public decimal Menu_ID { get; set; }
        public string LinkName { get; set; } = String.Empty;
        public string ActionName { get; set; } = String.Empty;
        public string Inserted_Host { get; set; }  = String.Empty;
        public Nullable<decimal> Inserted_User_ID { get; set; }
        public Nullable<System.DateTime> Inserted_Date { get; set; }
        public string Updated_Host { get; set; }  = String.Empty;
        public Nullable<decimal> Updated_User_ID { get; set; }
        public Nullable<System.DateTime> Updated_Date { get; set; }
        public Nullable<bool> Is_Transfered { get; set; }
        public Nullable<bool> Is_Purgeable { get; set; }
        public Nullable<bool> Is_Edited { get; set; }
        public string Documents_Name { get; set; } = String.Empty;
        public string Technical_Name { get; set; }  = String.Empty;
        public int Sort_Order { get; set; }
        public Nullable<decimal> Audit_Type_Id { get; set; }
        public Nullable<bool> Is_Active { get; set; }
    }
}