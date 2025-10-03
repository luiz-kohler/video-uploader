using Presentation;

namespace API.Interfaces
{
    public interface IObjectStorageService
    {
        Task<string> Upload(IFormFile file);
        string GeneratePreSignedUrl(string key, string fileName);
        Task<string> StartMultiPart(string key, string fileName);
        string PreSignedPart(string key, string fileName, string uploadId, int partNumber);
        Task CompleteMultiPart(string key, string uploadId, List<PartETagInfoDto> parts);
    }
}
