namespace APP.Services.Interfaces
{
    public interface IApiService
    {
        Task<T> GetAsync<T>(string url);

        Task<T> PostAsync<T>(string url, object data);

        Task<T> PutAsync<T>(string url, object data);

        Task<bool> DeleteAsync(string url);
    }
}
