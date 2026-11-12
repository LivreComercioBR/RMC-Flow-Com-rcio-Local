# RMC Flow Comércio Local — v0.8

Esta entrega adiciona o primeiro núcleo operacional da Edição Local.

- A licença já ativada abre diretamente o sistema.
- Computadores sem licença continuam na tela de ativação.
- Painel local com contagem de clientes e veículos.
- Cadastro, edição e exclusão de clientes.
- Cadastro, edição e exclusão de veículos para carros e motos.
- Dados salvos localmente em SQLite, em `AppData\\Local\\RMC Sites\\RMC Flow Comércio Local`.

## Teste

1. Copie a chave pública real para `src\\RMCFlowComercioLocal\\keys\\license-public.pem`.
2. Abra `RMCFlowComercioLocal.sln` no Visual Studio.
3. Se necessário, restaure os pacotes NuGet.
4. Compile e execute a configuração `Debug x64`.

Após a ativação previamente feita neste computador, a aplicação deve abrir no Painel. Para testar a edição x86, altere a plataforma para `x86`, compile e execute.
