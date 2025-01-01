# Documentação de Componentes UI

## Componentes Base

### BaseButton
Button personalizado com temas e estados visuais.
```csharp
var btn = new BaseButton { Text = "Confirmar" };
```

### BaseListView
ListView virtualizado e otimizado para grandes conjuntos de dados.
```csharp
var list = new BaseListView();
await list.LoadDataAsync(items);
```

### BaseDataGrid
DataGridView com virtualização, cache e ordenação otimizada.
```csharp
var grid = new BaseDataGrid();
grid.AddColumn("Nome", "name", 200);
```

### BaseChart/PriceChart
Gráficos otimizados com suporte a temas e animações.
```csharp
var chart = new PriceChart();
chart.SetPriceData(prices, dates);
```

## Serviços

### IconManager
```csharp
var icon = IconManager.Instance.GetIcon("save", 16);
btn.Image = icon;
```

### PerformanceOptimizer
```csharp
PerformanceOptimizer.OptimizeForm(this);
await PerformanceOptimizer.UpdateControlAsync(control, () => {
    // Atualizações pesadas
});
```

### ResponsiveManager
```csharp
ResponsiveManager.Instance.RegisterForm(this);
ResponsiveManager.Instance.SetScaleLimits(this, 0.8f, 1.5f);
```

## Padrões de Design

### Layout
- Margins: 8px, 16px, 24px
- Padding: 8px, 16px
- Grid: 8px base unit

### Cores (Dark/Light)
- Primary: #007AFF
- Surface: #FFFFFF/#1C1C1E
- Text: #000000/#FFFFFF

### Tipografia
- Font: Segoe UI
- Sizes: 12px, 14px, 16px, 20px
- Weights: Regular(400), Medium(500), Bold(700)

### Temas
- Cores configuráveis por tema
- Transições suaves (300ms)
- Estados: Normal, Hover, Active, Disabled

### Responsividade
- Breakpoints: 800px, 1200px, 1600px
- DPI aware scaling
- Layout fluido

## Performance
1. Virtualização automática (>1000 itens)
2. Double buffering otimizado
3. Cache de ícones e layouts
4. Atualizações assíncronas
5. Garbage collection controlado