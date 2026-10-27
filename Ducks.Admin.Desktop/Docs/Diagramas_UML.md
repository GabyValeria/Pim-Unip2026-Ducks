# Diagramas UML — Plataforma Educacional Gamificada Ducks

Este documento centraliza a documentação arquitetural e de modelagem UML (Unified Modeling Language) da plataforma **Ducks**. Os diagramas cobrem os aspectos de Casos de Uso, Estrutura de Classes (MVVM e Domínio), Sequência de Autenticação/Operações e Arquitetura de Componentes.

---

## 1. Diagrama de Casos de Uso (Use Case Diagram)

Descreve as interações dos atores (Administrador, Aluno/Usuário e Sistema/IA) com os principais módulos da plataforma Ducks.

```mermaid
usecaseDiagram
    actor "Administrador (Desktop)" as Admin
    actor "Aluno / Usuário (Web/Mobile)" as Student
    actor "Serviço de IA (OpenAI API)" as IAService

    rectangle "Ecossistema Ducks" {
        usecase "UC01 - Autenticar no Sistema (JWT)" as UC01
        usecase "UC02 - Gerenciar Usuários e Perfis" as UC02
        usecase "UC03 - Moderar Conteúdos e Denúncias" as UC03
        usecase "UC04 - Gerenciar Cursos e Trilhas" as UC04
        usecase "UC05 - Configurar Atividades e Desafios" as UC05
        usecase "UC06 - Auditar Pontuação de XP e Medalhas" as UC06
        usecase "UC07 - Gerar Sugestões e Resumos com IA" as UC07
        usecase "UC08 - Visualizar Dashboards e Métricas" as UC08
        usecase "UC09 - Consumir Trilhas e Realizar Atividades" as UC09
        usecase "UC10 - Receber Notificações em Tempo Real" as UC10
    }

    Admin --> UC01
    Admin --> UC02
    Admin --> UC03
    Admin --> UC04
    Admin --> UC05
    Admin --> UC06
    Admin --> UC07
    Admin --> UC08

    Student --> UC01
    Student --> UC09
    Student --> UC10

    UC07 ..> IAService : <<include>>
    UC03 ..> UC10 : <<trigger>>
```

---

## 2. Diagrama de Classes — Painel Desktop Administrativo (Pattern MVVM)

Estrutura das classes do aplicativo desktop `Ducks.Admin.Desktop` utilizando o **CommunityToolkit.Mvvm** e separação em camadas (View, ViewModel, Model, Service).

```mermaid
classDiagram
    namespace Models {
        class UserAdminDto {
            +int UsuarioID
            +string NomeUsuario
            +string Email
            +bool EstaAtivo
            +DateTime CriadoEm
        }
        class ModerationItemDto {
            +int ItemID
            +string Conteudo
            +string Autor
            +string Status
        }
        class CursoTrilhaDto {
            +int CursoID
            +string Titulo
            +string Categoria
            +int CargaHoraria
            +bool Publicado
        }
        class AuthResponseDto {
            +int UsuarioID
            +string NomeUsuario
            +string Token
            +DateTime Expiracao
            +bool Sucesso
        }
    }

    namespace Services {
        class ApiClient {
            -HttpClient _httpClient
            +Task~AuthResponseDto~ LoginAsync(string email, string senha)
            +Task~List~UserAdminDto~~ GetUsuariosAsync()
            +Task~List~ModerationItemDto~~ GetFilaModeracaoAsync()
            +Task~bool~ AtualizarStatusUsuarioAsync(int id, bool ativo)
        }
        class AiService {
            -string _apiKey
            +Task~string~ SummarizeModerationReportAsync(string reportText)
            +Task~string~ GenerateChallengeSuggestionAsync(string topic)
        }
    }

    namespace ViewModels {
        class MainViewModel {
            -string _currentPageTitle
            -string _totalUsuarios
            -string _statusConexao
            +IRelayCommand NavigateToUsersCommand
            +IRelayCommand NavigateToModerationCommand
            +IRelayCommand NavigateToAICommand
            +NavigateToDashboard()
            +RefreshConnectionAsync()
        }
        class LoginViewModel {
            -string _email
            -string _senha
            -bool _isBusy
            +IRelayCommand ExecuteLoginCommand
        }
        class UsersViewModel {
            -ObservableCollection~UserAdminDto~ _usuarios
            +IRelayCommand CarregarUsuariosCommand
            +IRelayCommand AlternarStatusUsuarioCommand
        }
        class AiAssistantViewModel {
            -string _promptText
            -string _iaResponseText
            +IRelayCommand GerarSugestaoCommand
        }
    }

    namespace Views {
        class MainWindow {
            +InitializeComponent()
        }
        class LoginWindow {
            +InitializeComponent()
        }
    }

    MainWindow ..> MainViewModel : DataContext
    LoginWindow ..> LoginViewModel : DataContext
    MainViewModel --> ApiClient : uses
    UsersViewModel --> ApiClient : uses
    LoginViewModel --> ApiClient : uses
    AiAssistantViewModel --> AiService : uses
    ApiClient ..> UserAdminDto : manages
    ApiClient ..> ModerationItemDto : manages
    ApiClient ..> AuthResponseDto : returns
```

---

## 3. Diagrama de Sequência — Autenticação e Carga do Dashboard Admin

Fluxo completo de comunicação desde a entrada do usuário na `LoginWindow` até o acesso seguro à `MainWindow` consumindo a API Backend via HTTP/REST JWT.

```mermaid
sequenceDiagram
    autonumber
    actor Admin as Administrador
    participant View as LoginWindow (WPF)
    participant VM as LoginViewModel
    participant Api as ApiClient
    participant Server as Ducks.Api (Backend Azure)
    participant DB as Azure SQL Database
    participant MainView as MainWindow (WPF)

    Admin->>View: Digita e-mail e senha
    Admin->>View: Clica em "Entrar no Sistema"
    View->>VM: ExecuteLoginCommand.Execute()
    VM->>VM: Define IsBusy = True
    VM->>Api: LoginAsync(email, senha)
    Api->>Server: POST /api/auth/login { email, senha }
    Server->>DB: Consultar Usuário e Validar PasswordHash
    DB-->>Server: Retorna dados da conta
    Server-->>Api: 200 OK { token JWT, usuarioID, nomeUsuario, expiracao }
    Api-->>VM: AuthResponseDto (Sucesso = True)
    VM->>VM: Armazena Token na sessão local
    VM->>View: Dispara evento de Sucesso
    View->>MainView: Show() / Instancia MainWindow
    View->>View: Close()
    MainView->>Admin: Exibe Dashboard Geral com Métricas e Sidebar
```

---

## 4. Diagrama de Componentes (Component Diagram)

Visão geral dos pacotes, projetos e subsistemas da arquitetura Ducks e suas dependências.

```mermaid
graph TD
    subgraph "Camada Client Desktop (WPF)"
        [Ducks.Admin.Desktop.exe]
        [Views & Styles / Design System]
        [ViewModels (CommunityToolkit.Mvvm)]
        [ApiClient & SignalR Service]
    end

    subgraph "Testes Unitários Client"
        [Ducks.Admin.UnitTests.dll (xUnit & Moq)]
    end

    subgraph "Nuvem / Backend (.NET 10.0)"
        [Ducks.Api (Controllers / REST)]
        [Ducks.Application (Casos de Uso)]
        [Ducks.Domain (Entidades & Regras)]
        [Ducks.Infrastructure (EF Core)]
    end

    subgraph "Serviços Externos & Nuvem"
        [Azure SQL Database]
        [OpenAI API (ChatGPT)]
        [SignalR WebSocket Hub]
    end

    [Ducks.Admin.Desktop.exe] --> [Views & Styles / Design System]
    [Views & Styles / Design System] --> [ViewModels (CommunityToolkit.Mvvm)]
    [ViewModels (CommunityToolkit.Mvvm)] --> [ApiClient & SignalR Service]
    [Ducks.Admin.UnitTests.dll (xUnit & Moq)] ..> [ViewModels (CommunityToolkit.Mvvm)] : Valida

    [ApiClient & SignalR Service] -->|HTTPS / REST (JWT)| [Ducks.Api (Controllers / REST)]
    [ApiClient & SignalR Service] -->|WebSockets| [SignalR WebSocket Hub]
    [Ducks.Admin.Desktop.exe] -->|HTTP REST| [OpenAI API (ChatGPT)]

    [Ducks.Api (Controllers / REST)] --> [Ducks.Application (Casos de Uso)]
    [Ducks.Application (Casos de Uso)] --> [Ducks.Domain (Entidades & Regras)]
    [Ducks.Api (Controllers / REST)] --> [Ducks.Infrastructure (EF Core)]
    [Ducks.Infrastructure (EF Core)] --> [Azure SQL Database]
```