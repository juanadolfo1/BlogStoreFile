using Microsoft.AspNetCore.Mvc;

namespace BlogStoreFile.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        // private readonly  _uploader;

        public UploadController(BlobZipUploader uploader)
        {
            _uploader = uploader;
        }

        [HttpPost("upload-zip")]
        public async Task<IActionResult> UploadZip(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            using var stream = file.OpenReadStream();
            await _uploader.UploadZipContentsAsync(stream);

            return Ok("ZIP uploaded and extracted successfully.");
        }

    }
}
