namespace API_PQ_Global_Reporting.Models.DTOs
{
    public class MM_DesignationDTO
    {
        public decimal Designation_ID { get; set; }
        public string Designation_Name { get; set; } = String.Empty;
    }

    public class MM_ShopGetDTO
    {
        public decimal Shop_ID { get; set; }
        public string Shop_Name { get; set; } = String.Empty;
    }

    public class UserListDTO
    {
        public decimal Employee_ID { get; set; }
        public string Employee_Name { get; set; } = String.Empty;
        public decimal Designation_ID { get; set; }
        public string Designation_Name { get; set; } = String.Empty;
    }


}