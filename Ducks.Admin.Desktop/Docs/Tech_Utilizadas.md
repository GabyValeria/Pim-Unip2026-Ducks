# Tecnologias Utilizadas e Escolhas Arquiteturais — Ducks Admin Desktop

Este documento descreve detalhadamente a stack tecnológica, bibliotecas, ferramentas de persistência, comunicação e infraestrutura utilizadas no desenvolvimento do **Ducks Admin Desktop**, justificando a escolha de cada componente na arquitetura da aplicação cliente.

---

## 1. Visão Geral da Stack Tecnológica

| Categoria | Tecnologia / Biblioteca | Versão | Função Principal no Sistema |
| :--- | :--- | :--- | :--- |
| **Linguagem** | C# | 12.0 / 13.0 | Linguagem principal fortemente tipada e orientada a objetos |
| **Runtime & Framework** | .NET | 10.0 SDK | Runtime de alta performance e suporte cross-platform |
| **UI Framework** | WPF (Windows Presentation Foundation) | .NET 10 | Construção de interfaces nativas para desktop Windows |
| **Arquitetura de UI** | MVVM (Model-View-ViewModel) | N/A | Separação estrita entre lógica de apresentação, dados e UI |
| **MVVM Toolkit** | `CommunityToolkit.Mvvm` | 8.x | Gerador de código (Source Generators) para propriedades reativas e comandos |
| **Comunicação HTTP** | `System.Net.Http.Json` / `HttpClient` | N/A | Consumo de endpoints RESTful com serialização/deserialização otimizada |
| **Comunicação Real-Time** | `Microsoft.AspNetCore.SignalR.Client` | 9.x / 10.x | Recebimento de alertas e atualizações de auditoria em tempo real |
| **Injeção de Dependência** | `Microsoft.Extensions.DependencyInjection` | 10.x | IoC Container para gestão de ciclo de vida de ViewModels, Views e Serviços |
| **Testes Unitários** | xUnit & Moq | 2.x / 4.x | Cobertura de testes unitários para ViewModels e clientes HTTP |
| **Banco de Dados Relacional** | SQL Server / Azure SQL Database | Cloud / Local | Persistência centralizada e relacional dos dados da plataforma |
| **Controle de Versão** | Git & GitHub | N/A | Versionamento de código, gestão de branches e CI/CD |

---

## 2. Justificativa e Papel de Cada Tecnologia

### 2.1. C# e .NET 10.0 SDK
* **Justificativa:** A escolha do .NET 10 como runtime principal garante o máximo de desempenho de execução, gerenciamento eficiente de memória (Garbage Collection otimizado), compilação AOT (Ahead-Of-Time) quando necessária e suporte de longo prazo (LTS). O C# oferece tipagem forte, recursos avançados de assincronismo (`async/await`) e suporte completo a padrão de tarefas concorrentes, fundamentais para uma aplicação desktop fluida e responsiva.
* **Papel na Arquitetura:** Serve como base de execução de toda a solução client-side (`Ducks.Admin.Desktop`) e de testes (`Ducks.Admin.UnitTests`).

### 2.2. WPF (Windows Presentation Foundation)
* **Justificativa:** O WPF possibilita o desenvolvimento de interfaces ricas, altamente customizáveis e aceleradas por hardware via DirectX. A separação entre código executável e definição de tela via XAML (Extensible Application Markup Language) permite criar um Design System proprietário com animações suaves, cantos arredondados, paletas modernas no estilo SaaS e responsividade nativa.
* **Papel na Arquitetura:** Camada de apresentação visual (Views, Controles de Usuário, Recursos de Estilo XAML e Templates de Dados).

### 2.3. Padrão Arquitetural MVVM com `CommunityToolkit.Mvvm`
* **Justificativa:** O padrão Model-View-ViewModel desacopla completamente a lógica de negócios da interface gráfica. O uso do `CommunityToolkit.Mvvm` (oficial da Microsoft) reduz o boilerplate code através de C# Source Generators (atributos como `[ObservableProperty]` e `[RelayCommand]`), melhorando a manutenibilidade e a testabilidade das ViewModels sem afetar a performance de tempo de compilação.
* **Papel na Arquitetura:**
  * **Model:** Espelhamento das entidades e DTOs da API REST.
  * **View:** Arquivos XAML focados apenas em layout e vinculação de dados (*Data Binding*).
  * **ViewModel:** Camada intermediária reativa que responde às interações do administrador e executa chamadas de serviço.

### 2.4. Cliente HTTP e Comunicação com API Backend
* **Justificativa:** A aplicação Desktop Ducks Admin opera como um cliente magro (*thin client*), desacoplado da camada de acesso direto ao banco de dados por questões de segurança. As requisições são enviadas com cabeçalhos de autorização `Bearer <JWT_TOKEN>` sobre conexão segura HTTPS.
* **Papel na Arquitetura:** O cliente de API encapsula as chamadas de CRUD e moderação, permitindo inclusive um modo de contingência/fallback local simulado para testes offline.

### 2.5. SQL Server e Azure SQL Database
* **Justificativa:** O banco de dados relacional Azure SQL garante integridade referencial, compatibilidade com Entity Framework Core na API backend, suporte a transações complexas e alta disponibilidade na nuvem Microsoft Azure (região Brazil South).
* **Papel na Arquitetura:** Armazenamento seguro de usuários, permissões, trilhas de aprendizagem, progresso, moedas de XP, conquistas e registros de moderação/auditoria.

### 2.6. SignalR (Comunicação em Tempo Real)
* **Justificativa:** Permite que o painel desktop receba notificações *push* do servidor instantaneamente sem a necessidade de *polling* HTTP constante.
* **Papel na Arquitetura:** Notificar o administrador sobre novos cadastros, conteúdos reportados para a Fila de Moderação e atualizações críticas nos parâmetros do sistema.

### 2.7. xUnit e Moq (Testes Unitários)
* **Justificativa:** xUnit é o padrão consagrado da comunidade .NET para testes de unidade, enquanto o Moq possibilita simular (*mockar*) contratos de interfaces HTTP e serviços de infraestrutura.
* **Papel na Arquitetura:** Testar os comportamentos reativos das ViewModels (`UsersViewModel`, `CursosTrilhasViewModel`, `AiAssistantViewModel`, etc.) garantindo regressão zero a cada commit no GitHub.