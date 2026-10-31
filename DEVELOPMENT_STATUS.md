# Development Status: Replay Manager

**Última Atualização**: 2026-10-31 02:30 (Fase 5 Concluída)  
**Versão Atual**: 0.6.0-alpha.5

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
| **Fase 6** | Integração YouTube API v3 | **Próxima** | 0% |
| **Fase 7** | Fila Persistente e Resiliente de Upload | Pendente | 0% |
| **Fase 8** | Backup no Google Drive | Pendente | 0% |
| **Fase 9** | Bandeja, Inicialização e Acabamento | Pendente | 0% |
| **Fase 10** | Validação, Estresse e Empacotamento | Pendente | 0% |

---

## 2. Detalhamento do Estado Atual (Fase 5 - 100% Concluída)

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
  - **Motor de Filtros Determinístico**:
    - `IFilterEvaluator` e `FilterEvaluator` em `ReplayManager.Application`:
      - Avaliação de extensões permitidas (`AllowedExtensions`, default `.mp4;.mkv;.avi`).
      - Avaliação de tamanho mínimo (`MinSizeBytes`, default 10 MB) e tamanho máximo opcional (`MaxSizeBytes`, default 100 MB, toggle `IsMaxSizeEnabled`).
      - Avaliação de duração mínima e máxima em segundos (`MinDurationSeconds`, `MaxDurationSeconds`).
      - Validação de expressões regulares / padrão no nome do arquivo (`NamePatternRegex`).
      - Validação de diretório raiz vs subpastas (`IncludeSubdirectories`).
    - DTO de resultado estruturado `FilterEvaluationResult` com status enumerado e mensagem descritiva amigável.
  - **Fluxo de Automação Integrado**:
    - Integração de `IFilterEvaluator` no `VideoMediaProcessor`:
      - Imediatamente após extrair metadados técnicos e gerar a miniatura, o vídeo é submetido às regras de filtro da sua pasta proprietária.
      - O resultado é salvo no registro (`FilterStatus` e `RejectionReason`).
      - Se a pasta monitorada possuir `AutoUploadEnabled = true` (ex: pasta de replays/destaques do PUBG) e o vídeo for aprovado, ele é automaticamente enfileirado na fila de uploads (`UploadQueueItem`) com prioridade e status `Queued`.
  - **Injeção de Dependências**:
    - Registro de `IFilterEvaluator` no container de injeção em `AddInfrastructureFFmpeg()`.
  - **Suíte de Testes Automatizados**:
    - 9 testes unitários em `FilterEvaluatorTests` cobrindo todas as permutações de regras de corte.
    - 2 testes de integração em `VideoMediaProcessorAutomationTests` simulando o fluxo de aprovação com auto-enfileiramento e reprovação documentada.
    - Total de 69 testes automatizados na solução (100% de sucesso).

### Módulos Em Andamento / Próxima Sessão
- **Fase 6 — Integração YouTube API v3**:
  - Autenticação OAuth 2.0 com `GoogleWebAuthorizationBroker` e armazenamento seguro de tokens via Windows DPAPI.
  - Gerador de títulos automáticos (`PUBG Replay - DD/MM/AAAA - HH:MM:SS`) e templates de descrição dinâmicos.
  - Resumable upload de vídeo para YouTube com privacidade padrão "Não listado" (`unlisted`).

---

## 3. Problemas Conhecidos e Mitigações

1. **Subpastas não autorizadas**: O `FilterEvaluator` compara o caminho canônico do diretório do arquivo com o diretório base da pasta cadastrada, rejeitando subdiretórios se `IncludeSubdirectories = false`.
2. **Regex inválido informado pelo usuário**: Capturado com `try/catch (ArgumentException)` no avaliador para evitar interrupção do pipeline de processamento de mídia.

---

## 4. Última Etapa Validada
- `dotnet test ReplayManager.slnx`: 69 testes executados, 69 aprovados, 0 falhas (100% de sucesso).
- `dotnet build ReplayManager.slnx`: 0 erros, 0 avisos.

## 5. Próxima Tarefa Exata
- Iniciar a **Fase 6**: Implementar serviços de autenticação OAuth 2.0 do Google/YouTube (`IYouTubeAuthService`), proteção de tokens via Windows DPAPI, gerador de títulos automáticos PUBG e upload resumable com metadados do vídeo.
