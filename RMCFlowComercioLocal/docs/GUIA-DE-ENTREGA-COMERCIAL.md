# RMC Flow Comércio Local — Guia de Entrega Comercial

Este guia é usado pela RMC Sites para preparar e entregar uma cópia licenciada do sistema local para cada empresa.

## 1. Antes de gerar o instalador

1. Abra `RMCFlowComercioLocal.sln` no Visual Studio 2022.
2. Restaure os pacotes NuGet, caso o Visual Studio solicite.
3. Confirme que a chave pública `src/RMCFlowComercioLocal/keys/license-public.pem` é a chave ativa do Painel de Licenças da RMC Sites.
4. Em **Gerenciador de Configurações**, selecione `Release` e gere primeiro `x86`, depois `x64`.

Não inclua a chave privada RSA no aplicativo, no instalador, no computador do cliente ou em repositórios. Ela fica somente no ambiente privado da RMC Sites.

## 2. Compilação para entrega

No Visual Studio, execute **Compilar > Recompilar Solução** para cada plataforma:

| Cliente | Plataforma | Resultado esperado |
| --- | --- | --- |
| Windows 7/8/10/11 de 32 bits | `Release | x86` | `src/RMCFlowComercioLocal/bin/x86/Release/` |
| Windows 7/8/10/11 de 64 bits | `Release | x64` | `src/RMCFlowComercioLocal/bin/x64/Release/` |

Escolha o pacote x86 quando houver dúvida sobre a arquitetura do computador. Ele também funciona em Windows de 64 bits.

## 3. Criação do instalador

Abra no Inno Setup o script correspondente e pressione **Compile**:

| Arquivo | Instalador gerado |
| --- | --- |
| `installer/RMCFlow-x86.iss` | `RMC-Flow-Comercio-Local-Setup-x86.exe` |
| `installer/RMCFlow-x64.iss` | `RMC-Flow-Comercio-Local-Setup-x64.exe` |

O instalador verifica a presença do .NET Framework 4.8. Se o requisito não existir, instale o componente antes de executar novamente o instalador.

## 4. Fluxo de ativação para cada venda

1. Envie ao cliente somente o instalador adequado e este guia simplificado de ativação.
2. O cliente instala e abre o sistema.
3. Na tela inicial, ele informa razão social/nome, nome fantasia, documento, nome e e-mail do administrador.
4. Ele clica em **Gerar código de solicitação** e envia o código para a RMC Sites.
5. No Painel de Licenças, cadastre a venda, informe o plano adquirido e gere a licença assinada.
6. Envie o bloco completo de licença ao e-mail da compra.
7. O cliente cola o bloco recebido e clica em **Validar e ativar licença**.
8. Após a ativação, ele cria a senha do administrador e inicia os cadastros.

A licença local é vinculada ao computador ativado. Para troca de computador, a RMC Sites deve tratar a transferência conforme a política comercial definida para a venda.

## 5. Orientação para o cliente após ativar

- Cadastre os dados da empresa em **Configurações > Dados da empresa**.
- Crie usuários adicionais em **Configurações > Usuários**.
- Cadastre categorias, produtos/peças, serviços, clientes, veículos e fornecedores antes da operação diária.
- Gere backup em **Configurações > Criar backup** e salve em OneDrive, Google Drive, pendrive ou HD externo.
- Antes de restaurar um backup, confirme que o arquivo pertence àquela oficina.

## 6. Dados locais importantes

O banco de dados da empresa fica no perfil do usuário do Windows, em:

`%LOCALAPPDATA%\RMC Sites\RMC Flow Comércio Local\rmc-flow-comercio-local.db`

Esse arquivo não deve ser enviado por WhatsApp ou e-mail sem autorização do cliente. O recurso de backup do sistema é a forma recomendada para cópias e restaurações.

## 7. Checklist antes da entrega

- [ ] Instalador compilado para a arquitetura correta.
- [ ] Teste de instalação feito em uma pasta/usuário de teste.
- [ ] Código de solicitação recebido do cliente.
- [ ] Licença assinada gerada no Painel de Licenças.
- [ ] E-mail de ativação enviado ao cliente.
- [ ] Primeiro backup orientado ou realizado com o cliente.
- [ ] Canal de suporte informado.
