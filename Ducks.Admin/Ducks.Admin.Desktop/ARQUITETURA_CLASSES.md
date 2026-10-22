# Arquitetura de Classes — Ducks.Admin.Desktop

Este documento descreve o mapeamento completo de classes, DTOs, ViewModels, Services, Stores e Views do aplicativo desktop administrativo.

## Estrutura de Pastas e Mapeamento de Arquivos

### 1. Models/ (DTOs e Contratos de Dados)
- **`Models/UsuarioAdminDto.cs`**: Representa os dados do usuário/autor e o resultado paginado.
- **`Models/AuthResponseDto.cs`**: Contém o token JWT e a mensagem de autenticação.
- **`Models/TrilhaAdminDto.cs`**: Representa as trilhas de aprendizagem e requisições de criação.
- **`Models/ConquistaAdminDto.cs`**: Representa as medalhas, conquistas e requisitos de XP.
- **`Models/AiDto.cs`**: DTOs para envio de prompts e respostas do assistente de IA.
- **`Models/AuditLogDto.cs`**: Representa os logs de auditoria do sistema.

### 2. Services/ (Comunicação HTTP / REST & Real-time)
- **`Services/IAuthApiService.cs`**: Interface Refit para autenticação (`/api/auth/login`).
- **`Services/IAdminApiService.cs`**: Interface Refit unificada para gerenciamento de usuários, moderação, trilhas, conquistas, IA e auditoria.
- **`Services/AdminRealtimeService.cs`**: Gerenciador SignalR para recebimento de métricas em tempo real e logs.

### 3. Stores/ (Gerenciamento de Estado Global)
- **`Stores/NavigationStore.cs`**: Gerencia o estado reativo da tela atual (`CurrentViewModel`).
- **`Stores/UserSessionStore.cs`**: Mantém o token JWT, e-mail do administrador e estado de sessão.

### 4. ViewModels/ (Lógica de Apresentação - MVVM)
- **`ViewModels/MainViewModel.cs`**: ViewModel principal coordenando a janela e a navegação.
- **`ViewModels/LoginViewModel.cs`**: Lógica da tela de login e validação de credenciais.
- **`ViewModels/DashboardViewModel.cs`**: Lógica do painel inicial de boas-vindas.
- **`ViewModels/UserManagementViewModel.cs`**: Lógica de listagem, paginação, filtros e bloqueio/desbloqueio de usuários.
- **`ViewModels/ContentGamificationViewModel.cs`**: Lógica para gestão de trilhas educacionais e regras de XP/Conquistas.
- **`ViewModels/AiAssistantViewModel.cs`**: Lógica de integração com IA, métricas via SignalR e auditoria.

### 5. Views/ (Interfaces Gráficas XAML)
- **`Views/LoginView.xaml`**: Tela de autenticação corporativa.
- **`Views/MainDashboardView.xaml`**: Visão geral do painel.
- **`Views/UserManagementView.xaml`**: Tabela de alta performance para moderação de usuários.
- **`Views/ContentGamificationView.xaml`**: Layout em abas para trilhas e gamificação.
- **`Views/AiAssistantView.xaml`**: Painel do assistente de IA, métricas e logs.

### 6. Resources/ (Design System)
- **`Resources/Colors.xaml`**: Paleta oficial Ducks (Amarelo, Roxo, Azul, Fundo escuro).
- **`Resources/Styles.xaml`**: Estilos globais para TextBox, Button, TextBlock e animações.