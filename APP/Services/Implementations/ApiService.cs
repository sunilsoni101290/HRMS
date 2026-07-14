using APP.Helpers;
using APP.Models.Auth;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

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

            _httpClient.DefaultRequestHeaders.Authorization =
                !string.IsNullOrEmpty(token)
                    ? new AuthenticationHeaderValue("Bearer", token)
                    : null;
        }

        private static StringContent BuildJsonContent(object data)
        {
            var jsonData = JsonConvert.SerializeObject(data);
            return new StringContent(jsonData, Encoding.UTF8, "application/json");
        }

        // Sends the request built by sendRequest and, if the API responds
        // with 401 (access token rejected - expired, revoked, or otherwise
        // invalid), silently refreshes the token ONCE via the refresh-token
        // flow and retries the SAME request with the new token attached.
        //
        // This replaces the previous behaviour where a 401 always threw an
        // exception straight back to the caller - even when the refresh
        // itself succeeded - because no controller action ever caught that
        // exception and retried. A successful silent refresh still looked
        // like a hard logout to the user (the "sometimes logged out after
        // calling an API" symptom). Now the retry happens transparently
        // and the caller only ever sees the final response.
        //
        // If the retried request also comes back 401, or the refresh call
        // itself fails, the refresh token is genuinely no longer valid
        // (expired past its window, or revoked by an explicit Logout
        // elsewhere) - HandleResponse throws UnauthorizedAccessException
        // in that case, which ApiSessionExpiredFilter turns into a clean
        // redirect to Login instead of a raw error page.
        private async Task<HttpResponseMessage> SendWithAutoRefreshAsync(
            Func<Task<HttpResponseMessage>> sendRequest)
        {
            AddAuthorizationHeader();

            var response = await sendRequest();

            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return response;

            var refreshed = await RefreshSessionTokenAsync();

            if (!refreshed)
                return response;

            AddAuthorizationHeader();

            return await sendRequest();
        }

        #endregion

        #region GET
        public async Task<TResponse> GetAsync<TResponse>(string url)
        {
            var response = await SendWithAutoRefreshAsync(() => _httpClient.GetAsync(url));

            return await HandleResponse<TResponse>(response);
        }

        public async Task<TResponse> GetAsync<TRequest, TResponse>(string url, TRequest data)
        {
            if (data != null)
            {
                var queryString = string.Join("&",
                    typeof(TRequest)
                        .GetProperties()
                        .Where(p => p.GetValue(data) != null)
                        .Select(p =>
                            $"{p.Name}={Uri.EscapeDataString(p.GetValue(data)?.ToString() ?? string.Empty)}"));

                url = $"{url}?{queryString}";
            }

            var response = await SendWithAutoRefreshAsync(() => _httpClient.GetAsync(url));

            return await HandleResponse<TResponse>(response);
        }
        #endregion

        #region POST
        public async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest data)
        {
            var response = await SendWithAutoRefreshAsync(
                () => _httpClient.PostAsync(url, BuildJsonContent(data)));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException("Session expired. Please login again.");

                throw new ApiException(
                    "API Error",
                    (int)response.StatusCode,
                    errorContent);
            }

            return await HandleResponse<TResponse>(response);
        }

        public async Task<T> PostAsync<T>(string url, object data)
        {
            var response = await SendWithAutoRefreshAsync(
                () => _httpClient.PostAsync(url, BuildJsonContent(data)));

            // Success
            if (response.IsSuccessStatusCode)
            {
                return await HandleResponse<T>(response);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Session expired. Please login again.");

            var responseContent = await response.Content.ReadAsStringAsync();

            var errorResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);

            throw new Exception(
                errorResponse?.Message ?? $"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}");
        }
        #endregion

        #region PUT
        public async Task<TResponse> PutAsync<TRequest, TResponse>(string url, TRequest data)
        {
            var response = await SendWithAutoRefreshAsync(
                () => _httpClient.PutAsync(url, BuildJsonContent(data)));

            return await HandleResponse<TResponse>(response);
        }

        public async Task<T> PutAsync<T>(string url, object data)
        {
            var response = await SendWithAutoRefreshAsync(
                () => _httpClient.PutAsync(url, BuildJsonContent(data)));

            return await HandleResponse<T>(response);
        }
        #endregion

        #region DELETE

        public async Task<bool> DeleteAsync(string url)
        {
            var response = await SendWithAutoRefreshAsync(() => _httpClient.DeleteAsync(url));

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Session expired. Please login again.");

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

            // By the time we get here, SendWithAutoRefreshAsync has already
            // attempted a silent refresh-and-retry for a 401 - if we're
            // still seeing Unauthorized, the refresh token itself is
            // genuinely no longer valid (fully expired, or revoked by an
            // explicit Logout elsewhere) and there's nothing left to do but
            // send the user back to Login.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException(
                    "Session expired. Please login again.");
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

        #endregion

        #region REFRESH TOKEN

        // Renamed from RefreshTokenAsync to avoid any confusion with the
        // API's own /auth/refresh-token endpoint path string used below -
        // this is the APP-side helper that calls it and updates Session.
        private async Task<bool> RefreshSessionTokenAsync()
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

                var content = BuildJsonContent(new { RefreshToken = refreshToken });

                // Deliberately uses a bare HttpClient call (not
                // SendWithAutoRefreshAsync) - refreshing the token can't
                // itself depend on the token being valid, and this
                // endpoint doesn't require [Authorize] on the API side.
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

                if (authResponse == null || string.IsNullOrEmpty(authResponse.AccessToken))
                    return false;

                // Save new tokens
                session.SetString(
                    "AccessToken",
                    authResponse.AccessToken);

                session.SetString(
                    "RefreshToken",
                    authResponse.RefreshToken ?? refreshToken);

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
