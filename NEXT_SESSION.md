# Next Session Guide: Replay Manager

**Data de Emissão**: 2026-10-31  
**Fase Alvo**: Fase 6 — Integração YouTube API v3

---

## 1. O que já está pronto
- **Fase 0 (Concluída)**: SDK .NET 10.0, Git, governança e solução modular.
- **Fase 1 (Concluída)**: Modelagem de domínio, SQLite WAL mode, repositórios, UnitOfWork e deduplicação intra-pasta (ADR 003).
- **Fase 2 (Concluída)**: Hasher SHA-256 em streaming 64 KB, detecção não bloqueante de término de gravação, watcher com debounce, deduplicação intra-pasta e reconciliação (ADR 007).
- **Fase 3 (Concluída)**: Resolução de binários FFmpeg/FFprobe, extração de metadados técnicos (duração, resolução, codec, framerate), captura segura de miniatura a 15% e substituição manual.
- **Fase 4 (Concluída)**: Interface WPF MVVM moderna Dark Theme em abas, grade virtualizada com rolagem suave, badges visuais coloridos, busca textual em tempo real, filtros de status, painel de detalhes com edição de metadados, reprodução via VLC/padrão e diálogos nativos.
- **Fase 5 (Concluída e Validada)**:
  - `IFilterEvaluator` e `FilterEvaluator`: avaliação determinística de critérios de corte (extensão, tamanho min/max, duração min/max, regex no nome do arquivo e inclusão de subdiretórios).
  - Justificativa detalhada de reprovação registrada no `VideoRecord.RejectionReason`.
  - Integração no `VideoMediaProcessor` para enfileiramento automático imediato quando `AutoUploadEnabled = true`.
  - 69 testes automatizados aprovados (100% de sucesso).
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

## 3. Qual tarefa deve ser executada em seguida (Fase 6)
Implementar a **Integração YouTube API v3**:
1. Pacotes NuGet necessários em `ReplayManager.Infrastructure` e `ReplayManager.Application`:
   - `Google.Apis.YouTube.v3`
   - `Google.Apis.Auth`
2. No `ReplayManager.Application`:
   - Contratos:
     - `IYouTubeAuthService`: autenticação OAuth 2.0 (`GoogleWebAuthorizationBroker`), consulta de perfil do canal (nome, avatar, ID), desconexão/revogação de credenciais.
     - `ITitleFormatter`: gerador de títulos automáticos (`PUBG Replay - DD/MM/AAAA - HH:MM:SS`) extraindo timestamp do arquivo ou nome original.
     - `IDescriptionTemplateEngine`: processador de templates de descrição com variáveis dinâmicas (`{FileName}`, `{Date}`, `{Duration}`, `{Resolution}`).
     - `IYouTubeUploader`: upload resumable (`ResumableUpload<Video>`), atualização de progresso em tempo real e privacidade padrão "Não listado" (`unlisted`).
   - Modelos/DTOs:
     - `YouTubeChannelProfile` (`ChannelId`, `Title`, `ThumbnailUrl`, `IsAuthenticated`).
     - `UploadProgressEventArgs` (`BytesSent`, `TotalBytes`, `Percentage`, `Status`).
     - `YouTubeUploadResult` (`Success`, `VideoId`, `YouTubeUrl`, `ErrorMessage`).
3. No `ReplayManager.Infrastructure`:
   - Armazenamento seguro de credenciais e tokens usando **Windows DPAPI** (`ProtectedData.Protect`).
   - Implementação de `YouTubeAuthService` e `YouTubeUploader`.
   - Implementação de `TitleFormatter` e `DescriptionTemplateEngine`.
4. No `ReplayManager.Tests`:
   - Testes unitários do formatador de títulos (`TitleFormatterTests`) cobrindo múltiplos formatos de data e nomes de replay PUBG.
   - Testes unitários do motor de templates (`DescriptionTemplateEngineTests`) validando substituição de variáveis e tratamento de variáveis ausentes.
   - Testes de upload com mocks de cliente YouTube.

---

## 4. Quais arquivos devem ser consultados
- `ARCHITECTURE.md` (Seção 3.5: Integração YouTube e Autenticação).
- `PROJECT_CONTEXT.md` (Seção 2: Integração YouTube v3, DPAPI, Templates e Títulos).
- `IMPLEMENTATION_PLAN.md` (Fase 6: Integração YouTube API v3).
- `DECISIONS.md` (ADR 005: Armazenamento de credenciais via Windows DPAPI).

---

## 5. Como testar o resultado
Executar:
```powershell
$env:DOTNET_ROOT = "C:\Users\steni\AppData\Local\Microsoft\dotnet"; $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
dotnet test ReplayManager.slnx
dotnet build ReplayManager.slnx
```
Garantir compilação limpa (0 erros, 0 avisos) e 100% de testes aprovados.
