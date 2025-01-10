# Models e ViewModels

## Models

### ConfigModel
Model para configurações da aplicação.

```csharp
public class ConfigModel
{
    public string FormatoExportacao { get; set; }
    public bool IncluirEstatisticas { get; set; }
    public bool TemaEscuro { get; set; }
    public string PastaBackup { get; set; }
    // ...
}
```

### UIStateModel
Gerencia estado da interface.

```csharp
public class UIStateModel
{
    public bool IsLoading { get; set; }
    public string LoadingMessage { get; set; }
    public Dictionary<string, object> ViewState { get; set; }
}
```

### ThemeSettingsModel
Configurações de tema.

```csharp
public class ThemeSettingsModel
{
    public string ThemeName { get; set; }
    public float FontSize { get; set; }
    public Dictionary<string, Color> CustomColors { get; set; }
}
```

## ViewModels Base

### ViewModelBase
Base para todos ViewModels.

```csharp
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null);
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null);
}
```

### MainWindowViewModel
ViewModel principal.

```csharp
public class MainWindowViewModel : ViewModelBase
{
    public ViewModelBase CurrentView { get; set; }
    public bool IsLoading { get; set; }
    public string StatusMessage { get; set; }
}
```

## ViewModels de Negócio

### ListaViewModel
ViewModel para listas de compras.

```csharp
public class ListaViewModel : ViewModelBase
{
    public string Nome { get; set; }
    public DateTime DataAtualizacao { get; set; }
    public bool IsConcluida { get; set; }
    public List<ItemModel> Itens { get; set; }
    public decimal ValorTotal { get; set; }
}
```

### ItemViewModel
ViewModel para itens.

```csharp
public class ItemViewModel : ViewModelBase
{
    public string Nome { get; set; }
    public decimal Quantidade { get; set; }
    public decimal Preco { get; set; }
    public string Categoria { get; set; }
    public bool IsComprado { get; set; }
}
```

### PrecoViewModel
ViewModel para preços.

```csharp
public class PrecoViewModel : ViewModelBase
{
    public decimal Valor { get; set; }
    public string Local { get; set; }
    public DateTime Data { get; set; }
    public decimal Variacao { get; set; }
}
```

## ViewModels de Funcionalidades

### BackupViewModel
ViewModel para backup.

```csharp
public class BackupViewModel : ViewModelBase
{
    public List<BackupInfo> Backups { get; set; }
    public bool IsBackupRunning { get; set; }
    public int Progress { get; set; }
}
```

### ExportViewModel
ViewModel para exportação.

```csharp
public class ExportViewModel : ViewModelBase
{
    public string SelectedFormat { get; set; }
    public string OutputPath { get; set; }
    public List<string> SelectedFields { get; set; }
}
```

## Boas Práticas

### Notificações
- Implementar INotifyPropertyChanged
- Usar SetField para mudanças
- OnPropertyChanged para atualizações
- Notificar dependências

### Validação
- DataAnnotations
- INotifyDataErrorInfo
- Validação em tempo real
- Feedback visual

### Estado
- Estado mínimo necessário
- Propriedades calculadas
- Cache inteligente
- Limpeza automática

### Performance
- Lazy loading
- Virtualização
- Paginação
- Cache

## Padrões

### MVVM
- Separação de responsabilidades
- Binding bidirecional
- Comandos
- Conversores

### Factory
```csharp
public static class ViewModelFactory
{
    public static ListaViewModel CreateListaViewModel(ListaModel model);
    public static ItemViewModel CreateItemViewModel(ItemModel model);
}
```

### Observer
- PropertyChanged
- CollectionChanged
- ErrorsChanged
- StatusChanged

### Command
```csharp
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool> _canExecute;
    
    public RelayCommand(Action execute, Func<bool> canExecute = null);
}
```
