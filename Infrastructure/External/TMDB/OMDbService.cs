using System;

namespace Infrastructure.External.TMDB
{
    public class TMDBService
    {
    private readonly RestClient _client;
    private readonly string _apiKey;
    private readonly ILogger<TMDBService> _logger;

    public TMDBService(IConfiguration config, ILogger<TMDBService> logger)
    {
        _apiKey = config["TMDB:ApiKey"] ?? throw new ArgumentNullException("TMDB API key is missing.");
        _client = new RestClient(config["TMDB:ApiEndpoint"] ?? throw new ArgumentNullException("TMDB API Endpoint is missing."));
        _client.AddDefaultHeader("Authorization",$"Bearer {_apiKey}");
        _logger = logger;
    }

    public async Task<MovieDTO?> GetMovieAsync(string title)
    {
        var request = new RestRequest();
        try
        {
            var response = await _client.GetAsync<MovieDTO>(request);
            return response ?? null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch movie from TMDB.");
            return null;
        }
    }
    }
}
