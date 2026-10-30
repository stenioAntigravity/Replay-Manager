# Architecture & Technical Decisions (ADR)

Este arquivo registra as decisões arquiteturais e técnicas tomadas para o **Replay Manager**, mantendo o histórico inalterado para auditoria e continuidade entre sessões.

---

## ADR 001: Adoção do .NET 10 LTS e SDK Local User-Level
- **Data**: 2026-10-30
- **Contexto**: A especificação solicita prioritariamente C# com .NET 10 LTS e WPF para Windows. O ambiente da máquina não possuía SDK instalado em `Program Files` e o instalador MSI/Bundle pelo winget falhou por requerer elevação administrativa (UAC).
- **Decisão**: Utilizar a ferramenta oficial `dotnet-install.ps1` da Microsoft para instalar o .NET SDK 10.0.403 diretamente no perfil de usuário (`%LOCALAPPDATA%\Microsoft\dotnet`), configurando `DOTNET_ROOT` e atualizando o `PATH` do usuário.
- **Alternativas Descartadas**:
  - .NET 6 (apenas runtime desatualizado estava instalado no host).
  - Execução com elevação interativa (inviável em ambiente CLI automatizado).
- **Consequências**: Ambiente 100% compatível com a especificação, com `Microsoft.WindowsDesktop.App` 10.0 disponível para WPF e sem requerer privilégios administrativos.

---

## ADR 002: Separação Arquitetural Clean Architecture + MVVM
- **Data**: 2026-10-30
- **Contexto**: O Replay Manager precisa de alta manutenibilidade, testabilidade sem UI e desacoplamento de serviços externos (FFmpeg, YouTube API, Google Drive, sistema de arquivos).
- **Decisão**:
  1. `ReplayManager.Domain`: Contém entidades de negócio, enums, contratos invariantes e zero referências externas.
  2. `ReplayManager.Application`: Orquestração de casos de uso, interfaces para infraestrutura e DTOs.
  3. `ReplayManager.Infrastructure`: Implementações EF Core/SQLite, FFmpeg runner, YouTube API v3, Google Drive, FileSystemWatcher e DPAPI.
  4. `ReplayManager.App`: Aplicação WPF com `CommunityToolkit.Mvvm`, XAML moderno, VirtualizingWrapPanel e Tray Icon.
  5. `ReplayManager.Tests`: Projeto de testes xUnit cobrindo regras de filtro, persistência e estabilização de arquivos.
- **Alternativas Descartadas**:
  - Arquitetura monolítica em camada única com ViewModels acessando banco diretamente (inadequada para testes e evolução modular).
- **Consequências**: Independência de componentes, facilidade para criar mocks e substituição controlada de implementações.

---

## ADR 003: Deduplicação de Arquivos por Hash com Escopo Intra-Pasta
- **Data**: 2026-10-30
- **Contexto**: A especificação estipula que arquivos idênticos em pastas diferentes devem ser tratados como registros distintos, enquanto na mesma pasta devem ser identificados como duplicatas via SHA-256 (mesmo que renomeados).
- **Decisão**: A chave de unicidade de conteúdo no banco SQLite será composta por `(MonitoredFolderId, ContentSha256)`. Ao escanear uma pasta, se o hash já existir para aquela mesma pasta, o registro existente é atualizado (ex: novo path se for renomeação) sem criar um novo vídeo. Se existir em outra pasta, cria-se um registro independente associado à respectiva pasta.
- **Alternativas Descartadas**: Deduplicação global por hash (descartada expressamente pela regra da seção 5.1).
- **Consequências**: Permite ao usuário monitorar pastas com propósitos diferentes (ex: pasta de clips editados vs pasta de replays brutos) mesmo contendo arquivos duplicados.

---

## ADR 004: Detecção Confiável de Conclusão de Gravação (File Lock + Stability)
- **Data**: 2026-10-30
- **Contexto**: Softwares de gravação (OBS, AMD Adrenalin, GeForce Experience) criam arquivos e mantêm streams abertos gravando em blocos ao longo de minutos. O `FileSystemWatcher` dispara `Created` no primeiro byte. Processar o arquivo imediatamente causaria leitura de vídeo incompleto ou exceção de compartilhamento de arquivo (`IOException: The process cannot access the file`).
- **Decisão**: Criar um componente `FileStabilityInspector` que adota estratégia mista:
  1. Monitoramento periódico do tamanho (`FileInfo.Length`).
  2. Tentativa não bloqueante de abertura com `FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None)`.
  3. Somente quando o tamanho permanecer inalterado por um limiar (ex: 3 a 5 segundos) E a abertura com exclusividade obtiver sucesso, o arquivo é marcado como liberado para cálculo de hash e análise FFmpeg.
  4. Rotina de reconciliação em background para arquivos que eventualmente fiquem em estado pendente.
- **Alternativas Descartadas**:
  - Aguardar apenas um tempo fixo (ineficaz para vídeos de durações imprevisíveis).
  - Bloquear a thread principal (inaceitável para responsividade).
- **Consequências**: Processamento 100% seguro contra arquivos incompletos.

---

## ADR 005: Tratamento de Vídeos Rejeitados por Filtros
- **Data**: 2026-10-30
- **Contexto**: Vídeos que não passam nas regras de tamanho mínimo/máximo, duração ou padrões de nome devem ser preservados na biblioteca, sem exclusão e sem auto-upload.
- **Decisão**: A entidade `VideoRecord` possui a propriedade `FilterEvaluationResult` com o status (`Approved`, `RejectedByMinSize`, etc.) e a propriedade `RejectionReason` contendo a justificativa formatada legível. A interface exibe essa informação nos detalhes e em badges. O usuário mantém a liberdade de acionar "Enviar Manualmente" ou "Ignorar Filtros".
- **Consequências**: Total transparência para o usuário sem descartar nenhum arquivo.

---

## ADR 006: Upload Resumable Sequencial e Persistência no SQLite
- **Data**: 2026-10-30
- **Contexto**: Falhas de rede, queda de energia ou fechamento acidental podem interromper uploads de arquivos pesados (centenas de megabytes).
- **Decisão**: A fila de upload persiste no SQLite cada transição de estado (`Queued`, `Uploading`, `Interrupted`, `Completed`), o `UploadUri` do resumable upload do Google e os bytes já enviados. Na primeira versão, o processamento será estritamente de 1 vídeo por vez para garantir estabilidade e economia de banda.
- **Alternativas Descartadas**:
  - Upload direto em memória sem salvar estado intermediário.
  - Uploads simultâneos concorrentes na primeira versão.
- **Consequências**: Recuperação confiável de interrupções sem duplicações de vídeo no YouTube.

---

## ADR 007: Preservação Irrestrita de Arquivos Locais
- **Data**: 2026-10-30
- **Contexto**: A integridade dos dados locais do usuário é prioritária. O aplicativo nunca deve apagar arquivos de vídeo.
- **Decisão**: Não existe código no Replay Manager que execute exclusão no disco para arquivos de vídeo monitorados. Caso um arquivo seja apagado pelo usuário fora do aplicativo, o registro do banco não é deletado: seu status passa para `LocalMissing`, mantendo o thumbnail salvo no cache interno, dados históricos e link do YouTube.
- **Consequências**: Previne qualquer perda acidental de gravações valiosas.

---

## ADR 008: Armazenamento Seguro de Credenciais e Tokens com Windows DPAPI
- **Data**: 2026-10-30
- **Contexto**: Tokens de acesso e refresh tokens do OAuth 2.0 não devem ser salvos em texto claro no banco de dados SQLite ou em arquivos de configuração.
- **Decisão**: Cifrar os tokens usando `ProtectedData.Protect` com escopo `DataProtectionScope.CurrentUser`. Apenas a conta de usuário atual do Windows consegue decifrar os dados.
- **Alternativas Descartadas**: Tokens em JSON plano no SQLite ou em App.config.
- **Consequências**: Conformidade com boas práticas de segurança desktop no Windows.
