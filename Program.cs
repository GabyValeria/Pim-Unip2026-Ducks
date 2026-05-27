using System;
using System.Collections.Generic;

namespace Ducks
{
    // ============================================
    // CLASSE ABSTRATA - USUÁRIO
    // ============================================

    abstract class Usuario
    {
        // Encapsulamento
        private string senha;

        public int Id { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }

        public void DefinirSenha(string novaSenha)
        {
            senha = novaSenha;
        }

        public bool ValidarSenha(string senhaInformada)
        {
            return senha == senhaInformada;
        }

        // Polimorfismo
        public abstract void ExibirPerfil();
    }

    // ============================================
    // CLASSE ALUNO
    // ============================================

    class Aluno : Usuario
    {
        public int Pontos { get; private set; }

        public List<TrilhaAprendizagem> Trilhas { get; set; }

        public Aluno()
        {
            Trilhas = new List<TrilhaAprendizagem>();
        }

        public void AdicionarPontos(int pontos)
        {
            Pontos += pontos;
        }

        public void InscreverTrilha(TrilhaAprendizagem trilha)
        {
            Trilhas.Add(trilha);

            Console.WriteLine($"{Nome} foi inscrito na trilha '{trilha.Titulo}'.");
        }

        public override void ExibirPerfil()
        {
            Console.WriteLine("===== PERFIL DO ALUNO =====");
            Console.WriteLine($"ID: {Id}");
            Console.WriteLine($"Nome: {Nome}");
            Console.WriteLine($"Email: {Email}");
            Console.WriteLine($"Pontos: {Pontos}");
        }
    }

    // ============================================
    // CLASSE ADMINISTRADOR
    // ============================================

    class Administrador : Usuario
    {
        public string NivelAcesso { get; set; }

        public void GerenciarUsuarios()
        {
            Console.WriteLine("Administrador gerenciando usuários...");
        }

        public override void ExibirPerfil()
        {
            Console.WriteLine("===== PERFIL DO ADMINISTRADOR =====");
            Console.WriteLine($"ID: {Id}");
            Console.WriteLine($"Nome: {Nome}");
            Console.WriteLine($"Email: {Email}");
            Console.WriteLine($"Nível de acesso: {NivelAcesso}");
        }
    }

    // ============================================
    // CLASSE ATIVIDADE
    // ============================================

    class Atividade
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public int Pontuacao { get; set; }

        public bool Concluir()
        {
            Console.WriteLine($"Atividade '{Titulo}' concluída com sucesso.");

            return true;
        }
    }

    // ============================================
    // CLASSE TRILHA DE APRENDIZAGEM
    // ============================================

    class TrilhaAprendizagem
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Categoria { get; set; }

        public List<Atividade> Atividades { get; set; }

        public TrilhaAprendizagem()
        {
            Atividades = new List<Atividade>();
        }

        public void AdicionarAtividade(Atividade atividade)
        {
            Atividades.Add(atividade);

            Console.WriteLine($"Atividade '{atividade.Titulo}' adicionada.");
        }

        public void ExibirTrilha()
        {
            Console.WriteLine("===== TRILHA DE APRENDIZAGEM =====");
            Console.WriteLine($"Título: {Titulo}");
            Console.WriteLine($"Categoria: {Categoria}");

            Console.WriteLine("\nLista de atividades:");

            foreach (var atividade in Atividades)
            {
                Console.WriteLine($"- {atividade.Titulo} ({atividade.Pontuacao} pontos)");
            }
        }
    }

    // ============================================
    // CLASSE RELATÓRIO
    // ============================================

    class RelatorioDesempenho
    {
        public Aluno Aluno { get; set; }

        public void GerarRelatorio()
        {
            Console.WriteLine("\n===== RELATÓRIO DE DESEMPENHO =====");

            Console.WriteLine($"Aluno: {Aluno.Nome}");
            Console.WriteLine($"Email: {Aluno.Email}");
            Console.WriteLine($"Pontuação Total: {Aluno.Pontos}");

            Console.WriteLine("\nTrilhas Inscritas:");

            foreach (var trilha in Aluno.Trilhas)
            {
                Console.WriteLine($"- {trilha.Titulo}");
            }
        }
    }

    // ============================================
    // CLASSE PRINCIPAL
    // ============================================

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("======================================");
            Console.WriteLine(" SISTEMA EDUCACIONAL DUCKS LEARNING ");
            Console.WriteLine("======================================\n");

            // ============================================
            // CRIANDO ALUNO
            // ============================================

            Aluno aluno = new Aluno();

            aluno.Id = 1;
            aluno.Nome = "Gabrielle";
            aluno.Email = "gabrielle@email.com";

            aluno.DefinirSenha("123456");

            // ============================================
            // CRIANDO ADMINISTRADOR
            // ============================================

            Administrador admin = new Administrador();

            admin.Id = 2;
            admin.Nome = "Administrador";
            admin.Email = "admin@ducks.com";
            admin.NivelAcesso = "Total";

            admin.DefinirSenha("admin123");

            // ============================================
            // CRIANDO TRILHA
            // ============================================

            TrilhaAprendizagem trilha = new TrilhaAprendizagem();

            trilha.Id = 1;
            trilha.Titulo = "Desenvolvimento Web";
            trilha.Categoria = "Programação";

            // ============================================
            // CRIANDO ATIVIDADES
            // ============================================

            Atividade atividade1 = new Atividade();

            atividade1.Id = 1;
            atividade1.Titulo = "HTML e CSS";
            atividade1.Descricao = "Introdução ao HTML5 e CSS3";
            atividade1.Pontuacao = 100;

            Atividade atividade2 = new Atividade();

            atividade2.Id = 2;
            atividade2.Titulo = "JavaScript";
            atividade2.Descricao = "Lógica de programação com JavaScript";
            atividade2.Pontuacao = 150;

            // ============================================
            // ADICIONANDO ATIVIDADES
            // ============================================

            trilha.AdicionarAtividade(atividade1);
            trilha.AdicionarAtividade(atividade2);

            // ============================================
            // INSCREVENDO ALUNO
            // ============================================

            aluno.InscreverTrilha(trilha);

            // ============================================
            // CONCLUINDO ATIVIDADES
            // ============================================

            if (atividade1.Concluir())
            {
                aluno.AdicionarPontos(atividade1.Pontuacao);
            }

            if (atividade2.Concluir())
            {
                aluno.AdicionarPontos(atividade2.Pontuacao);
            }

            // ============================================
            // EXIBINDO PERFIS
            // ============================================

            Console.WriteLine();

            aluno.ExibirPerfil();

            Console.WriteLine();

            admin.ExibirPerfil();

            Console.WriteLine();

            // ============================================
            // EXIBINDO TRILHA
            // ============================================

            trilha.ExibirTrilha();

            // ============================================
            // GERANDO RELATÓRIO
            // ============================================

            RelatorioDesempenho relatorio = new RelatorioDesempenho();

            relatorio.Aluno = aluno;

            relatorio.GerarRelatorio();

            // ============================================
            // TESTANDO LOGIN
            // ============================================

            Console.WriteLine("\n===== TESTE DE LOGIN =====");

            bool loginValido = aluno.ValidarSenha("123456");

            if (loginValido)
            {
                Console.WriteLine("Login realizado com sucesso.");
            }
            else
            {
                Console.WriteLine("Senha incorreta.");
            }

            Console.WriteLine("\nSistema finalizado.");
            Console.ReadLine();
        }
    }
}