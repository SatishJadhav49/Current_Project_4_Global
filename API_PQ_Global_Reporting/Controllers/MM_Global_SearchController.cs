using API_PQ_Global_Reporting.Data;
using API_PQ_Global_Reporting.Utils;
using Microsoft.AspNetCore.Mvc;

namespace API_PQ_Global_Reporting.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MM_Global_SearchController : BaseController
    {
        private readonly MM_Global_SearchDataService _globalSearchDataService;

        public MM_Global_SearchController(
            MM_Global_SearchDataService globalSearchDataService,
            IApiResponseHelper responseHelper,
            MM_ErrorLogDataService errorLogService)
            : base(responseHelper, errorLogService)
        {
            _globalSearchDataService = globalSearchDataService;
        }

        [HttpGet("GetVehicleInfo/{vehicleNo}")]
        public async Task<IActionResult> GetVehicleInfo(string vehicleNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(vehicleNo))
                {
                    var validationResponse = _responseHelper.CreateErrorResponse<object>(
                        Constants.Messages.VALIDATION_ERROR,
                        "Vehicle number is required"
                    );
                    return BadRequest(validationResponse);
                }

                var vehicleInfo = await _globalSearchDataService.GetVehicleInfo(vehicleNo.Trim());

                var response = _responseHelper.CreateSuccessResponse(vehicleInfo, "Success", "Vehicle information retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetVehicleInfo");
            }
        }

        [HttpGet("GetVehicleDefects")]
        public async Task<IActionResult> GetVehicleDefects([FromQuery] string? vinNumber, [FromQuery] string? biwNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(vinNumber) && string.IsNullOrWhiteSpace(biwNo))
                {
                    var validationResponse = _responseHelper.CreateErrorResponse<object>(
                        Constants.Messages.VALIDATION_ERROR,
                        "VIN number or BIW number is required"
                    );
                    return BadRequest(validationResponse);
                }

                var defects = await _globalSearchDataService.GetVehicleDefectsByNo(
                    vinNumber?.Trim() ?? string.Empty,
                    biwNo?.Trim() ?? string.Empty
                );

                var response = _responseHelper.CreateSuccessResponse(defects, "Success", "Vehicle defects retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetVehicleDefects");
            }
        }
    }
}
