namespace incode2.Services.Common
{
    public interface IImageService
    {
        Task<List<string>> UploadImagesAsync(IEnumerable<IFormFile> files);
        Task DeleteImageAsync(string fileNameOrUrl);
    }
}
