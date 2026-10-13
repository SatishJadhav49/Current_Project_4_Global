using API_PQ_Global_Reporting.Controllers;
using API_PQ_Global_Reporting.CorrelationModule.Data;
using API_PQ_Global_Reporting.CorrelationModule.Models;
using API_PQ_Global_Reporting.Data;
using API_PQ_Global_Reporting.Utils;
using Microsoft.AspNetCore.Mvc;

namespace API_PQ_Global_Reporting.CorrelationModule.Controllers
{
    /// <summary>
    /// Correlation Module - Parameter Mapping Master.
    /// Save / delete endpoints always return 200 with an MM_CR_ResultDto; the UI
    /// checks Is_Success and shows Message (duplicate name, not found, ...).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MM_CR_MappingController : BaseController
    {
        private readonly MM_CR_MappingDataService _mappingDataService;

        public MM_CR_MappingController(
            MM_CR_MappingDataService mappingDataService,
            IApiResponseHelper responseHelper,
            MM_ErrorLogDataService errorLogService)
            : base(responseHelper, errorLogService)
        {
            _mappingDataService = mappingDataService;
        }

        [HttpGet("GetModels")]
        public async Task<IActionResult> GetModels()
        {
            try
            {
                var models = await _mappingDataService.GetModels();
                return Ok(_responseHelper.CreateSuccessResponse(models, "Success", "Models retrieved successfully"));
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetModels");
            }
        }

        [HttpGet("GetApplications")]
        public async Task<IActionResult> GetApplications()
        {
            try
            {
                var applications = await _mappingDataService.GetApplications();
                return Ok(_responseHelper.CreateSuccessResponse(applications, "Success", "Applications retrieved successfully"));
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetApplications");
            }
        }

        [HttpGet("GetParameters")]
        public async Task<IActionResult> GetParameters([FromQuery] decimal modelId)
        {
            try
            {
                if (modelId <= 0)
                {
                    return BadRequest(_responseHelper.CreateErrorResponse<object>(Constants.Messages.VALIDATION_ERROR, "Model is required"));
                }

                var parameters = await _mappingDataService.GetParameters(modelId);
                return Ok(_responseHelper.CreateSuccessResponse(parameters, "Success", "Parameters retrieved successfully"));
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetParameters");
            }
        }

        [HttpPost("SaveParameter")]
        public async Task<IActionResult> SaveParameter([FromBody] MM_CR_ParameterSaveDto parameter)
        {
            try
            {
                if (parameter == null || parameter.Model_ID <= 0 || string.IsNullOrWhiteSpace(parameter.Parameter_Name))
                {
                    return BadRequest(_responseHelper.CreateErrorResponse<object>(Constants.Messages.VALIDATION_ERROR, "Model and parameter name are required"));
                }

                if (parameter.Parameter_Name.Trim().Length > 250)
                {
                    return BadRequest(_responseHelper.CreateErrorResponse<object>(Constants.Messages.VALIDATION_ERROR, "Parameter name cannot exceed 250 characters"));
                }

                var result = await _mappingDataService.SaveParameter(parameter);
                return Ok(_responseHelper.CreateSuccessResponse(result, "Success", result.Message ?? string.Empty));
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "SaveParameter");
            }
        }

        [HttpPost("DeleteParameter")]
        public async Task<IActionResult> DeleteParameter([FromBody] MM_CR_ParameterDeleteDto parameter)
        {
            try
            {
                if (parameter == null || parameter.CR_Parameter_ID <= 0)
                {
                    return BadRequest(_responseHelper.CreateErrorResponse<object>(Constants.Messages.VALIDATION_ERROR, "Parameter is required"));
                }

                var result = await _mappingDataService.DeleteParameter(parameter);
                return Ok(_responseHelper.CreateSuccessResponse(result, "Success", result.Message ?? string.Empty));
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "DeleteParameter");
            }
        }

        [HttpGet("GetParameterMappings/{crParameterId}")]
        public async Task<IActionResult> GetParameterMappings(int crParameterId)
        {
            try
            {
                var mappings = await _mappingDataService.GetParameterMappings(crParameterId);
                if (mappings.Parameter == null)
                {
                    return NotFound(_responseHelper.CreateErrorResponse<object>("Not Found", "Parameter not found or deleted"));
                }

                return Ok(_responseHelper.CreateSuccessResponse(mappings, "Success", "Mappings retrieved successfully"));
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetParameterMappings");
            }
        }

        [HttpGet("GetMasterData")]
        public async Task<IActionResult> GetMasterData([FromQuery] int appId, [FromQuery] int crParameterId)
        {
            try
            {
                if (appId <= 0 || crParameterId <= 0)
                {
                    return BadRequest(_responseHelper.CreateErrorResponse<object>(Constants.Messages.VALIDATION_ERROR, "Application and parameter are required"));
                }

                var masterData = await _mappingDataService.GetMasterData(appId, crParameterId);
                if (masterData == null)
                {
                    return NotFound(_responseHelper.CreateErrorResponse<object>("Not Found", "Parameter, model or application master procedure not found"));
                }

                return Ok(_responseHelper.CreateSuccessResponse(masterData, "Success", "Master data retrieved successfully"));
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetMasterData");
            }
        }

        [HttpPost("SaveMappings")]
        public async Task<IActionResult> SaveMappings([FromBody] MM_CR_MappingSaveDto mapping)
        {
            try
            {
                if (mapping == null || mapping.CR_Parameter_ID <= 0 || mapping.App_ID <= 0 || mapping.Items.Count == 0)
                {
                    return BadRequest(_responseHelper.CreateErrorResponse<object>(Constants.Messages.VALIDATION_ERROR, "Select at least one row to map"));
                }

                var result = await _mappingDataService.SaveMappings(mapping);
                return Ok(_responseHelper.CreateSuccessResponse(result, "Success", result.Message ?? string.Empty));
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "SaveMappings");
            }
        }

        [HttpPost("Unmap")]
        public async Task<IActionResult> Unmap([FromBody] MM_CR_UnmapDto unmap)
        {
            try
            {
                if (unmap == null || unmap.Mapping_IDs.Count == 0)
                {
                    return BadRequest(_responseHelper.CreateErrorResponse<object>(Constants.Messages.VALIDATION_ERROR, "Select at least one mapping to unmap"));
                }

                var result = await _mappingDataService.Unmap(unmap);
                return Ok(_responseHelper.CreateSuccessResponse(result, "Success", result.Message ?? string.Empty));
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "Unmap");
            }
        }
    }
}
