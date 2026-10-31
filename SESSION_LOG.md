# Session Log: Replay Manager

Este log documenta o histórico de sessões de desenvolvimento com escopo executado, arquivos tocados e orientações de transição.

---

## Sessão 01 — 2026-10-30 (Fase 0: Preparação Inicial e Arquitetura Concluída)
- Análise de requisitos, resolução de dependências, instalação do .NET 10 SDK (10.0.403).
- Criação dos documentos de governança e solução `ReplayManager.slnx` com 5 projetos modulares.
- Compilação validada e commit inicial `361e2c7`.

---

## Sessão 02 — 2026-10-30 (Fase 1: Fundação e Persistência SQLite Concluída)
- Modelagem de entidades do domínio e enums em `ReplayManager.Domain`.
- Instalação e configuração do EF Core SQLite 10.0.14 em `ReplayManager.Infrastructure`.
- Configuração do `ReplayManagerDbContext` com Fluent API, chaves primárias, estrangeiras e índice único composto para deduplicação intra-pasta `(MonitoredFolderId, ContentSha256)`.
- Implementação de `DatabaseInitializer` com pragmas SQLite (WAL mode, synchronous=NORMAL, foreign_keys=ON) e seeding idempotente.
- Criação de contratos de persistência em `ReplayManager.Application/Contracts/Persistence/`.
- Implementação de repositórios concretos e `UnitOfWork` em `ReplayManager.Infrastructure/Repositories/`.
- Resolução de ambiguidade em `App.xaml.cs` (`System.Windows.Application`).
- Criação de suíte de testes de integração com SQLite InMemory e validação de 9 testes aprovados (100%).

---

## Sessão 06 — 2026-10-31 (Fase 5: Motor de Filtros e Automação PUBG Concluída)

### 1. Escopo e Objetivos da Sessão
- Criação dos contratos e modelos para o motor de filtros em `ReplayManager.Application`:
  - `IFilterEvaluator` e `FilterEvaluationResult`.
- Implementação de `FilterEvaluator`:
  - Avaliação de extensões permitidas (`AllowedExtensions`).
  - Faixa de tamanho mínimo e máximo com alternador de ativação (`MinSizeBytes`, `MaxSizeBytes`, `IsMaxSizeEnabled`).
  - Limites de duração mínima e máxima (`MinDurationSeconds`, `MaxDurationSeconds`).
  - Padrões de nomenclatura via expressões regulares (`NamePatternRegex`).
  - Validação de diretório raiz vs subpastas recursivas (`IncludeSubdirectories`).
- Integração da automação em `VideoMediaProcessor`:
  - Execução imediata da avaliação de filtros pós-extração de metadados FFprobe.
  - Gravação dos status `FilterEvaluationStatus` e justificativas em `VideoRecord.RejectionReason`.
  - Enfileiramento automático na tabela `UploadQueueItem` caso a pasta monitorada possua `AutoUploadEnabled = true` e o vídeo seja aprovado.
- Criação da suíte de testes de filtros e automação:
  - `FilterEvaluatorTests.cs` com 9 cenários de validação de regras.
  - `VideoMediaProcessorAutomationTests.cs` com 2 testes de integração verificando auto-upload e reprovação com justificativa.

### 2. Ações Executadas
- Registro de `IFilterEvaluator` no container de injeção em `DependencyInjection.cs`.
- Execução de 69 testes automatizados com 100% de sucesso.
- Atualização da documentação viva de governança.

### 3. Arquivos Criados / Modificados
- `src/ReplayManager.Application/Contracts/Filters/IFilterEvaluator.cs`
- `src/ReplayManager.Application/Models/Filters/FilterEvaluationResult.cs`
- `src/ReplayManager.Application/Services/FilterEvaluator.cs`
- `src/ReplayManager.Application/Services/VideoMediaProcessor.cs`
- `src/ReplayManager.Infrastructure/DependencyInjection.cs`
- `tests/ReplayManager.Tests/Filters/FilterEvaluatorTests.cs`
- `tests/ReplayManager.Tests/Filters/VideoMediaProcessorAutomationTests.cs`
- `DEVELOPMENT_STATUS.md`
- `IMPLEMENTATION_PLAN.md`
- `TESTING.md`
- `SESSION_LOG.md`
- `CHANGELOG.md`
- `NEXT_SESSION.md`

### 4. Testes Realizados
- `dotnet test ReplayManager.slnx`: 69 testes executados, 69 aprovados, 0 falhas (100% de sucesso).
- `dotnet build ReplayManager.slnx`: 0 erros, 0 avisos.

### 5. Próxima Etapa
- **Fase 6**: Integração YouTube API v3 (fluxo OAuth 2.0 com `GoogleWebAuthorizationBroker`, DPAPI para armazenamento de segredos, gerador de títulos e templates de descrição, upload resumable).

---

## Sessão 05 — 2026-10-31 (Fase 4: Biblioteca de Vídeos e Interface WPF Concluída)

### 1. Escopo e Objetivos da Sessão
- Configuração de pacotes MVVM e Hosting no projeto `ReplayManager.App` (`CommunityToolkit.Mvvm` 8.4.2 e `Microsoft.Extensions.Hosting` 10.0.14).
- Implementação de `MediaPlayerLauncher`:
  - Detecção do player VLC em diretórios padrões ou via Registro do Windows (`SOFTWARE\VideoLAN\VLC`). Fallback não bloqueante para o player padrão do sistema com `Process.Start`.
- Implementação de `WpfDialogService`:
  - Diálogos nativos de confirmação, avisos e seleção de imagem (`OpenFileDialog`) com filtros `.jpg;.jpeg;.png`.
- Implementação de ViewModels MVVM:
  - `VideoCardViewModel`: formatação de duração (`MM:SS`), tamanho legível, status coloridos (`Local`, `Na Fila`, `Enviado`, `Arquivo Ausente`) e binds para miniatura.
  - `LibraryViewModel`: busca em tempo real com debounce, filtros por status, comandos de reprodução, alteração de miniatura, enfileiramento para upload e salvamento de metadados.
  - `MainViewModel`: orquestrador de navegação em abas superiores.
- Implementação das Views XAML:
  - `MainWindow.xaml`: Casca principal moderna Dark Theme com cabeçalho de navegação.
  - `LibraryView.xaml`: Grade de miniaturas virtualizada com rolagem suave, badges visuais e painel lateral de detalhes/edição.
  - Conversores: `ImagePathToBitmapConverter` seguro sem retenção de file lock e `EnumToBooleanConverter`.
- Criação de suíte de testes de UI e ViewModel em `ReplayManager.Tests/ViewModels/LibraryViewModelTests.cs`.

### 2. Ações Executadas
- Criação e registro de componentes de UI e DI em `App.xaml.cs`.
- Ajustes de referências e compilação do `ReplayManager.App` e `ReplayManager.Tests`.
- Execução de 58 testes automatizados na solução (100% aprovados).
- Atualização da documentação viva do projeto.

### 3. Arquivos Criados / Modificados
- `src/ReplayManager.App/ReplayManager.App.csproj`
- `src/ReplayManager.App/App.xaml`
- `src/ReplayManager.App/App.xaml.cs`
- `src/ReplayManager.App/MainWindow.xaml`
- `src/ReplayManager.App/MainWindow.xaml.cs`
- `src/ReplayManager.App/Services/IMediaPlayerLauncher.cs`
- `src/ReplayManager.App/Services/MediaPlayerLauncher.cs`
- `src/ReplayManager.App/Services/IDialogService.cs`
- `src/ReplayManager.App/Services/WpfDialogService.cs`
- `src/ReplayManager.App/ViewModels/LibraryFilterOption.cs`
- `src/ReplayManager.App/ViewModels/VideoCardViewModel.cs`
- `src/ReplayManager.App/ViewModels/LibraryViewModel.cs`
- `src/ReplayManager.App/ViewModels/MainViewModel.cs`
- `src/ReplayManager.App/Views/LibraryView.xaml`
- `src/ReplayManager.App/Views/LibraryView.xaml.cs`
- `src/ReplayManager.App/Converters/EnumToBooleanConverter.cs`
- `src/ReplayManager.App/Converters/ImagePathToBitmapConverter.cs`
- `tests/ReplayManager.Tests/ViewModels/LibraryViewModelTests.cs`
- `tests/ReplayManager.Tests/ReplayManager.Tests.csproj`
- `DEVELOPMENT_STATUS.md`
- `IMPLEMENTATION_PLAN.md`
- `TESTING.md`
- `SESSION_LOG.md`
- `CHANGELOG.md`
- `NEXT_SESSION.md`

### 4. Testes Realizados
- `dotnet test ReplayManager.slnx`: 58 testes executados, 58 aprovados, 0 falhas (100% de sucesso).
- `dotnet build ReplayManager.slnx`: 0 erros, 0 avisos.

### 5. Próxima Etapa
- **Fase 5**: Motor de Filtros e Automação PUBG (avaliador de regras `FilterEvaluator`, regras por pasta monitorada, motivo de reprovação e enfileiramento automático de destaques).

---

## Sessão 04 — 2026-10-31 (Fase 3: Integração FFmpeg e Miniaturas Concluída)

### 1. Escopo e Objetivos da Sessão
- Investigação e resolução de visibilidade de assembly (`InternalsVisibleTo`) para os parsers de processos no `ReplayManager.Infrastructure`.
- Criação dos contratos e modelos para processamento de mídia em `ReplayManager.Application`:
  - `IFFmpegPathResolver`, `IMediaMetadataExtractor`, `IThumbnailGenerator`, `IVideoMediaProcessor`.
  - Modelos `VideoMetadata` e `ThumbnailResult`.
- Implementação de `FFmpegPathResolver`:
  - Resolução dinâmica e resiliente procurando em diretório configurado, diretório do executável (`bin/ffmpeg`), `%LOCALAPPDATA%/ReplayManager/ffmpeg/` e PATH do SO.
- Implementação de `FFprobeMetadataExtractor`:
  - Execução silenciosa do `ffprobe` com saída JSON estruturada, extração de largura, altura, codec, duração, bitrate e cálculo de framerates fracionários (ex: 30000/1001, 60/1).
- Implementação de `FFmpegThumbnailGenerator`:
  - Captura assíncrona com seek rápido (`-ss <timestamp> -i ... -vframes 1 -q:v 2`), cálculo automático seguro a 15% da duração (evitando telas pretas iniciais de gravação) e substituição manual de miniaturas (`ReplaceThumbnailAsync`).
- Implementação de `VideoMediaProcessor`:
  - Coordenação da extração de metadados e miniatura atualizando o registro `VideoRecord` no banco.
  - Integração no `VideoIngestionService` para pós-processamento imediato de vídeos recém-ingeridos.
- Suíte completa de testes automatizados com mocks e dados sintéticos (26 novos testes adicionados, 53 testes no total na solução).

### 2. Ações Executadas
- Ajuste do `ReplayManager.Infrastructure.csproj` para expor membros internos a `ReplayManager.Tests`.
- Implementação da substituição manual de thumbnails e validação de caminhos no cache local (`%LOCALAPPDATA%/ReplayManager/Thumbnails/`).
- Atualização do `DependencyInjection.cs` com `AddInfrastructureFFmpeg()`.
- Execução e validação de 53 testes automatizados (100% aprovados).
- Atualização de todos os documentos de continuidade e governança.

### 3. Arquivos Criados / Modificados
- `src/ReplayManager.Application/Contracts/Media/IFFmpegPathResolver.cs`
- `src/ReplayManager.Application/Contracts/Media/IMediaMetadataExtractor.cs`
- `src/ReplayManager.Application/Contracts/Media/IThumbnailGenerator.cs`
- `src/ReplayManager.Application/Contracts/Media/IVideoMediaProcessor.cs`
- `src/ReplayManager.Application/Models/Media/VideoMetadata.cs`
- `src/ReplayManager.Application/Models/Media/ThumbnailResult.cs`
- `src/ReplayManager.Application/Services/VideoMediaProcessor.cs`
- `src/ReplayManager.Infrastructure/FFmpeg/FFmpegPathResolver.cs`
- `src/ReplayManager.Infrastructure/FFmpeg/FFprobeJsonModels.cs`
- `src/ReplayManager.Infrastructure/FFmpeg/FFprobeMetadataExtractor.cs`
- `src/ReplayManager.Infrastructure/FFmpeg/FFmpegThumbnailGenerator.cs`
- `src/ReplayManager.Infrastructure/DependencyInjection.cs`
- `src/ReplayManager.Infrastructure/ReplayManager.Infrastructure.csproj`
- `tests/ReplayManager.Tests/FFmpeg/FFprobeMetadataExtractorTests.cs`
- `tests/ReplayManager.Tests/FFmpeg/FFmpegThumbnailGeneratorTests.cs`
- `tests/ReplayManager.Tests/FFmpeg/VideoMediaProcessorTests.cs`
- `DEVELOPMENT_STATUS.md`
- `IMPLEMENTATION_PLAN.md`
- `TESTING.md`
- `SESSION_LOG.md`
- `CHANGELOG.md`
- `NEXT_SESSION.md`

### 4. Testes Realizados
- `dotnet test ReplayManager.slnx`: 53 testes executados, 53 aprovados, 0 falhas (100% de sucesso).
- `dotnet build ReplayManager.slnx`: 0 erros, 0 avisos.

### 5. Próxima Etapa
- **Fase 4**: Biblioteca de Vídeos e Interface WPF (MVVM, `MainWindow` / `ShellWindow`, grade de cartões de vídeo virtualizada, busca e filtros em tempo real, painel de detalhes e reprodução de mídia com VLC / player padrão).

---

## Sessão 03 — 2026-10-30 (Fase 2: Monitoramento de Pastas e Ingestão Concluída)

### 1. Escopo e Objetivos da Sessão
- Criação dos contratos de I/O de arquivos e ingestão em `ReplayManager.Application`:
  - `IContentHasher`, `IFileStabilityChecker`, `IFileWatcherService`, `IFolderReconciliationWorker`, `IVideoIngestionService`.
  - Modelos de evento e resultados: `FileDetectedEventArgs`, `FileDeletedEventArgs`, `ReconciliationResult`, `IngestionResult`.
- Implementação de `Sha256ContentHasher`:
  - Streaming assíncrono em blocos de 64 KB com `SHA256.HashDataAsync`, baixo consumo de memória sem carregar vídeos gigantes na RAM.
- Implementação de `FileStabilityChecker`:
  - Verificação dupla de término de gravação: monitoramento de estabilidade de tamanho e teste não bloqueante de lock exclusivo (`FileAccess.Read`, `FileShare.None`) com timeouts configuráveis.
- Implementação de `FileSystemWatcherService`:
  - Monitoramento concorrente de múltiplas pastas físicas, debounce thread-safe via temporizadores por caminho de arquivo, filtragem de extensões de vídeo configuráveis por pasta e tratamento de erros/buffer overflow.
- Implementação de `VideoIngestionService`:
  - Orquestração de detecção, estabilidade, cálculo de hash e deduplicação intra-pasta (ADR 003). Atualização transparente de arquivos renomeados e preservação de unicidade.
- Implementação de `FolderReconciliationWorker`:
  - Varredura de diretórios para capturar arquivos não detectados pelo watcher e detecção de arquivos apagados externamente (marcando como `LocalFileStatus.Missing` sem jamais apagar registros do banco, conforme ADR 007).
- Aprimoramento de `VideoRecordRepository` e `MonitoredFolderRepository`:
  - Suporte resiliente a atualizações em `UpdateAsync` com `ChangeTracker` local para evitar conflito de instâncias no EF Core.
  - Sobrecarga de construtor em `UnitOfWork` para criação direta a partir de DbContext.
- Criação de suíte com 18 novos testes automatizados unitários e de integração no `ReplayManager.Tests`.

### 2. Ações Executadas
- Criação dos contratos em `src/ReplayManager.Application/Contracts/FileSystem/` e `Ingestion/`.
- Criação dos DTOs em `src/ReplayManager.Application/Models/FileSystem/` e `Ingestion/`.
- Implementação dos serviços em `src/ReplayManager.Infrastructure/FileSystem/` e `ReplayManager.Application/Services/`.
- Atualização da injeção de dependência com `AddInfrastructureFileSystem()`.
- Testes cobrindo hash streaming, locks concorrentes de gravação, watcher com debounce, reconciliação e preservação de arquivos apagados.
- Execução de testes: 27 testes executados, 27 aprovados (100% de sucesso).
- Compilação limpa da solução: 0 erros, 0 avisos.

### 3. Arquivos Criados / Modificados
- `src/ReplayManager.Application/Contracts/FileSystem/IContentHasher.cs`
- `src/ReplayManager.Application/Contracts/FileSystem/IFileStabilityChecker.cs`
- `src/ReplayManager.Application/Contracts/FileSystem/IFileWatcherService.cs`
- `src/ReplayManager.Application/Contracts/FileSystem/IFolderReconciliationWorker.cs`
- `src/ReplayManager.Application/Contracts/Ingestion/IVideoIngestionService.cs`
- `src/ReplayManager.Application/Models/FileSystem/FileDetectedEventArgs.cs`
- `src/ReplayManager.Application/Models/FileSystem/FileDeletedEventArgs.cs`
- `src/ReplayManager.Application/Models/FileSystem/ReconciliationResult.cs`
- `src/ReplayManager.Application/Models/Ingestion/IngestionResult.cs`
- `src/ReplayManager.Application/Services/VideoIngestionService.cs`
- `src/ReplayManager.Infrastructure/FileSystem/Sha256ContentHasher.cs`
- `src/ReplayManager.Infrastructure/FileSystem/FileStabilityChecker.cs`
- `src/ReplayManager.Infrastructure/FileSystem/FileSystemWatcherService.cs`
- `src/ReplayManager.Infrastructure/FileSystem/FolderReconciliationWorker.cs`
- `src/ReplayManager.Infrastructure/Repositories/VideoRecordRepository.cs`
- `src/ReplayManager.Infrastructure/Repositories/MonitoredFolderRepository.cs`
- `src/ReplayManager.Infrastructure/Repositories/UnitOfWork.cs`
- `src/ReplayManager.Infrastructure/DependencyInjection.cs`
- `tests/ReplayManager.Tests/FileSystem/Sha256ContentHasherTests.cs`
- `tests/ReplayManager.Tests/FileSystem/FileStabilityCheckerTests.cs`
- `tests/ReplayManager.Tests/FileSystem/FileSystemWatcherServiceTests.cs`
- `tests/ReplayManager.Tests/FileSystem/FolderReconciliationWorkerTests.cs`
- `tests/ReplayManager.Tests/Ingestion/VideoIngestionServiceTests.cs`
- `DEVELOPMENT_STATUS.md`
- `IMPLEMENTATION_PLAN.md`
- `TESTING.md`
- `SESSION_LOG.md`
- `CHANGELOG.md`
- `NEXT_SESSION.md`

### 4. Testes Realizados
- `dotnet test ReplayManager.slnx`: 27 testes executados, 27 aprovados, 0 falhas.
- `dotnet build ReplayManager.slnx`: 0 erros, 0 avisos.

### 5. Próxima Etapa
- **Fase 3**: Integração FFmpeg e Miniaturas (localização de binários FFmpeg/FFprobe, extração de metadados de vídeo, geração de thumbnail no percentual seguro de 15% e cache local de miniaturas).
