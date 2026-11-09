namespace incode.MVC.Services.Common
{
  public class ImageService : IImageService
  {
    // 論壇編輯器上傳的圖片：存到 UploadStorage（網站目錄外），網址仍是 /img/fromApi/xxx
    private readonly UploadStorage _storage;

    public ImageService(UploadStorage storage)
    {
      _storage = storage;
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
        // 回傳給前端的網址路徑
        urls.Add(await _storage.SaveAsync(UploadFolders.ForumImages, fileName, file));
      }

      return urls;
    }

    public async Task DeleteImageAsync(string fileNameOrUrl)
    {
      if (string.IsNullOrWhiteSpace(fileNameOrUrl)) throw new ArgumentException("檔名無效");

      // 僅保留檔名避免路徑穿越（UploadStorage.Delete 內處理）；新舊兩個位置都會找
      if (!_storage.Delete(UploadFolders.ForumImages, fileNameOrUrl))
        throw new FileNotFoundException("檔案不存在", Path.GetFileName(fileNameOrUrl));
      await Task.CompletedTask;
    }
  }

}
