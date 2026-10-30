# Changelog: Replay Manager

Todas as alterações notáveis neste projeto serão documentadas neste arquivo.
O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Semantic Versioning](https://semver.org/lang/pt-BR/).

---

## [0.2.0-alpha.1] - 2026-10-30 (Fase 1)

### Added
- Entidades de Domínio no projeto `ReplayManager.Domain`:
  - `MonitoredFolder`, `FolderFilterRule`, `VideoRecord`, `UploadQueueItem`, `YouTubeAccount`, `DescriptionTemplate`, `AppSetting`.
  - Enums: `LocalFileStatus`, `UploadStatus`, `YouTubePrivacyStatus`, `FilterEvaluationStatus`.
- Persistência com SQLite e EF Core 10.0.14 em `ReplayManager.Infrastructure`:
  - `ReplayManagerDbContext` com Fluent API completa.
  - Regra de ouro da Seção 5.1 e ADR 003: índice único composto `(MonitoredFolderId, ContentSha256)` para deduplicação exclusivamente intra-pasta.
  - Índices otimizados para consultas de biblioteca (`UploadStatus`, `LocalStatus`, `FilterStatus`, `RecordedAtUtc`) e ordenação da fila (`Status`, `Priority`, `QueuedAtUtc`).
  - `DatabaseInitializer` com configuração de WAL mode, synchronous=NORMAL, foreign_keys=ON e seeding de dados padrão.
- Contratos de persistência em `ReplayManager.Application`:
  - `IVideoRecordRepository`, `IMonitoredFolderRepository`, `IUploadQueueRepository`, `IUnitOfWork`.
- Implementações de repositório em `ReplayManager.Infrastructure`:
  - `VideoRecordRepository`, `MonitoredFolderRepository`, `UploadQueueRepository`, `UnitOfWork`.
  - Método de recuperação resiliente `ResetActiveUploadsToInterruptedAsync()`.
- Extensão `AddInfrastructurePersistence` para injeção de dependência.
- Suíte de testes de integração com SQLite InMemory em `ReplayManager.Tests`:
  - Testes de integridade de deduplicação intra e inter-pastas, cascade delete, preservação de registros locais ausentes, consultas paginadas e recuperação pós-crash.

### Fixed
- Ambiguidade entre `System.Windows.Application` do WPF e o namespace `ReplayManager.Application` no arquivo `App.xaml.cs`.

---

## [0.1.0-alpha.0] - 2026-10-30 (Fase 0)

### Added
- Configuração inicial do repositório Git com branch `main` e `.gitignore`.
- Instalação e provisionamento do .NET 10 SDK (v10.0.403).
- Documentação inicial de governança e continuidade:
  - `PROJECT_CONTEXT.md`
  - `ARCHITECTURE.md`
  - `IMPLEMENTATION_PLAN.md`
  - `DEVELOPMENT_STATUS.md`
  - `DECISIONS.md`
  - `TESTING.md`
  - `SESSION_LOG.md`
  - `NEXT_SESSION.md`
- Solução `ReplayManager.slnx` com 5 projetos modulares compilando sem erros.
