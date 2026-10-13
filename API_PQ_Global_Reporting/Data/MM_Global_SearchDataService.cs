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

        public async Task<List<DefectsDataDto>> GetVehicleDefectsByNo(string vinNumber, string biwNo)
        {
            try
            {
                var defects = new List<DefectsDataDto>();

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand("SP_Get_Vehicle_Defects_By_No", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@Vehicle_No", string.IsNullOrWhiteSpace(vinNumber) ? (object)DBNull.Value : vinNumber));
                command.Parameters.Add(new SqlParameter("@BIW_No", string.IsNullOrWhiteSpace(biwNo) ? (object)DBNull.Value : biwNo));

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    defects.Add(new DefectsDataDto
                    {
                        Audit_Category = reader["Audit_Category"]?.ToString(),
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
                using var connection = await _connectionFactory.CreateConnectionAsync();

                // 1. Try the first Stored Procedure
                using (var command = new SqlCommand("SP_GetDataFrom_Vin_Number", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@VIN_Number", vehicleNo));
                    command.Parameters.Add(new SqlParameter("@BIW_No", vehicleNo));

                    using var reader = await command.ExecuteReaderAsync();

                    // Result Set 1 - Vehicle Details
                    if (await reader.ReadAsync())
                    {
                        var vehicle = MapVehicleFromReader(reader);

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

                        return vehicle; // Return early if data is found
                    }
                }

                // 2. Fallback: If no rows were returned above, call the 19_Server SP
                using (var fallbackCommand = new SqlCommand("SP_GetDataFrom_Vin_Number_19_Server", connection))
                {
                    fallbackCommand.CommandType = CommandType.StoredProcedure;
                    fallbackCommand.Parameters.Add(new SqlParameter("@VIN_Number", vehicleNo));
                    fallbackCommand.Parameters.Add(new SqlParameter("@BIW_No", vehicleNo));

                    using var fallbackReader = await fallbackCommand.ExecuteReaderAsync();

                    if (await fallbackReader.ReadAsync())
                    {
                        var vehicle = MapVehicleFromReader(fallbackReader);

                        if (await fallbackReader.NextResultAsync() && await fallbackReader.ReadAsync())
                        {
                            vehicle.LSP_RFD_Date = fallbackReader["LSP_RFD_Date"] != DBNull.Value
                                ? Convert.ToDateTime(fallbackReader["LSP_RFD_Date"])
                                : null;
                        }

                        if (await fallbackReader.NextResultAsync() && await fallbackReader.ReadAsync())
                        {
                            vehicle.Dock_Audit_Date = fallbackReader["Dock_Audit_Date"] != DBNull.Value
                                ? Convert.ToDateTime(fallbackReader["Dock_Audit_Date"])
                                : null;
                        }

                        return vehicle;
                    }
                }

                // Return empty DTO instead of null if both SPs return zero records
                return new VehicleInfoDto();
            }
            catch (Exception)
            {
                throw;
            }
        }

        // Helper method to keep your code DRY (Don't Repeat Yourself)
        private VehicleInfoDto MapVehicleFromReader(SqlDataReader reader)
        {
            return new VehicleInfoDto
            {
                VIN_Number = reader["VIN_Number"]?.ToString(),
                BIW_No = reader["BIW_No"]?.ToString(),
                Model_Description = reader["Model_Description"]?.ToString(),
                Colour_Desc = reader["Colour_Desc"]?.ToString(),
                Fuel = reader["Fuel"]?.ToString(),
                Engine_No = reader["Engine_No"]?.ToString(),
                RollDown_Date = reader["RollDown_Date"] != DBNull.Value ? Convert.ToDateTime(reader["RollDown_Date"]) : null,
                CAIOut_Date = reader["CAIOut_Date"] != DBNull.Value ? Convert.ToDateTime(reader["CAIOut_Date"]) : null,
                Model_Name = reader["Model_Name"]?.ToString(),
                Country = reader["Country"]?.ToString(),
                Drive_Type = reader["Drive_Type"]?.ToString()
            };
        }

    }
}
