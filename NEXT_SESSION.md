# Next Session Guide: Replay Manager

**Data de Emissão**: 2026-10-31  
**Fase Alvo**: Fase 8 — Backup no Google Drive

---

## 1. O que já está pronto
- **Fase 0 (Concluída)**: SDK .NET 10.0, Git, governança e solução modular.
- **Fase 1 (Concluída)**: Modelagem de domínio, SQLite WAL mode, repositórios, UnitOfWork e deduplicação intra-pasta (ADR 003).
- **Fase 2 (Concluída)**: Hasher SHA-256 em streaming 64 KB, detecção não bloqueante de término de gravação, watcher com debounce, deduplicação intra-pasta e reconciliação (ADR 007).
- **Fase 3 (Concluída)**: Resolução de binários FFmpeg/FFprobe, extração de metadados técnicos (duração, resolução, codec, framerate), captura segura de miniatura a 15% e substituição manual.
- **Fase 4 (Concluída)**: Interface WPF MVVM moderna Dark Theme em abas, grade virtualizada com rolagem suave, badges visuais coloridos, busca textual em tempo real, filtros de status, painel de detalhes com edição de metadados, reprodução via VLC/padrão e diálogos nativos.
- **Fase 5 (Concluída)**: Motor de filtros determinístico (`IFilterEvaluator`) e auto-enfileiramento de replays aprovados.
- **Fase 6 (Concluída)**: Integração YouTube API v3 (OAuth 2.0, DPAPI, gerador de títulos canônico PUBG, template engine de descrições e resumable uploader).
- **Fase 7 (Concluída e Validada)**:
  - Serviço de fila persistente `IUploadQueueService` e `UploadQueueService` com enfileiramento, pausa, retomada, cancelamento, reordenação e retry manual.
  - Controle de taxa de transferência via `ThrottledStream` (bytes/s configurável).
  - Processamento sequencial (1 vídeo por vez) com `UploadQueueProcessor` e persistência transacional com `UnitOfWork`.
  - Serviço em segundo plano contínuo `UploadQueueBackgroundService`.
  - Interface WPF dedicada `UploadQueueView.xaml` com `UploadQueueViewModel` e `UploadQueueItemViewModel` integrada em `MainWindow.xaml`.
  - Suíte de 86 testes automatizados aprovados (100% de sucesso).
  - Compilação limpa (0 erros, 0 avisos).

---

## 2. O que precisa ser verificado antes de iniciar
1. Configuração do ambiente .NET 10:
   ```powershell
   $env:DOTNET_ROOT = "C:\Users\steni\AppData\Local\Microsoft\dotnet"; $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
   dotnet --version
   ```
2. Execução dos testes existentes para validação do estado:
   ```powershell
   dotnet test ReplayManager.slnx
   ```
3. Status do repositório Git (`git status`).

---

## 3. Qual tarefa deve ser executada em seguida (Fase 8)
Implementar o **Backup no Google Drive**:
1. Pacotes NuGet necessários:
   - `Google.Apis.Drive.v3` em `ReplayManager.Infrastructure`.
2. No `ReplayManager.Application`:
   - Contratos:
     - `ISqliteBackupService`: criação de cópia atômica consistente do banco SQLite ativo em tempo de execução via `SqliteConnection.BackupDatabase` (sem interromper operações normais).
     - `IGoogleDriveBackupService`: envio do arquivo de backup consistente para a pasta dedicada do Google Drive (ou AppData do Drive), verificação de integridade pós-cópia (`PRAGMA integrity_check`) e retenção dos últimos N backups configurável.
   - Modelos/DTOs:
     - `BackupMetadata` (`BackupFileName`, `CreatedUtc`, `FileSizeBytes`, `DriveFileId`, `IsConsistent`).
3. No `ReplayManager.Infrastructure`:
   - Implementação de `SqliteBackupService` e `GoogleDriveBackupService`.
   - Reutilização da infraestrutura de credenciais autenticadas do Google OAuth.
4. No `ReplayManager.App` (WPF):
   - Controles visuais de backup na aba de Configurações ou aba dedicada de Nuvem/Backup.
5. No `ReplayManager.Tests`:
   - Testes unitários de clonagem atômica SQLite e verificação de integridade de banco de dados.

---

## 4. Quais arquivos devem ser consultados
- `ARCHITECTURE.md` (Seção 3.3: `SqliteBackupEngine` e `DriveInfra`).
- `PROJECT_CONTEXT.md` (Seção 2: Backup no Google Drive).
- `IMPLEMENTATION_PLAN.md` (Fase 8: Backup no Google Drive).
- `SESSION_LOG.md` (Histórico de sessões).

---

## 5. Como testar o resultado
Executar:
```powershell
$env:DOTNET_ROOT = "C:\Users\steni\AppData\Local\Microsoft\dotnet"; $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
dotnet test ReplayManager.slnx
dotnet build ReplayManager.slnx
```
Garantir compilação limpa (0 erros, 0 avisos) e 100% dos testes aprovados.
