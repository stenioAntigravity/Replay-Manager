# Architecture: Replay Manager

## 1. Visão Geral da Arquitetura
O **Replay Manager** adota os princípios da **Clean Architecture** combinados com o padrão de apresentação **MVVM (Model-View-ViewModel)**. A arquitetura é desenhada para garantir:
1. **Desacoplamento Total**: O núcleo do domínio e regras de negócio não conhecem WPF, SQLite, Google APIs ou o sistema de arquivos específico.
2. **Testabilidade Isolada**: Todas as fronteiras externas (disco, FFmpeg, APIs de nuvem, banco de dados) são acessadas através de interfaces injetáveis via Injeção de Dependência (DI).
3. **Resiliência a Falhas**: Operações assíncronas longas (como hash de arquivos, análise FFmpeg e upload) rodam em background sem bloquear a UI e com persistência de estado a cada transição crítica.
4. **Continuidade de Sessão**: Cada módulo possui limites estritos para permitir desenvolvimento incremental.

---

## 2. Diagrama de Camadas

```mermaid
flowchart TD
    subgraph UI ["ReplayManager.App (WPF + MVVM)"]
        Views[Views / Controls]
        ViewModels[ViewModels]
        Tray[Tray Icon & Background Service]
    end

    subgraph AppCore ["ReplayManager.Application"]
        Services[Application Services]
        UseCases[Use Cases / Commands / Queries]
        DTOs[DTOs & Mappers]
        Interfaces[Interfaces para Infraestrutura]
    end

    subgraph DomainCore ["ReplayManager.Domain"]
        Entities[Entities / Aggregates]
        ValueObjects[Value Objects]
        DomainEvents[Domain Events]
        Enums[Enums & Consts]
    end

    subgraph Infra ["ReplayManager.Infrastructure"]
        Persistence[EF Core / SQLite / Migrations]
        FFmpegInfra[FFmpeg Wrapper / Metadata / Thumbnails]
        FolderWatcher[Directory Watcher & FileLock Check]
        YouTubeInfra[YouTube Data API v3 & OAuth 2.0]
        DriveInfra[Google Drive Backup & SQLite Backup API]
        Security[DPAPI Credential Storage]
    end

    UI --> AppCore
    AppCore --> DomainCore
    Infra --> AppCore
    Infra --> DomainCore
    UI --> Infra
```

---

## 3. Divisão dos Projetos e Responsabilidades

### 3.1. `ReplayManager.Domain`
- **Responsabilidade**: Contém os conceitos de negócio puros, entidades e regras invariantes.
- **Dependências externas**: Zero (apenas BCL/.NET padrão).
- **Entidades Principais**:
  - `MonitoredFolder`: Pasta monitorada com suas regras de filtro e auto-upload.
  - `FolderFilterRule`: Regras de filtragem por extensão, tamanho (min/max), duração (min/max), subpastas, regex de nome.
  - `VideoRecord`: Registro canônico do vídeo, metadados (resolução, duração, bitrate), hash SHA-256 local, caminhos, situação local e situação remota.
  - `UploadQueueItem`: Item na fila de envio, status de progresso, bytes transferidos, velocidade, tentativas, erro e link resultante.
  - `YouTubeAccount`: Estado de autenticação da conta, channel ID, channel title, data de concessão de token.
  - `DescriptionTemplate`: Template com marcadores para geração de descrição.
- **Enums**:
  - `LocalFileStatus`: `Available`, `Missing`, `LockedInRecording`, `Corrupted`.
  - `UploadStatus`: `None`, `Queued`, `Preparing`, `Uploading`, `Paused`, `Interrupted`, `Failed`, `Completed`.
  - `YouTubePrivacyStatus`: `Unlisted` (padrão), `Private`, `Public`.
  - `FilterEvaluationStatus`: `Approved`, `RejectedByMinSize`, `RejectedByMaxSize`, `RejectedByMinDuration`, `RejectedByMaxDuration`, `RejectedByExtension`, `RejectedByNamePattern`.

### 3.2. `ReplayManager.Application`
- **Responsabilidade**: Casos de uso da aplicação, orquestração de fluxos e validações.
- **Principais Componentes**:
  - `IMonitoredFolderService`: Gestão de pastas cadastradas, acionamento de reconciliação manual/automática.
  - `IVideoCatalogService`: Consulta, paginação, filtros, pesquisa e ordenação de vídeos na biblioteca.
  - `IUploadQueueService`: Enfileiramento, pausa, cancelamento, reordenação e retry de uploads.
  - `IVideoAnalysisService`: Orquestração de cálculo de hash e extração de thumbnail/metadados.
  - `IYouTubeService`: Contrato para autenticação, verificação de canal, upload retomável e edição de metadados.
  - `IBackupService`: Contrato para criação de backup SQLite consistente e upload no Google Drive.
  - `ISystemSettingsService`: Preferências globais (comportamento de fechamento, limite de velocidade, inicialização com Windows).

### 3.3. `ReplayManager.Infrastructure`
- **Responsabilidade**: Implementações concretas de I/O, banco de dados, rede e bibliotecas nativas.
- **Principais Componentes**:
  - `ReplayManagerDbContext`: DbContext SQLite com configuração Fluent API e histórico de migrações.
  - `FileSystemWatcherEngine`: Monitoramento nativo via `FileSystemWatcher` com debounce, verificação de estabilidade de gravação (tentativa de leitura sem lock com intervalo configurável) e rotina periódica de varredura (polling de reconciliação) para compensar buffers cheios do OS.
  - `FFmpegMediaService`: Process execution do FFmpeg para extrair frame e capturar duração/resolução sem travar a aplicação.
  - `YouTubeClientWrapper`: Implementação utilizando `Google.Apis.YouTube.v3` e `ResumableUpload<T>` com reporte de progresso granular.
  - `SqliteBackupEngine`: Utilização de `sqlite3_backup` ou `Microsoft.Data.Sqlite` para clonar o banco ativo em estado consistente atômico.
  - `DpapiDataProtector`: Criptografia de tokens de refresh e credenciais com `ProtectedData.Protect` (`DataProtectionScope.CurrentUser`).

### 3.4. `ReplayManager.App`
- **Responsabilidade**: Interface de usuário WPF, views, estilos XAML, ViewModels e integração com a bandeja do sistema (Tray Icon).
- **Padrão**: MVVM com `CommunityToolkit.Mvvm` (`ObservableObject`, `RelayCommand`, `WeakReferenceMessenger`).
- **Navegação**: ShellWindow com menu lateral e áreas dedicadas:
  1. **Biblioteca**: Grid de miniaturas, cards de vídeo com badges visuais de estado, barra de busca e filtros, visualizador de detalhes lateral.
  2. **Fila de Upload**: Lista sequencial, progresso detalhado de bytes/segundo, botões de pausa/retomada/reordenação.
  3. **Pastas Monitoradas**: Gerenciamento de diretórios, regras por pasta, status de varredura.
  4. **YouTube**: Painel de login/logout, informações do canal, modelos de descrição.
  5. **Backup & Nuvem**: Histórico de backups, botão de backup imediato e restauração.
  6. **Configurações & Logs**: Opções de inicialização, comportamento de fechamento de janela (Opção C), throttling de banda, visualizador e exportador de logs.

### 3.5. `ReplayManager.Tests`
- **Responsabilidade**: Testes automatizados cobrindo:
  - Avaliação de regras de filtro de vídeo (Domain).
  - Deduplicação intra-pasta e isolamento inter-pastas (Application & Persistence).
  - Serialização de estado da fila de upload (Application & Infrastructure).
  - Geração de títulos e modelos de descrição.
  - Tratamento de locks de gravação de arquivos novos.

---

## 4. Fluxos de Dados Críticos

### 4.1. Detecção e Ingestão de Vídeo Novo
```mermaid
sequenceDiagram
    participant OS as Sistema de Arquivos / OBS
    participant Watcher as FileSystemWatcherEngine
    participant Stability as FileLockInspector
    participant Catalog as VideoCatalogService
    participant FFmpeg as FFmpegMediaService
    participant DB as SQLite DB
    participant Queue as UploadQueueService

    OS->>Watcher: Evento OnCreated / OnChanged
    Watcher->>Stability: Aguarda estabilidade de tamanho (polling não bloqueante)
    Stability->>Stability: Testa abertura com FileShare.None até sucesso
    Stability->>Catalog: Notifica arquivo liberado
    Catalog->>Catalog: Calcula SHA-256
    Catalog->>DB: Verifica duplicata na mesma pasta
    alt Arquivo não duplicado na mesma pasta
        Catalog->>FFmpeg: Extrai metadados e thumbnail
        FFmpeg-->>Catalog: Thumbnail salvo no cache local + Metadados
        Catalog->>Catalog: Aplica FolderFilterRules da pasta
        Catalog->>DB: Salva VideoRecord (com status de aprovação/reprovação)
        opt Auto-Upload habilitado e Aprovado nos filtros
            Catalog->>Queue: Adiciona item à fila de upload
            Queue->>DB: Persiste UploadQueueItem (Status: Queued)
        end
    else Duplicata detectada na mesma pasta
        Catalog->>Catalog: Descarta ou atualiza caminho se for renomeação
    end
```

### 4.2. Execução da Fila de Upload Resiliente
```mermaid
sequenceDiagram
    participant Queue as UploadQueueService
    participant DB as SQLite DB
    participant YT as YouTubeClientWrapper
    participant API as YouTube Data API v3

    Queue->>DB: Busca próximo item onde Status == Queued (ordenado por ordem)
    Queue->>DB: Atualiza Status para Uploading
    Queue->>YT: Inicia Resumable Upload (com limites de taxa)
    loop A cada chunk enviado
        YT->>API: Transmite bytes
        API-->>YT: Progresso (bytes enviados / total)
        YT->>Queue: Notifica progresso (bytes/s, %)
        Queue->>DB: Salva checkpoint de progresso
    end
    alt Sucesso no Envio
        API-->>YT: Retorna Video ID e link público
        YT->>Queue: Upload concluído com sucesso
        Queue->>DB: Atualiza VideoRecord (Status: Completed, YouTubeId, Url)
        Queue->>DB: Atualiza UploadQueueItem (Status: Completed)
    else Falha transitória de rede / Crash / Interrupção
        YT-->>Queue: Notifica interrupção
        Queue->>DB: Atualiza UploadQueueItem (Status: Interrupted / Retrying)
        Note over Queue,DB: Estado salvo para retomar após restart
    end
```

---

## 5. Decisões Técnicas Estruturantes
1. **Deduplicação Restrita à Pasta**: Arquivos idênticos em pastas diferentes são cadastrados como vídeos independentes. Dentro da mesma pasta, arquivos com o mesmo SHA-256 são tratados como duplicatas.
2. **Nenhuma Exclusão Automática**: O Replay Manager nunca invoca `File.Delete` em arquivos de vídeo do usuário.
3. **Persistência First na Fila**: Qualquer transição de estado da fila é imediatamente comitada no SQLite com transação antes de disparar o evento na UI.
4. **Resumable Upload do YouTube**: Uploads utilizam streaming em chunks, permitindo retomar de onde parou em caso de queda de conexão.
5. **Comportamento de Fechamento (Opção C)**: Janela modal de escolha ao fechar:
   - Minimizar para bandeja e continuar executando.
   - Fechar janela mantendo serviço de background.
   - Encerrar completamente (com aviso se houver upload ativo e garantia de cancelamento limpo com persistência de checkpoint).
