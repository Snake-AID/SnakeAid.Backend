using SnakeAid.Core.Requests.VenomType;
using SnakeAid.Core.Responses.VenomType;

namespace SnakeAid.Service.Interfaces;

public interface IVenomTypeService
{
    Task<VenomTypeResponse> CreateVenomTypeAsync(CreateVenomTypeRequest request);
    Task<VenomTypeResponse> GetVenomTypeByIdAsync(int id);
    Task<List<VenomTypeResponse>> GetAllVenomTypesAsync();
    Task<VenomTypeResponse> UpdateVenomTypeAsync(int id, UpdateVenomTypeRequest request);
    Task DeleteVenomTypeAsync(int id);
}
