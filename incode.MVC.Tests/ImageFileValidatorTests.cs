using Microsoft.AspNetCore.Http;

namespace incode.MVC.Tests;

public class ImageFileValidatorTests
{
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };
    private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0 };
    private static readonly byte[] Gif = { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0, 0, 0, 0, 0, 0 };
    private static readonly byte[] Webp = { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50 };

    private static IFormFile File(string name, byte[] bytes)
        => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name);

    [Theory]
    [InlineData("a.png")]
    [InlineData("A.PNG")]
    public void Valid_png_passes(string name) => Assert.Null(ImageFileValidator.Validate(File(name, Png)));

    [Fact]
    public void Valid_jpeg_gif_webp_pass()
    {
        Assert.Null(ImageFileValidator.Validate(File("a.jpg", Jpeg)));
        Assert.Null(ImageFileValidator.Validate(File("a.jpeg", Jpeg)));
        Assert.Null(ImageFileValidator.Validate(File("a.gif", Gif)));
        Assert.Null(ImageFileValidator.Validate(File("a.webp", Webp)));
    }

    [Fact]
    public void Content_must_match_extension()
        => Assert.NotNull(ImageFileValidator.Validate(File("a.png", Jpeg)));

    [Fact]
    public void Riff_without_webp_marker_is_rejected()
    {
        var wave = (byte[])Webp.Clone();
        wave[8] = 0x41; // "AVE?" 之類的 RIFF 容器
        Assert.NotNull(ImageFileValidator.Validate(File("a.webp", wave)));
    }

    [Theory]
    [InlineData("a.html")]
    [InlineData("a.svg")]
    [InlineData("a")]
    [InlineData("a.png.exe")]
    public void Disallowed_extensions_are_rejected(string name)
        => Assert.NotNull(ImageFileValidator.Validate(File(name, Png)));

    [Fact]
    public void Null_empty_and_oversize_files_are_rejected()
    {
        Assert.NotNull(ImageFileValidator.Validate(null));
        Assert.NotNull(ImageFileValidator.Validate(File("a.png", Array.Empty<byte>())));
        Assert.NotNull(ImageFileValidator.Validate(File("a.png", Png), maxBytes: 5));
    }

    [Theory]
    [InlineData("a.JPEG", ".jpg")]
    [InlineData("a.Png", ".png")]
    public void Extension_is_normalized(string name, string expected)
        => Assert.Equal(expected, ImageFileValidator.NormalizedExtension(File(name, Png)));
}
