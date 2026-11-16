# Checklist de Validação Manual — Fase 2 (RC v1.1.0)

> Use em conjunto com `packaging/validate_phase2.ps1`.  
> O script gera `qa/results/<timestamp>/report.md` com os gates automáticos e o registro das respostas.  
> Preencha também este arquivo com evidências (caminho do screenshot/log) quando aplicável.

## Ambiente de teste

| Campo | Valor |
|:---|:---|
| Data / hora | |
| Sistema operacional (build) | |
| Artefato testado (portable ou instalador + SHA256) | |
| Idioma do Windows (`pt-BR` / `en-US`) | |
| Cliente de e-mail instalado (Outlook / nenhum) | |
| Testador | |

**Matriz mínima:** Windows 11 x64 em `pt-BR` e `en-US`; Windows 10 build 19041+ em pelo menos um idioma; executar tanto o portable quanto o instalador.

---

## M01 — Menu em cascata em arquivos compactados

**Como:** clique direito em `.zip` e `.7z` no Explorer com "Usar submenu em cascata" ativado.

- [ ] 12 itens presentes e ordenados (Abrir, Extrair arquivos, Extrair aqui, Extrair em pasta separada, Testar, Adicionar, Comprimir/e-mail, 7Z, 7Z/e-mail, ZIP, ZIP/e-mail, CRC SHA) — agora é o **padrão do app e do instalador**
- [ ] Rótulos no idioma do sistema (sem mistura pt-BR/en-US)
- [ ] Ícone nativo do app visível
- [ ] Nenhum item duplicado ou órfão

Resultado: ☐ Passou ☐ Falhou ☐ N/A — Evidência: ______________________

## M02 — Toggles do menu de contexto

**Como:** Configurações → Explorador de Arquivos → alternar cada comando; reabrir o Explorer e conferir.

- [ ] `Abrir` aparece/desaparece
- [ ] `Extrair aqui`, `Extrair em pasta separada`, `Extrair para...` aparecem/desaparecem individualmente
- [ ] `Adicionar como ZIP` e `Adicionar como 7Z` aparecem/desaparecem individualmente
- [ ] Comandos sem toggle (Testar, CRC SHA, Adicionar ao arquivo compactado, Comprimir e enviar) permanecem disponíveis

Resultado: ☐ Passou ☐ Falhou ☐ N/A — Evidência: ______________________

## M03 — Alternância cascata ↔ flat e idioma

**Como:** alternar o submenu em cascata e o idioma (pt-BR ↔ en-US), aplicar e reabrir o Explorer.

- [ ] Modo flat mostra apenas os 6 verbos legados (Abrir, Extrair aqui, Extrair em pasta, Extrair para, ZIP, 7Z)
- [ ] Nenhum subkey numerado antigo (`01_Open`, `12_CrcSha`, ...) permanece ao desativar um comando
- [ ] Rótulos atualizados no idioma novo sem reiniciar o Explorer
- [ ] `Unregister` (remover menu) limpa todas as chaves `HKCU\Software\Classes`

Resultado: ☐ Passou ☐ Falhou ☐ N/A — Evidência: ______________________

## M04 — Teste de integridade: arquivo íntegro

**Como:** `ZipBuddy.UI.exe --test-archive "C:\...\valido.zip"` (ou menu `Testar arquivo compactado`).

- [ ] "Teste concluído" com N itens testados
- [ ] Barra de progresso chega a 100%
- [ ] Velocidade > 0 B/s (correção B4)
- [ ] Não solicita senha

Resultado: ☐ Passou ☐ Falhou ☐ N/A — Evidência: ______________________

## M05 — Teste de integridade: arquivo corrompido

**Como:** gerar um ZIP, alterar 1 byte do conteúdo com um editor hex e testar.

- [ ] Falha com mensagem " arquivos falharam na verificação de integridade" / "files failed integrity verification"
- [ ] Lista de entradas corrompidas aparece em "Ver mais detalhes"
- [ ] Sem travamento e sem processo órfão

Resultado: ☐ Passou ☐ Falhou ☐ N/A — Evidência: ______________________

## M06 — Teste com senha

**Como:** `--test-archive` em 7z criptografado; testar senha certa, errada (3x) e cancelar.

- [ ] Solicita senha quando necessário
- [ ] Senha correta conclui o teste
- [ ] 3 senhas erradas abortam com mensagem localizada (correção B8)
- [ ] Cancelar (deixar em branco) encerra limpo

Resultado: ☐ Passou ☐ Falhou ☐ N/A — Evidência: ______________________

## M07 — CRC SHA

**Como:** `--crc-sha` num arquivo com `"123456789"` e noutro de ≥ 200 MB.

- [ ] CRC-32 = `CBF43926`, CRC-64 = `995DC9BBDF1939FA` (vetores corrigidos na Fase 1)
- [ ] SHA-1 e SHA-256 conferem com outra ferramenta
- [ ] Progresso responsivo em arquivo grande; "Copiar Tudo" funciona
- [ ] Mensagens no idioma do sistema

Resultado: ☐ Passou ☐ Falhou ☐ N/A — Evidência: ______________________

## M08 — Diálogo "Adicionar ao arquivo compactado"

**Como:** `--compress-dialog` ou menu; testar ZIP/7z com senha e TAR/GZip.

- [ ] ZIP e 7Z com senha geram arquivos que reabrem com a senha
- [ ] TAR/GZip desabilitam senha e nível de compressão
- [ ] Destino igual à origem, vazio ou inválido é rejeitado com mensagem clara
- [ ] Sobrescrita pede confirmação

Resultado: ☐ Passou ☐ Falhou ☐ N/A — Evidência: ______________________

## M09 — Compactar e enviar por e-mail

**Como:** `--compress-email-zip`, `--compress-email-7z` e checkbox do diálogo; repetir sem cliente MAPI.

- [ ] Arquivo é criado antes de abrir o e-mail
- [ ] UI da janela de progresso não congela (correção B2)
- [ ] Com Outlook: abre rascunho com anexo
- [ ] Sem Outlook: abre `mailto:` + seleciona o arquivo no Explorer
- [ ] Cancelar/fechar o cliente não gera erro no log

Resultado: ☐ Passou ☐ Falhou ☐ N/A — Evidência: ______________________

## M10 — Cancelamento e fechamento

**Como:** cancelar/fechar as janelas novas durante operação em arquivo grande.

- [ ] Sem processo `ZipBuddy.UI.exe` órfão
- [ ] Sem exceção/crash report
- [ ] Sem arquivo parcial no destino

Resultado: ☐ Passou ☐ Falhou ☐ N/A — Evidência: ______________________

## M11 — Instalador sobre versão anterior

**Como:** instalar a RC sobre v1.0.1, verificar menu e desinstalar.

- [ ] Menu migrado sem duplicidade
- [ ] Instalação silenciosa: `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-` — deve registrar o menu via `skipifnotsilent` (checar 4/4 chaves HKCU; `packaging/measure_install_flow.ps1` já valida)
- [ ] Wizard interativo: última página mostra `★ Adicionar Zip Buddy ao menu de contexto do Windows (RECOMENDADO)` marcado, seguido de `Abrir Configurações de aplicativos padrão` desmarcado
- [ ] Desinstalação remove chaves `HKCU\Software\Classes` e arquivos
- [ ] `[UninstallRun]` executa `--unregister-context-menu` e remove as 4 chaves HKCU do menu (simular criando `Software\Classes\SystemFileAssociations\.zip\shell\Zip Buddy`, `*\shell\Zip Buddy`, `Directory\shell\Zip Buddy` antes de desinstalar). Automatizado em `packaging/measure_install_flow.ps1`
- [ ] `packaging/verify_release_candidate.ps1 -Version <versão>` passa (instalador, hash, winget)

Resultado: ☐ Passou ☐ Falhou ☐ N/A — Evidência: ______________________

---

## Go / No-Go

- [ ] Todos os gates automáticos passaram
- [ ] M01–M11 sem falhas
- [ ] Nenhum bloqueio 🔴 da Fase 0 em aberto
- [ ] README e notas de release atualizados (Fase 3)

**Decisão:** ☐ GO ☐ NO-GO — Responsável: ______________ — Data: __________
