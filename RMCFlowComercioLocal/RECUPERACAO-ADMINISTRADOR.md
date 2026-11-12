# Recuperação do administrador — v3.9.1

Esta atualização adiciona **Recuperar acesso** à tela de login da edição local.

Ela não altera nem recria o banco de dados local. Após instalar a versão compilada,
na tela de login informe o código da licença **ou a licença assinada completa**, o
e-mail que foi cadastrado para o administrador e uma nova senha. O administrador
é reativado e a senha é trocada. A licença assinada é validada contra o código de
solicitação daquele computador.

Na versão comercial v3.10.0, clique em **Gerar solicitação**, envie o código à
RMC Sites e cole a autorização temporária emitida no Painel de Licenças. A
autorização expira em 20 minutos, funciona uma única vez e o sistema cria um
backup local automático antes de trocar a senha.

## Compilação

1. Abra `RMCFlowComercioLocal.sln` no Visual Studio.
2. Selecione `Release` e a plataforma compatível com o computador (`x64` ou `x86`).
3. Use **Compilar Solução**.
4. Feche o sistema antes de substituir a versão instalada pelo novo executável.

Os dados da empresa continuam no banco local do Windows e não são removidos pela
atualização do aplicativo.
