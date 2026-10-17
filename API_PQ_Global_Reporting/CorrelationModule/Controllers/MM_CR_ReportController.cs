using API_PQ_Global_Reporting.Controllers;
using API_PQ_Global_Reporting.CorrelationModule.Data;
using API_PQ_Global_Reporting.Data;
using API_PQ_Global_Reporting.Utils;
using Microsoft.AspNetCore.Mvc;

namespace API_PQ_Global_Reporting.CorrelationModule.Controllers
{
    /// <summary>Correlation Module - Correlation Report (trend data of all mappings of a parameter).</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MM_CR_ReportController : BaseController
    {
        private const int MaxRangeDays = 366;

        private readonly MM_CR_ReportDataService _reportDataService;

        public MM_CR_ReportController(
            MM_CR_ReportDataService reportDataService,
            IApiResponseHelper responseHelper,
            MM_ErrorLogDataService errorLogService)
            : base(responseHelper, errorLogService)
        {
            _reportDataService = reportDataService;
        }

        [HttpGet("GetReport")]
        public async Task<IActionResult> GetReport([FromQuery] int crParameterId, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            try
            {
                if (crParameterId <= 0)
                {
                    return BadRequest(_responseHelper.CreateErrorResponse<object>(Constants.Messages.VALIDATION_ERROR, "Parameter is required"));
                }

                if (fromDate == default || toDate == default || fromDate.Date > toDate.Date)
                {
                    return BadRequest(_responseHelper.CreateErrorResponse<object>(Constants.Messages.VALIDATION_ERROR, "Select a valid date range"));
                }

                if ((toDate.Date - fromDate.Date).TotalDays > MaxRangeDays)
                {
                    return BadRequest(_responseHelper.CreateErrorResponse<object>(Constants.Messages.VALIDATION_ERROR, "Date range cannot be more than 12 months"));
                }

                var report = await _reportDataService.GetReport(crParameterId, fromDate, toDate);
                if (report == null)
                {
                    return NotFound(_responseHelper.CreateErrorResponse<object>("Not Found", "Parameter not found or deleted"));
                }

                return Ok(_responseHelper.CreateSuccessResponse(report, "Success", "Report generated successfully"));
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetReport");
            }
        }
    }
}
