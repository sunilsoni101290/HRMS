using Application.DTOs.Assets;

namespace Application.Interfaces.Assets
{
    public interface IAssetService
    {
        Task<List<AssetListDto>> GetAllAsync();
        Task<AssetDto> GetByIdAsync(string id);
        Task<string> CreateAsync(AssetDto dto);
        Task<string> UpdateAsync(string id, AssetDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
