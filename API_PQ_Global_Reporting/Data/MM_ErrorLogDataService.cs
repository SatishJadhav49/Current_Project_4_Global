using API_PQ_Global_Reporting.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace API_PQ_Global_Reporting.Data
{
    public class MM_ErrorLogDataService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public MM_ErrorLogDataService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> LogErrorAsync(string controllerName, string actionName, string methodName, Exception ex)
        {
            try
            {
                const string query = @"INSERT INTO MM_Error_Log 
                    (Controller_Name, Action_Name, Meta_Class_Name, Method_Name, Exception_Detail, 
                     Inner_Exception, Message, Inserted_Date)
                    VALUES 
                    (@Controller_Name, @Action_Name, @Meta_Class_Name, @Method_Name, @Exception_Detail, 
                     @Inner_Exception, @Message, @Inserted_Date)";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);

                // Add parameters
                command.Parameters.Add(new SqlParameter("@Controller_Name", controllerName ?? (object)DBNull.Value));
                command.Parameters.Add(new SqlParameter("@Action_Name", actionName ?? (object)DBNull.Value));
                command.Parameters.Add(new SqlParameter("@Meta_Class_Name", methodName ?? (object)DBNull.Value));
                command.Parameters.Add(new SqlParameter("@Method_Name", methodName ?? (object)DBNull.Value));
                command.Parameters.Add(new SqlParameter("@Exception_Detail", ex.ToString() ?? (object)DBNull.Value));
                command.Parameters.Add(new SqlParameter("@Inner_Exception", ex.InnerException?.ToString() ?? (object)DBNull.Value));
                command.Parameters.Add(new SqlParameter("@Message", ex.Message ?? (object)DBNull.Value));
                command.Parameters.Add(new SqlParameter("@Inserted_Date", DateTime.Now));

                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception)
            {
                // Don't throw exceptions from error logging to avoid infinite loops
                return false;
            }
        }
    }
}
