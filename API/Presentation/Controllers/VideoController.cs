using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Presentation;

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

            var fileId = await _objectStorageService.Upload(file);
            return Accepted(new { fileId });
        }

        [HttpPost("pre-signed")]
        public IActionResult PreSigned([FromBody] PreSignedDto request)
        {
            var key = Guid.NewGuid().ToString();
            var url = _objectStorageService.GeneratePreSignedUrl(key, request.FileName);

            return Ok(new { key, url });
        }

        [HttpPost("start-multipart")]
        public async Task<IActionResult> StartMultiPart([FromRoute] StartMultiPartDto request)
        {
            var key = Guid.NewGuid().ToString();
            var uploadId = await _objectStorageService.StartMultiPart(key, request.FileName);

            return Ok(new { key, uploadId });
        }

        [HttpPost("{key}/pre-signed-part")]
        public IActionResult PreSignedPart([FromRoute] string key, [FromBody] PreSignedPartDto request)
        {
            var url = _objectStorageService.PreSignedPart(key, request.FileName, request.UploadId, request.PartNumber);
            return Ok(new { key, url });
        }

        [HttpPost("{key}/complete-multipart")]
        public async Task<IActionResult> CompleteMultiPart([FromRoute] string key, [FromBody] CompleteMultiPartDto request)
        {
            await _objectStorageService.CompleteMultiPart(key, request.UploadId, request.Parts);
            return Ok();
        }
    }
}
