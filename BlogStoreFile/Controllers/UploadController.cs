using Microsoft.AspNetCore.Mvc;
using BlogStoreFile.Services;

namespace BlogStoreFile.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {

        private readonly UploadServices _blobZipUploader;

        public UploadController(UploadServices blobZipUploader)
        {
            _blobZipUploader = blobZipUploader;
        }

        [HttpPost("upload-zip")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadZipAsync(IFormFile zipFile)
        {
            if (zipFile == null || zipFile.Length == 0)
            {
                return BadRequest("No se ha seleccionado un archivo ZIP.");
            }

            using (var stream = zipFile.OpenReadStream())
            {
                var (summaryMessage, processedCount, failedCount) = await _blobZipUploader.UploadZipContentsAsync(stream);

                var result = new
                {
                    message = summaryMessage,  
                    processedFiles = processedCount,
                    failedFiles = failedCount
                };

              
                return Ok(result);
            }
        }
    }
}

