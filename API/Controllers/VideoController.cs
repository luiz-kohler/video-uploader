using API.Services;
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
                return BadRequest("Invalid file");

            var fileName = await _objectStorageService.Upload(file);

            return Accepted(new { fileName });
        }
    }
}
