# Next Session Guide: Replay Manager

**Data de Emissão**: 2026-10-30
**Fase Alvo**: Fase 1 — Fundação e Persistência SQLite

---

## 1. O que já está pronto
- Ambiente configurado com .NET 10 SDK (versão 10.0.403).
- Repositório Git inicializado com `.gitignore`.
- Documentos de continuidade criados na raiz:
  - `PROJECT_CONTEXT.md`
  - `ARCHITECTURE.md`
  - `IMPLEMENTATION_PLAN.md`
  - `DEVELOPMENT_STATUS.md`
  - `DECISIONS.md`
  - `TESTING.md`
  - `SESSION_LOG.md`
  - `CHANGELOG.md`
- Estrutura de projetos da solução `ReplayManager.sln` criada e compilada com sucesso:
  - `ReplayManager.Domain`
  - `ReplayManager.Application`
  - `ReplayManager.Infrastructure`
  - `ReplayManager.App` (WPF)
  - `ReplayManager.Tests`

---

## 2. O que precisa ser verificado antes de iniciar
1. Confirmar o ambiente do SDK executando:
   ```powershell
   $env:DOTNET_ROOT = "C:\Users\steni\AppData\Local\Microsoft\dotnet"; $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
   dotnet --version
   ```
2. Verificar o status do Git (`git status`) e compilação inicial (`dotnet build ReplayManager.sln`).
3. Ler `DEVELOPMENT_STATUS.md` e `DECISIONS.md` para relembrar as regras de negócio e persistência.

---

## 3. Qual tarefa deve ser executada em seguida (Fase 1)
Implementar as entidades e a camada de persistência com SQLite e EF Core:
1. No `ReplayManager.Domain`:
   - `MonitoredFolder` (Id, Path, Name, IsActive, AutoUploadEnabled, CreatedAt).
   - `FolderFilterRule` (Id, MonitoredFolderId, AllowedExtensions, MinSizeBytes, MaxSizeBytes, MinDurationSeconds, MaxDurationSeconds, NamePatternRegex, IncludeSubdirectories).
   - `VideoRecord` (Id, MonitoredFolderId, FilePath, FileName, ContentSha256, FileSizeBytes, DurationSeconds, ResolutionWidth, ResolutionHeight, RecordedAt, LocalStatus, UploadStatus, FilterStatus, RejectionReason, ThumbnailPath, YouTubeVideoId, YouTubeUrl).
   - `UploadQueueItem` (Id, VideoRecordId, Priority, Status, BytesUploaded, TotalBytes, UploadSpeedBytesPerSec, AttemptCount, LastError, QueuedAt, StartedAt, CompletedAt, ResumableSessionUri).
   - `YouTubeAccount` (Id, ChannelId, ChannelTitle, EncryptedToken, IsActive, LastAuthenticatedAt).
   - `DescriptionTemplate` (Id, MonitoredFolderId, Name, TemplateContent, IsDefault).
2. No `ReplayManager.Infrastructure`:
   - Instalar pacotes NuGet necessários: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`.
   - Criar `ReplayManagerDbContext` com Fluent API, chaves compostas e índices (especialmente índice único `(MonitoredFolderId, ContentSha256)`).
   - Configurar SQLite WAL mode e geração automática do banco de dados na inicialização.
3. No `ReplayManager.Tests`:
   - Adicionar testes de persistência com SQLite InMemory validando a integridade das entidades e da regra de duplicatas por pasta.

---

## 4. Quais arquivos devem ser consultados
- `ARCHITECTURE.md` (Seção 3 - Componentes e Diagrama de Camadas).
- `DECISIONS.md` (ADR 003 e ADR 005 sobre deduplicação e filtros).
- `PROJECT_CONTEXT.md` (Seção 2 e 3 sobre persistência e entidades).

---

## 5. Como testar o resultado
Executar:
```powershell
$env:DOTNET_ROOT = "C:\Users\steni\AppData\Local\Microsoft\dotnet"; $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
dotnet test
```
Validar se todos os testes unitários de persistência e validação passam com sucesso.
