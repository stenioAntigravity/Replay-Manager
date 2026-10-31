# Changelog: Replay Manager

Todas as alterações notáveis neste projeto serão documentadas neste arquivo.
O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Semantic Versioning](https://semver.org/lang/pt-BR/).

---

## [0.6.0-alpha.5] - 2026-10-31 (Fase 5)

### Added
- **Motor de Filtros de Pastas Monitoradas**:
  - `IFilterEvaluator` e `FilterEvaluator` em `ReplayManager.Application`: avaliação determinística de critérios técnicos e de nomenclatura.
  - Validação de extensões permitidas com suporte a lista delimitada por ponto e vírgula.
  - Validação de faixa de tamanho: tamanho mínimo (default 10 MB) e tamanho máximo (default 100 MB com toggle para desativar).
  - Validação de duração mínima e máxima em segundos.
  - Validação de expressões regulares (`Regex`) aplicadas ao nome do arquivo.
  - Validação de profundidade de diretório (`IncludeSubdirectories`).
  - DTO de resultado detalhado `FilterEvaluationResult` com justificativas claras gravadas no `VideoRecord.RejectionReason`.
- **Automação de Enfileiramento PUBG**:
  - Integração do motor de filtros no fluxo de pós-processamento de mídia em `VideoMediaProcessor`.
  - Enfileiramento automático (`AutoUploadEnabled`) para vídeos aprovados com criação transparente de item na fila de uploads (`UploadQueueItem`).
- **Suíte de Testes Automatizados**:
  - 11 novos testes automatizados (9 unitários para `FilterEvaluatorTests` e 2 de integração para `VideoMediaProcessorAutomationTests`), totalizando 69 testes aprovados (100% de sucesso).

---

## [0.5.0-alpha.4] - 2026-10-31 (Fase 4)

### Added
- **Arquitetura MVVM e Hospedagem Desktop**:
  - Integração do `CommunityToolkit.Mvvm` 8.4.2 e `Microsoft.Extensions.Hosting` 10.0.14 no projeto `ReplayManager.App`.
  - Configuração do ciclo de vida em `App.xaml.cs` com inicialização do banco SQLite, migrações/seeding e injeção de dependências completa.
- **Casca Principal e Navegação Dinâmica**:
  - `MainWindow.xaml` e `MainViewModel`: interface em Dark Theme com abas superiores (`Biblioteca`, `Fila de Upload`, `Pastas Monitoradas`, `Configurações`) e controle de navegação em abas.
- **Biblioteca de Vídeos**:
  - `LibraryView.xaml` e `LibraryViewModel`: grade de miniaturas com virtualização, busca em tempo real por nome/título, filtros rápidos por status de upload e arquivos ausentes no disco.
  - `VideoCardViewModel`: badges coloridos por status (`Local`, `Na Fila`, `Enviado`, `Arquivo Ausente`), formatação de duração (`MM:SS`), tamanho legível e resoluções.
  - Conversores XAML: `ImagePathToBitmapConverter` seguro sem retenção de file locks (`BitmapCacheOption.OnLoad`) e `EnumToBooleanConverter`.
- **Painel Lateral de Detalhes e Ações**:
  - Edição inline de título e descrição para YouTube com persistência direta no SQLite.
  - `MediaPlayerLauncher` e `IMediaPlayerLauncher`: detecção automática do VLC instalado no registro ou diretórios comuns, com fallback inteligente para o player padrão do Windows.
  - `WpfDialogService` e `IDialogService`: caixas de mensagem e diálogo nativo de seleção de arquivos de imagem.
  - Ação de enfileiramento rápido para upload e substituição manual de miniaturas.
- **Suíte de Testes Automatizados**:
  - Adição de 5 testes em `LibraryViewModelTests` cobrindo carregamento, buscas, filtros de status, edição de metadados e bloqueio de reprodução para arquivos ausentes (totalizando 58 testes aprovados).

---

## [0.4.0-alpha.3] - 2026-10-31 (Fase 3)

### Added
- **Resolução de Binários FFmpeg e FFprobe**:
  - `IFFmpegPathResolver` e `FFmpegPathResolver`: busca em ordem de prioridade (caminho customizado de configuração, diretórios da aplicação `bin/ffmpeg`, `%LOCALAPPDATA%/ReplayManager/ffmpeg/` e `PATH` do sistema).
- **Extração de Metadados Técnicos de Mídia**:
  - `IMediaMetadataExtractor` e `FFprobeMetadataExtractor`: execução silenciosa de processo ffprobe com saída JSON estruturada (`-v quiet -print_format json -show_format -show_streams`), parser com suporte a framerates fracionários (ex: 30000/1001, 60/1) e mapeamento em `VideoMetadata` (DurationSeconds, Width, Height, Codec, Bitrate, FrameRate).
- **Geração e Gestão de Miniaturas (Thumbnails)**:
  - `IThumbnailGenerator` e `FFmpegThumbnailGenerator`: captura precisa de frame com seek rápido (`-ss <timestamp> -i <video> -vframes 1 -q:v 2`) e cálculo automático de timestamp seguro a 15% da duração para evitar quadros pretos iniciais (ou centro para vídeos < 3s).
  - Suporte à substituição manual de thumbnail (`ReplaceThumbnailAsync`) com padronização no diretório `%LOCALAPPDATA%/ReplayManager/Thumbnails/{videoId}.*`.
- **Orquestração de Mídia e Ingestão Integrada**:
  - `IVideoMediaProcessor` e `VideoMediaProcessor`: coordenação do pipeline de extração de metadados e geração de thumbnail pós-ingestão, atualizando o `VideoRecord` no banco SQLite.
  - Integração no `VideoIngestionService` para acionar automaticamente o processador de mídia na chegada de novos arquivos de vídeo.
- **Injeção de Dependências**:
  - Método de extensão `AddInfrastructureFFmpeg` em `ReplayManager.Infrastructure.DependencyInjection`.
  - Configuração de `InternalsVisibleTo("ReplayManager.Tests")` em `ReplayManager.Infrastructure.csproj` para viabilizar testes unitários dos parsers internos de processos.
- **Suíte de Testes Automatizados**:
  - 26 novos testes adicionados (53 testes no total na solução), cobrindo cálculo de timestamps, substituição manual de thumbnails, parser de saídas JSON do FFprobe e orquestração completa em `VideoMediaProcessor` com mocks e SQLite.

---

## [0.3.0-alpha.2] - 2026-10-30 (Fase 2)

### Added
- **Serviço de Cálculo de Hash em Streaming**:
  - `IContentHasher` e `Sha256ContentHasher`: computação assíncrona de hash SHA-256 em blocos de 64 KB (`SHA256.HashDataAsync`), otimizado para vídeos de vários gigabytes sem alocação desnecessária de memória.
- **Detecção de Estabilidade e Liberação de Gravação (ADR 004)**:
  - `IFileStabilityChecker` e `FileStabilityChecker`: inspeção combinada de tamanho de arquivo inalterado por intervalo definido e teste não bloqueante de lock de escrita (`FileAccess.Read`, `FileShare.None`).
- **Monitoramento Concorrente com Debounce**:
  - `IFileWatcherService` e `FileSystemWatcherService`: gerenciamento de `FileSystemWatcher` para múltiplas pastas, debounce de eventos por arquivo (`ConcurrentDictionary<string, Timer>`), suporte a eventos de renomeação e exclusão, e filtragem de extensões.
- **Orquestração de Ingestão e Deduplicação Intra-Pasta (ADR 003)**:
  - `IVideoIngestionService` e `VideoIngestionService`: validação de estabilidade, cálculo de hash, deduplicação restrita à pasta monitorada, atualização de arquivos renomeados ou sobrescritos e persistência inicial do `VideoRecord`.
- **Reconciliação Periódica do Sistema de Arquivos (ADR 007)**:
  - `IFolderReconciliationWorker` e `FolderReconciliationWorker`: varredura de diretórios para reconciliar discrepâncias de buffer do sistema operacional e detecção de arquivos apagados externamente (marcando status como `Missing` sem jamais apagar metadados ou registros do catálogo).
- **Injeção de Dependências**:
  - Método de extensão `AddInfrastructureFileSystem` em `ReplayManager.Infrastructure`.
- **Suíte de Testes Automatizados**:
  - 18 novos testes unitários e de integração adicionados cobrindo hasher, detector de estabilidade, watcher com debounce, reconciliação e regras de deduplicação (totalizando 27 testes aprovados).

### Changed
- `VideoRecordRepository` e `MonitoredFolderRepository`: suporte no `UpdateAsync` a entidades rastreadas no cache local do `DbContext` para prevenir exceções de instâncias duplicadas no ChangeTracker.
- `UnitOfWork`: adicionada sobrecarga de construtor conveniente `UnitOfWork(ReplayManagerDbContext)`.

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
