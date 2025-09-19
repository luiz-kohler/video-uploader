using Microsoft.AspNetCore.Mvc;

namespace video_uploader_api.Controllers
{
    [Route("videos")]
    public class VideoController : Controller
    {
        [HttpPost("upload")]
        public IActionResult Upload()
        {
            return Ok();
        }

        [HttpPost("check-status")]
        public IActionResult CheckStatus()
        {
            return Ok();
        }
    }
}
