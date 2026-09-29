namespace TecAssist.Infrastructure.Nlp;

public sealed class NvidiaOptions
{
    public const string SectionName = "Nvidia";

    public string BaseUrl { get; set; } = "https://integrate.api.nvidia.com/v1";

    public string ApiKey { get; set; } = string.Empty;

    public string ChatModel { get; set; } = string.Empty;

    public string EmbeddingModel { get; set; } = string.Empty;

    public int MaxOutputTokens { get; set; } = 2048;
}
