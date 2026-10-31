# Development Status: Replay Manager

**Última Atualização**: 2026-10-31 13:35 (Fase 7 Concluída)  
**Versão Atual**: 0.8.0-alpha.7

---

## 1. Visão Geral do Progresso

| Fase | Descrição | Status | Progresso |
| :--- | :--- | :--- | :--- |
| **Fase 0** | Preparação do Ambiente e Estrutura Inicial | **Concluída** | 100% |
| **Fase 1** | Fundação e Persistência SQLite | **Concluída** | 100% |
| **Fase 2** | Monitoramento de Pastas e Ingestão | **Concluída** | 100% |
| **Fase 3** | Integração FFmpeg e Miniaturas | **Concluída** | 100% |
| **Fase 4** | Biblioteca de Vídeos e Interface WPF | **Concluída** | 100% |
| **Fase 5** | Motor de Filtros e Automação PUBG | **Concluída** | 100% |
| **Fase 6** | Integração YouTube API v3 | **Concluída** | 100% |
| **Fase 7** | Fila Persistente e Resiliente de Upload | **Concluída** | 100% |
| **Fase 8** | Backup no Google Drive | **Próxima** | 0% |
| **Fase 9** | Bandeja, Inicialização e Acabamento | Pendente | 0% |
| **Fase 10** | Validação, Estresse e Empacotamento | Pendente | 0% |

---

## 2. Detalhamento do Estado Atual (Fase 7 - 100% Concluída)

### Módulos Concluídos
- [x] **Fase 1 — Fundação e Persistência SQLite**:
  - Entidades de Domínio, SQLite WAL mode, Fluent API, repositórios, UnitOfWork e 9 testes de persistência.
- [x] **Fase 2 — Monitoramento de Pastas e Ingestão**:
  - `IContentHasher`, `IFileStabilityChecker`, `IFileWatcherService`, `IFolderReconciliationWorker`, `IVideoIngestionService`.
  - Hashing em streaming 64 KB, detecção não bloqueante de estabilidade e liberação de locks de escrita, monitoramento multi-pasta com debounce, deduplicação intra-pasta e reconciliação com `LocalFileStatus.Missing`.
  - 18 testes automatizados adicionados.
- [x] **Fase 3 — Integração FFmpeg e Miniaturas**:
  - `IFFmpegPathResolver`, `IMediaMetadataExtractor`, `IThumbnailGenerator`, `IVideoMediaProcessor`.
  - Extração de duração, resolução, codecs, taxa de bits e framerates via FFprobe.
  - Geração de thumbnails no percentual de 15% e substituição manual de miniaturas.
  - 26 testes automatizados adicionados.
- [x] **Fase 4 — Biblioteca de Vídeos e Interface WPF**:
  - MVVM com CommunityToolkit, casca Dark Theme em abas, grade virtualizada, busca em tempo real, badges de status, painel de detalhes, `MediaPlayerLauncher` (VLC/padrão) e diálogos nativos.
  - 5 testes automatizados adicionados.
- [x] **Fase 5 — Motor de Filtros e Automação PUBG**:
  - `IFilterEvaluator` e `FilterEvaluator` em `ReplayManager.Application`.
  - Avaliação de extensões permitidas, faixas de tamanho min/max, duração min/max, expressões regulares no nome e subpastas.
  - Auto-enfileiramento integrado no `VideoMediaProcessor` quando `AutoUploadEnabled = true`.
  - 11 testes automatizados adicionados.
- [x] **Fase 6 — Integração YouTube API v3**:
  - `DpapiCredentialStorage` para armazenamento seguro de credenciais via Windows DPAPI.
  - `YouTubeAuthService` OAuth 2.0 com `GoogleWebAuthorizationBroker` e perfil do canal.
  - `TitleFormatter` canônico PUBG e `DescriptionTemplateEngine` com variáveis dinâmicas.
  - `YouTubeUploader` com suporte a uploads resumable e miniaturas.
  - 12 testes automatizados adicionados.
- [x] **Fase 7 — Fila Persistente e Resiliente de Upload**:
  - **Serviço de Fila Sequencial e Resiliente**:
    - `IUploadQueueService` e `UploadQueueService`: operações completas de enfileiramento, pausa, retomada, cancelamento, reordenação de prioridade e retry manual com eventos em tempo real (`ProgressUpdated`, `StatusChanged`).
  - **Bandwidth Throttling (Limitador de Velocidade)**:
    - `ThrottledStream` limitando a taxa de bytes transmitidos por segundo em streaming contínuo.
  - **Processamento de Fila e Hosted Background Service**:
    - `UploadQueueProcessor` processando estritamente 1 vídeo por vez, calculando velocidade instantânea e persistindo checkpoints de transição de estado no SQLite com `UnitOfWork`.
    - `UploadQueueBackgroundService` executando como `IHostedService` em segundo plano para ciclo contínuo de uploads.
  - **Interface WPF da Fila de Upload**:
    - `UploadQueueViewModel` e `UploadQueueItemViewModel` gerenciando lista reativa, porcentagens, tamanhos legíveis, velocidades instantâneas e comandos de item.
    - `UploadQueueView.xaml` integrada na casca principal (`MainWindow.xaml`) com Dark Theme, miniatura, barra de progresso e botões operacionais.
  - **Suíte de Testes Automatizados**:
    - `UploadQueueServiceTests` (5 cenários de enfileiramento, pausa, retomada, cancelamento e limitação de taxa via `ThrottledStream`).
    - Total de 86 testes automatizados aprovados (100% de sucesso).

### Módulos Em Andamento / Próxima Sessão
- **Fase 8 — Backup no Google Drive**:
  - Clonagem atômica do SQLite em tempo de execução via `SqliteConnection.BackupDatabase` (sem interromper leituras ou escritas ativas).
  - Integração com Google Drive API v3 (pasta do aplicativo ou AppData folder no Drive).
  - Versionamento e retenção dos últimos N backups configurável.
  - Rotina de verificação de integridade pós-upload (`PRAGMA integrity_check` no arquivo antes de enviar).

---

## 3. Problemas Conhecidos e Mitigações

1. **Dispatcher na UI**: Eventos de progresso disparados por threads de background no `IUploadQueueService` são despachados de forma segura para o thread de UI via `System.Windows.Application.Current.Dispatcher.Invoke`.
2. **Throttling de Banda**: Implementado no nível do `Stream` transmitido, garantindo compatibilidade direta com qualquer classe consumidora de stream (incluindo `ResumableUpload` da Google API) sem acoplamentos de rede de baixo nível.

---

## 4. Última Etapa Validada
- `dotnet test ReplayManager.slnx`: 86 testes executados, 86 aprovados, 0 falhas (100% de sucesso).
- `dotnet build ReplayManager.slnx`: 0 erros, 0 avisos.

## 5. Próxima Tarefa Exata
- Iniciar a **Fase 8**: Implementar clonagem atômica do banco de dados SQLite via API nativa de backup (`ISqliteBackupService`), serviço de sincronização com Google Drive API v3 (`IGoogleDriveBackupService`) e rotina de validação de integridade do arquivo de backup.
