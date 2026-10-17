using API_PQ_Global_Reporting.CorrelationModule.Models;
using API_PQ_Global_Reporting.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace API_PQ_Global_Reporting.CorrelationModule.Data
{
    public class MM_CR_ReportDataService
    {
        private const int ReadingCommandTimeoutSeconds = 120;

        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<MM_CR_ReportDataService> _logger;

        public MM_CR_ReportDataService(IDbConnectionFactory connectionFactory, ILogger<MM_CR_ReportDataService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// Builds the correlation report: one series per active mapping of the parameter.
        /// Each application's reading SP (from MM_CR_Application) is called once with all
        /// of its mapped leaves; applications run in parallel on separate connections.
        /// Returns null when the parameter does not exist.
        /// </summary>
        public async Task<MM_CR_ReportDto?> GetReport(int crParameterId, DateTime fromDate, DateTime toDate)
        {
            var (parameter, mappings) = await GetReportMappings(crParameterId);
            if (parameter == null) return null;

            var report = new MM_CR_ReportDto
            {
                Parameter = parameter,
                From_Date = fromDate.Date,
                To_Date = toDate.Date
            };

            var series = mappings.Select(m => m.Series).ToList();
            report.Series = series;

            var appTasks = mappings
                .GroupBy(m => m.Series.App_ID)
                .Select(group => LoadApplicationReadings(group.ToList(), fromDate.Date, toDate.Date));

            await Task.WhenAll(appTasks);

            foreach (var item in series)
            {
                item.Stats = BuildStats(item.Points);
            }

            return report;
        }

        private async Task<(MM_CR_ParameterDto? Parameter, List<ReportMapping> Mappings)> GetReportMappings(int crParameterId)
        {
            MM_CR_ParameterDto? parameter = null;
            var mappings = new List<ReportMapping>();

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = new SqlCommand("SP_CR_Get_Report_Mappings", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.Add(new SqlParameter("@CR_Parameter_ID", crParameterId));

            using var reader = await command.ExecuteReaderAsync();

            // Result Set 1 - Parameter header
            if (await reader.ReadAsync())
            {
                parameter = new MM_CR_ParameterDto
                {
                    CR_Parameter_ID = Convert.ToInt32(reader["CR_Parameter_ID"]),
                    Model_ID = Convert.ToDecimal(reader["Model_ID"]),
                    Model_Name = reader["Model_Name"]?.ToString(),
                    Parameter_Name = reader["Parameter_Name"]?.ToString()
                };
            }

            // Result Set 2 - Active mappings with their application's reading SP
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    mappings.Add(new ReportMapping
                    {
                        Reading_SP_Name = reader["Reading_SP_Name"] != DBNull.Value ? reader["Reading_SP_Name"].ToString() : null,
                        Series = new MM_CR_ReportSeriesDto
                        {
                            Mapping_ID = Convert.ToInt32(reader["Mapping_ID"]),
                            App_ID = Convert.ToInt32(reader["App_ID"]),
                            App_Name = reader["App_Name"]?.ToString(),
                            Leaf_Level_Name = reader["Leaf_Level_Name"]?.ToString(),
                            Source_Leaf_ID = Convert.ToDecimal(reader["Source_Leaf_ID"]),
                            Source_Leaf_Type = NormalizeLeafType(reader["Source_Leaf_Type"]),
                            Display_Text = reader["Source_Display_Text"]?.ToString()
                        }
                    });
                }
            }

            return (parameter, mappings);
        }

        /// <summary>
        /// Runs one application's reading SP for all its mapped leaves and distributes
        /// the rows to the matching series. A failure only marks this application's series.
        /// </summary>
        private async Task LoadApplicationReadings(List<ReportMapping> appMappings, DateTime fromDate, DateTime toDate)
        {
            var spName = appMappings[0].Reading_SP_Name;
            if (string.IsNullOrWhiteSpace(spName))
            {
                appMappings.ForEach(m => m.Series.Error = "Readings are not configured for this application.");
                return;
            }

            var seriesByLeaf = appMappings.ToDictionary(
                m => LeafKey(m.Series.Source_Leaf_ID, m.Series.Source_Leaf_Type),
                m => m.Series);

            // Column order must match dbo.MM_CR_LeafList
            var leaves = new DataTable();
            leaves.Columns.Add("Source_Leaf_ID", typeof(decimal));
            leaves.Columns.Add("Source_Leaf_Type", typeof(string));
            leaves.Columns.Add("Source_Model_ID", typeof(decimal));
            leaves.Columns.Add("Source_Display_Text", typeof(string));
            foreach (var mapping in appMappings)
            {
                leaves.Rows.Add(
                    mapping.Series.Source_Leaf_ID,
                    (object?)mapping.Series.Source_Leaf_Type ?? DBNull.Value,
                    DBNull.Value,
                    DBNull.Value);
            }

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(spName, connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = ReadingCommandTimeoutSeconds
                };
                command.Parameters.Add(new SqlParameter("@Leaves", SqlDbType.Structured)
                {
                    TypeName = "dbo.MM_CR_LeafList",
                    Value = leaves
                });
                command.Parameters.Add(new SqlParameter("@From_Date", SqlDbType.Date) { Value = fromDate });
                command.Parameters.Add(new SqlParameter("@To_Date", SqlDbType.Date) { Value = toDate });

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (reader["Reading"] == DBNull.Value || reader["Audit_Date"] == DBNull.Value) continue;

                    var key = LeafKey(reader["Leaf_ID"], reader["Leaf_Type"]);
                    if (!seriesByLeaf.TryGetValue(key, out var series)) continue;

                    var reading = Convert.ToDecimal(reader["Reading"]);
                    var lsl = ToNullableDecimal(reader["LSL"]);
                    var usl = ToNullableDecimal(reader["USL"]);

                    series.Points.Add(new MM_CR_ReadingPointDto
                    {
                        Audit_Date = Convert.ToDateTime(reader["Audit_Date"]),
                        Reading_No = reader["Reading_No"] != DBNull.Value ? Convert.ToInt32(reader["Reading_No"]) : 1,
                        Reading = reading,
                        LSL = lsl,
                        USL = usl,
                        Is_Out_Of_Spec = (lsl.HasValue && reading < lsl.Value) || (usl.HasValue && reading > usl.Value)
                    });
                }

                foreach (var series in seriesByLeaf.Values)
                {
                    series.Points = series.Points
                        .OrderBy(p => p.Audit_Date)
                        .ThenBy(p => p.Reading_No)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Correlation report: reading SP {SpName} failed", spName);
                appMappings.ForEach(m =>
                {
                    m.Series.Points.Clear();
                    m.Series.Error = "Unable to load readings for this application.";
                });
            }
        }

        private static MM_CR_ReportStatsDto BuildStats(List<MM_CR_ReadingPointDto> points)
        {
            if (points.Count == 0) return new MM_CR_ReportStatsDto();

            var first = points[0];
            var last = points[^1];

            return new MM_CR_ReportStatsDto
            {
                Count = points.Count,
                Mean = Math.Round(points.Average(p => p.Reading), 4),
                Min = points.Min(p => p.Reading),
                Max = points.Max(p => p.Reading),
                Out_Of_Spec_Count = points.Count(p => p.Is_Out_Of_Spec),
                LSL_Start = first.LSL,
                USL_Start = first.USL,
                LSL_End = last.LSL,
                USL_End = last.USL
            };
        }

        /// <summary>A leaf is identified by its ID plus optional type (e.g. Gap / Flush).</summary>
        private static string LeafKey(object? leafId, object? leafType)
        {
            var id = leafId == null || leafId == DBNull.Value ? 0 : Convert.ToDecimal(leafId);
            return $"{id}|{NormalizeLeafType(leafType)?.ToUpperInvariant()}";
        }

        private static string? NormalizeLeafType(object? value)
        {
            if (value == null || value == DBNull.Value) return null;
            var text = value.ToString()?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }

        private static decimal? ToNullableDecimal(object value)
        {
            return value != DBNull.Value ? Convert.ToDecimal(value) : null;
        }

        private sealed class ReportMapping
        {
            public string? Reading_SP_Name { get; set; }
            public MM_CR_ReportSeriesDto Series { get; set; } = new();
        }
    }
}
