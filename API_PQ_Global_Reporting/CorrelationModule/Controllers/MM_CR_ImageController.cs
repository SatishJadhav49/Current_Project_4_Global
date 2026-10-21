using API_PQ_Global_Reporting.Controllers;
using API_PQ_Global_Reporting.CorrelationModule.Data;
using API_PQ_Global_Reporting.Data;
using API_PQ_Global_Reporting.Utils;
using Microsoft.AspNetCore.Mvc;

namespace API_PQ_Global_Reporting.CorrelationModule.Controllers
{
    /// <summary>
    /// Correlation Module - images of mapped items, served as image files so the
    /// UI can use them directly in &lt;img src&gt; (lazy loading, browser cache).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MM_CR_ImageController : BaseController
    {
        private const int CacheSeconds = 600;

        private readonly MM_CR_ImageDataService _imageDataService;

        public MM_CR_ImageController(
            MM_CR_ImageDataService imageDataService,
            IApiResponseHelper responseHelper,
            MM_ErrorLogDataService errorLogService)
            : base(responseHelper, errorLogService)
        {
            _imageDataService = imageDataService;
        }

        /// <summary>Image file of a mapped leaf; 404 when the item has no image.</summary>
        [HttpGet("GetImage")]
        public async Task<IActionResult> GetImage([FromQuery] int appId, [FromQuery] decimal leafId)
        {
            try
            {
                if (appId <= 0 || leafId <= 0)
                {
                    return BadRequest(_responseHelper.CreateErrorResponse<object>(Constants.Messages.VALIDATION_ERROR, "Application and item are required"));
                }

                var image = await _imageDataService.GetImage(appId, leafId);
                if (image == null)
                {
                    return NotFound();
                }

                Response.Headers.CacheControl = $"private, max-age={CacheSeconds}";
                // Content comes from other databases - never let it run as a document (e.g. scripted SVG)
                Response.Headers["X-Content-Type-Options"] = "nosniff";
                Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; sandbox";
                return File(image.Content, image.ContentType);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, "GetImage");
            }
        }
    }
}
