using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pinetree.Constants;
using Pinetree.Services;

namespace Pinetree.Components.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController(BlobStorageService blobStorageService, ILogger<ImagesController> logger) : ControllerBase
{
    [HttpPost("upload-from-indexeddb")]
    [Authorize]
    public async Task<IActionResult> UploadFromIndexedDB(string imageId, string extension, [FromBody] string base64Image)
    {
        try
        {
            if (string.IsNullOrEmpty(base64Image))
            {
                return BadRequest(new { Error = "画像データが含まれていません" });
            }

            // 拡張子のチェック（.が含まれていない場合は追加）
            var fileExtension = extension.StartsWith(".") ? extension : $".{extension}";
            if (!ImageConstants.IsAllowedExtension(fileExtension))
            {
                return BadRequest(new { Error = $"対応していないファイル形式です。対応形式: {string.Join(", ", ImageConstants.AllowedExtensions)}" });
            }

            // Base64文字列をバイトに変換
            byte[] imageBytes = Convert.FromBase64String(base64Image);

            // サイズチェック
            if (imageBytes.Length > ImageConstants.MaxFileSizeBytes)
            {
                return BadRequest(new { Error = $"ファイルサイズが大きすぎます。{ImageConstants.MaxFileSizeMB}MB以下にしてください" });
            }

            // メモリストリームを作成
            using var stream = new MemoryStream(imageBytes);

            // ファイル名を生成
            var fileName = $"{imageId}{fileExtension}";

            // Azure Blob Storageにアップロード
            var blobUrl = await blobStorageService.UploadImageAsync(stream, fileName);

            logger.LogInformation("Image uploaded from IndexedDB: {BlobUrl}", blobUrl);

            return Ok(new { Url = blobUrl });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading image from IndexedDB");
            return StatusCode(500, new { Error = "画像のアップロード中にエラーが発生しました" });
        }
    }

}
