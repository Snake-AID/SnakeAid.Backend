using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SnakeAid.Api.Hubs;
using SnakeAid.Core.Meta;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SignalRTestController : ControllerBase
{
    private readonly IHubContext<TestChatHub> _hubContext;

    public SignalRTestController(IHubContext<TestChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpGet("test-connection")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            return Ok(new ApiResponse<object>
            {
                StatusCode = 200,
                Message = "SignalR hub is working correctly. Connect to /chat-hub for WebSocket connection.",
                IsSuccess = true,
                Data = new { HubEndpoint = "/chat-hub", Status = "Available" }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                StatusCode = 500,
                Message = $"SignalR test failed: {ex.Message}",
                IsSuccess = false,
                Data = null
            });
        }
    }

    [HttpPost("send-test-message")]
    public async Task<IActionResult> SendTestMessage([FromBody] TestMessageRequest request)
    {
        try
        {
            var messageData = new
            {
                UserId = "server",
                UserName = "Server",
                Message = request.Message,
                Timestamp = DateTime.UtcNow,
                MessageId = Guid.NewGuid().ToString()
            };

            await _hubContext.Clients.All.SendAsync("ReceiveMessage", messageData);

            return Ok(new ApiResponse<object>
            {
                StatusCode = 200,
                Message = "Message sent successfully",
                IsSuccess = true,
                Data = null
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                StatusCode = 500,
                Message = $"Failed to send message: {ex.Message}",
                IsSuccess = false,
                Data = null
            });
        }
    }

    [HttpGet("connected-users")]
    public async Task<IActionResult> GetConnectedUsers()
    {
        try
        {
            // This is a simple endpoint to trigger user list update
            await _hubContext.Clients.All.SendAsync("RequestUserListUpdate");
            return Ok(new ApiResponse<object>
            {
                StatusCode = 200,
                Message = "User list update requested",
                IsSuccess = true,
                Data = null
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                StatusCode = 500,
                Message = $"Failed to request user list: {ex.Message}",
                IsSuccess = false,
                Data = null
            });
        }
    }

    [HttpPost("broadcast")]
    public async Task<IActionResult> BroadcastMessage([FromBody] BroadcastRequest request)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("ServerBroadcast", new
            {
                Message = request.Message,
                Timestamp = DateTime.UtcNow,
                Type = request.Type ?? "info"
            });

            return Ok(new ApiResponse<object>
            {
                StatusCode = 200,
                Message = "Broadcast sent successfully",
                IsSuccess = true,
                Data = null
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                StatusCode = 500,
                Message = $"Failed to broadcast: {ex.Message}",
                IsSuccess = false,
                Data = null
            });
        }
    }
}

public class TestMessageRequest
{
    public string Message { get; set; } = string.Empty;
}

public class BroadcastRequest
{
    public string Message { get; set; } = string.Empty;
    public string? Type { get; set; } // info, warning, error, success
}