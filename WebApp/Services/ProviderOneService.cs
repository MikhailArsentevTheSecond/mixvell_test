using System.Net;
using System.Runtime.Serialization;
using System.Text.Json;

namespace WebApp;

public class ProviderOneService : IProviderOneService
{
    public const string ProviderHttpClient = "providerOne";
    
    private readonly ILogger<ProviderOneService> _logger;

    private const string searchMethod = "/search";
    private const string pingMethod = "/ping";

    private readonly HttpClient _client;
    
    public ProviderOneService(IHttpClientFactory factory, ILogger<ProviderOneService> logger)
    {
        _logger = logger;
        _client = factory.CreateClient(ProviderHttpClient) 
                  ?? throw new NullReferenceException($"Ошибка при получении HttpClient-а для {ProviderHttpClient}");
    }

    public virtual async Task<ProviderOneSearchResponse> Search(SearchRequest request)
    {
        var responseBody  = await _client.PostAsync(searchMethod, JsonContent.Create(new ProviderOneSearchRequest(request)));
        if (responseBody.IsSuccessStatusCode)
        {
            var body = await responseBody.Content.ReadAsByteArrayAsync();
            var result = JsonSerializer.Deserialize<ProviderOneSearchResponse>(body);
            if (result == null)
            {
                throw new SerializationException("Не удалось прочитать данные");
            }
            return result;
        }
        else
        {
            _logger.LogWarning(string.Format("Запрос к {0} вернул статус {1}. {2}", 
                nameof(ProviderOneService),
                responseBody.StatusCode, responseBody.ReasonPhrase));
            return new ProviderOneSearchResponse();
        }
    }

    public virtual async ValueTask<bool> Ping()
    {
        var result = await _client.GetAsync(pingMethod);
        switch (result.StatusCode)
        {
            case HttpStatusCode.OK:
                return true;
                case HttpStatusCode.InternalServerError:
                return false;
                default:
                    _logger.LogWarning(string.Format("Запрос ping к {0} вернул непредвиденный статус {1}. {2}", 
                        nameof(ProviderOneService), result.StatusCode, result.ReasonPhrase));
                    return result.IsSuccessStatusCode;
        }
    }
}