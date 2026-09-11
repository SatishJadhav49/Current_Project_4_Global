namespace API_PQ_Global_Reporting.Models.DTOs
{
     public class MM_User_Role_CreateDto
    {
        public decimal Employee_ID { get; set; }
        public decimal Role_ID { get; set; }
        public bool Is_Create { get; set; }
        public bool Is_Edit { get; set; }
        public bool Is_Delete { get; set; }
        public decimal Plant_ID { get; set; }
        public decimal Audit_Type_Id { get; set; }
        public decimal Inserted_User_ID { get; set; }
        public string Inserted_Host { get; set; } = String.Empty;
        public Nullable<System.DateTime> Inserted_Date { get; set; }

    }


    public class UserRoleUpdateDto
    {
        public decimal User_Role_Key { get; set; }
        public decimal Employee_ID { get; set; }
        public decimal Role_ID { get; set; }
        public bool Is_Create { get; set; }
        public bool Is_Edit { get; set; }
        public bool Is_Delete { get; set; }
        public decimal Plant_ID { get; set; }
        public decimal Audit_Type_Id { get; set; }
        public decimal Updated_User_ID { get; set; }
        public string Updated_Host { get; set; } = string.Empty;
        public Nullable<System.DateTime> Inserted_Date { get; set; }



    }

    public class UserRoleListDto
    {
        public decimal User_Role_Key { get; set; }
        public decimal Employee_ID { get; set; }
        public decimal Role_ID { get; set; }
        public bool Is_Create { get; set; }
        public bool Is_Edit { get; set; }
        public bool Is_Delete { get; set; }
        public decimal Plant_ID { get; set; }
        public decimal Audit_Type_Id { get; set; }
        public decimal Inserted_User_ID { get; set; }
        public string Inserted_Host { get; set; } = String.Empty;
        public Nullable<System.DateTime> Inserted_Date { get; set; }
        public string? Employee_Name { get; set; }
        public string? Role_Name { get; set; }
    }

    public class RoleListDto
    {
        public decimal Role_ID { get; set; }
        public string? Role_Name { get; set; }
 
    }


}