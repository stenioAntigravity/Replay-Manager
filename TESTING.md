# Testing Strategy and Test Log: Replay Manager

## 1. Estratégia de Testes

A suíte de testes do **Replay Manager** é estruturada no projeto `ReplayManager.Tests` utilizando **xUnit**, **FluentAssertions**, **Moq** e **SQLite In-Memory**.

A cobertura é orientada a:
1. **Regras de Domínio e Filtros (Unitários Puros)**:
   - Validação de extensões aceitas (.mp4, .mkv, .avi).
   - Validação de intervalos de tamanho de arquivo (min/max).
   - Validação de duração e regex de nome.
   - Formatação e parsing de títulos padrão (`PUBG Replay - DD/MM/AAAA - HH:MM:SS`).
2. **Persistência e Concorrência (Testes de Integração com SQLite InMemory / Arquivo Temporário)**:
   - Deduplicação intra-pasta: tentar inserir o mesmo hash na mesma pasta deve disparar `DbUpdateException`; em pastas diferentes deve cadastrar ambos com sucesso.
   - Atomicidade da fila de uploads em transições de estado e ordenação por prioridade.
   - Preservação de registros com arquivo físico ausente (`LocalStatus.Missing`).
   - Recuperação de estado de uploads interrompidos (`ResetActiveUploadsToInterruptedAsync`).
   - Seeding consistente sem duplicação de dados padrão.
3. **Serviços de Infraestrutura (Mock e Simulação)**:
   - `FileStabilityInspector`: simulação de arquivo em escrita com liberação progressiva.
   - Throttling de velocidade: verificação do limitador de bytes por segundo.
   - Parser de saída de linha de comando do FFprobe/FFmpeg.

---

## 2. Registro de Execuções de Teste

| ID | Data | Módulo / Suíte | Descrição do Teste | Resultado | Notas |
| :--- | :--- | :--- | :--- | :--- | :--- |
| T001 | 2026-10-30 | Ambiente / SDK | Verificação de disponibilidade do .NET 10 SDK e WPF Workload | **PASS** | SDK 10.0.403 instalado e funcional. |
| T002 | 2026-10-30 | Build Solution | Compilação completa da solução `ReplayManager.slnx` com 5 projetos | **PASS** | 0 erros, 0 avisos. |
| T003 | 2026-10-30 | Test Runner | Execução inicial de testes via `dotnet test ReplayManager.slnx` | **PASS** | Suíte de testes xUnit executada e aprovada com sucesso. |
| T004 | 2026-10-30 | Persistence / SQLite | `SameHash_InSameFolder_ShouldThrowException_WhenDuplicateInserted` | **PASS** | Violação de unicidade intra-pasta disparada com sucesso. |
| T005 | 2026-10-30 | Persistence / SQLite | `SameHash_InDifferentFolders_ShouldBeAllowed_AndTreatedAsDistinctRecords` | **PASS** | Mesmos hashes em pastas distintas aceitos normalmente. |
| T006 | 2026-10-30 | Persistence / SQLite | `DeletingMonitoredFolder_ShouldCascadeDelete_AssociatedVideosAndFilterRules` | **PASS** | Cascade delete validado. |
| T007 | 2026-10-30 | Persistence / SQLite | `VideoLocalStatus_CanBeChangedToMissing_PreservingYouTubeInfoAndMetadata` | **PASS** | Preservação de vídeo deletado localmente validada. |
| T008 | 2026-10-30 | Repositories / Video | `GetAllAsync_WithSearchTerm_ShouldFilterByFileNameOrCustomTitle` | **PASS** | Busca por nome e título customizado validada. |
| T009 | 2026-10-30 | Repositories / Video | `ExistsInFolderAsync_ShouldReturnTrue_OnlyForMatchingFolderAndHash` | **PASS** | Verificação de existência isolada por pasta validada. |
| T010 | 2026-10-30 | Repositories / Queue | `GetNextPendingItemAsync_ShouldReturnItemWithLowestPriorityAndOldestQueuedAt` | **PASS** | Ordenação estrita da fila de envio validada. |
| T011 | 2026-10-30 | Repositories / Queue | `ResetActiveUploadsToInterruptedAsync_ShouldMarkUploadingAndPreparingItemsAsInterrupted` | **PASS** | Recuperação pós-crash validada. |
| T012 | 2026-10-30 | Persistence / Init | `InitializeAsync_ShouldSeedDefaultSettings_AndDefaultDescriptionTemplate` | **PASS** | Seeding idempotente validado. |

**Total de Testes da Fase 1**: 9 executados, 9 aprovados (100% de sucesso).

---

## 3. Testes Pendentes para Próximas Fases
- [ ] `FolderFilterRuleTests`: Validação isolada de todas as ramificações de aprovação/rejeição (Fase 5).
- [ ] `TitleFormatterTests`: Extração de timestamps de nomes de arquivo e data de criação (Fase 6).
- [ ] `FileStabilityInspectorTests`: Testes assíncronos com locks de leitura/escrita (Fase 2).
- [ ] `DirectoryWatcherTests`: Detecção de eventos e reconciliação periódica (Fase 2).
