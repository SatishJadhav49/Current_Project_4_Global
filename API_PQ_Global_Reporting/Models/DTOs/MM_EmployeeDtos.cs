namespace API_PQ_Global_Reporting.Models.DTOs
{
    public class MM_EmployeeCreateUserDto //create user
    {
        public string Employee_Name { get; set; } = String.Empty;
        public string Employee_No { get; set; } = String.Empty;
        public string Email_Address { get; set; } = String.Empty;
        public decimal Designation_ID { get; set; }
        public decimal Reporting_Manager_ID { get; set; }
        public decimal Audit_Type_Id { get; set; }
        public decimal Plant_ID { get; set; }
        public string Plant_Code { get; set; } = String.Empty;
        public string Inserted_Host { get; set; } = String.Empty;
        public decimal Inserted_User_ID { get; set; }
        public decimal Shop_ID { get; set; }
        public decimal Model_ID { get; set; }
    }

    public class MM_EmployeeUpdateUserDto //update user
    {
        public string Employee_Name { get; set; } = String.Empty;
        public string Employee_No { get; set; } = String.Empty;
        public string Email_Address { get; set; } = String.Empty;
        public decimal Designation_ID { get; set; }
        public decimal? Reporting_Manager_ID { get; set; }
        public decimal Audit_Type_Id { get; set; }
        public decimal Plant_ID { get; set; }
        public string Plant_Code { get; set; } = String.Empty;
        public string Updated_Host { get; set; } = String.Empty;
        public decimal Updated_User_ID { get; set; }
        public decimal Shop_ID { get; set; }
        public decimal Model_ID { get; set; }
    }

    public class MM_EmployeeListDto //get employee list
    {
        public decimal Employee_ID { get; set; }
        public string Employee_Name { get; set; } = String.Empty;
        public string Employee_No { get; set; } = String.Empty;
        public string Email_Address { get; set; } = String.Empty;
        public decimal Designation_ID { get; set; }
        public string Designation_Name { get; set; } = String.Empty;
        public decimal? Reporting_Manager_ID { get; set; }
        public decimal Audit_Type_Id { get; set; }
        public decimal Plant_ID { get; set; }
    }

    public class MM_EmployeeDetailDto //get employee detail with shop and models
    {
        public decimal Employee_ID { get; set; }
        public string Employee_Name { get; set; } = String.Empty;
        public string Employee_No { get; set; } = String.Empty;
        public string Email_Address { get; set; } = String.Empty;
        public decimal Designation_ID { get; set; }
        public string Designation_Name { get; set; } = String.Empty;
        public decimal? Reporting_Manager_ID { get; set; }
        public decimal Audit_Type_Id { get; set; }
        public decimal Plant_ID { get; set; }
        public string Plant_Code { get; set; } = String.Empty;
        public string Hostname { get; set; } = String.Empty;
        public List<int> Shop_ID { get; set; } = new List<int>();
        public List<int> Model_ID { get; set; } = new List<int>();
    }
}