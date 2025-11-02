using Microsoft.AspNetCore.Mvc;
using Service;
using Service.Services;

namespace API.Controllers
{
    [Route("videos")]
    public class VideoController : Controller
    {
        private readonly IVideoUploaderService _service;

        public VideoController(IVideoUploaderService videoUploaderService) { _service = videoUploaderService; }

        [HttpPost("start-multipart")]
        public async Task<ActionResult<StartMultiPartResponse>> StartMultiPart([FromBody] StartMultiPartRequest request)
        {
            var response = await _service.StartMultiPart(request);
            return Ok(response);
        }

        [HttpPost("{key}/pre-signed-part")]
        public ActionResult<PreSignedPartResponse> PreSignedPart([FromRoute] string key, [FromBody] PreSignedPartRequest request)
        {
            var response = _service.PreSignedPart(key, request);
            return Ok(response);
        }

        [HttpPost("{key}/complete-multipart")]
        public async Task<ActionResult> CompleteMultiPart([FromRoute] string key, [FromBody] CompleteMultiPartRequest request)
        {
            await _service.CompleteMultiPart(key, request);
            return Ok();
        }
    }
}
