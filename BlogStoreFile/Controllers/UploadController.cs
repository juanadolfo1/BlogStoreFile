using Microsoft.AspNetCore.Mvc;
using BlogStoreFile.Services;

namespace BlogStoreFile.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {

        private readonly UploadServices _blobZipUploader;

        public UploadController( UploadServices blobZipUploader)
        {
            _blobZipUploader = blobZipUploader;
        }

        [HttpPost("upload-zip")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadZip(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            using var stream = file.OpenReadStream();
            await _blobZipUploader.UploadZipContentsAsync(stream);

            return Ok("ZIP uploaded and files extracted successfully.");
        }
    }
}

