# README - Replay Manager

## 📋 Visão Geral do Projeto

O **Replay Manager** é um aplicativo desktop para Windows, independente e modular, projetado para gerenciar gravações de jogos e replays. O software monitora diretórios locais, indexa vídeos em um catálogo SQLite persistente, calcula hashes para deduplicação intra-pasta, extrai metadados e miniaturas via FFmpeg, aplica filtros automatizados configuráveis por pasta, gerencia uploads resilientes para o YouTube via YouTube Data API v3 com suporte a retomada (resumable), e realiza backups automáticos do banco de dados no Google Drive.

**Status Atual**: ✅ **Versão 1.0.0 Estável** - Todas as 11 fases concluídas com 96 testes automatizados aprovados (100% de sucesso).

---

## 📁 Estrutura de Arquivos .md do Projeto

### 🔴 **CRÍTICO - Alta Importância** (Nível 1)

#### `PROJECT_CONTEXT.md`
- **Finalidade**: Define o contexto geral do projeto, objetivos principais, tecnologias utilizadas e restrições fundamentais.
- **Relaciona-se com**: Todos os outros documentos - é a base conceitual do projeto.
- **Importância**: ⭐⭐⭐⭐⭐ (Essencial para entender o "porquê" do projeto)
- **Conteúdo Chave**:
  - Visão geral do Replay Manager
  - Objetivos principais (monitoramento, biblioteca visual, filtros, fila persistente, integração YouTube, segurança/backup)
  - Tecnologias: .NET 10, WPF/MVVM, SQLite, FFmpeg, Google APIs
  - Princípios fundamentais (Clean Architecture, Zero Deleção Automática, Integridade Offline)

#### `ARCHITECTURE.md`
- **Finalidade**: Descreve a arquitetura técnica detalhada do projeto com diagramas de camadas e fluxos de dados.
- **Relaciona-se com**: `PROJECT_CONTEXT.md` (contexto), `IMPLEMENTATION_PLAN.md` (como será implementado), `DECISIONS.md` (decisões técnicas)
- **Importância**: ⭐⭐⭐⭐⭐ (Essencial para desenvolvedores entenderem a estrutura do código)
- **Conteúdo Chave**:
  - Diagrama de camadas (UI → AppCore → DomainCore → Infra)
  - Divisão dos projetos (Domain, Application, Infrastructure, App, Tests)
  - Fluxos de dados críticos (detecção/ingestão, execução da fila de upload)
  - Decisões arquiteturais estruturantes

#### `IMPLEMENTATION_PLAN.md`
- **Finalidade**: Descreve o roteiro de desenvolvimento em 11 fases estruturadas com critérios de aceitação.
- **Relaciona-se com**: `PROJECT_CONTEXT.md` (o que será feito), `ARCHITECTURE.md` (como está estruturado), `DEVELOPMENT_STATUS.md` (progresso real)
- **Importância**: ⭐⭐⭐⭐⭐ (Essencial para roadmap e acompanhamento do desenvolvimento)
- **Conteúdo Chave**:
  - Matriz de fases e dependências (Fase 0 a Fase 10)
  - Detalhamento de cada fase com tarefas e critérios de aceitação
  - Todas as fases marcadas como CONCLUÍDA

#### `DEVELOPMENT_STATUS.md`
- **Finalidade**: Documenta o status real do desenvolvimento em relação ao plano, incluindo validações técnicas.
- **Relaciona-se com**: `IMPLEMENTATION_PLAN.md` (comparativo entre planejado e realizado), `CHANGELOG.md` (histórico de versões)
- **Importância**: ⭐⭐⭐⭐⭐ (Essencial para saber o estado atual do projeto)
- **Conteúdo Chave**:
  - Status de todas as fases (todas concluídas)
  - Detalhamento dos módulos concluídos
  - Última validação executada (compilação, testes)
  - Próxima tarefa exata

---

### 🟠 **ALTA IMPORTÂNCIA** (Nível 2)

#### `DECISIONS.md`
- **Finalidade**: Registra as decisões arquiteturais e técnicas tomadas (ADR - Architecture Decision Records).
- **Relaciona-se com**: `ARCHITECTURE.md` (justificativas das escolhas), `IMPLEMENTATION_PLAN.md` (impacto nas fases)
- **Importância**: ⭐⭐⭐⭐ (Essencial para entender o "porquê" de decisões técnicas específicas)
- **Conteúdo Chave**:
  - ADR 001: .NET 10 LTS e SDK Local User-Level
  - ADR 002: Clean Architecture + MVVM
  - ADR 003: Deduplicação Intra-Pasta
  - ADR 004: Detecção Confiável de Conclusão de Gravação
  - ADR 005: Tratamento de Vídeos Rejeitados por Filtros
  - ADR 006: Upload Resumable Sequencial e Persistência
  - ADR 007: Preservação Irrestrita de Arquivos Locais
  - ADR 008: Armazenamento Seguro de Credenciais com DPAPI

#### `CHANGELOG.md`
- **Finalidade**: Histórico de todas as alterações notáveis no projeto seguindo Keep a Changelog.
- **Relaciona-se com**: `DEVELOPMENT_STATUS.md` (versões implementadas), `IMPLEMENTATION_PLAN.md` (fases completadas)
- **Importância**: ⭐⭐⭐⭐ (Essencial para entender a evolução do projeto ao longo do tempo)
- **Conteúdo Chave**:
  - Versão 0.1.0-alpha.0: Configuração inicial
  - Versão 0.2.0-alpha.1: Fundação e Persistência SQLite
  - Versão 0.3.0-alpha.2: Monitoramento de Pastas e Ingestão
  - Versão 0.4.0-alpha.3: Integração FFmpeg e Miniaturas
  - Versão 0.5.0-alpha.4: Biblioteca de Vídeos e Interface WPF
  - Versão 0.6.0-alpha.5: Motor de Filtros e Automação PUBG
  - Versão 0.7.0-alpha.6: Integração YouTube API v3
  - Versão 0.8.0-alpha.7: Fila Persistente e Resiliente
  - Versão 0.9.0-alpha.8: Backup no Google Drive
  - Versão 1.0.0: Fase 9 e 10 - Sistema, Validação E2E e Lançamento

#### `TESTING.md`
- **Finalidade**: Documenta a estratégia de testes e registra todas as execuções de teste.
- **Relaciona-se com**: `IMPLEMENTATION_PLAN.md` (testes por fase), `DEVELOPMENT_STATUS.md` (cobertura de testes)
- **Importância**: ⭐⭐⭐⭐ (Essencial para qualidade e confiabilidade do software)
- **Conteúdo Chave**:
  - Estratégia de testes (xUnit, FluentAssertions, Moq, SQLite In-Memory)
  - Registro de execuções de teste (T001 a T096)
  - Total: 96 testes executados, 96 aprovados (100% de sucesso)
  - Testes por módulo e categoria

---

### 🟡 **MÉDIA IMPORTÂNCIA** (Nível 3)

#### `SESSION_LOG.md`
- **Finalidade**: Registro de atividades entre sessões de desenvolvimento para continuidade.
- **Relaciona-se com**: `NEXT_SESSION.md` (próximas tarefas), `DEVELOPMENT_STATUS.md` (progresso acumulado)
- **Importância**: ⭐⭐⭐ (Útil para manter contexto entre sessões)

#### `NEXT_SESSION.md`
- **Finalidade**: Define as próximas tarefas a serem executadas na próxima sessão.
- **Relaciona-se com**: `SESSION_LOG.md` (histórico), `IMPLEMENTATION_PLAN.md` (fases pendentes)
- **Importância**: ⭐⭐⭐ (Orienta o próximo passo do desenvolvimento)

---

### 🟢 **BAIXA IMPORTÂNCIA** (Nível 4)

#### `LEIA_PRIMEIRO.md`
- **Finalidade**: Instruções iniciais para quem está começando a explorar o projeto.
- **Relaciona-se com**: Todos os outros documentos (ponto de entrada)
- **Importância**: ⭐⭐ (Útil apenas no início da exploração do projeto)

#### `CONTROLE_DO_PROJETO.md`
- **Finalidade**: Documentação sobre controle e governança do projeto.
- **Relaciona-se com**: `PROJECT_CONTEXT.md` (governança), `IMPLEMENTATION_PLAN.md` (controle de versão)
- **Importância**: ⭐⭐ (Contextual para gestão do projeto)

#### `LIMITACOES_E_PENDENCIAS.md`
- **Finalidade**: Documentação sobre limitações conhecidas e pendências do projeto.
- **Relaciona-se com**: `PROJECT_CONTEXT.md` (restrições), `CHANGELOG.md` (futuras melhorias)
- **Importância**: ⭐⭐ (Informação importante para usuários e desenvolvedores)

#### `INVENTARIO_DE_ARQUIVOS.csv`
- **Finalidade**: Inventário completo de todos os arquivos do projeto.
- **Relaciona-se com**: `PATH.md` (caminhos), `ARCHITECTURE.md` (estrutura de pastas)
- **Importância**: ⭐⭐ (Referência para navegação no código)

#### `00_INVENTARIO_GERAL.md`
- **Finalidade**: Inventário geral do projeto com visão macro.
- **Relaciona-se com**: Todos os outros documentos (visão panorâmica)
- **Importância**: ⭐⭐ (Contexto geral do ecossistema do projeto)

#### `plano-desenvolvimento.md`
- **Finalidade**: Plano detalhado de desenvolvimento do projeto.
- **Relaciona-se com**: `IMPLEMENTATION_PLAN.md` (detalhamento das fases), `DEVELOPMENT_STATUS.md` (progresso)
- **Importância**: ⭐⭐ (Complementar ao plano de implementação)

#### `plano-desenvolvimento-frontend.md`
- **Finalidade**: Plano específico para desenvolvimento da interface frontend.
- **Relaciona-se com**: `IMPLEMENTATION_PLAN.md` (fase 4 - Interface WPF), `ARCHITECTURE.md` (camada UI)
- **Importância**: ⭐⭐ (Específico para camada de apresentação)

#### `procedimento-executor.md`
- **Finalidade**: Procedimentos operacionais para execução do projeto.
- **Relaciona-se com**: `IMPLEMENTATION_PLAN.md` (tarefas), `DEVELOPMENT_STATUS.md` (validações)
- **Importância**: ⭐⭐ (Guia prático para execução)

#### `relatorio-etapa-01.md`
- **Finalidade**: Relatório da etapa 01 do desenvolvimento.
- **Relaciona-se com**: `IMPLEMENTATION_PLAN.md` (fase correspondente), `CHANGELOG.md` (versão liberada)
- **Importância**: ⭐⭐ (Registro de progresso específico)

#### `relatorio-etapa-02.md`
- **Finalidade**: Relatório da etapa 02 do desenvolvimento.
- **Relaciona-se com**: `IMPLEMENTATION_PLAN.md` (fase correspondente), `CHANGELOG.md` (versão liberada)
- **Importância**: ⭐⭐ (Registro de progresso específico)

#### `relatorio-20-etapas-desing.md`
- **Finalidade**: Relatório abrangente das 20 etapas de design do projeto.
- **Relaciona-se com**: `IMPLEMENTATION_PLAN.md` (todas as fases), `ARCHITECTURE.md` (decisões de design)
- **Importância**: ⭐⭐ (Visão completa do processo de design)

---

## 📊 Matriz de Importância dos Arquivos .md

| Arquivo | Importância | Leitura Obrigatória | Público-Alvo |
|---------|-------------|---------------------|--------------|
| PROJECT_CONTEXT.md | ⭐⭐⭐⭐⭐ | Sim | Todos |
| ARCHITECTURE.md | ⭐⭐⭐⭐⭐ | Sim | Desenvolvedores |
| IMPLEMENTATION_PLAN.md | ⭐⭐⭐⭐⭐ | Sim | Desenvolvedores, Gestores |
| DEVELOPMENT_STATUS.md | ⭐⭐⭐⭐⭐ | Sim | Todos |
| DECISIONS.md | ⭐⭐⭐⭐ | Recomendada | Desenvolvedores |
| CHANGELOG.md | ⭐⭐⭐⭐ | Recomendada | Todos |
| TESTING.md | ⭐⭐⭐⭐ | Recomendada | QA, Desenvolvedores |
| SESSION_LOG.md | ⭐⭐⭐ | Opcional | Equipe de desenvolvimento |
| NEXT_SESSION.md | ⭐⭐⭐ | Opcional | Equipe de desenvolvimento |
| LEIA_PRIMEIRO.md | ⭐⭐ | Apenas no início | Novos membros da equipe |
| CONTROLE_DO_PROJETO.md | ⭐⭐ | Opcional | Gestores, Desenvolvedores |
| LIMITACOES_E_PENDENCIAS.md | ⭐⭐ | Recomendada | Usuários, Desenvolvedores |
| INVENTARIO_DE_ARQUIVOS.csv | ⭐⭐ | Opcional | Todos |
| 00_INVENTARIO_GERAL.md | ⭐⭐ | Opcional | Gestores |
| plano-desenvolvimento.md | ⭐⭐ | Opcional | Desenvolvedores |
| plano-desenvolvimento-frontend.md | ⭐⭐ | Opcional | Frontend Developers |
| procedimento-executor.md | ⭐⭐ | Opcional | Equipe de desenvolvimento |
| relatorio-etapa-01.md | ⭐⭐ | Opcional | Equipe de desenvolvimento |
| relatorio-etapa-02.md | ⭐⭐ | Opcional | Equipe de desenvolvimento |
| relatorio-20-etapas-desing.md | ⭐⭐ | Opcional | Gestores, Stakeholders |

---

## 🔗 Relações entre os Documentos

```
PROJECT_CONTEXT.md (Base Conceitual)
    ↓
ARCHITECTURE.md ←→ DECISIONS.md
    ↓              ↓
IMPLEMENTATION_PLAN.md → DEVELOPMENT_STATUS.md
    ↓              ↓
CHANGELOG.md ←→ TESTING.md
    ↓
SESSION_LOG.md ↔ NEXT_SESSION.md
```

---

## 📝 Guia de Leitura Recomendada

### Para Novos Membros da Equipe:
1. **LEIA_PRIMEIRO.md** (inicial)
2. **PROJECT_CONTEXT.md** (entender o projeto)
3. **ARCHITECTURE.md** (estrutura técnica)
4. **IMPLEMENTATION_PLAN.md** (roadmap)
5. **DEVELOPMENT_STATUS.md** (status atual)

### Para Desenvolvedores:
1. **PROJECT_CONTEXT.md** + **ARCHITECTURE.md**
2. **DECISIONS.md** (decisões técnicas)
3. **IMPLEMENTATION_PLAN.md** (fases pendentes)
4. **TESTING.md** (estratégia de testes)
5. **CHANGELOG.md** (histórico de mudanças)

### Para Gestores/Stakeholders:
1. **PROJECT_CONTEXT.md** (objetivos)
2. **DEVELOPMENT_STATUS.md** (progresso)
3. **IMPLEMENTATION_PLAN.md** (roadmap)
4. **CHANGELOG.md** (versões liberadas)
5. **LIMITACOES_E_PENDENCIAS.md** (desafios)

---

## ✅ Status do Projeto

- **Versão**: 1.0.0 Estável
- **Fases Concluídas**: 11 de 11 (100%)
- **Testes Automatizados**: 96/96 aprovados (100%)
- **Compilação**: 0 erros, 0 avisos
- **Pronto para**: Distribuição e uso em produção

---

## 📦 Próximos Passos

O projeto está completo e estável. As próximas ações podem incluir:
- Manutenção corretiva (bugs reportados por usuários)
- Melhorias de performance (otimizações não críticas)
- Novas funcionalidades (expansão do escopo)
- Documentação de usuário final

---

**Última Atualização**: 2026-11-08
**Mantido por**: Equipe Replay Manager
