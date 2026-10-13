using API_PQ_Global_Reporting.CorrelationModule.Models;
using API_PQ_Global_Reporting.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace API_PQ_Global_Reporting.CorrelationModule.Data
{
    public class MM_CR_MappingDataService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public MM_CR_MappingDataService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        //*************************************** Lookups ***************************************//

        public async Task<List<MM_CR_ModelDto>> GetModels()
        {
            var models = new List<MM_CR_ModelDto>();

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = CreateSpCommand("SP_CR_Get_Models", connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                models.Add(new MM_CR_ModelDto
                {
                    Model_ID = Convert.ToDecimal(reader["Model_ID"]),
                    Model_Name = reader["Model_Name"]?.ToString()
                });
            }

            return models;
        }

        public async Task<List<MM_CR_ApplicationDto>> GetApplications()
        {
            var applications = new List<MM_CR_ApplicationDto>();

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = CreateSpCommand("SP_CR_Get_Applications", connection);
            using var reader = await command.ExecuteReaderAsync();

            // Result Set 1 - Applications
            while (await reader.ReadAsync())
            {
                applications.Add(new MM_CR_ApplicationDto
                {
                    App_ID = Convert.ToInt32(reader["App_ID"]),
                    App_Code = reader["App_Code"]?.ToString(),
                    App_Name = reader["App_Name"]?.ToString(),
                    Leaf_Level_Name = reader["Leaf_Level_Name"]?.ToString(),
                    Sort_Order = Convert.ToInt32(reader["Sort_Order"])
                });
            }

            // Result Set 2 - Grid columns per application
            if (await reader.NextResultAsync())
            {
                var appLookup = applications.ToDictionary(a => a.App_ID);

                while (await reader.ReadAsync())
                {
                    var appId = Convert.ToInt32(reader["App_ID"]);
                    if (!appLookup.TryGetValue(appId, out var app)) continue;

                    app.Columns.Add(new MM_CR_ApplicationColumnDto
                    {
                        Column_Key = reader["Column_Key"]?.ToString() ?? string.Empty,
                        Column_Header = reader["Column_Header"]?.ToString() ?? string.Empty,
                        Sort_Order = Convert.ToInt32(reader["Sort_Order"]),
                        Is_Filterable = Convert.ToBoolean(reader["Is_Filterable"])
                    });
                }
            }

            return applications;
        }

        //*************************************** Parameters ***************************************//

        public async Task<List<MM_CR_ParameterDto>> GetParameters(decimal modelId)
        {
            var parameters = new List<MM_CR_ParameterDto>();

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = CreateSpCommand("SP_CR_Get_Parameters", connection);
            command.Parameters.Add(new SqlParameter("@Model_ID", modelId));

            using var reader = await command.ExecuteReaderAsync();

            // Result Set 1 - Parameters
            while (await reader.ReadAsync())
            {
                parameters.Add(new MM_CR_ParameterDto
                {
                    CR_Parameter_ID = Convert.ToInt32(reader["CR_Parameter_ID"]),
                    Model_ID = Convert.ToDecimal(reader["Model_ID"]),
                    Parameter_Name = reader["Parameter_Name"]?.ToString(),
                    Inserted_Date = ToNullableDate(reader["Inserted_Date"]),
                    Updated_Date = ToNullableDate(reader["Updated_Date"])
                });
            }

            // Result Set 2 - Mapped count per application
            if (await reader.NextResultAsync())
            {
                var parameterLookup = parameters.ToDictionary(p => p.CR_Parameter_ID);

                while (await reader.ReadAsync())
                {
                    var parameterId = Convert.ToInt32(reader["CR_Parameter_ID"]);
                    if (!parameterLookup.TryGetValue(parameterId, out var parameter)) continue;

                    var count = Convert.ToInt32(reader["Mapped_Count"]);
                    parameter.App_Counts.Add(new MM_CR_AppMappedCountDto
                    {
                        App_ID = Convert.ToInt32(reader["App_ID"]),
                        App_Name = reader["App_Name"]?.ToString(),
                        Mapped_Count = count
                    });
                    parameter.Total_Mapped += count;
                }
            }

            return parameters;
        }

        public async Task<MM_CR_ResultDto> SaveParameter(MM_CR_ParameterSaveDto parameter)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = CreateSpCommand("SP_CR_Save_Parameter", connection);
            command.Parameters.Add(new SqlParameter("@CR_Parameter_ID", parameter.CR_Parameter_ID));
            command.Parameters.Add(new SqlParameter("@Model_ID", parameter.Model_ID));
            command.Parameters.Add(new SqlParameter("@Parameter_Name", parameter.Parameter_Name.Trim()));
            AddUserParameters(command, parameter.User_ID, parameter.Host);

            return await ExecuteResultAsync(command);
        }

        public async Task<MM_CR_ResultDto> DeleteParameter(MM_CR_ParameterDeleteDto parameter)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = CreateSpCommand("SP_CR_Delete_Parameter", connection);
            command.Parameters.Add(new SqlParameter("@CR_Parameter_ID", parameter.CR_Parameter_ID));
            AddUserParameters(command, parameter.User_ID, parameter.Host);

            return await ExecuteResultAsync(command);
        }

        //*************************************** Mappings ***************************************//

        public async Task<MM_CR_ParameterMappingsDto> GetParameterMappings(int crParameterId)
        {
            var result = new MM_CR_ParameterMappingsDto();

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = CreateSpCommand("SP_CR_Get_Parameter_Mappings", connection);
            command.Parameters.Add(new SqlParameter("@CR_Parameter_ID", crParameterId));

            using var reader = await command.ExecuteReaderAsync();

            // Result Set 1 - Parameter header
            if (await reader.ReadAsync())
            {
                result.Parameter = new MM_CR_ParameterDto
                {
                    CR_Parameter_ID = Convert.ToInt32(reader["CR_Parameter_ID"]),
                    Model_ID = Convert.ToDecimal(reader["Model_ID"]),
                    Model_Name = reader["Model_Name"]?.ToString(),
                    Parameter_Name = reader["Parameter_Name"]?.ToString()
                };
            }

            // Result Set 2 - Active mappings
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    result.Mappings.Add(new MM_CR_MappingDto
                    {
                        Mapping_ID = Convert.ToInt32(reader["Mapping_ID"]),
                        CR_Parameter_ID = Convert.ToInt32(reader["CR_Parameter_ID"]),
                        App_ID = Convert.ToInt32(reader["App_ID"]),
                        App_Name = reader["App_Name"]?.ToString(),
                        Leaf_Level_Name = reader["Leaf_Level_Name"]?.ToString(),
                        Source_Model_ID = reader["Source_Model_ID"] != DBNull.Value ? Convert.ToDecimal(reader["Source_Model_ID"]) : null,
                        Source_Leaf_ID = Convert.ToDecimal(reader["Source_Leaf_ID"]),
                        Source_Display_Text = reader["Source_Display_Text"]?.ToString(),
                        Inserted_Date = ToNullableDate(reader["Inserted_Date"]),
                        Mapped_By = reader["Mapped_By"]?.ToString()
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Master data of one application for the parameter's model. The SP name is
        /// taken from MM_CR_Application only (never from the client). Each row gets
        /// Is_Mapped (mapped to this parameter) and Other_Parameters flags added.
        /// Returns null when the parameter or the application is not available.
        /// </summary>
        public async Task<MM_CR_MasterDataDto?> GetMasterData(int appId, int crParameterId)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // 1. Parameter -> Model (ID + Name)
            decimal modelId;
            string modelName;
            using (var command = new SqlCommand(@"
                SELECT p.Model_ID, mo.Model_Name
                FROM dbo.MM_CR_Parameter p
                JOIN dbo.MM_Model mo ON mo.Model_ID = p.Model_ID
                WHERE p.CR_Parameter_ID = @CR_Parameter_ID AND p.Is_Active = 1", connection))
            {
                command.Parameters.Add(new SqlParameter("@CR_Parameter_ID", crParameterId));
                using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return null;

                modelId = Convert.ToDecimal(reader["Model_ID"]);
                modelName = reader["Model_Name"]?.ToString() ?? string.Empty;
            }

            // 2. Application -> master SP name (validated to exist by the SP)
            string? masterSpName;
            using (var command = CreateSpCommand("SP_CR_Get_Application_Master_SP", connection))
            {
                command.Parameters.Add(new SqlParameter("@App_ID", appId));
                masterSpName = (await command.ExecuteScalarAsync())?.ToString();
            }

            if (string.IsNullOrWhiteSpace(masterSpName)) return null;

            // 3. Existing mappings of this application within the model
            var mappedHere = new HashSet<decimal>();
            var otherParameters = new Dictionary<decimal, List<string>>();
            using (var command = CreateSpCommand("SP_CR_Get_Leaf_Mappings", connection))
            {
                command.Parameters.Add(new SqlParameter("@App_ID", appId));
                command.Parameters.Add(new SqlParameter("@Model_ID", modelId));

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var leafId = Convert.ToDecimal(reader["Source_Leaf_ID"]);
                    if (Convert.ToInt32(reader["CR_Parameter_ID"]) == crParameterId)
                    {
                        mappedHere.Add(leafId);
                        continue;
                    }

                    if (!otherParameters.TryGetValue(leafId, out var names))
                    {
                        names = new List<string>();
                        otherParameters[leafId] = names;
                    }
                    names.Add(reader["Parameter_Name"]?.ToString() ?? string.Empty);
                }
            }

            // 4. Application master data
            var result = new MM_CR_MasterDataDto { App_ID = appId };
            using (var command = CreateSpCommand(masterSpName, connection))
            {
                command.Parameters.Add(new SqlParameter("@Model_Name", modelName));

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>(reader.FieldCount + 2);
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }

                    var leafId = row.TryGetValue("Leaf_ID", out var leaf) && leaf != null
                        ? Convert.ToDecimal(leaf)
                        : 0;

                    row["Is_Mapped"] = mappedHere.Contains(leafId);
                    row["Other_Parameters"] = otherParameters.TryGetValue(leafId, out var names)
                        ? string.Join(", ", names)
                        : string.Empty;

                    result.Rows.Add(row);
                }
            }

            return result;
        }

        public async Task<MM_CR_ResultDto> SaveMappings(MM_CR_MappingSaveDto mapping)
        {
            var leaves = new DataTable();
            leaves.Columns.Add("Source_Leaf_ID", typeof(decimal));
            leaves.Columns.Add("Source_Model_ID", typeof(decimal));
            leaves.Columns.Add("Source_Display_Text", typeof(string));

            foreach (var item in mapping.Items)
            {
                leaves.Rows.Add(
                    item.Leaf_ID,
                    item.Source_Model_ID.HasValue ? item.Source_Model_ID.Value : DBNull.Value,
                    string.IsNullOrWhiteSpace(item.Display_Text) ? DBNull.Value : Truncate(item.Display_Text, 1000)
                );
            }

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = CreateSpCommand("SP_CR_Save_Mappings", connection);
            command.Parameters.Add(new SqlParameter("@CR_Parameter_ID", mapping.CR_Parameter_ID));
            command.Parameters.Add(new SqlParameter("@App_ID", mapping.App_ID));
            command.Parameters.Add(new SqlParameter("@Leaves", SqlDbType.Structured)
            {
                TypeName = "dbo.MM_CR_LeafList",
                Value = leaves
            });
            AddUserParameters(command, mapping.User_ID, mapping.Host);

            return await ExecuteResultAsync(command);
        }

        public async Task<MM_CR_ResultDto> Unmap(MM_CR_UnmapDto unmap)
        {
            var ids = new DataTable();
            ids.Columns.Add("ID", typeof(int));
            foreach (var id in unmap.Mapping_IDs.Distinct())
            {
                ids.Rows.Add(id);
            }

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = CreateSpCommand("SP_CR_Unmap", connection);
            command.Parameters.Add(new SqlParameter("@Mapping_IDs", SqlDbType.Structured)
            {
                TypeName = "dbo.MM_CR_IdList",
                Value = ids
            });
            AddUserParameters(command, unmap.User_ID, unmap.Host);

            return await ExecuteResultAsync(command);
        }

        //*************************************** Helpers ***************************************//

        private static SqlCommand CreateSpCommand(string spName, SqlConnection connection)
        {
            return new SqlCommand(spName, connection) { CommandType = CommandType.StoredProcedure };
        }

        private static void AddUserParameters(SqlCommand command, decimal? userId, string? host)
        {
            command.Parameters.Add(new SqlParameter("@User_ID", userId.HasValue ? userId.Value : DBNull.Value));
            command.Parameters.Add(new SqlParameter("@Host", string.IsNullOrWhiteSpace(host) ? DBNull.Value : host));
        }

        /// <summary>Reads the single Result_Code / Message row returned by save / delete SPs.</summary>
        private static async Task<MM_CR_ResultDto> ExecuteResultAsync(SqlCommand command)
        {
            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new MM_CR_ResultDto { Result_Code = 0, Message = "No response from database." };
            }

            var columns = Enumerable.Range(0, reader.FieldCount)
                .Select(reader.GetName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            int ReadInt(string name) =>
                columns.Contains(name) && reader[name] != DBNull.Value ? Convert.ToInt32(reader[name]) : 0;

            return new MM_CR_ResultDto
            {
                Result_Code = ReadInt("Result_Code"),
                Message = columns.Contains("Message") ? reader["Message"]?.ToString() : null,
                ID = ReadInt("ID"),
                Inserted_Count = ReadInt("Inserted_Count"),
                Skipped_Count = ReadInt("Skipped_Count")
            };
        }

        private static DateTime? ToNullableDate(object value)
        {
            return value != DBNull.Value ? Convert.ToDateTime(value) : null;
        }

        private static string Truncate(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}
