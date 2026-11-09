
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.MVC.Models;

namespace RaceDay.MVC.Services;

public class RaceDayApiService
{
    private readonly IHttpClientFactory _factory;

    public RaceDayApiService(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(string? token, string? cookie)
    {
        var client = _factory.CreateClient("RaceDayAPI");

        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        if (!string.IsNullOrWhiteSpace(cookie))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "Cookie", cookie);
        }

        return client;
    }

    // ACCOUNT: REGISTER

    public async Task<(bool Success, string Message)> RegisterAsync(object data)
    {
        var client = CreateClient(null, null);

        var response = await client.PostAsJsonAsync(
            "api/account/register", data);

        var message = await response.Content.ReadAsStringAsync();

        return (response.IsSuccessStatusCode,
            string.IsNullOrWhiteSpace(message)
                ? response.ReasonPhrase ?? "Registration completed."
                : message);
    }

    // ACCOUNT: LOGIN

    public async Task<ApiLoginResult> LoginAsync(
        string email, string password)
    {
        var client = CreateClient(null, null);

        var response = await client.PostAsJsonAsync(
            "api/account/login", new { email, password });

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new ApiLoginResult
            {
                Success = false,
                Message = string.IsNullOrWhiteSpace(body)
                    ? "Login failed. Please check your details."
                    : body
            };
        }

        string? cookie = null;
        string? token = null;

        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            cookie = string.Join("; ",
                cookies.Select(c => c.Split(';')[0]));
        }

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var document = JsonDocument.Parse(body);

                token = FindValue(
                    document.RootElement,
                    "token", "accessToken", "jwt");
            }
            catch (JsonException)
            {
                // The API may return an empty or non-JSON response.
            }
        }

        var profileResponse = await CreateClient(token, cookie)
            .GetAsync("api/account/profile");

        string? role = null;

        if (profileResponse.IsSuccessStatusCode)
        {
            var profile =
                await profileResponse.Content.ReadAsStringAsync();

            try
            {
                using var document = JsonDocument.Parse(profile);
                role = FindValue(document.RootElement, "role");
            }
            catch (JsonException)
            {
                // Profile response was not JSON.
            }
        }

        return new ApiLoginResult
        {
            Success = !string.IsNullOrWhiteSpace(role),
            Message = string.IsNullOrWhiteSpace(role)
                ? "Login succeeded, but the API authentication or profile response needs checking."
                : "Login successful.",
            Token = token,
            Cookie = cookie,
            Role = role
        };
    }

    // ACCOUNT: LOGOUT

    public async Task<(bool Success, string Message)> LogoutAsync(
        string? token, string? cookie)
    {
        var client = CreateClient(token, cookie);

        var response = await client.PostAsync(
            "api/account/logout", null);

        return (response.IsSuccessStatusCode,
            response.ReasonPhrase ?? "Logout completed.");
    }

    // EVENTS: GET ALL EVENTS

    public async Task<List<EventViewModel>> GetEventsAsync(
        string? token, string? cookie)
    {
        var client = CreateClient(token, cookie);

        var response = await client.GetAsync("api/events");

        if (!response.IsSuccessStatusCode)
            return new List<EventViewModel>();

        var json = await response.Content.ReadAsStringAsync();

        try
        {
            return JsonSerializer.Deserialize<List<EventViewModel>>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<EventViewModel>();
        }
        catch (JsonException)
        {
            return new List<EventViewModel>();
        }
    }

    // EVENTS: CREATE EVENT

    public async Task<(bool Success, string Message)> CreateEventAsync(
        EventViewModel model, string? token, string? cookie)
    {
        var client = CreateClient(token, cookie);

        var response = await client.PostAsJsonAsync("api/events", new
        {
            name = model.Name,
            description = model.Description,
            eventDate = model.EventDate,
            location = model.Location,
            distance = model.Distance,
            eventType = model.EventType,
            bannerImageUrl = model.BannerImageUrl
        });

        var message = await response.Content.ReadAsStringAsync();

        return (response.IsSuccessStatusCode,
            string.IsNullOrWhiteSpace(message)
                ? response.ReasonPhrase ?? "Event request completed."
                : message);
    }

    // EVENTS: UPDATE EVENT

    public async Task<(bool Success, string Message)> UpdateEventAsync(
        EventViewModel model, string? token, string? cookie)
    {
        var client = CreateClient(token, cookie);

        var response = await client.PutAsJsonAsync(
            $"api/events/{model.EventId}", new
            {
                name = model.Name,
                description = model.Description,
                eventDate = model.EventDate,
                location = model.Location,
                distance = model.Distance,
                eventType = model.EventType,
                bannerImageUrl = model.BannerImageUrl
            });

        var message = await response.Content.ReadAsStringAsync();

        return (response.IsSuccessStatusCode,
            string.IsNullOrWhiteSpace(message)
                ? response.ReasonPhrase ?? "Event update completed."
                : message);
    }

    // EVENTS: GET ONE EVENT

    public async Task<EventViewModel?> GetEventAsync(
        int id, string? token, string? cookie)
    {
        var client = CreateClient(token, cookie);

        var response = await client.GetAsync($"api/events/{id}");

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();

        try
        {
            return JsonSerializer.Deserialize<EventViewModel>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // HELPER: FIND A VALUE IN JSON

    private static string? FindValue(
        JsonElement element, params string[] names)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (names.Any(n => string.Equals(
                    n, property.Name,
                    StringComparison.OrdinalIgnoreCase)))
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                        return property.Value.GetString();

                    if (property.Value.ValueKind == JsonValueKind.Number)
                        return property.Value.ToString();
                }
            }

            foreach (var property in element.EnumerateObject())
            {
                var found = FindValue(property.Value, names);

                if (found != null)
                    return found;
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var found = FindValue(item, names);

                if (found != null)
                    return found;
            }
        }

        return null;
    }



    // ============================
    // CATEGORIES
    // ============================

    public async Task<List<CategoryViewModel>> GetCategoriesAsync(
        int eventId, string? token, string? cookie)
    {
        var client = CreateClient(token, cookie);

        var response = await client.GetAsync(
            $"api/categories/event/{eventId}");

        if (!response.IsSuccessStatusCode)
            return new List<CategoryViewModel>();

        return await response.Content.ReadFromJsonAsync<
            List<CategoryViewModel>>() ?? new List<CategoryViewModel>();
    }

    public async Task<(bool Success, string Message)> CreateCategoryAsync(
        int eventId, CategoryViewModel model,
        string? token, string? cookie)
    {
        var client = CreateClient(token, cookie);

        var response = await client.PostAsJsonAsync(
            $"api/categories/event/{eventId}",
            new
            {
                name = model.Name,
                description = model.Description
            });

        var message = await response.Content.ReadAsStringAsync();

        return (response.IsSuccessStatusCode,
            string.IsNullOrWhiteSpace(message)
                ? response.ReasonPhrase ?? "Category created."
                : message);
    }

    public async Task<CategoryViewModel?> GetCategoryAsync(
        int id, string? token, string? cookie)
    {
        var client = CreateClient(token, cookie);

        var response = await client.GetAsync($"api/categories/{id}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<CategoryViewModel>();
    }

    public async Task<(bool Success, string Message)> UpdateCategoryAsync(
        CategoryViewModel model, string? token, string? cookie)
    {
        var client = CreateClient(token, cookie);

        var response = await client.PutAsJsonAsync(
            $"api/categories/{model.CategoryId}",
            new
            {
                name = model.Name,
                description = model.Description
            });

        var message = await response.Content.ReadAsStringAsync();

        return (response.IsSuccessStatusCode,
            string.IsNullOrWhiteSpace(message)
                ? response.ReasonPhrase ?? "Category updated."
                : message);
    }

    public async Task<(bool Success, string Message)> DeleteCategoryAsync(
        int id, string? token, string? cookie)
    {
        var client = CreateClient(token, cookie);

        var response = await client.DeleteAsync(
            $"api/categories/{id}");

        var message = await response.Content.ReadAsStringAsync();

        return (response.IsSuccessStatusCode,
            string.IsNullOrWhiteSpace(message)
                ? response.ReasonPhrase ?? "Category deleted."
                : message);
    }

    // ============================
    // ENROLLMENTS
    // ============================

    public async Task<(bool Success, string Message)> EnrollAsync(
        int eventId, int categoryId,
        string? token, string? cookie)
    {
        var client = CreateClient(token, cookie);

        var response = await client.PostAsJsonAsync(
            "api/enrollment",
            new
            {
                eventId,
                categoryId
            });

        var message = await response.Content.ReadAsStringAsync();

        return (response.IsSuccessStatusCode,
            string.IsNullOrWhiteSpace(message)
                ? response.ReasonPhrase ?? "Enrollment completed."
                : message);
    }

    public async Task<List<EnrollmentViewModel>> GetMyEnrollmentsAsync(
        string? token, string? cookie)
    {
        var client = CreateClient(token, cookie);

        var response = await client.GetAsync("api/enrollment/my");

        if (!response.IsSuccessStatusCode)
            return new List<EnrollmentViewModel>();

        return await response.Content.ReadFromJsonAsync<
            List<EnrollmentViewModel>>() ?? new List<EnrollmentViewModel>();
    }

    public async Task<List<EnrollmentViewModel>> GetEventEnrollmentsAsync(
        int eventId, string? token, string? cookie)
    {
        var client = CreateClient(token, cookie);

        var response = await client.GetAsync(
            $"api/enrollment/event/{eventId}");

        if (!response.IsSuccessStatusCode)
            return new List<EnrollmentViewModel>();

        return await response.Content.ReadFromJsonAsync<
            List<EnrollmentViewModel>>() ?? new List<EnrollmentViewModel>();
    }

    // ============================
    // RESULTS
    // ============================

    public async Task<(bool Success, string Message)> CreateResultAsync(
        ResultViewModel model, string? token, string? cookie)
    {
        var client = CreateClient(token, cookie);

        var response = await client.PostAsJsonAsync(
            $"api/results/enrollment/{model.EnrollmentId}",
            new
            {
                finishTime = model.FinishTime,
                finishingPosition = model.FinishingPosition
            });

        var message = await response.Content.ReadAsStringAsync();

        return (response.IsSuccessStatusCode,
            string.IsNullOrWhiteSpace(message)
                ? response.ReasonPhrase ?? "Result created."
                : message);
    }

    public async Task<(bool Success, string Message)> UpdateResultAsync(
        ResultViewModel model, string? token, string? cookie)
    {
        var client = CreateClient(token, cookie);

        var response = await client.PutAsJsonAsync(
            $"api/results/enrollment/{model.EnrollmentId}",
            new
            {
                finishTime = model.FinishTime,
                finishingPosition = model.FinishingPosition
            });

        var message = await response.Content.ReadAsStringAsync();

        return (response.IsSuccessStatusCode,
            string.IsNullOrWhiteSpace(message)
                ? response.ReasonPhrase ?? "Result updated."
                : message);
    }

    public async Task<List<ResultViewModel>> GetMyResultsAsync(
        string? token, string? cookie)
    {
        var client = CreateClient(token, cookie);

        var response = await client.GetAsync("api/results/my");

        if (!response.IsSuccessStatusCode)
            return new List<ResultViewModel>();

        return await response.Content.ReadFromJsonAsync<
            List<ResultViewModel>>() ?? new List<ResultViewModel>();
    }

    public async Task<List<ResultViewModel>> GetEventResultsAsync(
        int eventId, string? token, string? cookie)
    {
        var client = CreateClient(token, cookie);

        var response = await client.GetAsync(
            $"api/results/event/{eventId}");

        if (!response.IsSuccessStatusCode)
            return new List<ResultViewModel>();

        return await response.Content.ReadFromJsonAsync<
            List<ResultViewModel>>() ?? new List<ResultViewModel>();
    }



// LOGIN RESULT MODEL

public class ApiLoginResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = "";

        public string? Token { get; set; }

        public string? Cookie { get; set; }

        public string? Role { get; set; }
    }
}
