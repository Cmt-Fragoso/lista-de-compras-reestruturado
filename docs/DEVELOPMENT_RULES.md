# Regras de Desenvolvimento

## Responsabilidades dos Chats

### Chat 1 - Arquiteto
- Manutenção da estrutura
- Aprovação de mudanças arquiteturais
- Documentação base
- Garantia de padrões

### Chat 2 - Desenvolvedor Core
- Implementação do Core
- Lógica de negócio
- Testes unitários
- Otimizações

### Chat 3 - Especialista UI
- Interface Windows Forms
- Componentes visuais
- Temas e estilos
- UX/UI

### Chat 4 - Engenheiro de Rede
- Sistema P2P
- Sincronização
- Protocolo de rede
- Cache distribuído

### Chat 5 - QA e Documentação
- Testes
- Documentação técnica
- Logs e monitoramento
- Validação de qualidade

## Fluxo de Trabalho
1. Propor mudanças
2. Aguardar aprovação do Arquiteto
3. Implementar após aprovação
4. Documentar alterações
5. Passar por revisão

## Regras de Commits
- Prefixo [ARCH] para Arquiteto
- Prefixo [CORE] para Core
- Prefixo [UI] para Interface
- Prefixo [NET] para Rede
- Prefixo [DOC] para Documentação

## Políticas de Branches
- main: produção
- develop: desenvolvimento
- feature/*: novas features
- bugfix/*: correções
- release/*: preparação