using Microsoft.AspNetCore.Http;

namespace SnakeAid.Core.Requests.Media;

public class UploadFileRequest
{
    public IFormFile File { get; set; } = default!;
    public string Domain { get; set; } = "files";
}

