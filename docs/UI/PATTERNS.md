# Padrões de UI

## Padrões de Formulário

### Validação
- Validação em tempo real
- Mensagens de erro claras e próximas ao campo
- Indicadores visuais de estado (sucesso/erro)
- Prevenção de submissão com erros

### Layout
- Labels acima dos campos
- Agrupamento lógico de campos
- Ordem de tab adequada
- Botões de ação no final do formulário

### Máscaras e Formatação
- Formatação automática de números
- Máscara para campos especiais (telefone, CPF)
- Conversão de unidades em tempo real
- Sugestões de preenchimento

## Padrões de Lista

### Virtualização
```csharp
public class VirtualizedListView : ListView
{
    private const int ItemHeight = 44;
    private int VisibleItems => Height / ItemHeight;
    private List<T> AllItems = new();
    
    protected override void OnPaint(PaintEventArgs e)
    {
        var startIndex = Math.Max(0, VerticalScroll.Value / ItemHeight);
        var endIndex = Math.Min(AllItems.Count, startIndex + VisibleItems + 1);
        
        for (int i = startIndex; i < endIndex; i++)
        {
            DrawItem(e.Graphics, i);
        }
    }
}
```

### Paginação
- 20 itens por página por padrão
- Controles de navegação nas extremidades
- Indicador de página atual
- Opção de itens por página

### Filtragem e Ordenação
- Filtros rápidos pré-definidos
- Busca em tempo real
- Ordenação por qualquer coluna
- Indicadores visuais de ordem

## Padrões de Navegação

### Menu Principal
- Itens mais usados primeiro
- Máximo 7 itens principais
- Submenus quando necessário
- Ícones consistentes

### Breadcrumbs
- Sempre visível em hierarquias profundas
- Último item não clicável
- Separador visual claro
- Truncamento inteligente

### Tabs
- Máximo 6 tabs visíveis
- Scroll horizontal se necessário
- Indicador de tab ativa
- Conteúdo mantido em memória

## Padrões de Feedback

### Loading States
```csharp
public class LoadingState : UserControl
{
    private Timer _loadingTimer;
    private int _dots = 0;
    
    public LoadingState()
    {
        _loadingTimer = new Timer { Interval = 500 };
        _loadingTimer.Tick += (s, e) =>
        {
            _dots = (_dots + 1) % 4;
            UpdateText();
        };
    }
    
    private void UpdateText()
    {
        loadingLabel.Text = $"Carregando{new string('.', _dots)}";
    }
}
```

### Mensagens de Erro
- Mensagens claras e acionáveis
- Sugestões de resolução
- Opção de retry quando aplicável
- Log detalhado para debug

### Confirmações
- Feedback visual imediato
- Mensagens temporárias (toast)
- Confirmação para ações destrutivas
- Opção de desfazer quando possível

## Padrões de Performance

### Lazy Loading
```csharp
public class LazyLoadingImage : PictureBox
{
    private string _imagePath;
    private bool _isLoading;
    
    public async Task LoadImageAsync(string path)
    {
        if (_isLoading) return;
        _isLoading = true;
        
        try
        {
            using var stream = await File.OpenReadAsync(path);
            Image = await Task.Run(() => Image.FromStream(stream));
            _imagePath = path;
        }
        finally
        {
            _isLoading = false;
        }
    }
}
```

### Cache
- Cache de imagens em memória
- Cache de dados frequentes
- Limpeza periódica
- Política de expiração

### Otimização de Rendering
- Double buffering em controles pesados
- Throttling de eventos de resize
- Debounce em buscas
- Uso de região para repaint

## Padrões de Responsividade

### Layout Adaptativo
```csharp
public class AdaptivePanel : Panel
{
    private Size _breakpoint = new Size(800, 600);
    private LayoutMode _currentMode = LayoutMode.Normal;
    
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        
        var newMode = Width < _breakpoint.Width 
            ? LayoutMode.Compact 
            : LayoutMode.Normal;
            
        if (newMode != _currentMode)
        {
            _currentMode = newMode;
            ReorganizeLayout();
        }
    }
    
    private void ReorganizeLayout()
    {
        SuspendLayout();
        // Ajusta layout baseado no modo
        ResumeLayout();
    }
}
```

### Redimensionamento Fluido
- Uso de unidades relativas
- Limites mínimos e máximos
- Breakpoints suaves
- Aspect ratio preservado

### DPI Awareness
- Detecção automática de DPI
- Escala de elementos UI
- Assets em múltiplas resoluções
- Ajuste de fontes