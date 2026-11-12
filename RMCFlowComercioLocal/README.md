# RMC Flow Comércio Local — Edição Local Compatível

Base nativa para a edição Local, destinada a comércios em geral.

## Compatibilidade definida

| Pacote | Sistemas atendidos |
| --- | --- |
| `RMC-Flow-Comercio-Local-Setup-x86.exe` | Windows 7 SP1, 8, 8.1, 10 e 11, 32 ou 64 bits |
| `RMC-Flow-Comercio-Local-Setup-x64.exe` | Windows 7 SP1, 8, 8.1, 10 e 11, 64 bits |

Pré-requisito: .NET Framework 4.8. O instalador definitivo verificará esse requisito antes de iniciar.

## Licença

1. O cliente instala o programa e informa os dados da empresa.
2. O programa gera um **Código de solicitação** ligado ao computador.
3. A RMC Sites ativa a compra no painel de licenças e envia o bloco de licença assinado ao e-mail da compra.
4. O cliente cola esse bloco no programa; a assinatura e o vínculo ao computador são verificados localmente.
5. A licença Local permanece utilizável offline após a ativação.

## Versão 1.6 — Financeiro

- Contas a receber e a pagar lançadas manualmente.
- Baixa individual das contas pendentes.
- Vendas concluídas no PDV registradas automaticamente como receita paga.
- Resumo de valores pendentes no Painel.

## Versão 1.7 — Cadastros Financeiros

- Cadastro e manutenção de fornecedores.
- Cadastro e manutenção de devedores.
- Seleção obrigatória de fornecedor em contas a pagar.
- Seleção obrigatória de devedor em contas a receber.
- Botão de cadastro rápido dentro dos dois formulários de lançamento.

## Versão 1.8 — Pesquisa de Cadastros Financeiros

- Pesquisa rápida nas abas Fornecedores e Devedores.
- Localização por nome, CPF/CNPJ, telefone, e-mail e contato do fornecedor.
- Sugestão automática de nome ao selecionar fornecedor ou devedor no lançamento financeiro.

## Versão 1.8.1 — Correção Visual da Pesquisa

- Campo de pesquisa mantido visível em qualquer tamanho de janela.
- Botão **Buscar** nas abas Fornecedores e Devedores.
- A pesquisa continua filtrando automaticamente ao digitar.

## Versão 1.9 — Financeiro Completo

- Filtros de contas por status e texto.
- Status automático de vencimento em atraso.
- Baixa, cancelamento e fluxo de caixa de receitas e despesas.
- Baixa em dinheiro integrada ao Caixa quando houver caixa aberto.
- Correção do resumo do Painel e botão Sair com confirmação.

## Versão 1.9.1 — Usabilidade do Financeiro

- Botões de pesquisa de fornecedor e devedor dentro dos lançamentos.
- Cadastro padronizado de categorias financeiras e botão Nova categoria.
- Filtros financeiros identificados com rótulos claros.
- Vencimento reposicionado entre as primeiras colunas da listagem.

O arquivo de licença é assinado digitalmente pelo painel da RMC Sites. Antes de compilar uma versão para distribuição, copie a chave pública gerada no servidor (`license-public.pem`) para `src/RMCFlowComercioLocal/keys/license-public.pem`. O aplicativo conterá somente a chave pública de validação, nunca a chave privada que gera licenças.

## Versão 2.0 — Estoque, Categorias e PDV com Cliente

- Busca de produto por nome, código interno ou código de barras na movimentação de estoque.
- Categorias de produtos padronizadas, com cadastro de nova categoria diretamente na tela do produto.
- Seleção, pesquisa e cadastro de cliente diretamente no PDV.
- Venda de balcão preservada com a opção padrão **Consumidor não identificado**.
- Quando o cliente for selecionado, a venda fica vinculada a ele para recibos, histórico e futura integração fiscal.

## Versão 2.1 — Baixa Financeira Controlada

- Tela de confirmação de baixa com forma de pagamento e data efetiva.
- Contas em dinheiro só podem ser baixadas com o caixa aberto, gerando a movimentação física correspondente.
- PIX, débito, crédito, transferência e boleto são registrados no fluxo financeiro, sem alterar o caixa físico.
- Resumo de contas pendentes e vencidas na própria tela Financeiro.

## Versão 2.2 — Organização Financeira Diária

- Grade Financeiro com colunas curtas compactas e mais espaço para cliente e descrição.
- Descrição obrigatória para cada suprimento ou sangria de caixa.
- Botão **Resumo do dia** com entradas, saídas, resultado e totais por forma de pagamento.
- O resumo diário mostra lançamentos financeiros pagos no dia, inclusive débitos, Pix, cartão, transferências e boletos, além de suprimentos e sangrias.

## Hotfix 2.2.1

- Corrigida a abertura da tela Financeiro em algumas instalações do Windows.

## Hotfix 2.2.2

- Corrigida a consulta usada pelo botão **Resumo do dia** no SQLite.

## Versão 2.3 — Relatórios Gerenciais

- Área de Relatórios com filtros de hoje, semana, mês, ano e período personalizado.
- Indicadores de receitas recebidas, despesas pagas, resultado, pendências, vendas e OS no período.
- Vendas agrupadas por forma de pagamento.
- Produtos/peças mais vendidos no período.
- Relação atual de itens com estoque baixo.

## Versão 2.4 — Compras e Reposição

- Registro de compra por fornecedor, data e NF/documento.
- Inclusão de várias peças, quantidades e custos em uma única compra.
- Entrada automática no estoque com histórico de movimento do tipo **Compra**.
- Geração opcional de conta a pagar vinculada ao fornecedor e à compra.
- Histórico de compras na nova aba **Compras**.

## Versão 2.5 — Condições de pagamento em Compras

- Geração de contas a pagar em uma ou várias parcelas, vinculadas à compra e ao fornecedor.
- Condições à vista, parcelado e entrada mais parcelas.
- Intervalos configuráveis, como 30/60/90/120 dias.
- Prévia editável de vencimento e valor de cada parcela antes de concluir a compra.
- Entrada total do estoque preservada na data do lançamento da compra.

## Versão 2.6 — Cancelamento de Compras e valores monetários

- Valores de itens, totais e parcelas exibidos sempre com duas casas decimais.
- Arredondamento financeiro por centavo na geração das parcelas.
- Cancelamento/devolução integral de compra com estorno automático do estoque.
- Cancelamento automático das parcelas pendentes vinculadas à compra.
- Parcelas já pagas permanecem registradas para preservar o histórico financeiro.

## Versão 2.6.1 — Correção do cancelamento de compra

- Corrige a leitura do status da compra durante o cancelamento/devolução.

## Versão 2.7 — Visões de Contas a Pagar e a Receber

- Telas independentes para Contas a Pagar e Contas a Receber.
- Filtros por situação: Todos, Pendentes, Vencidas, Pagas e Canceladas.
- Filtros por vencimento: Hoje, Semana, Mês, Ano ou período personalizado.
- Pesquisa por pessoa, fornecedor, documento, descrição ou categoria.
- Totais de pendentes, vencidas, pagas e canceladas para o período selecionado.

## Versão 2.8 — Alertas financeiros

- Alertas de contas vencidas, que vencem hoje e nos próximos 7 dias.
- Indicador de alertas no Painel principal.
- Tela detalhada acessível pelo Painel ou pelo menu Financeiro.
- Alertas consideram apenas contas pendentes; contas pagas e canceladas ficam fora da lista.

## Versão 2.9 — Orçamentos separados da OS

- Aba própria para criar orçamentos com cliente, veículo, validade padrão de 10 dias e observações.
- Inclusão de serviços, peças ou outros itens, com total calculado automaticamente.
- Orçamento não altera estoque nem caixa.
- Aprovação, reprovação e conversão controlada de orçamento aprovado em Ordem de Serviço.
- A conversão copia os itens para a OS e impede nova conversão do mesmo orçamento.
- A baixa de peças continua ocorrendo somente quando a OS é concluída, preservando o controle de estoque.

## Versão 3.0 — Atendimento completo de orçamentos

- Pesquisa e cadastro rápido de cliente diretamente no orçamento.
- Veículos filtrados pelo cliente, com botão para novo veículo ou moto.
- Atendimento do orçamento com carrinho de itens visível, busca por peça, código interno ou código de barras, serviços e itens manuais.
- Visualização comercial do orçamento com cliente, veículo, condições, itens e total.
- Impressão ou salvamento em PDF e envio da mensagem do orçamento pelo WhatsApp.
- Os itens seguem sem movimentar estoque, caixa ou financeiro até a conclusão da OS convertida.

## Hotfix 3.0.1 — Campos e catálogo do orçamento

- Corrigida a sobreposição dos botões de cliente na tela de orçamento.
- Catálogo de serviços com busca por nome ou código.
- Botões **Nova peça** e **Novo serviço** disponíveis durante a inclusão de itens, mantendo o orçamento aberto.

## Versão 3.1 — Painel executivo

- Novo Dashboard em azul-marinho, cobalto, turquesa e aqua, inspirado no painel da JL Motos.
- Cards com entradas do dia, resultado do mês, caixa, contas a receber/pagar, OS e estoque baixo.
- Área de atenção operacional para contas vencidas e próximos vencimentos.
- Ações rápidas para Relatórios e Alertas financeiros.
- Botão de privacidade para ocultar/exibir valores financeiros no Painel.

## Versão 3.2 — Aprovação comercial do orçamento

- Aprovação registra automaticamente data e hora.
- Formas de aceite: presencial/assinatura, WhatsApp, telefone, e-mail ou outro.
- Campo de observação para protocolo, confirmação ou condições do cliente.
- Histórico de aprovação aparece no documento do orçamento.
- Ao converter em OS, a aprovação é copiada para as observações da OS.

## Versão 3.3 — OS derivada do orçamento

- OS convertida identifica o número do orçamento de origem na lista e no documento.
- Documento da OS mostra o histórico de aprovação comercial.
- Botão **Avançar status** para conduzir a OS por Aprovada, Em manutenção, Serviço concluído, Pronta para retirada e Finalizada.
- Baixa de peças é disparada somente ao chegar em Serviço concluído, mantendo o controle de estoque existente.

## Versão 3.4 — Recebimento integrado da OS

- Botão **Receber OS** disponível após o serviço concluído.
- Lançamento automático de receita paga no Financeiro, vinculado à OS.
- Dinheiro exige caixa aberto e cria entrada física no Caixa.
- PIX, cartão, transferência e boleto permanecem no fluxo financeiro.
- Proteção contra registrar o mesmo recebimento duas vezes.

## Versão 3.5 — Entrega e quitação da Ordem de Serviço

- Fluxo operacional: **Serviço concluído → Aguardando pagamento → Pronta para retirada → Finalizada**.
- O recebimento muda automaticamente a OS para **Pronta para retirada**.
- A finalização passa a confirmar a retirada do veículo.
- A grade identifica cada OS como **Quitada** ou **Pendente**.
- O documento da OS mostra situação, forma e data/hora do pagamento.

## Versão 3.5.1 — Pesquisa em cadastros e lançamentos

- Pesquisa de cliente na abertura de Ordem de Serviço e no cadastro de veículo.
- Pesquisa de fornecedor e peça no lançamento de compras.
- Pesquisa de peça ou serviço na inserção de itens de Ordem de Serviço e orçamento.
- Campos de cliente, veículo, fornecedor, produto, serviço e categoria com digitação e sugestões de busca.

## Versão 3.5.2 — Correção de vínculo na edição da OS

- Correção no carregamento de cliente e veículo ao editar uma Ordem de Serviço.
- A tela passa a selecionar os registros pelo identificador interno, evitando troca visual de cliente após uma busca.

## Versão 3.5.3 — Correção definitiva da edição da OS

- A edição consulta novamente a OS pelo código selecionado antes de preencher o formulário.
- Cliente e veículo são carregados diretamente do banco, independentemente da grade e da pesquisa visual.

## Versão 3.5.4 — Estabilidade da seleção na edição da OS

- Correção da exceção de índice durante o carregamento de cliente e veículo.
- A seleção passa a usar o valor interno do registro, sem depender de a lista visual já estar pronta.

## Versão 3.5.5 — Carregamento pós-abertura da OS

- Cliente e veículo da OS são selecionados somente após a janela terminar de carregar.
- Evita que a lista ainda em montagem mostre o primeiro cliente em vez do vínculo real da OS.

## Versão 3.5.6 — Seleção direta na edição da OS

- A tela de OS não usa mais a seleção automática do componente do Windows para cliente e veículo.
- Cada opção da lista mantém o identificador real do cadastro, garantindo que a edição apresente exatamente o cliente e o veículo gravados na OS.

## Versão 3.5.7 — Agenda de Serviços

- Nova aba **Agenda** para registrar data/hora, cliente, veículo, serviço previsto e observações.
- Status de agendamento: Agendado, Confirmado, Cancelado e Convertido.
- Ação **Gerar OS** cria uma única Ordem de Serviço a partir do agendamento, preservando a ligação entre os dois registros.

## Versão 3.5.8 — Confirmação e lembrete da Agenda

- Botão **Confirmar** para registrar o aceite do atendimento agendado.
- Botão **WhatsApp** abre uma mensagem pronta com data, horário, veículo e serviço previsto.
- Lembretes não são enviados automaticamente: o atendente sempre confere a mensagem antes de enviá-la.

## Versão 3.5.9 — Histórico do Veículo

- Botão **Histórico** na aba Veículos.
- Consulta das OS anteriores do veículo, com situação, problema relatado, peças/serviços utilizados e total de cada atendimento.

## Versão 3.6.0 — Dados da Oficina

- Nova aba **Configurações** para cadastrar nome fantasia, razão social, CNPJ/CPF, telefone, e-mail e endereço.
- Os dados informados passam a aparecer no cabeçalho dos documentos de Ordem de Serviço.

## Versão 3.6.1 — Backup Local

- Botão **Criar backup** na aba Configurações.
- Gera uma cópia completa do banco local, com cadastros, OS, estoque, agenda, caixa e financeiro.
- O destino é escolhido pelo usuário, facilitando salvar em nuvem sincronizada, pendrive ou HD externo.

## Versão 3.6.2 — Restauração segura

- Botão **Restaurar backup** na aba Configurações.
- Valida se o arquivo selecionado pertence ao sistema antes da restauração.
- Cria automaticamente uma cópia da base atual e reinicia o aplicativo após concluir.

## Versão 3.7.0 — Usuários e permissões

- Primeiro acesso cria a conta de Administrador protegida por senha.
- Login obrigatório em toda abertura do sistema.
- Administrador pode cadastrar, ativar ou desativar usuários com perfil Administrador, Atendente ou Mecânico.
- Perfil Mecânico tem acesso apenas às áreas operacionais necessárias ao trabalho.

## Versão 3.7.1 — Gestão de senhas

- Botão **Minha senha** disponível para cada usuário conectado.
- A troca exige a senha atual e confirmação da nova senha.
- Administrador pode redefinir a senha de usuários cadastrados em Configurações > Usuários.

## Versão 3.7.2 — Auditoria visível

- Botão **Auditoria** nas Configurações do administrador.
- Consulta os últimos mil eventos, incluindo cadastros, status de usuário, senha, backup, restauração e ações operacionais registradas.

## Versão 3.7.3 — Auditoria em português

- Ações técnicas da auditoria passam a ser apresentadas em português.
- Identificadores antigos são contextualizados, por exemplo: “Agendamento nº 1 confirmado”.

## Versão 3.7.4 — Cobertura completa da auditoria

- Tradução de eventos de orçamento, compras, fornecedores, devedores, financeiro, caixa, venda e categorias.
- Registros existentes e novos passam a ser exibidos em linguagem operacional em português.

## Versão 3.8.0 — Garantia da Ordem de Serviço

- Campo de garantia em dias ao criar ou editar uma OS.
- Documento da OS apresenta o prazo e a data de validade calculada.
- Não altera OS anteriores: elas permanecem com garantia de zero dias até serem editadas, se necessário.

## Versão 3.8.1 — Consulta de Garantias

- Nova aba **Garantias**.
- Lista OS finalizadas com prazo de garantia, cliente, veículo e data de validade.
- Identifica automaticamente situações “Em garantia” e “Vencida”.

## Versão 3.8.2 — Pesquisa de Ordens de Serviço

- Filtro por status na aba Ordens de Serviço.
- Busca por número da OS, nome do cliente, veículo ou placa.

## Versão 3.8.3 — Pesquisa de Clientes e Veículos

- Busca direta na aba Clientes por nome, CPF/CNPJ, telefone ou e-mail.
- Busca direta na aba Veículos por cliente, placa, marca, modelo ou tipo.

## Versão 3.8.4 — Pesquisa de Produtos e Serviços

- Busca de produtos/peças por código interno, código de barras, nome ou categoria.
- Busca de serviços por código, nome ou categoria.

## Versão 3.8.5 — Localização de Peças

- O código técnico interno do banco deixa de ser exibido na grade de Produtos/Peças.
- Novo cadastro de **Prateleira** e **Localização** para cada peça.
- Grade redistribuída para priorizar nome e preço da peça.
- A localização aparece também nas buscas operacionais de produtos.

## Versão 3.8.6 — Grade de Peças e Privacidade do Painel

- Corrigida a grade de Produtos/Peças para manter **Preço de venda**, estoque e status visíveis.
- Larguras ajustadas para favorecer o nome da peça sem esconder informações importantes.
- Adicionado botão de ocultação de valores com ícone de olho no Dashboard, compatível com Windows 7.

## Versão 3.8.7 — Revisão de Grades Operacionais

- Botão de privacidade do Dashboard agora usa um ícone de olho destacado e compatível com Windows 7.
- Revisadas as grades de Clientes, Veículos, Serviços, Agenda, Ordens de Serviço, Orçamentos, Estoque, Compras, Fornecedores e Devedores.
- Colunas secundárias que causavam cortes foram redistribuídas ou ocultadas, preservando os dados operacionais principais visíveis.
- Corrigida também a distribuição de colunas em Produtos/Peças, mantendo preço, estoque e status acessíveis.

## Versão 3.8.8 — Botão de Privacidade do Dashboard

- Corrigido o posicionamento do botão de ocultar valores no Dashboard.
- O botão agora fica sobre o cabeçalho, no canto superior direito, com fundo turquesa e ícone de olho branco visível.

## Versão 3.8.9 — Correção de Escala do Dashboard

- Corrigida a ancoragem dos botões do cabeçalho do Dashboard em telas com escala do Windows.
- Os botões **Ver relatórios** e **Ocultar valores** passam a permanecer visíveis dentro do cabeçalho.

## Versão 3.9.0 — Privacidade no Painel Operacional

- O comando de ocultar/exibir valores foi movido para o painel **Atenção operacional**, ao lado do botão de alertas financeiros.
- Mantém ícone de olho e alterna corretamente entre ocultar e exibir os valores do Dashboard.

## Versão 3.9.1 — Recuperação de Administrador

- Novo botão **Recuperar acesso** na tela de login.
- Permite redefinir a senha do único administrador sem reinstalação e sem perda dos dados, exigindo o código da licença e o e-mail cadastrado do administrador.

## Versão 3.9.2 — Validação de Recuperação por Licença Assinada

- A recuperação de administrador também aceita a licença assinada completa, validada contra o computador em que o sistema foi ativado.

## Versão 3.9.3 — Recuperação Local em Instalação Ativada

- Disponibiliza recuperação do administrador diretamente no computador já ativado, com confirmação explícita e registro em auditoria.

## Versão 3.10.0 — Recuperação Comercial Segura

- Recuperação passa a exigir solicitação vinculada ao computador e autorização assinada temporária emitida pelo Painel RMC.
- Cria backup automático antes de redefinir a senha e registra solicitação, login e recuperação na auditoria.
- Remove a recuperação local direta da versão comercial.

## Hotfix 3.10.1 — Estabilidade do banco local

- Corrigido o bloqueio temporário do SQLite ao registrar a auditoria de login.
- A auditoria agora é gravada somente após liberar a leitura do banco, evitando a mensagem `database is locked` na entrada.
- Aplicada a mesma proteção às rotinas de abertura de caixa e conversão de orçamento em OS.

## Como compilar no Windows

Abra `RMCFlowComercioLocal.sln` no Visual Studio 2022 com a carga de trabalho **Desenvolvimento para desktop com .NET** e o targeting pack do .NET Framework 4.8.

```powershell
nuget restore RMCFlowComercioLocal.sln
msbuild RMCFlowComercioLocal.sln /p:Configuration=Release /p:Platform=x86
msbuild RMCFlowComercioLocal.sln /p:Configuration=Release /p:Platform=x64
```

Depois, use os scripts em `installer/` para produzir os dois instaladores. A ativação offline já está estruturada; as telas operacionais serão adicionadas nas próximas etapas.

Para o processo completo de compilação, instalação, ativação por licença e entrega ao cliente, consulte [GUIA-DE-ENTREGA-COMERCIAL.md](docs/GUIA-DE-ENTREGA-COMERCIAL.md).

> Ao abrir uma atualização do projeto, clique com o botão direito na solução e selecione **Restaurar Pacotes NuGet** antes de compilar.
