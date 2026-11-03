# Diagramas UML — Ducks Desktop Admin

Este documento apresenta os diagramas UML estruturais e comportamentais da aplicação desktop **Ducks.Desktop** (desenvolvida em WPF com o padrão MVVM, .NET 10), detalhando sua arquitetura, hierarquia de classes e fluxos de comunicação com a API.

---

## 1. Diagrama de Arquitetura (Camadas do Desktop)

A aplicação desktop adota uma separação estrita em três camadas/projetos para desacoplar a interface gráfica da lógica de negócio e consumo de serviços HTTP:

```mermaid
graph TD
    subgraph "Camada de Apresentação"
        UI["Ducks.Desktop.UI (WPF)"]
    end

    subgraph "Camada de Aplicação"
        App["Ducks.Desktop.Application (ViewModels & Services)"]
    end

    subgraph "Camada de Domínio / Core"
        Core["Ducks.Desktop.Core (Models & DTOs)"]
    end

    subgraph "Backend / API"
        API["Ducks.Api (.NET 10)"]
    end

    UI -->|Bindings & Commands| App
    App -->|Usa DTOs| Core
    App -->|Requisições HTTP / SignalR| API
```

---

## 2. Diagrama de Classes (Padrão MVVM - Gestão de Usuários)

O diagrama abaixo ilustra as principais classes e relações do módulo de Gestão de Usuários, evidenciando o uso do padrão **MVVM** e ligação de dados (*Data Binding*):

```mermaid
classDiagram
    class MainWindow {
        +ContentControl ViewModelAtual
        +NavegarUsuariosCommand
        +NavegarVisaoGeralCommand
    }

    class UserManagementView {
        +DataGrid UsuariosGrid
        +Button AtualizarListaButton
    }

    class UserManagementViewModel {
        -IUserAdminApiService _userAdminService
        +ObservableCollection~UserDto~ Usuarios
        +UserDto SelectedUsuario
        +IAsyncRelayCommand CarregarUsuariosCommand
        +IAsyncRelayCommand ToggleBlockCommand
        +CarregarUsuariosAsync()
        +ToggleBlockAsync(UserDto usuario)
    }

    class UserDto {
        +int UsuarioID
        +string NomeUsuario
        +string Email
        +string Role
        +bool IsBlocked
        +DateTime CriadoEm
    }

    class IUserAdminApiService {
        <<interface>>
        +GetUsersAsync() Task~IEnumerable~UserDto~~
        +ToggleBlockUserAsync(int id, bool block) Task~bool~
    }

    class UserAdminApiService {
        -HttpClient _httpClient
        +GetUsersAsync() Task~IEnumerable~UserDto~~
        +ToggleBlockUserAsync(int id, bool block) Task~bool~
    }

    MainWindow --> UserManagementViewModel : DataContext
    UserManagementView --> UserManagementViewModel : Binding
    UserManagementViewModel --> UserDto : Gerencia lista
    UserManagementViewModel --> IUserAdminApiService : Consome
    UserAdminApiService ..|> IUserAdminApiService : Implementa
```

---

## 3. Diagrama de Sequência (Fluxo de Bloqueio de Usuário)

Este diagrama demonstra a interação temporal entre a interface WPF, a camada de aplicação, o serviço de API cliente e o backend quando um administrador executa a ação de bloquear ou desbloquear um usuário:

```mermaid
sequenceDiagram
    autonumber
    actor Admin as Administrador
    participant View as UserManagementView (WPF)
    participant VM as UserManagementViewModel
    participant Service as UserAdminApiService
    participant API as Ducks.Api (.NET 10)
    participant DB as Azure SQL Database

    Admin->>View: Clica no botão "Alternar Bloqueio"
    View->>VM: Aciona ToggleBlockCommand (CommandParameter: UserDto)
    VM->>Service: ToggleBlockUserAsync(UsuarioID, novoStatus)
    Service->>API: PUT /api/admin/users/{id}/block?block={novoStatus} (com Token JWT)
    activate API
    API->>DB: Consulta e atualiza campo EstaAtivo = !block
    DB-->>API: Confirmação de alteração (Commit)
    API-->>Service: HTTP 200 OK ({ sucesso: true })
    deactivate API
    Service-->>VM: Retorna true (Sucesso)
    VM->>VM: Atualiza propriedade IsBlocked no UserDto local
    VM->>View: Notifica alteração de propriedade (INotifyPropertyChanged)
    View-->>Admin: Atualiza visualmente o status da tabela em tempo real
```

---

## Tecnologias e Componentes do Desktop
* **Framework:** WPF (.NET 10.0 Windows)
* **Binding & Commands:** CommunityToolkit.Mvvm (`ObservableObject`, `RelayCommand`, `ObservableProperty`)
* **Comunicação:** `IHttpClientFactory` com injeção automática de token JWT Bearer.
* **Estilização:** Clean Light Theme customizado com paleta moderna e responsiva.