using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("videos")]
    public class VideoController : Controller
    {
        private readonly IObjectStorageService _objectStorageService;

        public VideoController(IObjectStorageService objectStorageService)
        {
            _objectStorageService = objectStorageService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File must be informed");

            if (!string.Equals(file.ContentType, "video/mp4", StringComparison.OrdinalIgnoreCase))
                return BadRequest("File must be .mp4");

            try
            {
                var fileId = await _objectStorageService.Upload(file);
                return Accepted(new { fileId });
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpPut("pre-signed-url")]
        public IActionResult PreSign()
        {
            try
            {
                var key = Guid.NewGuid().ToString();    
                var url = _objectStorageService.PreSignedUrl(key);

                return Ok(new { key, url });
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
    }
}
