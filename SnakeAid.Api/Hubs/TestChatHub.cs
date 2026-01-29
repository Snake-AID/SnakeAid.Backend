using Microsoft.AspNetCore.SignalR;

namespace SnakeAid.Api.Hubs;

public class TestChatHub : Hub
{
    private static readonly Dictionary<string, string> ConnectedUsers = new();
    private static readonly Dictionary<string, UserLocationData> UserLocations = new();

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        Console.WriteLine($"Client connected: {Context.ConnectionId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Remove user from connected users when they disconnect
        if (ConnectedUsers.ContainsValue(Context.ConnectionId))
        {
            var userToRemove = ConnectedUsers.FirstOrDefault(x => x.Value == Context.ConnectionId);
            if (!string.IsNullOrEmpty(userToRemove.Key))
            {
                ConnectedUsers.Remove(userToRemove.Key);
                UserLocations.Remove(userToRemove.Key); // Remove location data

                // Notify others that user left
                await Clients.Others.SendAsync("UserLeft", userToRemove.Key);
                await Clients.Others.SendAsync("UpdateUserList", ConnectedUsers.Keys.ToList());
            }
        }

        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    // Client joins with their UUID
    public async Task JoinChat(string userId, string userName)
    {
        try
        {
            // Store user mapping
            ConnectedUsers[userId] = Context.ConnectionId;

            // Join a group for easier management (optional)
            await Groups.AddToGroupAsync(Context.ConnectionId, "TestChatGroup");

            // Notify all clients about new user
            await Clients.Others.SendAsync("UserJoined", userId, userName);

            // Send current user list to the new user
            await Clients.Caller.SendAsync("UpdateUserList", ConnectedUsers.Keys.ToList());

            // Send latest locations of other users to the new user
            await SendLatestLocationsToUser(userId);

            // Send success confirmation to the joining user
            await Clients.Caller.SendAsync("JoinedSuccessfully", userId);

            Console.WriteLine($"User {userName} ({userId}) joined the chat");
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to join chat: {ex.Message}");
        }
    }

    // Send message to all connected clients
    public async Task SendMessageToAll(string userId, string userName, string message)
    {
        try
        {
            var messageData = new
            {
                UserId = userId,
                UserName = userName,
                Message = message,
                Timestamp = DateTime.UtcNow,
                MessageId = Guid.NewGuid().ToString()
            };

            // Send to all clients in the test chat group
            await Clients.Group("TestChatGroup").SendAsync("ReceiveMessage", messageData);

            Console.WriteLine($"Message from {userName}: {message}");
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to send message: {ex.Message}");
        }
    }

    // Send private message to specific user
    public async Task SendPrivateMessage(string fromUserId, string fromUserName, string toUserId, string message)
    {
        try
        {
            if (ConnectedUsers.TryGetValue(toUserId, out var toConnectionId))
            {
                var messageData = new
                {
                    FromUserId = fromUserId,
                    FromUserName = fromUserName,
                    ToUserId = toUserId,
                    Message = message,
                    Timestamp = DateTime.UtcNow,
                    MessageId = Guid.NewGuid().ToString(),
                    IsPrivate = true
                };

                // Send to specific user
                await Clients.Client(toConnectionId).SendAsync("ReceivePrivateMessage", messageData);

                // Send confirmation to sender
                await Clients.Caller.SendAsync("MessageSent", messageData);

                Console.WriteLine($"Private message from {fromUserName} to {toUserId}: {message}");
            }
            else
            {
                await Clients.Caller.SendAsync("Error", $"User {toUserId} not found or not connected");
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to send private message: {ex.Message}");
        }
    }

    // Get list of connected users
    public async Task GetConnectedUsers()
    {
        try
        {
            await Clients.Caller.SendAsync("UpdateUserList", ConnectedUsers.Keys.ToList());
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to get user list: {ex.Message}");
        }
    }

    // Test ping method
    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
    }

    // Typing indicator
    public async Task UserTyping(string userId, string userName, bool isTyping)
    {
        try
        {
            await Clients.Others.SendAsync("UserTyping", userId, userName, isTyping);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to send typing indicator: {ex.Message}");
        }
    }

    // Location sharing methods
    public async Task UpdateLocation(string userId, string userName, double latitude, double longitude)
    {
        try
        {
            var locationData = new UserLocationData
            {
                UserId = userId,
                UserName = userName,
                Latitude = latitude,
                Longitude = longitude,
                Timestamp = DateTime.UtcNow
            };

            // Update stored location
            UserLocations[userId] = locationData;

            // Broadcast location to all other users
            await Clients.All.SendAsync("LocationUpdated", locationData);

            Console.WriteLine($"Location updated for {userName}: {latitude}, {longitude}");
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to update location: {ex.Message}");
        }
    }

    public async Task GetAllLocations()
    {
        try
        {
            await Clients.Caller.SendAsync("AllLocations", UserLocations.Values.ToList());
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to get locations: {ex.Message}");
        }
    }

    public async Task RequestLocationUpdate(string userId)
    {
        try
        {
            if (ConnectedUsers.TryGetValue(userId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("LocationUpdateRequested");
            }
            else
            {
                await Clients.Caller.SendAsync("Error", $"User {userId} not found");
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to request location update: {ex.Message}");
        }
    }

    // Gửi thông tin vị trí mới nhất của tất cả users
    public async Task BroadcastLatestLocations()
    {
        try
        {
            // Lọc chỉ lấy location của users đang connected
            var activeUserLocations = UserLocations
                .Where(loc => ConnectedUsers.ContainsKey(loc.Key))
                .Select(loc => loc.Value)
                .OrderByDescending(loc => loc.Timestamp)
                .ToList();

            // Gửi tới tất cả clients
            await Clients.All.SendAsync("LatestUserLocations", new
            {
                Locations = activeUserLocations,
                TotalUsers = activeUserLocations.Count,
                Timestamp = DateTime.UtcNow
            });

            Console.WriteLine($"Broadcasted latest locations for {activeUserLocations.Count} users");
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to broadcast latest locations: {ex.Message}");
        }
    }

    // Gửi vị trí mới nhất cho user cụ thể khi họ join
    public async Task SendLatestLocationsToUser(string requestingUserId)
    {
        try
        {
            // Lấy vị trí của các user khác (không bao gồm user đang request)
            var otherUserLocations = UserLocations
                .Where(loc => ConnectedUsers.ContainsKey(loc.Key) && loc.Key != requestingUserId)
                .Select(loc => loc.Value)
                .OrderByDescending(loc => loc.Timestamp)
                .ToList();

            // Gửi chỉ cho user đang request
            await Clients.Caller.SendAsync("OtherUserLocations", new
            {
                Locations = otherUserLocations,
                TotalOtherUsers = otherUserLocations.Count,
                Timestamp = DateTime.UtcNow
            });

            Console.WriteLine($"Sent latest locations of {otherUserLocations.Count} other users to {requestingUserId}");
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Failed to send latest locations: {ex.Message}");
        }
    }
}

public class UserLocationData
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
}