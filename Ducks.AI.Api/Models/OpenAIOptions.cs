namespace Ducks.AI.Api.Models;

public class OpenAIOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public int MaxMessageLength { get; set; } = 2000;
    public int MaxOutputTokens { get; set; } = 600;
    public int MaxContextMessages { get; set; } = 10;
}