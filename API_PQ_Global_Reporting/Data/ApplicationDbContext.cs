using Microsoft.Data.SqlClient;

namespace API_PQ_Global_Reporting.Data
{
    public interface IDbConnectionFactory
    {
        SqlConnection CreateConnection();
        Task<SqlConnection> CreateConnectionAsync();
    }

    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;
        // private readonly string _connectionStringDockAudit;
        // private readonly string _connectionStringTorqueAudit;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string not found");
            // _connectionStringDockAudit = configuration.GetConnectionString("PQ_Dock_Audit")
            //     ?? throw new ArgumentNullException(nameof(configuration), "Connection string not found");
            // _connectionStringTorqueAudit = configuration.GetConnectionString("PQ_Torque_Audit")
            //     ?? throw new ArgumentNullException(nameof(configuration), "Connection string not found");
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task<SqlConnection> CreateConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        // public SqlConnection CreateConnectionForDockAudit()
        // {
        //     return new SqlConnection(_connectionStringDockAudit);
        // }
    }
}
