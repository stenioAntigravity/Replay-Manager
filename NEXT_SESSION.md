# Next Session Guide: Replay Manager

**Data de Emissão**: 2026-10-30
**Fase Alvo**: Fase 2 — Monitoramento de Pastas e Ingestão

---

## 1. O que já está pronto
- **Fase 0 (Concluída)**:
  - SDK .NET 10.0 (10.0.403) instalado e validado.
  - Repositório Git configurado com branch `main`.
  - Documentação viva de governança e arquitetura.
  - Solução `ReplayManager.slnx` com os 5 projetos interligados.
- **Fase 1 (Concluída e Validada)**:
  - Entidades de domínio completas em `src/ReplayManager.Domain`.
  - Persistência SQLite com EF Core em `src/ReplayManager.Infrastructure`.
  - Índice único composto `(MonitoredFolderId, ContentSha256)` garantindo deduplicação intra-pasta e isolamento entre pastas distintas.
  - Repositórios, `UnitOfWork` e injeção de dependência.
  - Inicialização segura com WAL mode e seeding idempotente.
  - Suíte com 9 testes automatizados com SQLite InMemory aprovados com 100% de sucesso.
  - Compilação limpa (0 avisos, 0 erros).

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

## 3. Qual tarefa deve ser executada em seguida (Fase 2)
Implementar o motor de **Monitoramento de Pastas e Ingestão**:
1. No `ReplayManager.Application`:
   - Interfaces `IFileStabilityChecker`, `IFileWatcherService`, `IContentHasher`.
   - Eventos de domínio / DTOs de arquivos detectados.
2. No `ReplayManager.Infrastructure`:
   - Implementar `FileStabilityChecker`:
     - Polling de tamanho com intervalo de estabilidade (ex: 3 a 5 segundos sem alteração de bytes).
     - Teste de abertura não bloqueante com `FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None)` para garantir que a gravação do OBS/PUBG terminou.
   - Implementar `FileSystemWatcherService`:
     - Monitoramento de eventos `Created`, `Changed`, `Renamed`.
     - Debounce de eventos para evitar disparos repetidos.
     - Suporte a múltiplas instâncias de pastas simultâneas.
   - Implementar `Sha256ContentHasher`:
     - Streaming de leitura assíncrona com `SHA256.Create()` sem carregar arquivos gigantes na memória RAM.
   - Implementar `FolderReconciliationWorker`:
     - Varredura periódica em background (ex: a cada 60 segundos ou configurável) para reconciliar o diretório físico com o banco SQLite, capturando arquivos que possam ter sido perdidos por estouro de buffer do `FileSystemWatcher`.
3. No `ReplayManager.Tests`:
   - Testes unitários para o cálculo de hash em streaming.
   - Testes assíncronos de detecção de término de gravação simulando escrita concorrente.

---

## 4. Quais arquivos devem ser consultados
- `ARCHITECTURE.md` (Seção 4.1: Diagrama de sequência de detecção e ingestão).
- `DECISIONS.md` (ADR 003 e ADR 004 sobre estabilidade de arquivos e deduplicação).
- `PROJECT_CONTEXT.md` (Seção 5: Monitoramento de Pastas).

---

## 5. Como testar o resultado
Executar:
```powershell
$env:DOTNET_ROOT = "C:\Users\steni\AppData\Local\Microsoft\dotnet"; $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
dotnet test ReplayManager.slnx
```
Garantir que todos os testes passem com 100% de sucesso.
