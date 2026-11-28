using System.Globalization;
using System.Text.Json;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Services
{
    /// <summary>
    /// Converte o JSON de moderação (fila e histórico) em ModerationItemDto.
    /// O contrato exato do IModeracaoService não foi fornecido; por isso a leitura é tolerante
    /// a nomes alternativos de campos e a envelopes paginados ({ items: [...] }).
    /// </summary>
    public static class ModerationItemParser
    {
        private static readonly string[] EnvelopeKeys = { "items", "itens", "dados", "data", "resultados", "results" };

        public static List<ModerationItemDto> ParseList(JsonElement root)
        {
            var result = new List<ModerationItemDto>();
            var array = root;

            if (root.ValueKind == JsonValueKind.Object)
            {
                array = default;
                foreach (var key in EnvelopeKeys)
                {
                    if (TryGet(root, key, out var inner) && inner.ValueKind == JsonValueKind.Array)
                    {
                        array = inner;
                        break;
                    }
                }
            }

            if (array.ValueKind != JsonValueKind.Array) return result;

            foreach (var element in array.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.Object)
                    result.Add(Parse(element));
            }
            return result;
        }

        public static ModerationItemDto Parse(JsonElement e) => new()
        {
            Tipo = Str(e, "tipo", "tipoConteudo", "tipoItem"),
            ConteudoID = Int(e, "conteudoId", "itemId", "id"),
            Titulo = Str(e, "titulo", "nome", "nomeTrilha", "tituloOuDescricao", "descricao", "resumo"),
            Autor = Str(e, "autor", "autorNome", "nomeAutor", "enviadoPor", "usuario"),
            EnviadoEm = Date(e, "enviadoEm", "dataEnvio", "criadoEm", "atualizadoEm", "dataHora", "decididoEm"),
            Status = Str(e, "status", "estado"),
            Acao = Str(e, "acao", "decisao"),
            Justificativa = Str(e, "justificativa", "comentario")
        };

        private static bool TryGet(JsonElement obj, string name, out JsonElement value)
        {
            if (obj.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in obj.EnumerateObject())
                {
                    if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = p.Value;
                        return true;
                    }
                }
            }
            value = default;
            return false;
        }

        private static string Str(JsonElement e, params string[] names)
        {
            foreach (var n in names)
            {
                if (!TryGet(e, n, out var v)) continue;
                switch (v.ValueKind)
                {
                    case JsonValueKind.String when !string.IsNullOrWhiteSpace(v.GetString()):
                        return v.GetString()!;
                    case JsonValueKind.Number:
                        return v.GetRawText();
                }
            }
            return string.Empty;
        }

        private static int Int(JsonElement e, params string[] names)
        {
            foreach (var n in names)
            {
                if (!TryGet(e, n, out var v)) continue;
                if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)) return i;
                if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s)) return s;
            }
            return 0;
        }

        private static DateTime? Date(JsonElement e, params string[] names)
        {
            foreach (var n in names)
            {
                if (!TryGet(e, n, out var v) || v.ValueKind != JsonValueKind.String) continue;
                if (DateTime.TryParse(v.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d))
                    return d;
            }
            return null;
        }
    }
}
