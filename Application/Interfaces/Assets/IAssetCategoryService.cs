using Application.DTOs.Assets;

namespace Application.Interfaces.Assets
{
    public interface IAssetCategoryService
    {
        Task<List<AssetCategoryListDto>> GetAllAsync();
        Task<AssetCategoryDto> GetByIdAsync(string id);
        Task<string> CreateAsync(AssetCategoryDto dto);
        Task<string> UpdateAsync(string id, AssetCategoryDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
