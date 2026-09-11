using System.Threading.Tasks;
using API_PQ_Global_Reporting.Data;
using API_PQ_Global_Reporting.Models.DTOs;
using API_PQ_Global_Reporting.Utils;
using Microsoft.AspNetCore.Mvc;

namespace API_PQ_Global_Reporting.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MM_CommonController : BaseController
    {
        private readonly MM_CommonDataService _CommonDataService;

        public MM_CommonController(
            MM_CommonDataService CommonDataService,
            IApiResponseHelper responseHelper,
            MM_ErrorLogDataService errorLogService)
            : base(responseHelper, errorLogService)
        {
            _CommonDataService = CommonDataService;
        }

        [HttpGet("GetDesignationList")]
        public async Task<IActionResult> GetDesignationList()
        {
            try
            {
                var designationList = await _CommonDataService.GetDesignationList();
                var response = _responseHelper.CreateSuccessResponse(designationList, "Success", "Designations retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetDesignationList");
            }
        }

        [HttpGet("GetShopList")]
        public async Task<IActionResult> GetShopList()
        {
            try
            {
                var shopList = await _CommonDataService.GetShopList();
                var response = _responseHelper.CreateSuccessResponse(shopList, "Success", "Shops retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetShopList");
            }
        }

        [HttpGet("GetPQStatusList")]
        public async Task<IActionResult> GetPQStatusList()
        {
            try
            {
                await _CommonDataService.SendTestMail();
                return Ok();
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetPQStatusList");
            }
        }

    }
}
