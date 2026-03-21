using SnakeAid.Core.Requests.Antivenom;
using SnakeAid.Core.Responses.Antivenom;

namespace SnakeAid.Service.Interfaces;

public interface IAntivenomService
{
    Task<AntivenomResponse> CreateAntivenomAsync(CreateAntivenomRequest request);
    Task<AntivenomResponse> GetAntivenomByIdAsync(int id);
    Task<List<AntivenomResponse>> GetAllAntivenomsAsync();
    Task<AntivenomResponse> UpdateAntivenomAsync(int id, UpdateAntivenomRequest request);
    Task DeleteAntivenomAsync(int id);
}