using Ducks.Application.DTOs;
using Ducks.Domain.Entities;
using Ducks.Domain.Exceptions;
using Ducks.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Ducks.UnitTests;

public sealed class ConteudoEModeracaoTests
{
    [Fact]
    public async Task Modulos_E_Aulas_Sao_Criados_Em_Rascunho_E_Ficam_Ocultos_Ao_Estudante()
    {
        await using var context = TestesBase.CriarContexto();
        var (trilha, _, _, _) = TestesBase.CriarTrilhaAprovada(context);
        var service = new ConteudoService(context);

        var modulo = await service.CriarModuloAsync(trilha.TrilhaID, new ModuloRequestDto { Titulo = "Novo módulo" }, TestesBase.CuradorId);
        Assert.Equal(StatusConteudo.Rascunho, modulo.Status);
        Assert.Equal(2, modulo.IndiceOrdem); // vai para o final

        var aula = await service.CriarAulaAsync(modulo.ModuloID, new AulaRequestDto { Titulo = "Aula nova", Conteudo = "texto" }, TestesBase.CuradorId);
        Assert.Equal(StatusConteudo.Rascunho, aula.Status);

        var visaoEstudante = await service.ObterTrilhaAsync(trilha.TrilhaID, somenteAprovado: true);
        Assert.Single(visaoEstudante.Modulos);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.ObterModuloAsync(modulo.ModuloID, somenteAprovado: true));
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.ObterAulaAsync(aula.AulaID, somenteAprovado: true));

        var visaoCurador = await service.ObterTrilhaAsync(trilha.TrilhaID, somenteAprovado: false);
        Assert.Equal(2, visaoCurador.Modulos.Count);
        Assert.Contains(await context.LogsAuditoria.Select(l => l.Acao).ToListAsync(), a => a == "MODULO_CRIADO");
    }

    [Fact]
    public async Task Reordenar_Exige_Lista_Completa_E_Renumera()
    {
        await using var context = TestesBase.CriarContexto();
        var (_, modulo, a1, a2) = TestesBase.CriarTrilhaAprovada(context);
        var service = new ConteudoService(context);
        var a3 = await service.CriarAulaAsync(modulo.ModuloID, new AulaRequestDto { Titulo = "Aula 3", IndiceOrdem = 1 }, TestesBase.CuradorId);

        // Inserida na posição 1: as outras descem.
        var ordem = await context.Aulas.OrderBy(a => a.IndiceOrdem).Select(a => a.AulaID).ToListAsync();
        Assert.Equal([a3.AulaID, a1.AulaID, a2.AulaID], ordem);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.ReordenarAulasAsync(modulo.ModuloID, [a1.AulaID, a2.AulaID], TestesBase.CuradorId));
        await service.ReordenarAulasAsync(modulo.ModuloID, [a2.AulaID, a3.AulaID, a1.AulaID], TestesBase.CuradorId);
        ordem = await context.Aulas.OrderBy(a => a.IndiceOrdem).Select(a => a.AulaID).ToListAsync();
        Assert.Equal([a2.AulaID, a3.AulaID, a1.AulaID], ordem);
    }

    [Fact]
    public async Task Fluxo_De_Revisao_Envia_Rejeita_Com_Justificativa_E_Aprova()
    {
        await using var context = TestesBase.CriarContexto();
        var (_, modulo, _, _) = TestesBase.CriarTrilhaAprovada(context);
        var conteudo = new ConteudoService(context);
        var moderacao = new ModeracaoService(context, conteudo);
        var aula = await conteudo.CriarAulaAsync(modulo.ModuloID, new AulaRequestDto { Titulo = "Aula em revisão", UrlConteudo = "https://x/3" }, TestesBase.CuradorId);

        // Não dá para aprovar o que não foi enviado.
        await Assert.ThrowsAsync<ConflictException>(() => moderacao.AprovarAsync("aula", aula.AulaID, TestesBase.ModeradorId, null));

        await moderacao.EnviarParaRevisaoAsync("aula", aula.AulaID, TestesBase.CuradorId);
        var pendentes = await moderacao.ListarPendentesAsync(new FiltroPendentesDto());
        Assert.Contains(pendentes.Items, p => p.Tipo == TipoConteudo.Aula && p.ConteudoID == aula.AulaID);
        await Assert.ThrowsAsync<ConflictException>(() => moderacao.EnviarParaRevisaoAsync("aula", aula.AulaID, TestesBase.CuradorId));

        await Assert.ThrowsAsync<BusinessRuleException>(() => moderacao.RejeitarAsync("aula", aula.AulaID, TestesBase.ModeradorId, "  "));
        var rejeicao = await moderacao.RejeitarAsync("aula", aula.AulaID, TestesBase.ModeradorId, "Falta o material de apoio.");
        Assert.Equal(StatusConteudo.Rejeitado, rejeicao.StatusNovo);

        // Curador corrige e reenvia; moderador aprova; estudante passa a ver.
        await conteudo.AtualizarAulaAsync(aula.AulaID, new AulaRequestDto { Titulo = "Aula corrigida", UrlConteudo = "https://x/3" }, TestesBase.CuradorId);
        await moderacao.EnviarParaRevisaoAsync("AULA", aula.AulaID, TestesBase.CuradorId);
        await moderacao.AprovarAsync("aula", aula.AulaID, TestesBase.ModeradorId, "ok");
        var visivel = await conteudo.ObterAulaAsync(aula.AulaID, somenteAprovado: true);
        Assert.Equal("Aula corrigida", visivel.Titulo);

        var historico = await moderacao.ListarHistoricoAsync(new FiltroHistoricoRevisaoDto { Tipo = "aula", ConteudoId = aula.AulaID, Ordem = "asc" });
        Assert.Equal([AcaoRevisao.EnviadoParaRevisao, AcaoRevisao.Rejeitado, AcaoRevisao.EnviadoParaRevisao, AcaoRevisao.Aprovado],
            historico.Items.Select(h => h.Acao).ToArray());
        Assert.Equal("Falta o material de apoio.", historico.Items[1].Justificativa);
        Assert.Equal(4, await context.LogsAuditoria.CountAsync(l => l.Acao.StartsWith("CONTEUDO_")));
    }

    [Fact]
    public async Task Remocao_Pela_Moderacao_Esconde_E_Bloqueia_Edicao()
    {
        await using var context = TestesBase.CriarContexto();
        var (trilha, _, aula, _) = TestesBase.CriarTrilhaAprovada(context);
        var conteudo = new ConteudoService(context);
        var moderacao = new ModeracaoService(context, conteudo);

        await Assert.ThrowsAsync<BusinessRuleException>(() => moderacao.RemoverAsync("aula", aula.AulaID, TestesBase.ModeradorId, ""));
        await moderacao.RemoverAsync("aula", aula.AulaID, TestesBase.ModeradorId, "Conteúdo inadequado");
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => conteudo.ObterAulaAsync(aula.AulaID, somenteAprovado: true));
        await Assert.ThrowsAsync<ConflictException>(() =>
            conteudo.AtualizarAulaAsync(aula.AulaID, new AulaRequestDto { Titulo = "tentativa" }, TestesBase.CuradorId));

        await moderacao.RemoverAsync("trilha", trilha.TrilhaID, TestesBase.ModeradorId, "Trilha duplicada");
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => conteudo.ObterTrilhaAsync(trilha.TrilhaID, somenteAprovado: true));
    }

    [Fact]
    public async Task Excluir_Modulo_Remove_Aulas_E_Progresso()
    {
        await using var context = TestesBase.CriarContexto();
        var (_, modulo, aula, _) = TestesBase.CriarTrilhaAprovada(context);
        context.ProgressosUsuario.Add(new ProgressoUsuario { UsuarioID = TestesBase.AlunoId, AulaID = aula.AulaID });
        await context.SaveChangesAsync();

        await new ConteudoService(context).ExcluirModuloAsync(modulo.ModuloID, TestesBase.CuradorId);
        Assert.False(await context.Modulos.AnyAsync());
        Assert.False(await context.Aulas.AnyAsync());
        Assert.False(await context.ProgressosUsuario.AnyAsync());
    }
}
