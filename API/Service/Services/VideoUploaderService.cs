using Microsoft.Extensions.DependencyInjection;

namespace Service.Services
{
    public interface IVideoUploaderService
    {
        Task<StartMultiPartResponse> StartMultiPart(StartMultiPartRequest request);
        PreSignedPartResponse PreSignedPart(string key, PreSignedPartRequest request);
        Task CompleteMultiPart(string key, CompleteMultiPartRequest request);
    }

    public class VideoUploaderService : IVideoUploaderService
    {
        private readonly IS3Service _s3Service;

        public VideoUploaderService(IS3Service s3Service) { _s3Service = s3Service; }

        public async Task<StartMultiPartResponse> StartMultiPart(StartMultiPartRequest request)
        {
            var key = Guid.NewGuid().ToString();
            var uploadId = await _s3Service.StartMultiPart(key, request.FileName);

            return new StartMultiPartResponse(key, uploadId);
        }

        public PreSignedPartResponse PreSignedPart(string key, PreSignedPartRequest request)
        {
            var url = _s3Service.PreSignedPart(key, request.UploadId, request.PartNumber);
            return new PreSignedPartResponse(url);
        }

        public async Task CompleteMultiPart(string key, CompleteMultiPartRequest request)
        {
            await _s3Service.CompleteMultiPart(key, request.UploadId, request.Parts);
        }
    }

    public static class VideoUploaderServiceExtensions
    {
        public static void AddVideoUploaderService(this IServiceCollection services)
        {
            services.AddSingleton<IVideoUploaderService, VideoUploaderService>();
        }
    }
}
