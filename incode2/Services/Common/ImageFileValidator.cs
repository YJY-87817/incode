namespace incode2.Services.Common
{
    /// <summary>
    /// 圖片上傳檢查：只允許常見圖片格式，並比對檔頭 (magic bytes)，
    /// 避免把 .html / .svg / 偽裝副檔名的檔案寫進 wwwroot 造成 XSS
    /// </summary>
    public static class ImageFileValidator
    {
        public const long DefaultMaxBytes = 20L * 1024 * 1024; // 單檔 20 MB

        private static readonly Dictionary<string, Func<byte[], bool>> Signatures = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = IsJpeg,
            [".jpeg"] = IsJpeg,
            [".png"] = h => StartsWith(h, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),
            [".gif"] = h => StartsWith(h, 0x47, 0x49, 0x46, 0x38), // GIF8
            [".webp"] = h => StartsWith(h, 0x52, 0x49, 0x46, 0x46)  // RIFF....WEBP
                             && h.Length >= 12 && h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50,
        };

        public static IReadOnlyCollection<string> AllowedExtensions => Signatures.Keys;

        /// <summary>檢查失敗回傳錯誤訊息，通過回傳 null</summary>
        public static string? Validate(IFormFile? file, long maxBytes = DefaultMaxBytes)
        {
            if (file == null || file.Length == 0)
                return "檔案無效";

            if (file.Length > maxBytes)
                return $"檔案 {Path.GetFileName(file.FileName)} 超過 {maxBytes / 1024 / 1024} MB";

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !Signatures.TryGetValue(extension, out var matches))
                return $"不支援的檔案格式，僅允許 {string.Join(" / ", AllowedExtensions)}";

            var header = new byte[12];
            using (var stream = file.OpenReadStream())
            {
                var read = stream.Read(header, 0, header.Length);
                if (read < header.Length)
                    Array.Resize(ref header, read);
            }

            return matches(header) ? null : $"檔案 {Path.GetFileName(file.FileName)} 內容與副檔名不符";
        }

        /// <summary>統一使用小寫副檔名 (.jpeg → .jpg)</summary>
        public static string NormalizedExtension(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return extension == ".jpeg" ? ".jpg" : extension;
        }

        private static bool IsJpeg(byte[] h) => StartsWith(h, 0xFF, 0xD8, 0xFF);

        private static bool StartsWith(byte[] data, params byte[] prefix)
            => data.Length >= prefix.Length && data.AsSpan(0, prefix.Length).SequenceEqual(prefix);
    }
}
