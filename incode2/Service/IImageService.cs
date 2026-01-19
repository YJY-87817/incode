namespace incode2.Service
{
    public interface IImageService
    {
        Task<string> UploadImageAsync(IFormFile file);
    }
}
