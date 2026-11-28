using System;

namespace Ducks.Desktop.Core.Models
{
    /// <summary>
    /// Item da fila/histórico de moderação. Os campos são extraídos de forma tolerante
    /// (ver ModerationItemParser), pois o contrato do IModeracaoService não foi fornecido.
    /// </summary>
    public class ModerationItemDto
    {
        /// <summary>Valor usado no segmento {tipo} das rotas (ex.: TRILHA, MODULO, AULA, QUESTAO).</summary>
        public string Tipo { get; set; } = string.Empty;
        public int ConteudoID { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Autor { get; set; } = string.Empty;
        public DateTime? EnviadoEm { get; set; }
        public string Status { get; set; } = string.Empty;
        /// <summary>Apenas no histórico: decisão tomada (aprovar, rejeitar, remover).</summary>
        public string Acao { get; set; } = string.Empty;
        public string Justificativa { get; set; } = string.Empty;

        public bool PodeAgir => !string.IsNullOrWhiteSpace(Tipo) && ConteudoID > 0;
    }
}
