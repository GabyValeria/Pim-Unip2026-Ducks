using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ducks.Infrastructure.Integrations.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Ducks.IntegrationTests;

[Collection("api")]
public sealed class RecuperacaoSenhaApiTests(ApiFixture api)
{
    private async Task<string> AguardarTokenAsync(string email)
    {
        var mock = api.Factory.Services.GetRequiredService<MockEmailService>();
        for (var i = 0; i < 50; i++)
        {
            var msg = mock.Enviadas.LastOrDefault(m => m.Para == email);
            if (msg is not null) return Uri.UnescapeDataString(msg.CorpoTexto.Split("token=")[1].Split('\n')[0].Trim());
            await Task.Delay(100);
        }
        throw new TimeoutException("O e-mail simulado não chegou.");
    }

    [FactSql]
    public async Task Fluxo_Completo_De_Recuperacao_Encerra_Sessoes_Antigas()
    {
        var client = api.Factory.CreateClient();
        var email = $"recupera_{Guid.NewGuid():N}@teste.com";
        var cadastro = await client.PostJsonAsync("/api/auth/registrar", new { NomeUsuario = "Recupera" + Guid.NewGuid().ToString("N")[..6], Email = email, Senha = "SenhaAntiga123" });
        var tokenAntigo = cadastro.Str("token");
        var refreshAntigo = cadastro.Str("refreshToken");
        await Task.Delay(1100); // o token antigo precisa ter sido emitido antes do segundo da troca

        // Mesma resposta para e-mail existente e inexistente.
        var r1 = await client.PostAsJsonAsync("/api/auth/esqueci-senha", new { Email = email });
        var r2 = await client.PostAsJsonAsync("/api/auth/esqueci-senha", new { Email = "nao-existe-" + Guid.NewGuid().ToString("N") + "@teste.com" });
        Assert.Equal(HttpStatusCode.Accepted, r1.StatusCode);
        Assert.Equal(r1.StatusCode, r2.StatusCode);
        Assert.Equal(await r1.Content.ReadAsStringAsync(), await r2.Content.ReadAsStringAsync());

        var token = await AguardarTokenAsync(email);
        await client.PostJsonAsync("/api/auth/redefinir-senha", new { Token = token, NovaSenha = "curta" }, HttpStatusCode.BadRequest);
        await client.PostJsonAsync("/api/auth/redefinir-senha", new { Token = token, NovaSenha = "SenhaNova456" }, HttpStatusCode.NoContent);
        await client.PostJsonAsync("/api/auth/redefinir-senha", new { Token = token, NovaSenha = "SenhaNova789" }, HttpStatusCode.BadRequest); // uso único

        // Token de acesso e refresh token anteriores deixam de valer.
        var antigo = api.Factory.CreateClient();
        antigo.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAntigo);
        Assert.Equal(HttpStatusCode.Unauthorized, (await antigo.GetAsync("/api/educacional/matriculas")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = refreshAntigo })).StatusCode);

        await client.PostJsonAsync("/api/auth/login", new { Email = email, Senha = "SenhaAntiga123" }, HttpStatusCode.Unauthorized);
        var login = await client.PostJsonAsync("/api/auth/login", new { Email = email, Senha = "SenhaNova456" });
        var novo = api.Factory.CreateClient();
        novo.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Str("token"));
        Assert.Equal(HttpStatusCode.OK, (await novo.GetAsync("/api/educacional/matriculas")).StatusCode);
        Assert.Equal(64, Convert.ToInt32(await api.ConsultarAsync($"SELECT TOP 1 LEN(TokenHash) FROM dbo.TokensRecuperacaoSenha t JOIN dbo.Usuarios u ON u.UsuarioID = t.UsuarioID WHERE u.Email = '{email}'")));
    }

    [FactSql]
    public async Task Rotas_De_Senha_Tem_Limite_Por_IP()
    {
        await using var factory = api.Factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:RecuperacaoSenha:PermitLimit", "2"));
        var client = factory.CreateClient();
        var codigos = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
            codigos.Add((await client.PostAsJsonAsync("/api/auth/esqueci-senha", new { Email = "limite@teste.com" })).StatusCode);
        Assert.Equal([HttpStatusCode.Accepted, HttpStatusCode.Accepted, HttpStatusCode.TooManyRequests], codigos);
    }
}
