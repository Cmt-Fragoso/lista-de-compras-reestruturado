# Documentação dos Componentes UI

## BaseButton
Botão base customizado com suporte a temas e estados visuais.

### Propriedades
- `Theme`: Define o tema do botão
- `ButtonStyle`: Estilo do botão (Primary, Secondary, Outline)
- `IconAlignment`: Alinhamento do ícone quando presente
- `CornerRadius`: Raio dos cantos arredondados

### Eventos
- `ThemeChanged`: Disparado quando o tema é alterado
- `StyleChanged`: Disparado quando o estilo é alterado

### Exemplo de Uso
```csharp
var button = new BaseButton 
{
    Text = "Salvar",
    ButtonStyle = ButtonStyle.Primary,
    CornerRadius = 4
};
```

## BaseChart
Componente de gráfico com suporte a diferentes tipos de visualização.

### Tipos de Gráfico
- Linha
- Barra
- Área

### Recursos
- Zoom interativo
- Tooltips
- Média móvel
- Indicadores de min/max
- Variação percentual
- Responsivo a diferentes tamanhos

### Exemplo de Uso
```csharp
var chart = new BaseChart 
{
    ChartType = "Linha",
    ShowGrid = true,
    EnableZoom = true
};
chart.SetData(new List<double> { 1, 2, 3, 4, 5 });
```

## PriceChart
Especialização do BaseChart para visualização de preços.

### Recursos Específicos
- Média Móvel Aritmética (MMA)
- Indicador de Volatilidade
- Formatação monetária
- Comparação de períodos

### Exemplo de Uso
```csharp
var priceChart = new PriceChart();
priceChart.SetPriceData(prices, dates, volumes);
priceChart.ShowMovingAverage = true;
priceChart.MovingAveragePeriod = 5;
```

## IconButton
Extensão do BaseButton com suporte a ícones.

### Propriedades
- `IconName`: Nome do ícone a ser exibido
- `IconSize`: Tamanho do ícone em pixels
- `IconOnly`: Se deve mostrar apenas o ícone

### Exemplo de Uso
```csharp
var saveButton = new IconButton 
{
    IconName = "save",
    Text = "Salvar",
    IconSize = 16
};
```

## IconManager
Gerenciador centralizado de ícones da aplicação.

### Funcionalidades
- Cache de ícones
- Suporte a múltiplos tamanhos
- Geração vetorial
- Adaptação a temas

### Exemplo de Uso
```csharp
var icon = IconManager.Instance.GetIcon("save", 16);
```

## ResponsiveManager
Gerenciador de responsividade para formulários e controles.

### Recursos
- Adaptação a diferentes resoluções
- Suporte a DPI variados
- Redimensionamento fluido
- Posicionamento relativo

### Exemplo de Uso
```csharp
public partial class MainForm : Form 
{
    public MainForm() 
    {
        InitializeComponent();
        ResponsiveManager.Instance.RegisterForm(this);
    }
}
```

## Guia de Uso

### Inicialização
1. Registre o formulário no ResponsiveManager
2. Configure os controles base conforme necessidade
3. Utilize IconManager para ícones consistentes

### Boas Práticas
- Use controles base para consistência visual
- Implemente responsividade desde o início
- Mantenha padrões de espaçamento
- Siga as diretrizes de temas

### Dicas de Performance
- Use virtualização para listas longas
- Implemente lazy loading quando apropriado
- Otimize operações de rendering
- Cache dados frequentemente usados