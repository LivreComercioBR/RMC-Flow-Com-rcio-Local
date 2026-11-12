# Fase 5 — Documentos, PDF e impressão

Nas telas de documento da OS e do orçamento estão disponíveis dois formatos:

- **Imprimir A4 / PDF**: abre primeiro a visualização de impressão do aplicativo. Na prévia, use o comando de imprimir e selecione uma impressora ou **Microsoft Print to PDF** para salvar em PDF.
- **Imprimir cupom**: abre a prévia em largura compacta para impressora térmica. Na impressão, escolha a impressora de cupom e o papel configurado pelo driver.
- **Enviar pelo WhatsApp**: abre uma conversa com o texto da OS/orçamento. Para anexar o PDF, salve-o primeiro em A4 e anexe o arquivo na conversa.

O documento da OS inclui dados da oficina, cliente e equipamento, problema, diagnóstico, observações, itens, total, situação de pagamento, garantia e origem/aprovação de orçamento quando disponíveis. O orçamento inclui validade, itens, total e aprovação registrada.

## Compilação

Projeto compilado em Debug x86 com MSBuild/.NET Framework 4.8. Permaneceram dois avisos preexistentes sobre os métodos `LoginForm.Enter()` e `CashForm.Move(string)` esconderem membros herdados.
