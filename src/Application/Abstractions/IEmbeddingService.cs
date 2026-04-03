using Pgvector;

namespace JobRag.Application.Abstractions;

/// <summary>
/// Service for generating text embeddings using an external model (OpenAI, local, etc.).
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate an embedding vector for the given text.
    /// </summary>
    Task<Vector> EmbedTextAsync(string text, CancellationToken ct = default);
}
