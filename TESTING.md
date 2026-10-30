# Testing Strategy and Test Log: Replay Manager

## 1. Estratégia de Testes

A suíte de testes do **Replay Manager** é estruturada no projeto `ReplayManager.Tests` utilizando **xUnit**, **FluentAssertions** e **Moq**.

A cobertura é orientada a:
1. **Regras de Domínio e Filtros (Unitários Puros)**:
   - Validação de extensões aceitas (.mp4, .mkv, .avi).
   - Validação de intervalos de tamanho de arquivo (min/max).
   - Validação de duração e regex de nome.
   - Formatação e parsing de títulos padrão (`PUBG Replay - DD/MM/AAAA - HH:MM:SS`).
2. **Persistência e Concorrência (Testes de Integração com SQLite InMemory / Arquivo Temporário)**:
   - Deduplicação intra-pasta: tentar inserir o mesmo hash na mesma pasta deve atualizar/rejeitar; em pastas diferentes deve cadastrar ambos.
   - Atomicidade da fila de uploads em transições de estado.
   - Preservação de registros com arquivo físico ausente.
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

---

## 3. Testes Pendentes para Próximas Fases
- [ ] `FolderFilterRuleTests`: Validação isolada de todas as ramificações de aprovação/rejeição.
- [ ] `TitleFormatterTests`: Extração de timestamps de nomes de arquivo e data de criação.
- [ ] `VideoRepositoryTests`: Testes de unicidade com chave composta `(MonitoredFolderId, ContentSha256)`.
- [ ] `FileStabilityInspectorTests`: Testes assíncronos com locks de leitura/escrita.
- [ ] `UploadQueueManagerTests`: Transições de estado da fila e recuperação após interrupção.
