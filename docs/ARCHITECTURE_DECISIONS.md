# Decisões Arquiteturais

## Tecnologias Escolhidas

### C# (.NET 8)
- Maturidade da plataforma
- Performance nativa
- Ferramentas robustas
- Bom suporte Windows

### Windows Forms
- Interface nativa
- Performance excelente
- Controles ricos
- Fácil manutenção

### SQLite
- Banco local
- Fácil sincronização
- Zero configuração
- Confiável

### P2P para Rede
- Sem servidor central
- Rede local apenas
- Protocolo leve
- Fácil manutenção

## Decisões Estruturais

### Modular
- Componentes isolados
- Baixo acoplamento
- Alta coesão
- Fácil teste

### Cache Multinível
- Memória para ativo
- Disco para histórico
- SQLite para permanente

### UI Responsiva
- Background workers
- Async operations
- UI thread livre
- Feedback contínuo