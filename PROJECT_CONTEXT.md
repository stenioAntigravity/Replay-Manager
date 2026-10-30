# Project Context: Replay Manager

## 1. Visão Geral
O **Replay Manager** é um aplicativo desktop para Windows, independente e modular, projetado para gerenciar gravações de jogos e replays (com foco inicial em PUBG, porém totalmente desacoplado de softwares específicos como OBS Studio, Steam ou o próprio jogo).

O software monitora diretórios locais, indexa vídeos em um catálogo SQLite persistente, calcula hashes para deduplicação intra-pasta, extrai metadados e miniaturas via FFmpeg, aplica filtros automatizados configuráveis por pasta, gerencia uploads resilientes para o YouTube via YouTube Data API v3 com suporte a retomada (resumable), e realiza backups automáticos do banco de dados no Google Drive.

## 2. Objetivos Principais
1. **Monitoramento Robusto e Não Intrusivo**:
   - Detecção contínua de novos arquivos em múltiplas pastas.
   - Detecção de término de gravação por estabilidade de tamanho e liberação de locks de arquivo.
   - Rotina periódica de reconciliação para prevenir perdas de eventos de `FileSystemWatcher`.
2. **Biblioteca Visual e Gerenciamento Local**:
   - Interface WPF moderna e responsiva com grade de miniaturas.
   - Busca, filtros, ordenação e seleção em lote.
   - Reprodução rápida via VLC (se instalado) ou reprodutor padrão do Windows.
   - Preservação estrita dos arquivos locais: o aplicativo **nunca** apaga vídeos locais automaticamente. Se o usuário apagar um arquivo externamente, os metadados, links e miniatura são mantidos com indicador de status "Local Indisponível".
3. **Filtros e Automação Configuráveis por Pasta**:
   - Regras por pasta: limites de tamanho (min/max), duração (min/max), extensões (MP4, MKV, AVI), regex/padrões de nome, subpastas.
   - Valores padrão de tamanho: mínimo 10 MB, máximo 100 MB (limite superior opcional/desativável).
   - Pastas com flag de auto-upload (ex: pasta PUBG) direcionam arquivos aprovados para a fila de envio. Arquivos reprovados permanecem na biblioteca com o motivo explicitado.
4. **Fila Persistente e Resiliente de Uploads**:
   - Upload de 1 vídeo por vez na primeira versão.
   - Fila serializada no SQLite, sobrevivendo a reinicializações, crashes ou falhas de conexão.
   - Controle de taxa de transferência (bandwidth throttling).
   - Tratamento de status do YouTube e prevenção de envios duplicados.
5. **Integração com YouTube (Data API v3)**:
   - Autenticação OAuth 2.0 segura (tokens cifrados via Windows DPAPI).
   - Títulos automáticos padronizados (`PUBG Replay - DD/MM/AAAA - HH:MM:SS`) extraídos do nome original ou timestamp do arquivo.
   - Modelos de descrição parametrizáveis por pasta.
   - Política de privacidade padrão: Não listado (`unlisted`), com monitoramento das restrições para projetos não verificados na API do Google.
6. **Segurança, Backup e Continuidade**:
   - Backup automático versionado do SQLite no Google Drive utilizando a API de backup consistente do SQLite (sem bloquear o banco ativo).
   - Continuidade obrigatória entre sessões guiada por documentação detalhada na raiz do repositório.

## 3. Tecnologias e Dependências
- **Linguagem**: C# (moderno, C# 13 / 14 features).
- **Plataforma**: .NET 10.0 (Windows Desktop).
- **UI Framework**: WPF (Windows Presentation Foundation) com padrão MVVM (CommunityToolkit.Mvvm).
- **Banco de Dados**: SQLite gerenciado via Entity Framework Core (e SQLite Backup API nativa para backups).
- **Processamento de Mídia**: FFmpeg (executáveis locais ou gerenciados via wrapper de processo para extração de metadados e thumbnails).
- **APIs Google**: Google.Apis.YouTube.v3, Google.Apis.Auth, Google.Apis.Drive.v3.
- **Proteção de Credenciais**: Windows DPAPI (`System.Security.Cryptography.ProtectedData`).
- **Testes**: xUnit, FluentAssertions, Moq.

## 4. Restrições e Princípios Fundamentais
- **Clean Architecture & Separação Rígida**: O domínio não referencia a interface gráfica, infraestrutura ou frameworks externos.
- **Não Concorrência Prematura em Uploads**: Um upload por vez garantido por semáforo e estado persistido.
- **Zero Deleção Automática**: Nenhum arquivo de vídeo do usuário é deletado sem ação explícita manual.
- **Integridade Offline**: O sistema é 100% utilizável localmente sem conexão com a internet. Falhas na nuvem não bloqueiam nem corrompem a biblioteca local.

