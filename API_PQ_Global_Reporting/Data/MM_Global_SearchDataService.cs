using API_PQ_Global_Reporting.Models.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;

namespace API_PQ_Global_Reporting.Data
{
    public class MM_Global_SearchDataService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public MM_Global_SearchDataService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<DefectsDataDto>> GetVehicleDefectsByNo(string vehicleNo)
        {
            try
            {
                var defects = new List<DefectsDataDto>();

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand("SP_Get_Vehicle_Defects_By_No", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@Vehicle_No", vehicleNo));

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    defects.Add(new DefectsDataDto
                    {
                        Audit_Type = reader["Audit_Type"]?.ToString(),
                        Auditor_Name = reader["Auditor_Name"]?.ToString(),
                        Problem_Desc = reader["Problem_Desc"]?.ToString(),
                        Severity_Name = reader["Severity_Name"]?.ToString(),
                        Attribution_Name = reader["Attribution_Name"]?.ToString(),
                        Shop_Name = reader["Shop_Name"]?.ToString(),
                        Reported_Date = reader["Reported_Date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["Reported_Date"])
                            : null
                    });
                }

                return defects
                    .OrderByDescending(x => x.Reported_Date)
                    .ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<VehicleInfoDto> GetVehicleInfo(string vehicleNo)
        {
            try
            {
                var vehicle = new VehicleInfoDto();

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand("SP_GetDataFrom_Vin_Number", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@VIN_Number", vehicleNo));

                using var reader = await command.ExecuteReaderAsync();

                // Result Set 1 - Vehicle Details
                if (await reader.ReadAsync())
                {
                    vehicle = new VehicleInfoDto
                    {
                        VIN_Number = reader["VIN_Number"]?.ToString(),
                        BIW_No = reader["BIW_No"]?.ToString(),
                        Model_Description = reader["Model_Description"]?.ToString(),
                        Colour_Desc = reader["Colour_Desc"]?.ToString(),
                        Fuel = reader["Fuel"]?.ToString(),
                        Engine_No = reader["Engine_No"]?.ToString(),
                        RollDown_Date = reader["RollDown_Date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["RollDown_Date"])
                            : null,
                        CAIOut_Date = reader["CAIOut_Date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["CAIOut_Date"])
                            : null,
                        Model_Name = reader["Model_Name"]?.ToString(),
                        Country = reader["Country"]?.ToString(),
                        Drive_Type = reader["Drive_Type"]?.ToString()
                    };
                }

                // Result Set 2 - LSP_RFD_Date
                if (await reader.NextResultAsync() && await reader.ReadAsync())
                {
                    vehicle.LSP_RFD_Date = reader["LSP_RFD_Date"] != DBNull.Value
                        ? Convert.ToDateTime(reader["LSP_RFD_Date"])
                        : null;
                }

                // Result Set 3 - Dock_Audit_Date
                if (await reader.NextResultAsync() && await reader.ReadAsync())
                {
                    vehicle.Dock_Audit_Date = reader["Dock_Audit_Date"] != DBNull.Value
                        ? Convert.ToDateTime(reader["Dock_Audit_Date"])
                        : null;
                }

                return vehicle;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
