using Microsoft.AspNetCore.Http;

namespace SnakeAid.Core.Requests.Media;

public class UploadImageRequest
{
    public IFormFile File { get; set; } = default!;
    public string Domain { get; set; } = "uploads";
}

