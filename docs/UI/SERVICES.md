# Serviços da UI

## DialogService
Gerencia diálogos e interações com o usuário.

### Funcionalidades
- Mensagens padronizadas
- Diálogos de confirmação
- Seleção de arquivos e pastas
- Tratamento de erros

### Exemplo de Uso
```csharp
public class ExampleForm 
{
    private readonly IDialogService _dialogService;
    
    public void SaveData()
    {
        if (_dialogService.ShowQuestion("Deseja salvar as alterações?") == DialogResult.Yes)
        {
            // Salvar dados
        }
    }
}
```

## NotificationService
Sistema de notificações toast e alertas.

### Recursos
- Notificações temporárias
- Diferentes tipos (Info, Success, Warning, Error)
- Suporte a ações
- Gerenciamento de pilha de notificações

### Exemplo de Uso
```csharp
_notificationService.ShowNotification("Dados salvos com sucesso!", NotificationType.Success);
```

## NavigationService
Controle de navegação entre telas.

### Funcionalidades
- Navegação com histórico
- Suporte a voltar
- Gerenciamento de estado
- Navegação para main

### Exemplo de Uso
```csharp
_navigationService.NavigateTo<ListasViewModel>();
if (_navigationService.CanNavigateBack)
    _navigationService.NavigateBack();
```

## StateService
Gerenciamento de estado da aplicação.

### Recursos
- Estado global e por tela
- Cache de dados
- Limpeza automática
- Estado temporário

### Exemplo de Uso
```csharp
_stateService.SetState("filtros", filtrosAtivos);
var estado = _stateService.GetState<FiltrosModel>("filtros");
```

## ThemeService
Gerenciamento de temas e aparência.

### Funcionalidades
- Temas claro/escuro
- Cores customizadas
- Fontes e tamanhos
- Aplicação dinâmica

### Exemplo de Uso
```csharp
_themeService.SetTheme(isDark: true);
_themeService.CustomizeTheme(new ThemeSettingsModel 
{
    FontSize = 12f,
    FontFamily = "Segoe UI"
});
```

## ValidationService
Validação de dados e formulários.

### Recursos
- Validação em tempo real
- Mensagens de erro
- Indicadores visuais
- Regras customizáveis

### Exemplo de Uso
```csharp
var validation = _validationService.Validate(model);
if (!validation.IsValid)
{
    _validationService.ApplyValidation(control, validation);
}
```

## Backup e Exportação

### BackupService
- Backup automático
- Compressão de dados
- Restauração
- Histórico

### ExportService
- Múltiplos formatos
- Layouts customizáveis
- Gráficos e estatísticas
- Dados selecionáveis

## Boas Práticas

### Injeção de Dependência
```csharp
public class MainForm
{
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private readonly INavigationService _navigationService;
    private readonly IStateService _stateService;
    private readonly IThemeService _themeService;
    private readonly IValidationService _validationService;

    public MainForm(
        IDialogService dialogService,
        INotificationService notificationService,
        INavigationService navigationService,
        IStateService stateService,
        IThemeService themeService,
        IValidationService validationService)
    {
        _dialogService = dialogService;
        _notificationService = notificationService;
        _navigationService = navigationService;
        _stateService = stateService;
        _themeService = themeService;
        _validationService = validationService;
    }
}
```

### Cache e Performance
- Cache de dados frequentes
- Lazy loading
- Virtualização de listas
- Throttling de eventos

### Tratamento de Erros
- Logging centralizado
- Mensagens amigáveis
- Retry automático
- Fallback gracioso

## Arquitetura

### MVVM
- ViewModels para cada tela
- Notificação de mudanças
- Comandos
- Validação

### Serviços
- Interfaces bem definidas
- Injeção de dependência
- Single Responsibility
- Testabilidade

### Temas
- Consistência visual
- Adaptação dinâmica
- Customização
- Acessibilidade

## Performance

### Otimizações
- Virtualização
- Lazy loading
- Cache em memória
- Async/await

### Monitoramento
- Logging
- Métricas
- Diagnóstico
- Alertas

## Segurança

### Dados
- Validação de input
- Sanitização
- Backup seguro
- Logs de auditoria

### UI
- Controle de acesso
- Timeout de sessão
- Proteção XSS
- CSRF tokens
