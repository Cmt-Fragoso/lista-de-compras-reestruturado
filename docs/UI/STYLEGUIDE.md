# Guia de Estilos UI

## Cores

### Paleta Principal
- Primary: #007AFF
- Secondary: #5856D6
- Success: #34C759
- Warning: #FF9500
- Error: #FF3B30
- Info: #5AC8FA

### Tons de Cinza
- Background: #FFFFFF
- Surface: #F2F2F7
- Border: #C6C6C8
- TextPrimary: #000000
- TextSecondary: #8E8E93

### Variações para Tema Escuro
- Background: #000000
- Surface: #1C1C1E
- Border: #38383A
- TextPrimary: #FFFFFF
- TextSecondary: #8E8E93

## Tipografia

### Famílias de Fonte
- Principal: Segoe UI
- Secundária: Roboto
- Monoespaçada: Consolas

### Tamanhos
- Título 1: 24px
- Título 2: 20px
- Título 3: 16px
- Corpo: 14px
- Small: 12px
- Caption: 10px

### Pesos
- Regular: 400
- Medium: 500
- Bold: 700

## Espaçamento

### Grid Base
- Unidade base: 8px
- Padding padrão: 16px
- Margem entre elementos: 8px
- Margem entre seções: 24px

### Layout
- Largura máxima de container: 1200px
- Gutters laterais: 24px
- Breakpoints responsivos:
  - Mobile: < 768px
  - Tablet: 768px - 1024px
  - Desktop: > 1024px

## Componentes

### Botões
- Altura: 32px
- Padding horizontal: 16px
- Border radius: 6px
- Espaçamento entre ícone e texto: 8px

### Campos de Entrada
- Altura: 32px
- Padding horizontal: 12px
- Border radius: 6px
- Border width: 1px

### Cards
- Padding: 16px
- Border radius: 8px
- Shadow: 0 2px 4px rgba(0,0,0,0.1)

### Listas
- Item height: 44px
- Padding vertical: 12px
- Padding horizontal: 16px
- Divider height: 1px

## Ícones

### Tamanhos
- Small: 16px
- Medium: 24px
- Large: 32px

### Estilos
- Line weight: 1.5px
- Corner radius: 2px
- Padding interno: 2px

## Animações

### Duração
- Muito rápida: 100ms
- Rápida: 200ms
- Normal: 300ms
- Lenta: 400ms
- Muito lenta: 500ms

### Curvas de Easing
- Linear: linear
- Suave: ease
- Entrada: ease-in
- Saída: ease-out
- Entrada/Saída: ease-in-out

## Responsividade

### Breakpoints
- Mobile S: 320px
- Mobile M: 375px
- Mobile L: 425px
- Tablet: 768px
- Laptop: 1024px
- Laptop L: 1440px
- 4K: 2560px

### Regras de Adaptação
- Layout fluido entre breakpoints
- Fontes responsivas (min: 14px, max: 16px)
- Imagens e ícones com escala mantendo proporção
- Margens e paddings proporcionais

### Grid System
- Container máximo: 1200px
- Colunas: 12
- Gutters: 24px
- Breakpoints de grade:
  - XS: < 576px (1 coluna)
  - SM: ≥ 576px (2 colunas)
  - MD: ≥ 768px (3 colunas)
  - LG: ≥ 992px (4 colunas)
  - XL: ≥ 1200px (6 colunas)

## Acessibilidade

### Contraste
- Texto normal: mínimo 4.5:1
- Texto grande: mínimo 3:1
- Componentes interativos: mínimo 3:1

### Foco
- Outline visível: 2px solid Primary
- Offset: 2px
- Radius: igual ao componente

### Estados Interativos
- Normal: cor base
- Hover: 10% mais escuro
- Pressed: 20% mais escuro
- Focused: outline + highlight
- Disabled: 50% opacidade

## DPI e Escala

### Regras de Escala
- Base DPI: 96 (100%)
- Suporte até 400% (384 DPI)
- Ícones vetoriais quando possível
- Imagens @2x e @3x para raster

### Ajustes Automáticos
- Tamanho de fonte
- Espessura de bordas
- Tamanho de ícones
- Espaçamentos

## Consistência Visual

### Elevação
- Nível 0: sem sombra
- Nível 1: 0 2px 4px rgba(0,0,0,0.1)
- Nível 2: 0 4px 8px rgba(0,0,0,0.1)
- Nível 3: 0 8px 16px rgba(0,0,0,0.1)
- Nível 4: 0 16px 24px rgba(0,0,0,0.1)

### Estados de Cartões
- Repouso: Nível 1
- Hover: Nível 2
- Ativo: Nível 3
- Modal: Nível 4

### Hierarquia Visual
1. Ações principais (CTAs)
2. Informações críticas
3. Conteúdo principal
4. Navegação
5. Informações secundárias
6. Elementos decorativos