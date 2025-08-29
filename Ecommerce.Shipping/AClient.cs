namespace Ecommerce.Shipping;

public abstract class AApiClient
{
    private readonly string _apiKey;
    private readonly Uri _apiUri;
    protected readonly HttpClient _httpClient;
    protected AApiClient(string apiKey, string apiUrl, bool useJwt = true)
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey;
        _httpClient.BaseAddress = _apiUri = new Uri(apiUrl);
        if(useJwt)
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        else throw new NotImplementedException("Only JWT auth is implemented");
    }
}