namespace Ducks.Admin.Desktop.Models
{
    public class AiPromptDto
    {
        public string Prompt { get; set; } = string.Empty;
        public string Contexto { get; set; } = "AdminDashboard";
    }

    public class AiResponseDto
    {
        public string Resposta { get; set; } = string.Empty;
        public bool Sucesso { get; set; }
        public string MensagemErro { get; set; } = string.Empty;
    }
}