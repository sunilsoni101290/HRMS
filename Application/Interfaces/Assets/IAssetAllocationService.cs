using Application.DTOs.Assets;

namespace Application.Interfaces.Assets
{
    public interface IAssetAllocationService
    {
        Task<List<AssetAllocationListDto>> GetAllAsync();
        Task<List<AssetAllocationListDto>> GetByAssetIdAsync(string assetId);
        Task<AssetAllocationDto> GetByIdAsync(string id);
        Task<string> CreateAsync(AssetAllocationDto dto);
        Task<string> UpdateAsync(string id, AssetAllocationDto dto);
        Task<string> ReturnAsync(string id, AssetAllocationDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
