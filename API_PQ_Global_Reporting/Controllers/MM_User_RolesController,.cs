using API_PQ_Global_Reporting.Data;
using API_PQ_Global_Reporting.Models.DTOs;
using API_PQ_Global_Reporting.Utils;
using Microsoft.AspNetCore.Mvc;

namespace API_PQ_Global_Reporting.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MM_User_RolesController : BaseController
    {
        private readonly MM_User_RolesDataService _UserRolesDataService;
        public MM_User_RolesController(MM_User_RolesDataService UserRolesData, IApiResponseHelper responseHelper, MM_ErrorLogDataService logerrorService) : base(responseHelper, logerrorService)
        {
            _UserRolesDataService = UserRolesData;
        }

        // Add api to get all ro;les list from :  from MM_Roles

        [HttpPost("CreateUserRole")]
        public async Task<IActionResult> CreateUserRole(MM_User_Role_CreateDto[] createUserRoles)
        {
            try
            {
                var result = await _UserRolesDataService.CreateUserRole(createUserRoles);
                var response = _responseHelper.CreateSuccessResponse(result, "Success", "Saved User Roles..");
                return Ok(response);
            }
            catch (Exception Ex)
            {

                return await HandleExceptionAsync(Ex, "CreateUserRole");
            }
        }

        [HttpPost("DeleteUserRole")]
        public async Task<IActionResult> DeleteUserRole([FromBody]  decimal User_Role_Key)
        {
            try
            {
                var result = await _UserRolesDataService.DeleteUserRole(User_Role_Key);
                var response = _responseHelper.CreateSuccessResponse(result, "Success", "User Role Deleted Successfully");
                return Ok(response);
            }
            catch (System.Exception)
            {

                throw;
            }
        }

        [HttpPost("UpdateUserRole/{UserRoleKey}")]
        public async Task<IActionResult> UpdateUserRole(decimal UserRoleKey, UserRoleUpdateDto userroleUpdateList)
        {
            try
            {
                var result = await _UserRolesDataService.UpdateUserRole(UserRoleKey, userroleUpdateList);
                var response = _responseHelper.CreateSuccessResponse(result, "Success", "userRole updated successfully.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "UpdateUserRole");
            }
        }

        [HttpGet("GetAllUserRole")]
        public async Task<IActionResult> GetAllUserRole()
        {
            try
            {
                var result = await _UserRolesDataService.GetAllUserRole();
                var response = _responseHelper.CreateSuccessResponse(result, "Success", "Fetched all User Role.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetAllUserRole");
            }
        }

        [HttpGet("GetRoleList")]
        public async Task<IActionResult> GetRoleList()
        {
            try
            {
                var roleList = await _UserRolesDataService.GetRoleList();
                var response = _responseHelper.CreateSuccessResponse(roleList, "Success", "Role retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetRoleList");
            }
        }



    }
}