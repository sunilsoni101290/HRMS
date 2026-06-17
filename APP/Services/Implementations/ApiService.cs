using APP.Helpers;
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
        public async Task<TResponse> GetAsync<TResponse>(string url)
        {
            AddAuthorizationHeader();

            var response = await _httpClient.GetAsync(url);

            return await HandleResponse<TResponse>(response);
        }
        #endregion

        #region POST
        public async Task<TResponse> PostAsync<TRequest, TResponse>(string url,TRequest data)
        {
            AddAuthorizationHeader();

            var jsonData = JsonConvert.SerializeObject(data);

            var content = new StringContent(
                jsonData,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                url,
                content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                throw new ApiException(
                "API Error",
                (int)response.StatusCode,
                errorContent);
            }

            return await HandleResponse<TResponse>(response);
        }
        
        public async Task<T> PostAsync<T>(string url,object data)
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

                //Read Response
               var responseContent =
                   await response.Content.ReadAsStringAsync();

                //====================================
                //SUCCESS
                //====================================

               if (response.IsSuccessStatusCode)
               {
                   return await HandleResponse<T>(response);
               }

                //====================================
                //ERROR
                //====================================

               var errorResponse =
                   JsonConvert.DeserializeObject<
                       ApiResponse<object>>(responseContent);

               throw new Exception(errorResponse.Message);
           }
        #endregion

        #region PUT
        public async Task<TResponse> PutAsync<TRequest, TResponse>(string url,TRequest data)
        {
            AddAuthorizationHeader();

            var jsonData = JsonConvert.SerializeObject(data);

            var content = new StringContent(
                jsonData,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PutAsync(
                url,
                content);

            return await HandleResponse<TResponse>(response);
        }
        
        public async Task<T> PutAsync<T>(string url,object data)
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

            var response = await _httpClient.DeleteAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();

            throw new ApiException(
                "Delete request failed",
                (int)response.StatusCode,
                errorContent);
        }

        #endregion

        #region HANDLE RESPONSE
        private async Task<T> HandleResponse<T>(HttpResponseMessage response)
        {
            string json = await response.Content.ReadAsStringAsync();

            // Success
            if (response.IsSuccessStatusCode)
            {
                if (string.IsNullOrWhiteSpace(json))
                    return default;

                try
                {
                    if (typeof(T) == typeof(string))
                        return (T)(object)json;

                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch (JsonException ex)
                {
                    throw new Exception(
                        $"Failed to deserialize API response to '{typeof(T).Name}'.\n" +
                        $"Response: {json}\n" +
                        $"Error: {ex.Message}");
                }
            }

            // Unauthorized
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                bool refreshed = await RefreshTokenAsync();

                if (!refreshed)
                {
                    throw new UnauthorizedAccessException(
                        "Session expired. Please login again.");
                }

                throw new UnauthorizedAccessException(
                    "Access token refreshed. Please retry the request.");
            }

            // Bad Request
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new ApplicationException(
                    $"Bad Request (400): {json}");
            }

            // Forbidden
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException(
                    $"Access Denied (403): {json}");
            }

            // Not Found
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new KeyNotFoundException(
                    $"Resource Not Found (404): {json}");
            }

            // Internal Server Error
            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                throw new Exception(
                    $"Server Error (500): {json}");
            }

            // Other Errors
            throw new Exception(
                $"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}\n{json}");
        }

        //private async Task<T> HandleResponse<T>(HttpResponseMessage response)
        //{
        //    var json = await response.Content.ReadAsStringAsync();

        //    // Unauthorized
        //    if (response.StatusCode == HttpStatusCode.Unauthorized)
        //    {
        //        bool refreshed = await RefreshTokenAsync();

        //        if (!refreshed)
        //        {
        //            throw new Exception(
        //                "Session expired. Please login again.");
        //        }

        //        throw new Exception(
        //            "Token refreshed. Retry request.");
        //    }

        //    // Other Errors
        //    if (!response.IsSuccessStatusCode)
        //    {
        //        throw new Exception(json);
        //    }

        //    // Empty Response
        //    if (string.IsNullOrWhiteSpace(json))
        //    {
        //        return default(T);
        //    }

        //    try
        //    {
        //        return JsonConvert.DeserializeObject<T>(json);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(
        //            $"JSON Deserialize Error\n" +
        //            $"Type: {typeof(T).Name}\n" +
        //            $"JSON: {json}\n" +
        //            $"Message: {ex.Message}");
        //    }
        //}

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
}
