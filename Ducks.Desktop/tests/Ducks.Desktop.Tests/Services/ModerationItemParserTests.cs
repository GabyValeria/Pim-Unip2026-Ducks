using System.Text.Json;
using Ducks.Desktop.Application.Services;

namespace Ducks.Desktop.Tests
{
    public class ModerationItemParserTests
    {
        private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

        [Fact]
        public void ParseList_WithPlainArray_ParsesAllItems()
        {
            var list = ModerationItemParser.ParseList(Json(
                "[{\"tipo\":\"TRILHA\",\"conteudoId\":1,\"titulo\":\"A\"},{\"tipo\":\"AULA\",\"conteudoId\":2,\"titulo\":\"B\"}]"));

            Assert.Equal(2, list.Count);
            Assert.Equal("AULA", list[1].Tipo);
        }

        [Theory]
        [InlineData("items")]
        [InlineData("itens")]
        [InlineData("data")]
        [InlineData("resultados")]
        public void ParseList_WithEnvelope_FindsArrayUnderKnownKeys(string key)
        {
            var list = ModerationItemParser.ParseList(Json($"{{\"{key}\":[{{\"tipo\":\"MODULO\",\"id\":9,\"titulo\":\"M\"}}]}}"));

            var item = Assert.Single(list);
            Assert.Equal(9, item.ConteudoID);
        }

        [Fact]
        public void ParseList_WithUnexpectedShape_ReturnsEmptyList()
        {
            Assert.Empty(ModerationItemParser.ParseList(Json("{\"foo\":1}")));
            Assert.Empty(ModerationItemParser.ParseList(Json("\"texto\"")));
            Assert.Empty(ModerationItemParser.ParseList(default));
        }

        [Fact]
        public void ParseList_IgnoresNonObjectEntries()
        {
            Assert.Empty(ModerationItemParser.ParseList(Json("[1,2,\"x\",null]")));
        }

        [Fact]
        public void Parse_AcceptsAlternativeFieldNamesCaseInsensitively()
        {
            var item = ModerationItemParser.Parse(Json(
                "{\"TipoConteudo\":\"QUESTAO\",\"ItemId\":\"12\",\"TituloOuDescricao\":\"Questão 12\",\"Autor\":\"Maria\",\"DataEnvio\":\"2026-11-20T10:30:00Z\",\"Status\":\"PENDENTE\"}"));

            Assert.Equal("QUESTAO", item.Tipo);
            Assert.Equal(12, item.ConteudoID);
            Assert.Equal("Questão 12", item.Titulo);
            Assert.Equal("Maria", item.Autor);
            Assert.Equal("PENDENTE", item.Status);
            Assert.NotNull(item.EnviadoEm);
            Assert.True(item.PodeAgir);
        }

        [Fact]
        public void Parse_ReadsHistoryDecisionFields()
        {
            var item = ModerationItemParser.Parse(Json(
                "{\"tipo\":\"AULA\",\"conteudoId\":4,\"titulo\":\"Aula 4\",\"acao\":\"REJEITAR\",\"justificativa\":\"Erro de português\"}"));

            Assert.Equal("REJEITAR", item.Acao);
            Assert.Equal("Erro de português", item.Justificativa);
        }

        [Fact]
        public void Parse_WithMissingTypeOrId_CannotAct()
        {
            var semTipo = ModerationItemParser.Parse(Json("{\"conteudoId\":4,\"titulo\":\"x\"}"));
            var semId = ModerationItemParser.Parse(Json("{\"tipo\":\"AULA\",\"titulo\":\"x\"}"));

            Assert.False(semTipo.PodeAgir);
            Assert.False(semId.PodeAgir);
        }

        [Fact]
        public void Parse_WithEmptyObject_ReturnsDefaults()
        {
            var item = ModerationItemParser.Parse(Json("{}"));

            Assert.Equal(string.Empty, item.Titulo);
            Assert.Equal(0, item.ConteudoID);
            Assert.Null(item.EnviadoEm);
        }
    }
}
