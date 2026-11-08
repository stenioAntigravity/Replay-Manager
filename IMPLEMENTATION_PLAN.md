# Implementation Plan: Replay Manager

Este plano descreve o roteiro de desenvolvimento em 11 fases estruturadas, respeitando o princípio de desenvolvimento modular, testes e continuidade entre sessões.

---

## Matriz de Fases e Dependências

`mermaid
flowchart TD
    F0[Fase 0: Preparação do Ambiente e Estrutura] --> F1[Fase 1: Fundação e Persistência SQLite]
    F1 --> F2[Fase 2: Monitoramento de Pastas e Ingestão]
    F2 --> F3[Fase 3: Integração FFmpeg e Miniaturas]
    F3 --> F4[Fase 4: Biblioteca de Vídeos e Interface WPF]
    F4 --> F5[Fase 5: Motor de Filtros e Automação PUBG]
    F5 --> F6[Fase 6: Integração YouTube API v3]
    F6 --> F7[Fase 7: Fila Persistente e Resiliente de Upload]
    F7 --> F8[Fase 8: Backup no Google Drive]
    F8 --> F9[Fase 9: Bandeja, Inicialização e Acabamento]
    F9 --> F10[Fase 10: Testes Integrados e Empacotamento]
`

---

## Detalhamento das Fases

### Fase 0 — Preparação do Ambiente e Estrutura (CONCLUÍDA)
- **Objetivo**: Garantir que o ambiente possua os SDKs necessários, repositório Git limpo, documentação de continuidade viva, solução .NET criada com a arquitetura definida e compilação sem erros.
- **Tarefas**:
  - [x] Avaliar especificações e riscos técnicos.
  - [x] Inicializar Git e .gitignore.
  - [x] Configurar .NET 10 SDK no ambiente.
  - [x] Criar solução ReplayManager.slnx com os projetos:
    - ReplayManager.Domain (ClassLib, net10.0)
    - ReplayManager.Application (ClassLib, net10.0)
    - ReplayManager.Infrastructure (ClassLib, net10.0-windows)
    - ReplayManager.App (WinExe WPF, net10.0-windows)
    - ReplayManager.Tests (xUnit, net10.0)
  - [x] Configurar referências entre os projetos.
  - [x] Gerar documentação de continuidade.
  - [x] Compilar solução inicial com dotnet build e validar saída.
- **Critério de Aceite**: Solução criada e compilando com zero erros e warnings críticos; documentos criados e git commit inicial realizado.

---

### Fase 1 — Fundação e Persistência SQLite (CONCLUÍDA)
- **Objetivo**: Modelar as entidades do domínio e persistência com EF Core e SQLite.
- **Tarefas**:
  - [x] Implementar entidades do Domínio: VideoRecord, MonitoredFolder, FolderFilterRule, UploadQueueItem, YouTubeAccount, DescriptionTemplate.
  - [x] Configurar ReplayManagerDbContext com Fluent API, chaves, índices e restrições de integridade.
  - [x] Configurar modo WAL (PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;).
  - [x] Implementar repositórios e UnitOfWork.
  - [x] Testes de persistência (operações CRUD, integridade referencial, concorrência básica).
- **Critério de Aceite**: Banco SQLite criado, migrations/database initializer funcionando e testes de persistência passando (9/9 testes).

---

### Fase 2 — Monitoramento de Pastas e Ingestão (CONCLUÍDA)
- **Objetivo**: Implementar o monitoramento do sistema de arquivos e detecção de replays.
- **Tarefas**:
  - [x] Monitor de pastas com suporte a múltiplos diretórios e subdiretórios configuráveis.
  - [x] Detecção segura de término de gravação (verificação de file lock e tamanho estável para não processar arquivos incompletos do OBS/GeForce).
  - [x] Identificação de arquivos duplicados por hash SHA-256 (escopo por pasta).
  - [x] Reconciliação em segundo plano para arquivos apagados pelo usuário no disco (marcar LocalMissing sem deletar registro).
- **Critério de Aceite**: Testes de estabilização de arquivo e monitoramento aprovados (18/18 testes).

---

### Fase 3 — Integração FFmpeg e Miniaturas (CONCLUÍDA)
- **Objetivo**: Integrar binários FFmpeg para extração de metadados e miniaturas.
- **Tarefas**:
  - [x] Resolução automática de binários fmpeg.exe e fprobe.exe (pasta tools ou PATH).
  - [x] Extração confiável de metadados técnicos (duração, resolução, codec, bitrate, framerate).
  - [x] Geração automática de miniatura a 15% da duração do vídeo.
  - [x] Suporte a alteração manual de miniatura a partir de arquivo de imagem existente.
  - [x] Tratamento de exceções para arquivos de vídeo corrompidos ou não reconhecidos.
- **Critério de Aceite**: Extração de metadados e miniaturas funcionais e testadas com arquivos de teste (26/26 testes).

---

### Fase 4 — Biblioteca de Vídeos e Interface WPF (CONCLUÍDA)
- **Objetivo**: Desenvolver a interface visual principal para navegação dos replays.
- **Tarefas**:
  - [x] Janela principal WPF moderna no padrão Dark Theme.
  - [x] Navegação por abas: Biblioteca, Fila de Upload, Pastas Monitoradas, Configurações.
  - [x] Grade virtualizada de miniaturas com rolagem suave.
  - [x] Busca textual por nome/título e filtros por status (Local, Na Fila, Enviado, Ausente).
  - [x] Painel de detalhes com edição de metadados para envio (título e descrição).
  - [x] Ação de reproduzir vídeo com player padrão do Windows ou VLC.
  - [x] Diálogos nativos e feedback visual de carregamento.
- **Critério de Aceite**: Interface fluida, sem travamentos, com binding MVVM CommunityToolkit e testes de ViewModel (5/5 testes).

---

### Fase 5 — Motor de Filtros e Automação PUBG (CONCLUÍDA)
- **Objetivo**: Automatizar a triagem de gravações conforme critérios definidos por pasta.
- **Tarefas**:
  - [x] Avaliação de regras por pasta: extensão, tamanho mínimo e máximo, duração mínima e máxima, padrão de nome (Regex).
  - [x] Suporte ao toggle de ativação/desativação de limite máximo de tamanho.
  - [x] Regra de auto-enfileiramento condicional (AutoUploadEnabled).
  - [x] Registro detalhado do motivo da reprovação no vídeo sem descartá-lo da biblioteca.
- **Critério de Aceite**: Testes de avaliação de regras e auto-enfileiramento validados (11/11 testes).

---

### Fase 6 — Integração YouTube API v3 (CONCLUÍDA)
- **Objetivo**: Integrar autenticação e envio de vídeos para o YouTube.
- **Tarefas**:
  - [x] Fluxo OAuth 2.0 com GoogleWebAuthorizationBroker para conta Google do usuário.
  - [x] Armazenamento seguro de tokens com DPAPI do Windows (ProtectedData).
  - [x] Obtenção do perfil do canal autenticado.
  - [x] Upload retomável (Resumable Upload) com envio de chunks.
  - [x] Formatação canônica de título a partir de timestamps PUBG e substituição dinâmica de descrição via templates.
  - [x] Envio de thumbnail personalizada pós-upload.
- **Critério de Aceite**: Autenticação, encriptação DPAPI e fluxo de upload unitariamente testados (12/12 testes).

---

### Fase 7 — Fila Persistente e Resiliente de Upload (CONCLUÍDA)
- **Objetivo**: Orquestrar a fila de uploads com garantia de sobrevivência a interrupções.
- **Tarefas**:
  - [x] Serviço de fila singleton: processamento sequencial (1 upload por vez na v1).
  - [x] Transições de estado da fila persistidas imediatamente no SQLite com checkpoints.
  - [x] Implementação de Throttling de velocidade de upload (ThrottledStream).
  - [x] Retomada de uploads interrompidos (UploadUri salvo).
  - [x] Comandos de UI para pausar, retomar, reordenar, remover da fila e retry manual (UploadQueueView.xaml).
- **Critério de Aceite**: Fila resiliente testada e validada (5/5 testes).

---

### Fase 8 — Backup no Google Drive (CONCLUÍDA)
- **Objetivo**: Proteger o catálogo SQLite contra perda de dados.
- **Tarefas**:
  - [x] Implementar clonagem atômica do SQLite em tempo de execução via SqliteConnection.BackupDatabase.
  - [x] Integração com Google Drive API v3 (pasta dedicada no Drive).
  - [x] Versionamento e retenção dos últimos N backups configurável.
  - [x] Rotina de verificação formal de consistência PRAGMA integrity_check.
  - [x] Interface gráfica WPF de controle de backup local, envio para a nuvem e listagem remota (SettingsViewModel e SettingsView.xaml).
- **Critério de Aceite**: Backup consistente gerado e enviado ao Google Drive com testes de integridade validados (4/4 testes).

---

### Fase 9 — Bandeja, Inicialização e Acabamento (CONCLUÍDA)
- **Objetivo**: Polimento operacional do aplicativo como serviço desktop contínuo.
- **Tarefas**:
  - [x] Notificação na bandeja do sistema (WindowsSystemTrayService / NotifyIcon) com menu de contexto (Abrir, Sair) e duplo clique.
  - [x] Comportamento de fechamento: Evento Closing em MainWindow minimizando para bandeja para manter o serviço de background ativo.
  - [x] Configuração para iniciar com o Windows via chave Run do Registro do usuário (WindowsStartupRegistrationService), reativo na tela de Configurações.
  - [x] Exportação da biblioteca para CSV e JSON (LibraryExportService).
  - [x] Cobertura de testes de sistema e exportação (SystemIntegrationAndExportTests).
- **Critério de Aceite**: App integrado à bandeja, auto-start no registro e exportações testadas (3/3 testes).

---

### Fase 10 — Validação e Distribuição (CONCLUÍDA)
- **Objetivo**: Homologação completa, testes de robustez e distribuição final.
- **Tarefas**:
  - [x] Testes de carga simulando fluxos gigantes (> 5 GB) com footprint constante de memória (EndToEndValidationAndStressTests).
  - [x] Testes de tolerância a desconexão e interrupções durante o envio de vídeos.
  - [x] Testes de resiliência e tratamento seguro de arquivos de vídeo corrompidos/inválidos.
  - [x] Scripts de publicação portátil publish.ps1 e publish-portable.bat.
  - [x] Pacote autossuficiente (self-contained win-x64) gerado em eleases/ReplayManager-Portable.zip.
  - [x] Documentação de usuário final para instalação, uso e criação das credenciais de API no Google Cloud Console.
- **Critério de Aceite**: Aplicação estável, sem memory leaks nem travamentos na UI, 96 testes automatizados aprovados e pacote distribuível pronto.