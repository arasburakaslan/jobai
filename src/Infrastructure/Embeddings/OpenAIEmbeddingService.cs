using System.Net.Http.Json;
using System.Text.Json.Serialization;
using JobRag.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace JobRag.Infrastructure.Embeddings;

/// <summary>
/// Embedding service using OpenAI's text-embedding API.
/// Supports text-embedding-3-small (1536 dimensions).
/// </summary>
public class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIEmbeddingService> _logger;
    private readonly string _model;

    public OpenAIEmbeddingService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAIEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _model = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";

        var apiKey = configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }

        _httpClient.BaseAddress = new Uri(configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/");
    }

    public async Task<Vector> EmbedTextAsync(string text, CancellationToken ct = default)
    {
        _logger.LogDebug("Generating embedding for text of length {Length}", text.Length);

        // Truncate to ~8000 tokens (rough estimate: 4 chars per token)
        if (text.Length > 32000)
        {
            text = text[..32000];
        }

        var request = new EmbeddingRequest
        {
            Model = _model,
            Input = text
        };

        var response = await _httpClient.PostAsJsonAsync("v1/embeddings", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(ct);
        if (result?.Data is null || result.Data.Count == 0)
        {
            throw new InvalidOperationException("OpenAI returned empty embedding response");
        }

        var embedding = result.Data[0].Embedding;
        _logger.LogDebug("Generated embedding with {Dimensions} dimensions", embedding.Length);

        return new Vector(embedding);
    }

    private class EmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = default!;

        [JsonPropertyName("input")]
        public string Input { get; set; } = default!;
    }

    private class EmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingData> Data { get; set; } = [];
    }

    private class EmbeddingData
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = [];
    }
}
