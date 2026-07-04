namespace APP.Services.Interfaces
{
    public interface IApiService
    {
        Task<TResponse> GetAsync<TResponse>(string url);

        Task<TResponse> GetAsync<TRequest, TResponse>(string url, TRequest data);

        Task<TResponse> PostAsync<TRequest, TResponse>(
            string url,
            TRequest data);

        Task<T> PostAsync<T>(string url, object data);

        Task<T> PutAsync<T>(string url, object data);

        Task<TResponse> PutAsync<TRequest, TResponse>(
            string url,
            TRequest data);

        Task<bool> DeleteAsync(string url);
    }
}
