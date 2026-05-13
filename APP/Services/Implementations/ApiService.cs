using APP.Models.Auth;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;

namespace APP.Services.Implementations
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;

        private readonly IHttpContextAccessor
            _httpContextAccessor;

        public ApiService(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;

            _httpContextAccessor = httpContextAccessor;
        }

        #region COMMON METHOD

        private void AddAuthorizationHeader()
        {
            var token =
                _httpContextAccessor.HttpContext?
                .Session
                .GetString("AccessToken");

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        token);
            }
        }

        #endregion

        #region GET

        public async Task<T> GetAsync<T>(string url)
        {
            AddAuthorizationHeader();

            var response =
                await _httpClient.GetAsync(url);

            return await HandleResponse<T>(response);
        }

        #endregion

        #region POST

        public async Task<T> PostAsync<T>(
            string url,
            object data)
        {
            AddAuthorizationHeader();

            var jsonData =
                JsonConvert.SerializeObject(data);

            var content = new StringContent(
                jsonData,
                Encoding.UTF8,
                "application/json");

            var response =
                await _httpClient.PostAsync(
                    url,
                    content);

            return await HandleResponse<T>(response);
        }

        #endregion

        #region PUT

        public async Task<T> PutAsync<T>(
            string url,
            object data)
        {
            AddAuthorizationHeader();

            var jsonData =
                JsonConvert.SerializeObject(data);

            var content = new StringContent(
                jsonData,
                Encoding.UTF8,
                "application/json");

            var response =
                await _httpClient.PutAsync(
                    url,
                    content);

            return await HandleResponse<T>(response);
        }

        #endregion

        #region DELETE

        public async Task<bool> DeleteAsync(string url)
        {
            AddAuthorizationHeader();

            var response =
                await _httpClient.DeleteAsync(url);

            return response.IsSuccessStatusCode;
        }

        #endregion

        #region HANDLE RESPONSE

        private async Task<T> HandleResponse<T>(
            HttpResponseMessage response)
        {
            var json =
                await response.Content
                .ReadAsStringAsync();

            // Unauthorized
            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                bool refreshed =
                    await RefreshTokenAsync();

                if (!refreshed)
                {
                    throw new Exception(
                        "Session expired. Please login again.");
                }

                throw new Exception(
                    "Token refreshed. Retry request.");
            }

            // Other Errors
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(json);
            }

            return JsonConvert
                .DeserializeObject<T>(json);
        }

        #endregion

        #region REFRESH TOKEN

        private async Task<bool> RefreshTokenAsync()
        {
            try
            {
                var session =
                    _httpContextAccessor.HttpContext
                    ?.Session;

                var refreshToken =
                    session?.GetString("RefreshToken");

                if (string.IsNullOrEmpty(refreshToken))
                    return false;

                var request = new
                {
                    RefreshToken = refreshToken
                };

                var jsonData =
                    JsonConvert.SerializeObject(request);

                var content = new StringContent(
                    jsonData,
                    Encoding.UTF8,
                    "application/json");

                var response =
                    await _httpClient.PostAsync(
                        "auth/refresh-token",
                        content);

                if (!response.IsSuccessStatusCode)
                    return false;

                var json =
                    await response.Content
                    .ReadAsStringAsync();

                var authResponse =
                    JsonConvert.DeserializeObject<AuthResponse>(
                        json);

                // Save new tokens
                session.SetString(
                    "AccessToken",
                    authResponse.AccessToken);

                session.SetString(
                    "RefreshToken",
                    authResponse.RefreshToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
    //public class ApiService : IApiService
    //{
    //    private readonly HttpClient _httpClient;

    //    public ApiService(HttpClient httpClient)
    //    {
    //        _httpClient = httpClient;
    //    }

    //    #region GET

    //    public async Task<T> GetAsync<T>(string url)
    //    {
    //        var response = await _httpClient.GetAsync(url);

    //        response.EnsureSuccessStatusCode();

    //        var json = await response.Content.ReadAsStringAsync();

    //        return JsonConvert.DeserializeObject<T>(json);
    //    }

    //    #endregion

    //    #region POST

    //    public async Task<T> PostAsync<T>(string url, object data)
    //    {
    //        var jsonData = JsonConvert.SerializeObject(data);

    //        var content = new StringContent(
    //            jsonData,
    //            Encoding.UTF8,
    //            "application/json");

    //        var response = await _httpClient.PostAsync(url, content);

    //        response.EnsureSuccessStatusCode();

    //        var json = await response.Content.ReadAsStringAsync();

    //        return JsonConvert.DeserializeObject<T>(json);
    //    }

    //    #endregion

    //    #region PUT

    //    public async Task<T> PutAsync<T>(string url, object data)
    //    {
    //        var jsonData = JsonConvert.SerializeObject(data);

    //        var content = new StringContent(
    //            jsonData,
    //            Encoding.UTF8,
    //            "application/json");

    //        var response = await _httpClient.PutAsync(url, content);

    //        response.EnsureSuccessStatusCode();

    //        var json = await response.Content.ReadAsStringAsync();

    //        return JsonConvert.DeserializeObject<T>(json);
    //    }

    //    #endregion

    //    #region DELETE

    //    public async Task<bool> DeleteAsync(string url)
    //    {
    //        var response = await _httpClient.DeleteAsync(url);

    //        return response.IsSuccessStatusCode;
    //    }

    //    #endregion
    //}
}
