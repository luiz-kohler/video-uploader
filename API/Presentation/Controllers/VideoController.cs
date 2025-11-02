using Microsoft.AspNetCore.Mvc;
using Service;
using Service.Services;

namespace API.Controllers
{
    [Route("videos")]
    public class VideoController : Controller
    {
        private readonly IS3Service _s3Service;

        public VideoController(IS3Service s3Service) { _s3Service = s3Service; }

        [HttpPost("start-multipart")]
        public async Task<IActionResult> StartMultiPart([FromBody] StartMultiPartDto request)
        {
            var key = Guid.NewGuid().ToString();
            var uploadId = await _s3Service.StartMultiPart(key, request.FileName);

            return Ok(new { key, uploadId });
        }

        [HttpPost("{key}/pre-signed-part")]
        public IActionResult PreSignedPart([FromRoute] string key, [FromBody] PreSignedPartDto request)
        {
            var url = _s3Service.PreSignedPart(key, request.UploadId, request.PartNumber);
            return Ok(new { key, url });
        }

        [HttpPost("{key}/complete-multipart")]
        public async Task<IActionResult> CompleteMultiPart([FromRoute] string key, [FromBody] CompleteMultiPartDto request)
        {
            await _s3Service.CompleteMultiPart(key, request.UploadId, request.Parts);
            return Ok();
        }
    }
}
