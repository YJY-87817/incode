namespace incode2.Services
{
  public class ImageService : IImageService
  {
    // [可自訂]：設定圖片儲存的資料夾名稱，建議放在 wwwroot 之下以便網頁存取
    private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/fromApi");

    public ImageService()
    {
      // 作用：檢查資料夾是否存在，不存在就建立，避免寫入失敗
      if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
    }

    public const int MaxFilesPerUpload = 20;

    public async Task<List<string>> UploadImagesAsync(IEnumerable<IFormFile> files)
    {
      var list = files?.Where(f => f != null && f.Length > 0).ToList();
      if (list == null || list.Count == 0) throw new ArgumentException("沒有有效的檔案");
      if (list.Count > MaxFilesPerUpload) throw new ArgumentException($"一次最多上傳 {MaxFilesPerUpload} 個檔案");

      // 先全部檢查通過才寫入，避免部分檔案已存入
      foreach (var file in list)
      {
        var error = ImageFileValidator.Validate(file);
        if (error != null) throw new ArgumentException(error);
      }

      var urls = new List<string>();
      foreach (var file in list)
      {
        // 不使用原始檔名 (避免路徑符號或覆蓋)，Guid + 正規化後的副檔名
        var fileName = $"{Guid.NewGuid()}{ImageFileValidator.NormalizedExtension(file)}";
        var filePath = Path.Combine(_storagePath, fileName);

        using (var stream = new FileStream(filePath, FileMode.CreateNew))
        {
          await file.CopyToAsync(stream);
        }

        // 回傳給前端的網址路徑
        urls.Add($"/img/fromApi/{fileName}");
      }

      return urls;
    }

    public async Task DeleteImageAsync(string fileNameOrUrl)
    {
      if (string.IsNullOrWhiteSpace(fileNameOrUrl)) throw new ArgumentException("檔名無效");

      // 僅保留檔名避免路徑穿越
      var fileName = Path.GetFileName(fileNameOrUrl);
      var filePath = Path.Combine(_storagePath, fileName);

      if (!File.Exists(filePath)) throw new FileNotFoundException("檔案不存在", fileName);

      File.Delete(filePath);
      await Task.CompletedTask;
    }
  }

}
