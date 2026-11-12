# Hotfix v3.10.1 — banco local

Esta atualização corrige a mensagem `database is locked` que podia aparecer
logo após a recuperação de senha ou durante o login.

## Instalação de teste

1. Feche o RMC Flow Comércio Local e pare a depuração no Visual Studio com
   **Shift + F5**.
2. Abra `RMCFlowComercioLocal.sln`.
3. Selecione **Release | x64** e clique em **Compilar Solução**.
4. Inicie pelo executável gerado em
   `src\RMCFlowComercioLocal\bin\x64\Release\RMCFlowComercioLocal.exe`.
5. Entre usando a senha que você acabou de redefinir.

Para a versão comercial, repita a compilação em **Release | x86** e gere os
instaladores Inno Setup como de costume. Não apague, mova ou substitua o
arquivo de dados do cliente: o banco continua em `%LocalAppData%`.

## Observação

O erro era um bloqueio momentâneo do arquivo SQLite e não corrupção dos
dados. Nenhum backup ou recuperação adicional é necessário.
