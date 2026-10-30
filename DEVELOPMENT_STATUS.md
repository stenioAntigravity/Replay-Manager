# Development Status: Replay Manager

**Última Atualização**: 2026-10-30 15:25 (Fase 0 Concluída)
**Versão Atual**: 0.1.0-alpha.0

---

## 1. Visão Geral do Progresso

| Fase | Descrição | Status | Progresso |
| :--- | :--- | :--- | :--- |
| **Fase 0** | Preparação do Ambiente e Estrutura Inicial | **Concluída** | 100% |
| **Fase 1** | Fundação e Persistência SQLite | **Próxima** | 0% |
| **Fase 2** | Monitoramento de Pastas e Ingestão | Pendente | 0% |
| **Fase 3** | Integração FFmpeg e Miniaturas | Pendente | 0% |
| **Fase 4** | Biblioteca de Vídeos e Interface WPF | Pendente | 0% |
| **Fase 5** | Motor de Filtros e Automação PUBG | Pendente | 0% |
| **Fase 6** | Integração YouTube API v3 | Pendente | 0% |
| **Fase 7** | Fila Persistente e Resiliente de Upload | Pendente | 0% |
| **Fase 8** | Backup no Google Drive | Pendente | 0% |
| **Fase 9** | Bandeja, Inicialização e Acabamento | Pendente | 0% |
| **Fase 10** | Validação, Estresse e Empacotamento | Pendente | 0% |

---

## 2. Detalhamento do Estado Atual (Fase 0 - 100% Concluída)

### Módulos Concluídos
- [x] Avaliação detalhada dos requisitos funcionais, técnicos e regras de continuidade.
- [x] Identificação de riscos técnicos (UAC em instaladores, limitação de APIs e concorrência).
- [x] Instalação do .NET 10 SDK (v10.0.403) via script oficial `dotnet-install.ps1` no perfil de usuário.
- [x] Configuração de variáveis de ambiente (`DOTNET_ROOT` e `PATH`).
- [x] Inicialização do repositório Git com branch `main` e `.gitignore` completo.
- [x] Criação da solução `ReplayManager.slnx` contendo 5 projetos:
  - `src/ReplayManager.Domain` (net10.0)
  - `src/ReplayManager.Application` (net10.0)
  - `src/ReplayManager.Infrastructure` (net10.0-windows)
  - `src/ReplayManager.App` (WPF, net10.0-windows)
  - `tests/ReplayManager.Tests` (xUnit, net10.0-windows)
- [x] Estabelecimento do grafo de dependências entre projetos sem dependências circulares.
- [x] Compilação da solução com sucesso (0 erros, 0 avisos).
- [x] Execução de testes de fumaça (xUnit) com 100% de aprovação.
- [x] Documentação de continuidade viva e completa (`PROJECT_CONTEXT.md`, `ARCHITECTURE.md`, `IMPLEMENTATION_PLAN.md`, `DECISIONS.md`, `TESTING.md`, `SESSION_LOG.md`, `CHANGELOG.md`, `NEXT_SESSION.md`).

### Módulos Em Andamento / Próxima Sessão
- **Fase 1 — Fundação e Persistência SQLite**:
  - Modelagem das entidades de Domínio no `ReplayManager.Domain`.
  - Instalação e configuração do EF Core SQLite e migrações no `ReplayManager.Infrastructure`.
  - Implementação do isolamento de deduplicação por pasta.
  - Testes unitários e de integração de persistência no `ReplayManager.Tests`.

---

## 3. Problemas Conhecidos e Mitigações

1. **Tentativa de instalação via Winget**: Falhou com código 50 por exigir elevação UAC.
   - *Status*: Resolvido via `dotnet-install.ps1` user-level.
2. **Incompatibilidade de TFM no projeto de testes**: Inicialmente o projeto de testes estava em `net10.0` e `ReplayManager.Infrastructure` em `net10.0-windows`.
   - *Status*: Resolvido ajustando `ReplayManager.Tests` para `net10.0-windows`.
3. **Disponibilidade do FFmpeg**: FFmpeg global não está no PATH.
   - *Ação*: Na Fase 3 será provido provisionamento automático / gerenciamento de binário local.

---

## 4. Última Etapa Validada
- `dotnet build ReplayManager.slnx` (0 avisos, 0 erros) e `dotnet test ReplayManager.slnx` (100% aprovado).

## 5. Próxima Tarefa Exata
- Iniciar a **Fase 1**: criar as classes de entidade do domínio (`MonitoredFolder`, `FolderFilterRule`, `VideoRecord`, `UploadQueueItem`, `YouTubeAccount`, `DescriptionTemplate`) e configurar o `ReplayManagerDbContext` com Fluent API no `ReplayManager.Infrastructure`.
