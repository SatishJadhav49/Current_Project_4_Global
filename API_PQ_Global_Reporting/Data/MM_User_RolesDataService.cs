using Microsoft.Data.SqlClient;
using API_PQ_Global_Reporting.Models.DTOs;
using System.Reflection.Metadata;

namespace API_PQ_Global_Reporting.Data
{
    public class MM_User_RolesDataService
    {

        private readonly IDbConnectionFactory _connectionFactory;

        public MM_User_RolesDataService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> CreateUserRole(MM_User_Role_CreateDto[] createUserRoles)
        {
            try
            {

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var transaction = connection.BeginTransaction();
                try
                {
                    const string query = @"INSERT INTO MM_User_Roles 
                    (Employee_ID,Role_ID,Is_Create,Is_Edit,Is_Delete,Plant_ID,Audit_Type_Id,Inserted_User_ID,Inserted_Host,Inserted_Date)
                    OUTPUT INSERTED.User_Role_Key
                    VALUES
                    (@Employee_ID,@Role_ID,@Is_Create,@Is_Edit,@Is_Delete,@Plant_ID,@Audit_Type_Id,
                    @Inserted_User_ID,@Inserted_Host,@Inserted_Date)
                    ";

                    for (int i = 0; i < createUserRoles.Length; i++)
                    {
                        using var command = new SqlCommand(query, connection, transaction);
                        command.Parameters.Add(new SqlParameter("@Employee_ID", createUserRoles[i].Employee_ID));
                        command.Parameters.Add(new SqlParameter("@Role_ID", createUserRoles[i].Role_ID));
                        command.Parameters.Add(new SqlParameter("@Is_Create", createUserRoles[i].Is_Create));
                        command.Parameters.Add(new SqlParameter("@Is_Edit", createUserRoles[i].Is_Edit));
                        command.Parameters.Add(new SqlParameter("@Is_Delete", createUserRoles[i].Is_Delete));
                        command.Parameters.Add(new SqlParameter("@Plant_ID", createUserRoles[i].Plant_ID));
                        command.Parameters.Add(new SqlParameter("@Audit_Type_Id", createUserRoles[i].Audit_Type_Id));
                        command.Parameters.Add(new SqlParameter("@Inserted_Host", createUserRoles[i].Inserted_Host));
                        command.Parameters.Add(new SqlParameter("@Inserted_User_ID", createUserRoles[i].Inserted_User_ID));
                        command.Parameters.Add(new SqlParameter("@Inserted_Date", DateTime.Now));
                        await command.ExecuteNonQueryAsync();
                    }
                    transaction.Commit();
                    return true;

                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }


        public async Task<bool> DeleteUserRole(decimal User_Role_Key)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var transaction = connection.BeginTransaction();
                try
                {
                    const string query = "DELETE FROM MM_User_Roles where User_Role_Key=@User_Role_Key";
                    using var command = new SqlCommand(query, connection, transaction);
                    command.Parameters.Add(new SqlParameter("@User_Role_Key", User_Role_Key));

                    await command.ExecuteNonQueryAsync();
                    transaction.Commit();
                    return true;
                }
                catch (System.Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public async Task<bool> UpdateUserRole(decimal UserRoleKey, UserRoleUpdateDto updates)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var transaction = connection.BeginTransaction();

                try
                {

                    const string updateQuery = @"UPDATE MM_User_Roles
                SET Employee_ID = @Employee_ID,
                    Role_ID = @Role_ID,
                    Is_Create = @Is_Create,
                    Is_Edit = @Is_Edit,
                    Is_Delete = @Is_Delete,
                    Plant_ID = @Plant_ID,
                    Audit_Type_Id = @Audit_Type_Id,
                    Updated_Host = @Updated_Host,
                    Updated_User_ID = @Updated_User_ID,
                    Updated_Date = @Updated_Date
                WHERE User_Role_Key = @User_Role_Key";


                    using var updateCommand = new SqlCommand(updateQuery, connection, transaction);
                    updateCommand.Parameters.Add(new SqlParameter("@User_Role_Key", updates.User_Role_Key));
                    updateCommand.Parameters.Add(new SqlParameter("@Employee_ID", updates.Employee_ID));
                    updateCommand.Parameters.Add(new SqlParameter("@Role_ID", updates.Role_ID));
                    updateCommand.Parameters.Add(new SqlParameter("@Is_Create", updates.Is_Create));
                    updateCommand.Parameters.Add(new SqlParameter("@Is_Edit", updates.Is_Edit));
                    updateCommand.Parameters.Add(new SqlParameter("@Is_Delete", updates.Is_Delete));
                    updateCommand.Parameters.Add(new SqlParameter("@Plant_ID", updates.Plant_ID));
                    updateCommand.Parameters.Add(new SqlParameter("@Audit_Type_Id", updates.Audit_Type_Id));
                    updateCommand.Parameters.Add(new SqlParameter("@Updated_Host", updates.Updated_Host));
                    updateCommand.Parameters.Add(new SqlParameter("@Updated_User_ID", updates.Updated_User_ID));
                    updateCommand.Parameters.Add(new SqlParameter("@Updated_Date", DateTime.Now));

                    await updateCommand.ExecuteNonQueryAsync();


                    transaction.Commit();
                    return true;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }



        public async Task<List<UserRoleListDto>> GetAllUserRole()
        {
            var userRoles = new List<UserRoleListDto>();

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                const string query = @"
            SELECT 
                ur.User_Role_Key,
                ur.Employee_ID,
                ur.Role_ID,
                ur.Is_Create,
                ur.Is_Edit,
                ur.Is_Delete,
                ur.Plant_ID,
                ur.Audit_Type_Id,
                ur.Inserted_User_ID,
                ur.Inserted_Host,
                ur.Inserted_Date,
                emp.Employee_Name,
                role.Role_Name
            FROM MM_User_Roles ur
            INNER JOIN MM_Employee emp ON ur.Employee_ID = emp.Employee_ID
            INNER JOIN MM_Roles role ON ur.Role_ID = role.Role_ID
        ";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var role = new UserRoleListDto
                    {
                        User_Role_Key = reader["User_Role_Key"] != DBNull.Value ? Convert.ToDecimal(reader["User_Role_Key"]) : 0,
                        Employee_ID = reader["Employee_ID"] != DBNull.Value ? Convert.ToDecimal(reader["Employee_ID"]) : 0,
                        Role_ID = reader["Role_ID"] != DBNull.Value ? Convert.ToDecimal(reader["Role_ID"]) : 0,
                        Is_Create = reader["Is_Create"] != DBNull.Value ? Convert.ToBoolean(reader["Is_Create"]) : false,
                        Is_Edit = reader["Is_Edit"] != DBNull.Value ? Convert.ToBoolean(reader["Is_Edit"]) : false,
                        Is_Delete = reader["Is_Delete"] != DBNull.Value ? Convert.ToBoolean(reader["Is_Delete"]) : false,
                        Plant_ID = reader["Plant_ID"] != DBNull.Value ? Convert.ToDecimal(reader["Plant_ID"]) : 0,
                        Audit_Type_Id = reader["Audit_Type_Id"] != DBNull.Value ? Convert.ToDecimal(reader["Audit_Type_Id"]) : 0,
                        Inserted_User_ID = reader["Inserted_User_ID"] != DBNull.Value ? Convert.ToDecimal(reader["Inserted_User_ID"]) : 0,
                        Inserted_Host = reader["Inserted_Host"] != DBNull.Value ? reader["Inserted_Host"].ToString() : string.Empty,
                        Inserted_Date = reader["Inserted_Date"] != DBNull.Value ? Convert.ToDateTime(reader["Inserted_Date"]) : DateTime.MinValue,
                        Employee_Name = reader["Employee_Name"] != DBNull.Value ? reader["Employee_Name"].ToString() : string.Empty,
                        Role_Name = reader["Role_Name"] != DBNull.Value ? reader["Role_Name"].ToString() : string.Empty,
                    };

                    userRoles.Add(role);
                }
            }
            catch (Exception)
            {
                throw;
            }

            return userRoles;
        }

        public async Task<List<RoleListDto>> GetRoleList()
        {
            try
            {
                const string query = @"
                    SELECT 
                    Role_ID,
                    Role_Name
                    FROM MM_Roles";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                var roleList = new List<RoleListDto>();

                while (await reader.ReadAsync())
                {
                    roleList.Add(new RoleListDto
                    {
                        Role_ID = reader.GetDecimal(reader.GetOrdinal("Role_ID")),
                        Role_Name = reader.GetString(reader.GetOrdinal("Role_Name"))
                    });
                }
                return roleList;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}