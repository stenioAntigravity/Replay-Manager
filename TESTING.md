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
3. **Serviços de Infraestrutura e Sistema de Arquivos**:
   - `Sha256ContentHasher`: verificação de hash em streaming assíncrono para strings conhecidas, arquivos binários e streams abertos.
   - `FileStabilityChecker`: simulação de arquivos em gravação com stream concorrente exclusivo (`FileShare.None`), liberação progressiva e timeout.
   - `FileSystemWatcherService`: detecção de criação de arquivos de vídeo, debounce e filtragem de extensões não-vídeo.
   - `VideoIngestionService`: ingestão de vídeos novos, deduplicação intra-pasta, renomeação e suporte a arquivos iguais em pastas distintas (ADR 003).
   - `FolderReconciliationWorker`: sincronização de arquivos em disco com banco SQLite e marcação de arquivos ausentes sem exclusão (ADR 007).

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
| T013 | 2026-10-30 | FileSystem / Hasher | `ComputeSha256Async_WithKnownContent_ReturnsCorrectHex` | **PASS** | Hash SHA-256 de 64 caracteres validado contra payload padrão. |
| T014 | 2026-10-30 | FileSystem / Hasher | `ComputeSha256Async_StreamAndFilePath_ProduceIdenticalHash` | **PASS** | Stream e caminho físico produzem o mesmo hash. |
| T015 | 2026-10-30 | FileSystem / Hasher | `ComputeSha256Async_FileNotFound_ThrowsFileNotFoundException` | **PASS** | Lançamento correto de exceção para arquivos inexistentes. |
| T016 | 2026-10-30 | FileSystem / Stability | `IsFileLocked_WhenFileIsNotLocked_ReturnsFalse` | **PASS** | Detecção de arquivo livre de locks. |
| T017 | 2026-10-30 | FileSystem / Stability | `IsFileLocked_WhenFileIsHeldExclusively_ReturnsTrue` | **PASS** | Detecção precisa de lock exclusivo retido por processo externo. |
| T018 | 2026-10-30 | FileSystem / Stability | `WaitForStabilityAsync_WhenFileAlreadyStable_ReturnsTrueQuickly` | **PASS** | Arquivo estável liberado sem espera desnecessária. |
| T019 | 2026-10-30 | FileSystem / Stability | `WaitForStabilityAsync_WhenFileIsContinuouslyLocked_TimesOutAndReturnsFalse` | **PASS** | Timeout respeitado quando arquivo permanece bloqueado. |
| T020 | 2026-10-30 | FileSystem / Stability | `WaitForStabilityAsync_WhenRecordingCompletesDuringWait_ReturnsTrue` | **PASS** | Conclusão de gravação concorrente detectada com sucesso. |
| T021 | 2026-10-30 | FileSystem / Watcher | `StartMonitoring_WhenVideoFileCreated_DispatchesFileDiscoveredEvent` | **PASS** | Evento de detecção de vídeo disparado após debounce. |
| T022 | 2026-10-30 | FileSystem / Watcher | `StartMonitoring_WhenNonVideoFileCreated_DoesNotDispatchEvent` | **PASS** | Arquivos de extensão irrelevante (.txt) ignorados. |
| T023 | 2026-10-30 | FileSystem / Watcher | `StopMonitoring_StopsTrackingFolder` | **PASS** | Encerramento limpo do monitoramento por pasta. |
| T024 | 2026-10-30 | Ingestion / Ingest | `IngestVideoFileAsync_NewFile_CreatesVideoRecordSuccessfully` | **PASS** | Novo registro persistido com tamanho, hash e status corretos. |
| T025 | 2026-10-30 | Ingestion / Ingest | `IngestVideoFileAsync_DuplicateContentInSameFolder_ReturnsDuplicateWithoutInsertingSecondRow` | **PASS** | Deduplicação intra-pasta validada. |
| T026 | 2026-10-30 | Ingestion / Ingest | `IngestVideoFileAsync_IdenticalContentInDifferentFolders_CreatesTwoDistinctRecords` | **PASS** | Deduplicação isolada por pasta (ADR 003) validada. |
| T027 | 2026-10-30 | Ingestion / Ingest | `IngestVideoFileAsync_RenamedFileInSameFolder_UpdatesExistingRecord` | **PASS** | Atualização automática de arquivo renomeado. |
| T028 | 2026-10-30 | FileSystem / Reconcile | `ReconcileFolderAsync_WithNewFilesOnDisk_IngestsAllDiscoveredVideos` | **PASS** | Ingestão em lote de arquivos físicos não catalogados. |
| T029 | 2026-10-30 | FileSystem / Reconcile | `ReconcileFolderAsync_WhenFileDeletedExternally_MarksAsMissingWithoutDeletingRecord` | **PASS** | Arquivo ausente marcado como Missing sem exclusão do banco (ADR 007). |
| T030 | 2026-10-30 | FileSystem / Reconcile | `ReconcileFolderAsync_WhenMissingFileReappears_RestoresAvailableStatus` | **PASS** | Arquivo que volta ao disco é restaurado para Available. |
| T031 | 2026-10-31 | FFmpeg / Metadata | `ParseFFprobeJson_WithStandard1080pVideo_ExtractsAllFieldsCorrectly` | **PASS** | Extração correta de duração, resolução, codecs, taxa de bits e fps. |
| T032 | 2026-10-31 | FFmpeg / Metadata | `ParseFFprobeJson_WithFractionalFramerate_ComputesCorrectFps` | **PASS** | Cálculo de taxas fracionárias (29.970 e 23.976 fps). |
| T033 | 2026-10-31 | FFmpeg / Metadata | `ParseFFprobeJson_WithAudioOnlyStream_ReturnsEmptyVideoFields` | **PASS** | Tratamento resiliente quando não há streams de vídeo. |
| T034 | 2026-10-31 | FFmpeg / Metadata | `ParseFFprobeJson_WithInvalidJson_ReturnsNull` | **PASS** | Tratamento gracioso de payloads JSON corrompidos. |
| T035 | 2026-10-31 | FFmpeg / Metadata | `TryParseFrameRate_HandlesVariousFormats` | **PASS** | Matriz de formatos de framerate (fracionário, direto, 0/0, nulo). |
| T036 | 2026-10-31 | FFmpeg / Thumbnail | `CalculateThumbnailTimestamp_StandardVideos_CalculatesExactPercentage` | **PASS** | Cálculo do timestamp seguro a 15% para durações variadas. |
| T037 | 2026-10-31 | FFmpeg / Thumbnail | `CalculateThumbnailTimestamp_VeryShortVideos_CapturesMiddleFrame` | **PASS** | Vídeos curtos (< 3s) capturam no ponto médio. |
| T038 | 2026-10-31 | FFmpeg / Thumbnail | `CalculateThumbnailTimestamp_ZeroOrNegativeDuration_ReturnsZero` | **PASS** | Tratamento de borda para durações inválidas ou zero. |
| T039 | 2026-10-31 | FFmpeg / Thumbnail | `GenerateThumbnailAsync_NonExistentVideo_ReturnsFailedResult` | **PASS** | Retorno gracioso de falha para arquivo de vídeo ausente. |
| T040 | 2026-10-31 | FFmpeg / Thumbnail | `ReplaceThumbnailAsync_ValidImage_CopiesToTargetDirectoryAndReturnsPath` | **PASS** | Cópia padronizada de imagem para cache local com nome baseado no VideoId. |
| T041 | 2026-10-31 | FFmpeg / Thumbnail | `ReplaceThumbnailAsync_NonExistentImage_ThrowsFileNotFoundException` | **PASS** | Exceção correta ao tentar substituir miniatura com arquivo inexistente. |
| T042 | 2026-10-31 | FFmpeg / Processor | `ProcessVideoMediaAsync_WhenMetadataAndThumbnailSucceed_UpdatesVideoRecord` | **PASS** | Atualização completa de duração, resolução e thumbnail no banco. |
| T043 | 2026-10-31 | FFmpeg / Processor | `ProcessVideoMediaAsync_WhenMetadataExtractorReturnsNull_PreservesVideoWithoutUpdating` | **PASS** | Preservação dos campos do vídeo quando extração falha. |
| T044 | 2026-10-31 | FFmpeg / Processor | `ProcessVideoMediaAsync_WhenVideoDoesNotExist_ReturnsFalse` | **PASS** | Retorno falso imediato quando VideoId não existe no catálogo. |
| T045 | 2026-10-31 | UI / LibraryViewModel | `LoadVideosAsync_PopulatesFilteredVideos_AndSetsFirstAsSelected` | **PASS** | Carregamento da biblioteca e seleção padrão do vídeo mais recente. |
| T046 | 2026-10-31 | UI / LibraryViewModel | `SearchText_FiltersVideosByFileNameOrCustomTitle` | **PASS** | Busca textual combinada por nome de arquivo e título customizado. |
| T047 | 2026-10-31 | UI / LibraryViewModel | `CurrentFilter_FiltersByStatusCorrectly` | **PASS** | Filtros rápidos (Apenas Local, Na Fila, Enviado e Arquivo Ausente). |
| T048 | 2026-10-31 | UI / LibraryViewModel | `PlayVideo_WhenVideoIsMissing_ShowsWarningAndDoesNotLaunchPlayer` | **PASS** | Proteção contra abertura de arquivos físicos ausentes no disco. |
| T049 | 2026-10-31 | UI / LibraryViewModel | `SaveMetadataChangesAsync_UpdatesDatabaseSuccessfully` | **PASS** | Persistência no banco SQLite de título e descrição customizados. |
| T050 | 2026-10-31 | Filters / Evaluator | `Evaluate_WithoutRule_AllowsStandardVideoExtensions` | **PASS** | Validação padrão de extensões de vídeo (.mp4, .mkv, .avi) e rejeição de .txt. |
| T051 | 2026-10-31 | Filters / Evaluator | `Evaluate_WhenFileSizeBelowMinimum_RejectsWithMinSizeStatus` | **PASS** | Rejeição por tamanho inferior ao mínimo configurado (ex: < 10 MB). |
| T052 | 2026-10-31 | Filters / Evaluator | `Evaluate_WhenFileSizeExceedsMaximumAndEnabled_RejectsWithMaxSizeStatus` | **PASS** | Rejeição por tamanho superior ao máximo quando limite está ativado. |
| T053 | 2026-10-31 | Filters / Evaluator | `Evaluate_WhenMaxSizeDisabled_AllowsFilesLargerThanMaxSizeBytes` | **PASS** | Aprovação de arquivos grandes quando o limite de tamanho máximo está desativado. |
| T054 | 2026-10-31 | Filters / Evaluator | `Evaluate_WhenDurationBelowMinimum_RejectsWithMinDurationStatus` | **PASS** | Rejeição por duração inferior ao limite de corte. |
| T055 | 2026-10-31 | Filters / Evaluator | `Evaluate_WhenDurationExceedsMaximum_RejectsWithMaxDurationStatus` | **PASS** | Rejeição por duração superior ao limite de corte. |
| T056 | 2026-10-31 | Filters / Evaluator | `Evaluate_WithRegexPattern_ValidatesFileNameCorrectly` | **PASS** | Validação de expressão regular sobre o nome do arquivo (ex: `^PUBG_`). |
| T057 | 2026-10-31 | Filters / Evaluator | `Evaluate_WhenSubdirectoriesNotAllowed_RejectsSubdirectoryFiles` | **PASS** | Rejeição de arquivos localizados em subpastas quando `IncludeSubdirectories = false`. |
| T058 | 2026-10-31 | Filters / Evaluator | `Evaluate_WhenSubdirectoriesAllowed_ApprovesSubdirectoryFiles` | **PASS** | Aceite de arquivos em subpastas quando permitido. |
| T059 | 2026-10-31 | Filters / Automation | `ProcessVideoMediaAsync_WhenAutoUploadEnabledAndFiltersApproved_EnqueuesVideoAutomatically` | **PASS** | Enfileiramento automático de replay aprovado em pasta com auto-upload ativo. |
| T060 | 2026-10-31 | Filters / Automation | `ProcessVideoMediaAsync_WhenFiltersRejected_SetsRejectedStatusAndDoesNotEnqueue` | **PASS** | Registro de status rejeitado e motivo descritivo sem enfileirar. |

**Total de Testes da Fase 5**: 69 executados, 69 aprovados (100% de sucesso).

---

## 3. Testes Pendentes para Próximas Fases
- [ ] `TitleFormatterTests`: Extração de timestamps de nomes de arquivo e formatação PUBG (Fase 6).
- [ ] `DescriptionTemplateEngineTests`: Substituição de variáveis dinâmicas em templates (Fase 6).
- [ ] `YouTubeUploaderTests`: Upload resumable e gerenciamento de cotas/tokens (Fase 6).
- [ ] `UploadQueueServiceTests`: Fila sequencial, persistência de checkpoints e throttling (Fase 7).
- [ ] `SqliteBackupEngineTests`: Criação de clone atômico do banco ativo em tempo de execução (Fase 8).

| T061 | 2026-10-31 | YouTube / Title | `FormatTitle_WhenFileNameContainsPubgTimestamp_ExtractsDateCorrectly` | **PASS** | Extra��o de data do nome do arquivo com separadores de ponto. |
| T062 | 2026-10-31 | YouTube / Title | `FormatTitle_WhenFileNameHasDashSeparators_ExtractsDateCorrectly` | **PASS** | Extra��o de data do nome com separadores tra�o. |
| T063 | 2026-10-31 | YouTube / Title | `FormatTitle_WhenNoTimestampInFileName_UsesRecordedAtUtc` | **PASS** | Fallback para data de grava��o UTC formatada em hora local. |
| T064 | 2026-10-31 | YouTube / Template | `RenderDescription_ReplacesAllSupportedVariables` | **PASS** | Substitui��o correta de todas as vari�veis no template de descri��o. |
| T065 | 2026-10-31 | YouTube / Template | `RenderDescription_EmptyTemplate_ReturnsEmptyString` | **PASS** | Template vazio retorna string vazia. |
| T066 | 2026-10-31 | Security / DPAPI | `SaveSecretAsync_AndReadSecretAsync_EncryptsAndDecryptsSuccessfully` | **PASS** | Criptografia com DPAPI e valida��o de arquivo bin�rio protegido no disco. |
| T067 | 2026-10-31 | Security / DPAPI | `DeleteSecretAsync_RemovesFileFromDisk` | **PASS** | Remo��o de credenciais do disco. |
| T068 | 2026-10-31 | YouTube / Uploader | `UploadVideoAsync_ReturnsFailed_WhenFileMissing` | **PASS** | Retorno gracioso de falha caso o arquivo n�o exista no disco. |
| T069 | 2026-10-31 | YouTube / Uploader | `UploadVideoAsync_ReturnsFailed_WhenUnauthenticated` | **PASS** | Bloqueio de upload quando n�o h� credencial do YouTube salva. |
| T070 | 2026-10-31 | Upload / Processor | `ProcessQueueAsync_WhenQueueEmpty_ExitsGracefully` | **PASS** | Fila vazia finaliza processamento sem invocar uploader. |
| T071 | 2026-10-31 | Upload / Processor | `ProcessQueueAsync_WhenUploadSucceeds_UpdatesQueueItemToCompleted` | **PASS** | Item processado marcado como Completed e ID do YouTube salvo. |
| T072 | 2026-10-31 | Upload / Processor | `ProcessQueueAsync_WhenUploadFails_UpdatesQueueItemToFailed` | **PASS** | Falha de upload salva mensagem de erro e marca status como Failed. |

**Total de Testes da Fase 6**: 81 executados, 81 aprovados (100% de sucesso).

| T073 | 2026-10-31 | Upload / Queue | `EnqueueVideoAsync_WhenVideoExists_CreatesAndPersistsQueueItem` | **PASS** | Enfileiramento de v�deo com defini��o correta de prioridade e persist�ncia. |
| T074 | 2026-10-31 | Upload / Queue | `PauseItemAsync_WhenItemExists_UpdatesStatusToPaused` | **PASS** | Transi��o para estado Paused persistida no SQLite. |
| T075 | 2026-10-31 | Upload / Queue | `ResumeItemAsync_WhenPaused_UpdatesStatusToQueued` | **PASS** | Retomada de item pausado para a fila. |
| T076 | 2026-10-31 | Upload / Queue | `CancelItemAsync_RemovesItemFromQueue_AndResetsVideoStatus` | **PASS** | Cancelamento remove da fila e restaura status do v�deo para None. |
| T077 | 2026-10-31 | Upload / Throttle | `ThrottledStream_EnforcesMaxBytesPerSecond` | **PASS** | Limitador de taxa em bytes por segundo aplicado corretamente em streaming. |

**Total de Testes da Fase 7**: 86 executados, 86 aprovados (100% de sucesso).
