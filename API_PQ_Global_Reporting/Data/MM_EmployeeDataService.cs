
using API_PQ_Global_Reporting.Data;
using API_PQ_Global_Reporting.Models.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;

namespace API_PQ_Global_Reporting.Data
{
    public class MM_EmployeeDataService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public MM_EmployeeDataService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<MM_EmployeeListDto>> GetAllEmployeesAsync()
        {
            var employees = new List<MM_EmployeeListDto>();

            try
            {
                const string query = @"
                    SELECT 
                        e.Employee_ID,
                        e.Employee_Name,
                        e.Employee_No,
                        ISNULL(e.Email_Address, '') as Email_Address,
                        e.Reporting_Manager_ID,
                        e.Audit_Type_Id,
                        e.Plant_ID
                    FROM MM_Employee e
                    WHERE e.Is_Deleted IS NULL OR e.Is_Deleted = 0
                    ORDER BY e.Employee_Name";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    employees.Add(new MM_EmployeeListDto
                    {
                        Employee_ID = reader.GetDecimal("Employee_ID"),
                        Employee_Name = reader.GetString("Employee_Name"),
                        Employee_No = reader.GetString("Employee_No"),
                        Email_Address = reader.GetString("Email_Address"),
                        Reporting_Manager_ID = reader.IsDBNull("Reporting_Manager_ID") ? null : reader.GetDecimal("Reporting_Manager_ID"),
                        Audit_Type_Id = reader.GetDecimal("Audit_Type_Id"),
                        Plant_ID = reader.GetDecimal("Plant_ID")
                    });
                }
            }
            catch (Exception)
            {
                throw;
            }

            return employees;
        }

        public async Task<MM_EmployeeDetailDto?> GetEmployeeDetailsAsync(decimal employeeId, string empno)
        {
            MM_EmployeeDetailDto? employee = null;

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                // First query: Get employee basic data
                string employeeQuery;
                if (!string.IsNullOrEmpty(empno) && empno.Length > 2)
                {
                    employeeQuery = @"
                SELECT 
                    e.Employee_ID,
                    e.Employee_Name,
                    e.Employee_No,
                    ISNULL(e.Email_Address, '') as Email_Address,
                    e.Reporting_Manager_ID,
                    e.Audit_Type_Id,
                    e.Plant_ID,
                    p.Plant_Code
                FROM MM_Employee e
                LEFT JOIN MM_Plant p ON e.Plant_ID = p.Plant_ID
                WHERE e.Employee_No = @Employee_No AND (e.Is_Deleted IS NULL OR e.Is_Deleted = 0)";
                }
                else
                {
                    employeeQuery = @"
                SELECT 
                    e.Employee_ID,
                    e.Employee_Name,
                    e.Employee_No,
                    ISNULL(e.Email_Address, '') as Email_Address,
                    e.Reporting_Manager_ID,
                    e.Audit_Type_Id,
                    e.Plant_ID,
                     p.Plant_Code
                FROM MM_Employee e
                LEFT JOIN MM_Plant p ON e.Plant_ID = p.Plant_ID
                WHERE e.Employee_ID = @Employee_ID AND (e.Is_Deleted IS NULL OR e.Is_Deleted = 0)";
                }

                using (var command = new SqlCommand(employeeQuery, connection))
                {
                    if (!string.IsNullOrEmpty(empno) && empno.Length > 2)
                    {
                        command.Parameters.Add(new SqlParameter("@Employee_No", empno));
                    }
                    else
                    {
                        command.Parameters.Add(new SqlParameter("@Employee_ID", employeeId));
                    }
                    using var reader = await command.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        employee = new MM_EmployeeDetailDto
                        {
                            Employee_ID = reader.GetDecimal("Employee_ID"),
                            Employee_Name = reader.GetString("Employee_Name"),
                            Employee_No = reader.GetString("Employee_No"),
                            Email_Address = reader.GetString("Email_Address"),
                            Reporting_Manager_ID = reader.IsDBNull("Reporting_Manager_ID") ? null : reader.GetDecimal("Reporting_Manager_ID"),
                            Audit_Type_Id = reader.GetDecimal("Audit_Type_Id"),
                            Plant_ID = reader.GetDecimal("Plant_ID"),
                            Plant_Code = reader.IsDBNull("Plant_Code") ? string.Empty : reader.GetString("Plant_Code"),
                            Shop_ID = new List<int>(),
                            Model_ID = new List<int>(),
                            Hostname = ""
                        };
                    }
                }


                // Save user login activity to database
                if (employee != null)
                {
                    const string activityLogQuery = @"
                        INSERT INTO MM_User_Activity_Logs (User_Name, User_Token_No, Logged_In_Time, Audit_Type, Shop_Name)
                        VALUES (@User_Name, @User_Token_No, @Logged_In_Time, @Audit_Type, @Shop_Name)";

                    using var activityCommand = new SqlCommand(activityLogQuery, connection);
                    activityCommand.Parameters.Add(new SqlParameter("@User_Name", employee.Employee_Name));
                    activityCommand.Parameters.Add(new SqlParameter("@User_Token_No", employee.Employee_No));
                    activityCommand.Parameters.Add(new SqlParameter("@Logged_In_Time", DateTime.Now));
                    activityCommand.Parameters.Add(new SqlParameter("@Audit_Type", "LSP"));
                    activityCommand.Parameters.Add(new SqlParameter("@Shop_Name", "LSP"));
                    await activityCommand.ExecuteNonQueryAsync();
                }
            }
            catch (Exception)
            {
                throw;
            }

            return employee;
        }

        public async Task<List<object>> GetUserAuthenticationAsync(string employeeNo, decimal plantId, decimal auditTypeId)
        {
            var result = new List<object>();

            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                const string query = @"
                    SELECT DISTINCT
                        mr.Menu_ID,
                        m.Sort_Order,
                        e.Employee_ID,
                        e.Employee_Name,
                        e.Employee_No,
                        e.Audit_Type_Id,
                        ur.Role_ID,
                        r.Role_Name,
                        ur.Is_Create,
                        ur.Is_Edit,
                        ur.Is_Delete
                    FROM MM_Employee e
                    INNER JOIN MM_User_Roles ur ON e.Employee_ID = ur.Employee_ID AND e.Audit_Type_Id = ur.Audit_Type_Id
                    INNER JOIN MM_Menu_Role mr ON ur.Role_ID = mr.Role_ID
                    INNER JOIN MM_Menus m ON mr.Menu_ID = m.Menu_ID
                    INNER JOIN MM_Roles r ON ur.Role_ID = r.Role_ID
                    WHERE e.Employee_No = @EmployeeNo 
                        AND r.Plant_ID = @PlantId 
                        AND m.Is_Active = 1 
                        AND e.Audit_Type_Id = @AuditTypeId
                    ORDER BY m.Sort_Order";

                using var command = new SqlCommand(query, connection);
                command.Parameters.Add(new SqlParameter("@EmployeeNo", employeeNo));
                command.Parameters.Add(new SqlParameter("@PlantId", plantId));
                command.Parameters.Add(new SqlParameter("@AuditTypeId", auditTypeId));

                using var reader = await command.ExecuteReaderAsync();
                var tempResults = new List<dynamic>();

                while (await reader.ReadAsync())
                {
                    var item = new
                    {
                        Menu_ID = reader.GetDecimal("Menu_ID"),
                        Sort_Order = reader.IsDBNull("Sort_Order") ? 0 : reader.GetInt32("Sort_Order"),
                        Employee_ID = reader.GetDecimal("Employee_ID"),
                        Employee_Name = reader.GetString("Employee_Name"),
                        Employee_No = reader.GetString("Employee_No"),
                        Audit_Type_Id = reader.GetDecimal("Audit_Type_Id"),
                        Role_ID = reader.GetDecimal("Role_ID"),
                        Role_Name = reader.GetString("Role_Name"),
                        Is_Create = reader.GetBoolean("Is_Create"),
                        Is_Edit = reader.GetBoolean("Is_Edit"),
                        Is_Delete = reader.GetBoolean("Is_Delete")
                    };
                    tempResults.Add(item);
                }
                // Get submenu data for each role
                foreach (var group in tempResults.GroupBy(x => x.Role_ID))
                {
                    var firstItem = group.First();

                    // Get submenus for this role
                    const string subMenuQuery = @"
                        SELECT DISTINCT 
                            sm.ActionName,
                            sm.LinkName,
                            sm.Sort_Order
                        FROM MM_Menu_Role mr
                        INNER JOIN MM_Sub_Menus sm ON mr.Menu_ID = sm.Menu_ID
                        WHERE mr.Role_ID = @RoleId AND sm.Is_Active = 1
                        ORDER BY sm.Sort_Order";

                    using var subMenuCommand = new SqlCommand(subMenuQuery, connection);
                    subMenuCommand.Parameters.Add(new SqlParameter("@RoleId", firstItem.Role_ID));

                    var subMenuList = new List<object>();
                    using var subMenuReader = await subMenuCommand.ExecuteReaderAsync();

                    while (await subMenuReader.ReadAsync())
                    {
                        subMenuList.Add(new
                        {
                            ActionName = subMenuReader.GetString("ActionName"),
                            LinkName = subMenuReader.GetString("LinkName"),
                            Sort_Order = subMenuReader.GetInt32("Sort_Order")
                        });
                    }

                    result.Add(new
                    {
                        Menu_ID = firstItem.Menu_ID,
                        Sort_Order = firstItem.Sort_Order,
                        Employee_ID = firstItem.Employee_ID,
                        Employee_Name = firstItem.Employee_Name,
                        Employee_No = firstItem.Employee_No,
                        Audit_Type_Id = firstItem.Audit_Type_Id,
                        Role_ID = firstItem.Role_ID,
                        Role_Name = firstItem.Role_Name,
                        Is_Create = firstItem.Is_Create,
                        Is_Edit = firstItem.Is_Edit,
                        Is_Delete = firstItem.Is_Delete,
                        SubMenuList = subMenuList
                    });
                }

                // Sort by Sort_Order
                result = result.OrderBy(x => ((dynamic)x).Sort_Order).ToList();
            }
            catch (Exception)
            {
                throw;
            }

            return result;
        }

        public async Task<bool> CreateUser(MM_EmployeeCreateUserDto[] emplist)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    const string query = @"INSERT INTO MM_Employee 
                        (Employee_Name, Employee_No, Email_Address, Reporting_Manager_ID, 
                         Audit_Type_Id, Plant_ID, Plant_Code, Inserted_Host, Inserted_User_ID, Inserted_Date)
                        OUTPUT INSERTED.Employee_ID
                        VALUES 
                        (@Employee_Name, @Employee_No, @Email_Address,  @Reporting_Manager_ID, 
                         @Audit_Type_Id, @Plant_ID, @Plant_Code, @Inserted_Host, @Inserted_User_ID, @Inserted_Date)";

                    using var command = new SqlCommand(query, connection, transaction);

                    // Add parameters
                    command.Parameters.Add(new SqlParameter("@Employee_Name", emplist[0].Employee_Name));
                    command.Parameters.Add(new SqlParameter("@Employee_No", emplist[0].Employee_No));
                    command.Parameters.Add(new SqlParameter("@Email_Address", emplist[0].Email_Address));
                    command.Parameters.Add(new SqlParameter("@Reporting_Manager_ID", emplist[0].Reporting_Manager_ID));
                    command.Parameters.Add(new SqlParameter("@Audit_Type_Id", emplist[0].Audit_Type_Id));
                    command.Parameters.Add(new SqlParameter("@Plant_ID", emplist[0].Plant_ID));
                    command.Parameters.Add(new SqlParameter("@Plant_Code", emplist[0].Plant_Code));
                    command.Parameters.Add(new SqlParameter("@Inserted_Host", emplist[0].Inserted_Host));
                    command.Parameters.Add(new SqlParameter("@Inserted_User_ID", emplist[0].Inserted_User_ID));
                    command.Parameters.Add(new SqlParameter("@Inserted_Date", DateTime.Now));

                    // Execute query
                    var empid = 0;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            empid = (int)reader.GetDecimal("Employee_ID");
                        }
                    } // Reader is properly closed here



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

        public async Task<bool> UpdateUser(decimal employeeId, MM_EmployeeUpdateUserDto[] empupdatelist)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Update employee main record
                    const string updateEmployeeQuery = @"UPDATE MM_Employee 
                        SET Employee_Name = @Employee_Name,
                            Employee_No = @Employee_No,
                            Email_Address = @Email_Address,
                            Reporting_Manager_ID = @Reporting_Manager_ID,
                            Audit_Type_Id = @Audit_Type_Id,
                            Plant_ID = @Plant_ID,
                            Plant_Code = @Plant_Code,
                            Updated_Host = @Updated_Host,
                            Updated_User_ID = @Updated_User_ID,
                            Updated_Date = @Updated_Date,
                            Is_Edited = 1
                        WHERE Employee_ID = @Employee_ID";

                    using var updateCommand = new SqlCommand(updateEmployeeQuery, connection, transaction);
                    updateCommand.Parameters.Add(new SqlParameter("@Employee_ID", employeeId));
                    updateCommand.Parameters.Add(new SqlParameter("@Employee_Name", empupdatelist[0].Employee_Name));
                    updateCommand.Parameters.Add(new SqlParameter("@Employee_No", empupdatelist[0].Employee_No));
                    updateCommand.Parameters.Add(new SqlParameter("@Email_Address", empupdatelist[0].Email_Address));
                    updateCommand.Parameters.Add(new SqlParameter("@Reporting_Manager_ID", empupdatelist[0].Reporting_Manager_ID ?? (object)DBNull.Value));
                    updateCommand.Parameters.Add(new SqlParameter("@Audit_Type_Id", empupdatelist[0].Audit_Type_Id));
                    updateCommand.Parameters.Add(new SqlParameter("@Plant_ID", empupdatelist[0].Plant_ID));
                    updateCommand.Parameters.Add(new SqlParameter("@Plant_Code", empupdatelist[0].Plant_Code));
                    updateCommand.Parameters.Add(new SqlParameter("@Updated_Host", empupdatelist[0].Updated_Host));
                    updateCommand.Parameters.Add(new SqlParameter("@Updated_User_ID", empupdatelist[0].Updated_User_ID));
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

        public async Task<bool> CheckEmployeeExistsByNoAsync(string employeeNo)
        {
            try
            {
                const string query = @"
                    SELECT COUNT(1) 
                    FROM MM_Employee 
                    WHERE Employee_No = @Employee_No 
                    AND (Is_Deleted IS NULL OR Is_Deleted = 0)";

                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var command = new SqlCommand(query, connection);
                command.Parameters.Add(new SqlParameter("@Employee_No", employeeNo));

                var result = await command.ExecuteScalarAsync();
                var count = result != null ? (int)result : 0;
                return count > 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> DeleteEmployeeAsync(decimal employeeId)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var transaction = connection.BeginTransaction();

                try
                {

                    // Then, delete from MM_Employee table
                    const string deleteEmployeeQuery = @"DELETE FROM MM_Employee WHERE Employee_ID = @Employee_ID";
                    using var employeeCommand = new SqlCommand(deleteEmployeeQuery, connection, transaction);
                    employeeCommand.Parameters.AddWithValue("@Employee_ID", employeeId);

                    var rowsAffected = await employeeCommand.ExecuteNonQueryAsync();

                    // Commit the transaction
                    transaction.Commit();

                    return rowsAffected > 0;
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


    }
}
