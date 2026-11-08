# Guia de Configuração das APIs do Google (YouTube & Google Drive)

Este guia explica como obter as credenciais OAuth 2.0 necessárias para que o **Replay Manager** possa realizar uploads para o seu canal do YouTube e efetuar backups automáticos do catálogo no Google Drive.

---

## 1. Criar um Projeto no Google Cloud Console

1. Acesse o [Google Cloud Console](https://console.cloud.google.com/).
2. Faça login com a conta Google dona do canal do YouTube / Google Drive.
3. No topo da página, clique no seletor de projetos e selecione **Novo Projeto**.
4. Defina o nome como `Replay Manager` e clique em **Criar**.

---

## 2. Ativar as APIs Necessárias

Com o projeto selecionado:
1. No menu lateral, navegue até **APIs e Serviços** > **Biblioteca**.
2. Pesquise por **YouTube Data API v3**, selecione-a e clique em **Ativar**.
3. Retorne à **Biblioteca**, pesquise por **Google Drive API**, selecione-a e clique em **Ativar**.

---

## 3. Configurar a Tela de Consentimento OAuth

1. No menu lateral, acesse **APIs e Serviços** > **Tela de permissão OAuth**.
2. Selecione o tipo de usuário **Externo** e clique em **Criar**.
3. Preencha os campos obrigatórios:
   - **Nome do app**: `Replay Manager`
   - **E-mail para suporte do usuário**: seu e-mail
   - **Dados de contato do desenvolvedor**: seu e-mail
4. Clique em **Salvar e continuar**.
5. Na etapa de **Escopos**, adicione:
   - `../auth/youtube.upload`
   - `../auth/youtube`
   - `../auth/drive.file`
6. Clique em **Salvar e continuar**.
7. Na etapa de **Usuários de teste**, adicione o seu próprio e-mail Google (enquanto o app estiver em modo de teste, apenas contas cadastradas aqui conseguem fazer login).
8. Conclua a configuração.

---

## 4. Criar Credenciais OAuth 2.0 (client_secrets.json)

1. No menu lateral, vá para **APIs e Serviços** > **Credenciais**.
2. Clique em **+ Criar Credenciais** > **ID do cliente OAuth**.
3. Em **Tipo de aplicativo**, selecione **Aplicativo para Computador** (Desktop App).
4. Defina o nome como `ReplayManager Desktop Client`.
5. Clique em **Criar**.
6. Na janela exibida, clique em **Fazer o download do JSON**.
7. Renomeie o arquivo baixado para `client_secrets.json`.

---

## 5. Instalação das Credenciais no Replay Manager

Copie o arquivo `client_secrets.json` para o diretório do aplicativo ou coloque-o na pasta:
```text
%APPDATA%\ReplayManager\client_secrets.json
```

Ao abrir o **Replay Manager**:
1. Acesse a aba **Configurações & Backup**.
2. Clique em **Autenticar Conta Google / YouTube**.
3. Seu navegador padrão abrirá a tela oficial de login do Google.
4. Conceda as permissões solicitadas.
5. Os tokens de acesso serão criptografados localmente com a **Windows DPAPI** (`ProtectedData`), garantindo máxima segurança.
