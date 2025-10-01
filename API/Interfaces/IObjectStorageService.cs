namespace API.Interfaces
{
    public interface IObjectStorageService
    {
        Task<string> Upload(IFormFile file);
        string PreSignedUrl(string key);
    }
}
