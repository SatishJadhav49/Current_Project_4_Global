using System.Reflection.Metadata;
using API_PQ_Global_Reporting.Data;
using API_PQ_Global_Reporting.Models.DTOs;
using API_PQ_Global_Reporting.Utils;
using DocumentFormat.OpenXml.Office2010.Word;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;


namespace API_PQ_Global_Reporting.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MM_Documents_MasterController : BaseController
    {
        private readonly MM_Documents_MasterDataService _documentsMasterDataService;

        public MM_Documents_MasterController(
            MM_Documents_MasterDataService documentsMasterDataService,
            IApiResponseHelper responseHelper,
            MM_ErrorLogDataService logerrorService
        ) : base(responseHelper, logerrorService)
        {
            _documentsMasterDataService = documentsMasterDataService;
        }

        [HttpPost("UploadDocumentFile")]
        public async Task<IActionResult> UploadDocumentFile([FromForm] IFormFile File, [FromForm] DocumentsMasterCreateDtos dto)
        {
            try
            {
                string imagePath = await _documentsMasterDataService.UploadDocumentFile(File, dto);

                var response = _responseHelper.CreateSuccessResponse(imagePath, "Success", "File Uploaded successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "UploadDocumentFile");
            }
        }

        [HttpGet("GetTableData")]
        public async Task<IActionResult> GetTableData()
        {
            try
            {
                var result = await _documentsMasterDataService.GetTableDatas();
                var response = _responseHelper.CreateSuccessResponse(result, "Success", "Fetched all table data.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetTableDatas");
            }
        }

        [HttpPost("DeleteFile")]
        public async Task<IActionResult> DeleteFile([FromBody] decimal Document_ID)
        {
            try
            {
                var result = await _documentsMasterDataService.DeleteFile(Document_ID);
                var response = _responseHelper.CreateSuccessResponse(result, "Success", "Document deleted successfully.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "DeleteFile");
            }
        }

        [HttpPost("PostDownloadDocument")]
        public async Task<IActionResult> PostDownloadDocument([FromBody] DocumentRequest data)
        {
            try
            {
                var Document_Path = data.Document_Path;
                if (string.IsNullOrEmpty(Document_Path))
                {
                    var validationResponse = _responseHelper.CreateErrorResponse<object>(
                        "Validation Error",
                        "Invalid Document_Path provided"
                    );
                    return BadRequest(validationResponse);
                }


                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/Document_File/", Document_Path);

                if (!System.IO.File.Exists(filePath))
                {
                    var fileNotFoundResponse = _responseHelper.CreateErrorResponse<object>(
                        "File Not Found",
                        "The Document file could not be found on the server"
                    );
                    return NotFound(fileNotFoundResponse);
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var fileName = Document_Path;

                // Extract original filename (remove GUID prefix)
                if (fileName.Contains('_'))
                {
                    var underscoreIndex = fileName.IndexOf('_');
                    if (underscoreIndex >= 0 && underscoreIndex < fileName.Length - 1)
                    {
                        fileName = fileName.Substring(underscoreIndex + 1); // Get original filename without GUID
                    }
                }

                // Determine content type based on file extension
                string contentType = GetContentType(fileName);

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "PostDownloadDocument");
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                _ => "application/octet-stream"
            };
        }
        public class DocumentRequest
        {
            public string Document_Path { get; set; }
        }

    }
}