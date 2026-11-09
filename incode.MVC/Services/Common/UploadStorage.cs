using Microsoft.Extensions.FileProviders;

namespace incode.MVC.Services.Common
{
    /// <summary>上傳檔案的子資料夾（同時也是網址路徑，例如 /img/chat/xxx.png）</summary>
    public static class UploadFolders
    {
        public const string ForumImages = "img/fromApi";   // 論壇編輯器上傳的圖片
        public const string ChatImages = "img/chat";       // 客服聊天圖片
        public const string Avatars = "img/user";          // 會員大頭貼
        public const string Products = "img/product";      // 商城商品圖片（後台上傳）
        public const string ContentBodies = "contents";    // 論壇文章內文 HTML
    }

    /// <summary>
    /// 會員 / 後台上傳的檔案存放處。放在網站目錄（wwwroot）外，避免重新部署覆蓋而遺失、也不會被提交進 git。
    ///
    /// - 位置：appsettings 的 App:UploadRoot（相對路徑以專案目錄 ContentRoot 為準），預設 App_Data/uploads。
    ///   正式環境請設成部署資料夾以外的絕對路徑，例如 App__UploadRoot=D:\incode-uploads 或 /var/incode/uploads
    /// - 網址不變：Program.cs 以 UseStaticFiles 把這個資料夾掛在網站根目錄，/img/chat/xxx.png 會讀 {UploadRoot}/img/chat/xxx.png
    /// - 舊檔：改版前上傳的檔案還在 wwwroot 底下（已提交進 git），仍由 wwwroot 提供；刪除時兩處都會找
    /// </summary>
    public class UploadStorage
    {
        public const string DefaultRoot = "App_Data/uploads";

        private readonly string _legacyRoot;

        public UploadStorage(IConfiguration configuration, IWebHostEnvironment environment)
        {
            var configured = configuration["App:UploadRoot"];
            Root = Path.GetFullPath(Path.Combine(environment.ContentRootPath,
                string.IsNullOrWhiteSpace(configured) ? DefaultRoot : configured));
            Directory.CreateDirectory(Root);

            _legacyRoot = string.IsNullOrWhiteSpace(environment.WebRootPath)
                ? Path.Combine(environment.ContentRootPath, "wwwroot")
                : environment.WebRootPath;
        }

        /// <summary>上傳根目錄（絕對路徑）</summary>
        public string Root { get; }

        /// <summary>提供給 UseStaticFiles 的檔案來源</summary>
        public IFileProvider CreateFileProvider() => new PhysicalFileProvider(Root);

        /// <summary>寫入檔案並回傳網址路徑（/{folder}/{fileName}）。fileName 必須由伺服器產生（例如 Guid），不可使用使用者提供的檔名</summary>
        public async Task<string> SaveAsync(string folder, string fileName, IFormFile file)
        {
            var path = PreparePath(folder, fileName);
            await using var stream = new FileStream(path, FileMode.CreateNew);
            await file.CopyToAsync(stream);
            return Url(folder, fileName);
        }

        /// <summary>寫入文字檔（UTF-8 無 BOM）並回傳網址路徑</summary>
        public async Task<string> SaveTextAsync(string folder, string fileName, string text)
        {
            var path = PreparePath(folder, fileName);
            await File.WriteAllTextAsync(path, text ?? string.Empty, new System.Text.UTF8Encoding(false));
            return Url(folder, fileName);
        }

        /// <summary>刪除檔案（只取檔名，避免路徑穿越）；上傳根目錄與舊的 wwwroot 位置都會找。回傳是否有刪到</summary>
        public bool Delete(string folder, string fileNameOrUrl)
        {
            var fileName = Path.GetFileName(fileNameOrUrl ?? string.Empty);
            if (string.IsNullOrEmpty(fileName)) return false;

            var deleted = false;
            foreach (var root in new[] { Root, _legacyRoot })
            {
                var path = Path.Combine(root, folder, fileName);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    deleted = true;
                }
            }
            return deleted;
        }

        private string PreparePath(string folder, string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || Path.GetFileName(fileName) != fileName)
                throw new ArgumentException("檔名無效", nameof(fileName));

            var directory = Path.Combine(Root, folder);
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, fileName);
        }

        private static string Url(string folder, string fileName) => $"/{folder}/{fileName}";
    }
}
