Replay Manager
O Replay Manager é um aplicativo desktop para Windows desenvolvido para monitorar pastas de vídeos de forma autônoma, organizar sua biblioteca local com miniaturas automáticas, gerenciar filtros personalizados por pasta e realizar uploads automáticos ou manuais para o YouTube.

O projeto foi projetado com uma arquitetura estritamente modular, separação clara de responsabilidades (Clean Architecture / MVVM) e suporte a continuidade entre sessões de desenvolvimento, garantindo robustez, persistência resiliente e manutenibilidade.

🚀 Principais Funcionalidades
Monitoramento de Pastas: Acompanhamento em tempo real de múltiplas pastas de vídeos (compatível com MP4, AVI, MKV) com detecção de término de gravação e reconciliação periódica.

Identificação de Duplicatas: Separação por pasta monitorada e detecção por hash SHA-256 para arquivos idênticos na mesma origem.

Filtros e Automação: Regras configuráveis por pasta (tamanho, duração, extensão, nomenclaturas) com suporte a envio automático condicionado (ex: para o PUBG).

Biblioteca Local Visual: Grade moderna com miniaturas geradas via FFmpeg, pesquisa, filtros, ordenação, seleção múltipla e reprodução integrada (suporte ao VLC ou reprodutor padrão do Windows).

Integração Oficial com o YouTube: Autenticação OAuth 2.0, upload retomável (videos.insert), controle de privacidade (não listado por padrão), gerenciamento de títulos e descrições personalizadas com atualização remota explícita.

Fila de Upload Persistente: Sobrevive a reinicializações, quedas de energia e falhas de rede, com controle de velocidade e prevenção de envios duplicados.

Backup no Google Drive: Cópia consistente do banco de dados SQLite por meio de API dedicada, com suporte a versionamento e restauração.

Confiabilidade e Segurança: Proteção de dados sensíveis via Windows DPAPI, funcionamento integral sem internet para recursos locais, e garantia de que nenhum arquivo local é apagado automaticamente.

🏗️ Arquitetura e Estrutura Modular
O projeto adota uma estrutura em camadas para garantir independência entre interface, lógica de negócios, persistência e integrações externas:

Plaintext
ReplayManager.sln
├── ReplayManager.App           # Aplicação WPF (Janelas, Views, ViewModels, Recursos)
├── ReplayManager.Domain        # Entidades, Regras de Negócio, Interfaces e Enums
├── ReplayManager.Application   # Serviços de aplicação, Casos de Uso e Orquestração
├── ReplayManager.Infrastructure# SQLite, Repositórios, Sistema de Arquivos, FFmpeg, OAuth, YouTube API
├── ReplayManager.Tests         # Testes Unitários, de Integração e de Regras de Negócio
└── ReplayManager.Shared        # Componentes compartilhados essenciais
🛠️ Tecnologias Utilizadas
Linguagem: C# (.NET 10 LTS)

Interface Gráfica: WPF (Windows Presentation Foundation)

Banco de Dados: SQLite com Entity Framework Core / Acesso isolado e migrações versionadas

Multimídia: FFmpeg (análise de vídeos e extração de frames/miniaturas)

APIs Externas: YouTube Data API v3 (OAuth 2.0)

Controle de Versão: Git

📖 Documentação de Continuidade e Desenvolvimento
Para garantir a continuidade perfeita entre sessões de desenvolvimento (especialmente em ambientes com interrupções de cota), o repositório mantém documentação detalhada na raiz:

PROJECT_CONTEXT.md: Visão geral, objetivos, restrições e tecnologias.

ARCHITECTURE.md: Estrutura de módulos, responsabilidades e fluxos de dados.

IMPLEMENTATION_PLAN.md: Etapas do desenvolvimento divididas de forma incremental (Fases 0 a 10).

DEVELOPMENT_STATUS.md: Status atual, módulos concluídos, pendências e próxima tarefa.

DECISIONS.md: Registro de decisões técnicas e alternativas descartadas.

TESTING.md: Relatório de testes realizados e pendentes.

SESSION_LOG.md: Histórico resumido das sessões de trabalho.

NEXT_SESSION.md: Instruções diretas e objetivas para retomar a próxima sessão.

⚙️ Como Executar o Projeto
Certifique-se de possuir o SDK do .NET 10 LTS instalado em sua máquina.

Certifique-se de ter o FFmpeg configurado e acessível no PATH do sistema (ou na pasta de binários).

Clone o repositório e abra a solução ReplayManager.sln no seu IDE de preferência (Visual Studio 2022 ou VS Code com extensão C#).

Restaure os pacotes NuGet e compile a solução:

Bash
dotnet restore
dotnet build
Execute os testes automatizados para validar o ambiente:

Bash
dotnet test
