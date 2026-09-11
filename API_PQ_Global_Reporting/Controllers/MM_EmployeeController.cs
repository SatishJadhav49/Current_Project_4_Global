using API_PQ_Global_Reporting.Data;
using API_PQ_Global_Reporting.Controllers;
using API_PQ_Global_Reporting.Data;
using API_PQ_Global_Reporting.Models.DTOs;
using API_PQ_Global_Reporting.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace API_PQ_Global_Reporting.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MM_EmployeeMasterController : BaseController
    {
        private readonly MM_EmployeeDataService _EmployeeService;

        public MM_EmployeeMasterController(
            MM_EmployeeDataService EmployeeService,
            IApiResponseHelper responseHelper,
            MM_ErrorLogDataService errorLogService)
            : base(responseHelper, errorLogService)
        {
            _EmployeeService = EmployeeService;
        }

        [HttpGet("GetAllEmployees")]
        public async Task<IActionResult> GetAllEmployees()
        {
            try
            {
                var employees = await _EmployeeService.GetAllEmployeesAsync();
                var response = _responseHelper.CreateSuccessResponse(employees, "Success", "Employees retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetAllEmployees");
            }
        }

        [HttpGet("GetEmployeeById/{employeeId}")]
        public async Task<IActionResult> GetEmployeeById(decimal employeeId)
        {
            try
            {
                var employee = await _EmployeeService.GetEmployeeDetailsAsync(employeeId, "");

                if (employee == null)
                {
                    var notFoundResponse = _responseHelper.CreateErrorResponse<MM_EmployeeDetailDto>("Not Found", "Employee not found");
                    return NotFound(notFoundResponse);
                }

                var response = _responseHelper.CreateSuccessResponse(employee, "Success", "Employee retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetEmployeeById");
            }
        }

        [HttpPost("CreateEmployee")]
        public async Task<IActionResult> CreateEmployee(MM_EmployeeCreateUserDto[] empcreatelist)
        {
            try
            {
                // Check if employee already exists by Employee_No
                var employeeNo = empcreatelist[0].Employee_No;
                var employeeExists = await _EmployeeService.CheckEmployeeExistsByNoAsync(employeeNo);

                if (employeeExists)
                {
                    var alertResponse = _responseHelper.CreateAlertResponse<object>(
                        "Employee Already Exists",
                        $"Employee with Employee No '{employeeNo}' already exists in the system",
                        null
                    );
                    return Ok(alertResponse);
                }

                var emp = await _EmployeeService.CreateUser(empcreatelist);
                var response = _responseHelper.CreateSuccessResponse(
                   emp,
                   "Success",
                   "User created successfully"
               );

                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "CreateEmployee");
            }
        }

        [HttpPost("UpdateEmployee/{employeeId}")]
        public async Task<IActionResult> UpdateEmployee(decimal employeeId, MM_EmployeeUpdateUserDto[] empupdatelist)
        {
            try
            {
                var emp = await _EmployeeService.UpdateUser(employeeId, empupdatelist);
                var response = _responseHelper.CreateSuccessResponse(
                   emp,
                   Constants.Messages.SUCCESS,
                   "Employee updated successfully"
               );

                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "UpdateEmployee");
            }
        }

        [HttpPost("DeleteEmployee/{employeeId}")]
        public async Task<IActionResult> DeleteEmployee(decimal employeeId)
        {
            try
            {
                // First check if employee exists
                var employee = await _EmployeeService.GetEmployeeDetailsAsync(employeeId, "");
                if (employee == null)
                {
                    var notFoundResponse = _responseHelper.CreateErrorResponse<object>("Not Found", "Employee not found");
                    return NotFound(notFoundResponse);
                }

                // Delete the employee and related shop model data
                var isDeleted = await _EmployeeService.DeleteEmployeeAsync(employeeId);

                if (isDeleted)
                {
                    var response = _responseHelper.CreateSuccessResponse(
                        new { EmployeeId = employeeId },
                        "Success",
                        "Employee data deleted successfully"
                    );
                    return Ok(response);
                }
                else
                {
                    var errorResponse = _responseHelper.CreateErrorResponse<object>("Error", "Failed to delete employee");
                    return BadRequest(errorResponse);
                }
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "DeleteEmployee");
            }
        }

        [HttpGet("GetTokenCurrent")]
        public async Task<IActionResult> GetTokenCurrent()
        {
            try
            {
                EncryptDecrypt encDecobj = new EncryptDecrypt();

                // Get current user from HttpContext 
                string currentUser = string.Empty;

                // Try to get Windows Identity first
                if (HttpContext.User?.Identity?.Name != null)
                {
                    currentUser = HttpContext.User.Identity.Name;
                }
                else
                {
                    // Fallback to environment user if no authenticated user
                    currentUser = Environment.UserName;
                }

                // Extract username from domain\username format
                string[] tokenNumber = currentUser.Split(new char[] { '\\' });
                string username = tokenNumber.Length > 1 ? tokenNumber[1] : tokenNumber[0];

                // Encrypt the username
                var encrypt = encDecobj.EnryptString(username);

                var response = _responseHelper.CreateSuccessResponse(encrypt, "Success", "Token retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetTokenCurrent");
            }
        }

        [HttpGet("UserAuthentication/{id},{plantid},{Audit_Type_Id}")]
        public async Task<IActionResult> UserAuthentication(string id, decimal plantid, decimal Audit_Type_Id)
        {
            try
            {
                EncryptDecrypt encDecobj = new EncryptDecrypt();
                var userno = encDecobj.DecryptString(id);

                var userCred = await _EmployeeService.GetUserAuthenticationAsync(userno, plantid, Audit_Type_Id);

                if (userCred == null || !userCred.Any())
                {
                    var notFoundResponse = _responseHelper.CreateErrorResponse<object>("Not Found", "User authentication failed or no permissions found");
                    return NotFound(notFoundResponse);
                }

                var response = _responseHelper.CreateSuccessResponse(userCred, "Success", "User authentication successful");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "UserAuthentication");
            }
        }

        [HttpGet("SearchToken/{id}")]
        public async Task<IActionResult> SearchToken(string id)
        {
            try
            {
                EncryptDecrypt encDecobj = new EncryptDecrypt();
                var decrypt = encDecobj.DecryptString(id);

                var empObj = await _EmployeeService.GetEmployeeDetailsAsync(0, decrypt);

                if (empObj == null)
                {
                    var response = _responseHelper.CreateSuccessResponse(new List<object>(), "Success", "No employee found");
                    return Ok(response);
                }

                var successResponse = _responseHelper.CreateSuccessResponse(empObj, "Success", "Employee search completed successfully");
                return Ok(successResponse);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "SearchToken");
            }
        }

        [HttpGet("getCurrentHostName")]
        public IActionResult GetCurrentHostName()
        {
            try
            {
                string? hostName = null;

                // Get client IP address
                var clientIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                if (!string.IsNullOrEmpty(clientIpAddress))
                {
                    try
                    {
                        // Get hostname from IP address
                        var hostEntry = Dns.GetHostEntry(clientIpAddress);
                        hostName = hostEntry.HostName;

                        // Extract machine name (remove domain suffix)
                        if (!string.IsNullOrEmpty(hostName))
                        {
                            string[] parts = hostName.Split(new char[] { '.' });
                            hostName = parts[0].ToLower();
                        }
                    }
                    catch
                    {
                        // Fallback to server hostname if client hostname resolution fails
                        hostName = Dns.GetHostName();
                        if (!string.IsNullOrEmpty(hostName))
                        {
                            string[] parts = hostName.Split(new char[] { '.' });
                            hostName = parts[0].ToLower();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(hostName) && hostName.Trim() != string.Empty)
                {
                    var response = _responseHelper.CreateSuccessResponse(hostName, "Success", "Host name retrieved successfully");
                    return Ok(response);
                }
                else
                {
                    var errorResponse = _responseHelper.CreateErrorResponse<string>("Error", "Unable to retrieve host name");
                    return BadRequest(errorResponse);
                }
            }
            catch (Exception ex)
            {
                return HandleExceptionAsync(ex, "GetCurrentHostName").Result;
            }
        }

    }
}
