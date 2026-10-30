# Development Status: Replay Manager

**Última Atualização**: 2026-10-30 18:53 (Fase 1 Concluída)
**Versão Atual**: 0.2.0-alpha.1

---

## 1. Visão Geral do Progresso

| Fase | Descrição | Status | Progresso |
| :--- | :--- | :--- | :--- |
| **Fase 0** | Preparação do Ambiente e Estrutura Inicial | **Concluída** | 100% |
| **Fase 1** | Fundação e Persistência SQLite | **Concluída** | 100% |
| **Fase 2** | Monitoramento de Pastas e Ingestão | **Próxima** | 0% |
| **Fase 3** | Integração FFmpeg e Miniaturas | Pendente | 0% |
| **Fase 4** | Biblioteca de Vídeos e Interface WPF | Pendente | 0% |
| **Fase 5** | Motor de Filtros e Automação PUBG | Pendente | 0% |
| **Fase 6** | Integração YouTube API v3 | Pendente | 0% |
| **Fase 7** | Fila Persistente e Resiliente de Upload | Pendente | 0% |
| **Fase 8** | Backup no Google Drive | Pendente | 0% |
| **Fase 9** | Bandeja, Inicialização e Acabamento | Pendente | 0% |
| **Fase 10** | Validação, Estresse e Empacotamento | Pendente | 0% |

---

## 2. Detalhamento do Estado Atual (Fase 1 - 100% Concluída)

### Módulos Concluídos
- [x] Modelagem das entidades e enums de Domínio em `src/ReplayManager.Domain`:
  - `BaseEntity`
  - `MonitoredFolder`
  - `FolderFilterRule`
  - `VideoRecord`
  - `UploadQueueItem`
  - `YouTubeAccount`
  - `DescriptionTemplate`
  - `AppSetting`
  - Enums: `LocalFileStatus`, `UploadStatus`, `YouTubePrivacyStatus`, `FilterEvaluationStatus`.
- [x] Configuração de Persistência com SQLite e EF Core 10.0.14 em `src/ReplayManager.Infrastructure`:
  - `ReplayManagerDbContext` com Fluent API completa.
  - **Índice Único Composto de Deduplicação**: `IX_Videos_MonitoredFolderId_ContentSha256` (deduplicação por hash estritamente restrita à mesma pasta; pastas diferentes permitem o mesmo hash).
  - Índices para filtros rápidos: `UploadStatus`, `LocalStatus`, `FilterStatus`, `RecordedAtUtc`.
  - Índice composto para fila sequencial: `(Status, Priority, QueuedAtUtc)`.
  - Configuração de WAL mode, synchronous=NORMAL e foreign_keys=ON em `DatabaseInitializer`.
  - Seeding de configurações padrão do sistema e modelo padrão de descrição.
- [x] Contratos e Repositórios em `ReplayManager.Application` e `ReplayManager.Infrastructure`:
  - `IVideoRecordRepository` / `VideoRecordRepository`
  - `IMonitoredFolderRepository` / `MonitoredFolderRepository`
  - `IUploadQueueRepository` / `UploadQueueRepository` (incluindo `ResetActiveUploadsToInterruptedAsync` para recuperação pós-queda)
  - `IUnitOfWork` / `UnitOfWork`
  - Extensão `AddInfrastructurePersistence` para DI.
- [x] Resolução de ambiguidade de namespace em WPF (`System.Windows.Application`).
- [x] Suíte de testes automatizados com SQLite In-Memory em `tests/ReplayManager.Tests/Persistence`:
  - 9 testes implementados, 9 aprovados com 100% de sucesso.

### Módulos Em Andamento / Próxima Sessão
- **Fase 2 — Monitoramento de Pastas e Ingestão**:
  - Implementar o serviço de monitoramento com `FileSystemWatcher`.
  - Desenvolver o `FileStabilityInspector` para verificar estabilidade de tamanho e ausência de lock de gravação.
  - Implementar cálculo de hash SHA-256 em streaming.
  - Implementar motor de reconciliação periódica de diretórios (timer).

---

## 3. Problemas Conhecidos e Mitigações

1. **Ambiguidade de namespace `Application` em WPF**: O namespace `ReplayManager.Application` colidiu com `System.Windows.Application` em `App.xaml.cs`.
   - *Status*: Resolvido tipando explicitamente como `System.Windows.Application`.
2. **Crash/queda de energia com uploads em andamento**:
   - *Mitigação*: Implementado método `ResetActiveUploadsToInterruptedAsync()` no `UploadQueueRepository`, executado na inicialização para recuperar itens deixados em `Uploading`/`Preparing`.

---

## 4. Última Etapa Validada
- `dotnet test ReplayManager.slnx`: 9 testes executados, 9 aprovados, 0 falhas.
- `dotnet build ReplayManager.slnx`: 0 erros, 0 avisos.

## 5. Próxima Tarefa Exata
- Iniciar a **Fase 2**: Implementar `IFileWatcherService` e `IFileStabilityChecker` na camada `Application`, com implementação concreta em `ReplayManager.Infrastructure/FileSystem/` e testes com arquivos assíncronos em `ReplayManager.Tests`.
