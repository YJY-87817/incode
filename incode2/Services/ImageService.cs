namespace incode2.Services
{
  public class ImageService : IImageService
  {
    // [可自訂]：設定圖片儲存的資料夾名稱，建議放在 wwwroot 之下以便網頁存取
    private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img");

    public ImageService()
    {
      // 作用：檢查資料夾是否存在，不存在就建立，避免寫入失敗
      if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
    }

    public async Task<List<string>> UploadImagesAsync(IEnumerable<IFormFile> files)
    {
      if (files == null) throw new Exception("未提供檔案");

      var urls = new List<string>();
      foreach (var file in files)
      {
        if (file == null || file.Length == 0) continue;

        // 作用：基本的防呆檢查，確保有接收到檔案
        if (file.Length == 0) throw new Exception("檔案無效");

        // [可自訂]：產生新檔名。Guid 確保檔名不重複；Path.GetExtension 取得原始副檔名 (如 .jpg)
        // 建議不要用原始檔名，避免安全性問題（如檔名包含路徑符號）或檔名重複被覆蓋
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        // 作用：合成完整的伺服器實體路徑
        var filePath = Path.Combine(_storagePath, fileName);

        // 作用：開啟檔案串流，並將檔案寫入硬碟
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
          await file.CopyToAsync(stream);
        }

        // [可自訂]：回傳給前端的 URL 路徑。這裡回傳的是網址路徑，不是硬碟路徑
        urls.Add($"/img/{fileName}");
      }

      if (urls.Count == 0) throw new Exception("沒有有效的檔案");
      return urls;
    }

    public async Task DeleteImageAsync(string fileNameOrUrl)
    {
      if (string.IsNullOrWhiteSpace(fileNameOrUrl)) throw new Exception("檔名無效");

      // 僅保留檔名避免路徑穿越
      var fileName = Path.GetFileName(fileNameOrUrl);
      var filePath = Path.Combine(_storagePath, fileName);

      if (!File.Exists(filePath)) throw new FileNotFoundException("檔案不存在", fileName);

      File.Delete(filePath);
      await Task.CompletedTask;
    }
  }

}
