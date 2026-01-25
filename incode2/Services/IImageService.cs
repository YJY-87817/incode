namespace incode2.Services
{
    public interface IImageService
    {
        Task<string> UploadImageAsync(IFormFile file);
        Task<List<string>> UploadImagesAsync(IEnumerable<IFormFile> files);
    }
}
