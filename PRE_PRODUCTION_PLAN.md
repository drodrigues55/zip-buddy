# Plano de Validação Pré-Produção — WIP v1.1.0 (Checksum, Teste, Compactar/e-mail, Menu de Contexto)

> **Status:** Fases 0, 1, 3 e 5 concluídas ✅; Fase 4 (GO/NO-GO + publicação) pendente ⏳  
> **Data:** 2026-11-16  
> **Progresso:** build Release 0 erros · **246/246 testes aprovados** · artefatos v1.1.0 gerados e verificados (RC suite 100%)  
> **Baseline analisado:** commit `73f6b6f` + 11 arquivos alterados/não commitados  
> **Objetivo:** transformar o trabalho em andamento em uma release candidata segura, com correções bloqueantes, cobertura de testes, matriz de validação manual e critérios claros de Go/No-Go.

---

## 1. Escopo do trabalho em andamento

| # | Feature | Arquivos | Superfície de entrada |
|:-:|:---|:---|:---|
| F1 | Cálculo de hashes (CRC-32, CRC-64, SHA-1, SHA-256) | `Core/Services/ChecksumCalculator.cs`, `Views/ChecksumWindow.xaml(.cs)`, `Tests/ChecksumCalculatorTests.cs` | `--crc-sha`, menu `CRC SHA` |
| F2 | Teste de integridade via janela de progresso | `TaskProgressWindow.xaml.cs` (`TaskType.Test`) | `--test-archive`, menu `Testar arquivo compactado` |
| F3 | Diálogo "Adicionar ao arquivo compactado" | `Views/CompressOptionsWindow.xaml(.cs)` | `--compress-dialog`, `--compress-email-dialog` |
| F4 | Envio por e-mail após compactação | `Windows/System/EmailService.cs`, `openEmailAfter` no `TaskProgressWindow` | `--compress-email-*`, checkbox no diálogo F3 |
| F5 | Novos atalhos CLI de compactação direta | `App.xaml.cs` | `--compress-email-zip`, `--compress-email-7z` |
| F6 | Reformulação do menu de contexto do Explorer | `Windows/Shell/ContextMenuManager.cs`, `SettingsViewModel.cs` | 12 verbos em cascata (arquivos) / 7 (pastas e `*`), rótulos pt-BR/en-US |
| F7 | Localização dos rótulos do menu de contexto | `SettingsViewModel.cs` (parâmetro `isPortuguese`) | Detecção via `ILocalizationService.CurrentCulture` |

**Baseline medido em 16/11/2026:**
- `dotnet build ZipBuddy.slnx -c Release --no-restore` → **0 erros**, 184 avisos (predominantemente `CA1707` preexistente nos testes).
- `dotnet test --no-build -c Release` → **201/201 aprovados**, 0 falhas (antes do WIP eram 121; os 3 testes novos são de `ChecksumCalculator`).
- Ou seja: **o código compila e a suíte atual passa**. Os riscos abaixo são funcionais/UX e de regressão, não de compilação.

---

## 2. Achados bloqueantes (corrigir antes do RC)

| ID | Severidade | Achado | Evidência | Ação corretiva |
|:--|:--|:---|:---|:---|
| **B1** | 🔴 Alta | O menu em cascata **ignora os toggles de configuração** (`enableOpenWith`, `enableExtractHere`, `enableExtractToFolder`, `enableExtractTo`, `enableCompressToZip`, `enableCompressTo7z`). Todos os 12/7 verbos são registrados incondicionalmente. | `ContextMenuManager.cs` linhas 56-80 e 116-127 — os `if (enable*)` existem apenas no ramo *flat*. | Aplicar os mesmos `if` no ramo cascata; gerar lista de verbos a partir de uma função pura (`BuildArchiveVerbs(flags, culture)`) para permitir teste unitário. |
| **B2** | 🔴 Alta | `EmailService.SendFileByEmail` usa `MAPISendMail` com `MAPI_DIALOG` de forma **síncrona na thread de UI** (`FinishWithSuccess`), podendo congelar a janela de progresso. | `TaskProgressWindow.xaml.cs:593-596`. | Deslocar a chamada para `Task.Run`/fire-and-forget com tratamento de erro, ou abrir o `mailto:` diretamente; validar em manual test com Outlook instalado e sem cliente MAPI. |
| **B3** | 🔴 Alta | **i18n quebrado nas novas janelas**: `ChecksumWindow.xaml`, `CompressOptionsWindow.xaml`, mensagens de status e `FinishWithSuccess` estão fixas em pt-BR, enquanto o produto promete en-US/pt-BR. | Ex.: `OnCloseClick` → "Fechar", `StatusTextBlock` → "Calculando...", `FinishWithSuccess` → "Concluído com sucesso". | Usar `ILocalizationService` (mesmo padrão de `LocalizationService`), adicionar chaves nos dois catálogos e estender `LocalizationTests` com `AllChecksumAndCompressKeys_AreLocalizedInBothLanguages`. |
| **B4** | 🟠 Média | No **teste de integridade a velocidade fica sempre 0 B/s**: `CommonTestArchiveAsync` reporta bytes/arquivos, mas nunca preenche `BytesPerSecond`. | `ArchiveProviderBase.cs:431-441` vs `TaskProgressWindow.xaml.cs:284`. | Calcular throughput no `Progress` da janela (Stopwatch + delta de bytes) ou preencher `BytesPerSecond` no provider; adicionar teste de progresso. |
| **B5** | 🟠 Média | **Divergência de padrão**: `ContextMenuManager.RegisterContextMenu(..., useCascadingMenu = true)` vs `AppSettings.UseCascadingContextMenu = false`. O fluxo principal passa o valor explicitamente, mas chamadas futuras/herdadas ficam inconsistentes. | Assinatura alterada no diff; `AppSettings.cs:21`. | Alinhar o default do parâmetro ao `AppSettings` (ou torná-lo obrigatório) e documentar a migração. |
| **B6** | 🟠 Média | `CompressOptionsWindow.OnCompressClick` valida apenas destino vazio. Faltam: destino == origem, caracteres inválidos, sobrescrever sem confirmação, e **senha com TAR/GZip** (o motor rejeita: *"Password protection is supported only for ZIP and 7z archives"*). | `CompressOptionsWindow.xaml.cs:127-168`; `ArchiveProviderBase.cs:484-485`. | Reutilizar validação do `MainViewModel`/`PathSanitizer`; desabilitar `PasswordBox` quando formato ≠ ZIP/7z; diálogo de confirmação de sobrescrita. |
| **B7** | 🟠 Média | **Chaves de registro obsoletas**: ao desativar um comando no modo cascata, o subkey numerado (`01_Open`, `12_CrcSha`, ...) não é removido; o item continua no Explorer. | `ContextMenuManager.cs` — limpeza só remove os 4 verbos legados. | Antes de registrar, apagar todo o subkey `...\shell\Zip Buddy` e reconstruí-lo; idem para `*` e `Directory`. |
| **B8** | 🟡 Baixa | Detecção de senha no teste é por substring do `ErrorMessage` ("password"/"encryption") e não tem limite de tentativas. | `TaskProgressWindow.xaml.cs:280-294`. | Usar um campo estruturado em `OperationResult` (ex.: `RequiresPassword`) e limitar a 3 tentativas. |

---

## 3. Plano de implementação por fases

### Fase 0 — Estabilização (pré-RC) — ✅ CONCLUÍDA
1. ✅ **B1/B5/B7** — verbos do Explorer agora vêm de `Core/Services/ContextMenuDefinitions.cs` (função pura), respeitam todos os toggles, o default de `useCascadingMenu` foi alinhado a `AppSettings` (`false`) e o grupo é reconstruído do zero a cada registro, sem chaves órfãs. `UnregisterContextMenu` usa a mesma lista de chaves.
2. ✅ **B6** — `Core/Services/CompressionOptionsMapper.cs` centraliza formato/nível/extensão/validação de destino; `CompressOptionsWindow` valida destino vazio/inválido/igual à origem, exige confirmação de sobrescrita e desabilita senha/nível para TAR/GZip.
3. ✅ **B3** — todas as mensagens novas de `ChecksumWindow`, `CompressOptionsWindow` e `TaskProgressWindow` (teste) passaram a usar `ILocalizationService`; 54 chaves adicionadas aos catálogos en-US e pt-BR, com teste de paridade.
4. ✅ **B2** — `EmailService.SendFileByEmailAsync` executa o diálogo MAPI em thread STA dedicada com `CoInitializeEx`; `FinishWithSuccess` faz `await` e registra falhas no log sem derrubar a operação.
5. ✅ **B4** — `CommonTestArchiveAsync` reporta `BytesPerSecond` e `EstimatedTimeRemaining`, eliminando o "0 B/s" no teste de integridade.
6. ✅ **B8 (bônus)** — loop de senha no teste limitado a 3 tentativas, com mensagem localizada.
7. **Verificação:** `dotnet build ZipBuddy.slnx -c Release` → 0 erros; `dotnet test` → **223/223 aprovados**. Novos testes: `ContextMenuDefinitionsTests`, `CompressionOptionsMapperTests`, `NewFeatureLocalizationTests`.
8. **Pendente para Fase 2:** validar em Windows real os itens M01, M02, M03 e M09 (comportamento de registro/Explorer e MAPI não é simulável em teste unitário).

### Fase 1 — Cobertura de testes — ✅ CONCLUÍDA
| Novo teste | Objetivo | Resultado |
|:---|:---|:---|
| `ChecksumCalculatorTests` (4 novos) | Vetores CRC-64/XZ e SHA-1/SHA-256 conhecidos, progresso monotônico em arquivo de 2 MB e cancelamento em pleno stream | ✅ **Encontrou bug:** o CRC-64 usava o polinômio em forma normal (`0x42F0E1EB…`) num algoritmo refletido. Corrigido para `0xC96C5795D7870F42`, agora o vetor confere `995DC9BBDF1939FA`. |
| `ContextMenuDefinitionsTests` (3 novos) | 64 combinações de toggles, paridade pt-BR/en-US, chaves únicas e default compartilhado com `AppSettings` | ✅ |
| `CompressionOptionsMapperTests` (4 novos) | Extensões do seletor de arquivo, idempotência, limites de nível por formato e destino padrão | ✅ |
| `NewFeatureLocalizationTests` (1 novo) | Fallback para cultura não suportada (fr-FR → en-US) | ✅ |
| `ArchiveIntegrityTests` (3 novos) | ZIP com byte corrompido, ZIP criptografado com/sem senha | ✅ **Encontrou bug:** `TestArchiveAsync` reportava sucesso mesmo com CRC divergente. `CommonTestArchiveAsync` agora compara o CRC32 calculado com o CRC da entrada e preenche `CorruptedEntries`. |
| `OperationResultTests` (2 novos) | Heurística `IndicatesPasswordFailure` e default de `RequiresPassword` | ✅ |
| `EmailServiceTests` | Fallback `mailto:` sem MAPI | ⏭️ Não viável: o projeto de testes (`net10.0-windows`) não referencia `ZipBuddy.Windows` (`net10.0-windows10.0.19041.0`), e invocar MAPI em teste poderia abrir o Outlook. Coberto pelo teste manual **M09**. |

**Contratos adicionados na Fase 1:** `OperationResult.RequiresPassword` (campo estruturado, usado pelo UI em vez de só substring — B8 completo) e `ChecksumCalculator.Crc32Update/Crc32Finalize` (reutilizados na verificação de integridade de arquivos).

**Meta atingida:** 201 → **246 testes**, 100% aprovados, 0 erros de build.

> ⚠️ **Mudança visível ao usuário:** os valores de **CRC-64** calculados antes desta correção estavam errados. Qualquer documento/release note deve mencionar que o CRC-64 agora segue o padrão CRC-64/XZ.

### Fase 2 — Validação manual (matriz de aceite) — ~1 dia — 🛠️ Ferramentas prontas

**Checklist executável:**
```powershell
# Gates automáticos (build, 246 testes, versão, contratos CLI/i18n, snapshot do Registry)
powershell -ExecutionPolicy Bypass -File packaging\validate_phase2.ps1

# + checklist interativo M01–M11, gravando PASS/FAIL/N/A e evidências
powershell -ExecutionPolicy Bypass -File packaging\validate_phase2.ps1 -Interactive

# Opcional: publicar portable e calcular SHA256 antes dos testes manuais
powershell -ExecutionPolicy Bypass -File packaging\validate_phase2.ps1 -Publish -Interactive
```
- Detalhes passo a passo: `qa/PHASE2_CHECKLIST.md`
- Saída: `qa/results/<timestamp>/report.md` + `results.json` (ignoreado pelo Git)
- O script existente `packaging/verify_release_candidate.ps1` continua responsável pelo instalador/winget (item M11).
- **Execução final em 16/11/2026:** A02–A06 PASS (inclusive portable e SHA256), A06i/A07 INFO, M01–M11 PENDING (dependem de validação humana em Explorer/Outlook).
Executar o binário **portable** e o **instalador** em Windows 11 x64 e Windows 10 19041+, nos dois idiomas do sistema.

| ID | Cenário | Resultado esperado |
|:--|:---|:---|
| M01 | Clique direito em `.zip`/`.7z` com menu em cascata | 12 itens ordenados, rótulos no idioma do sistema, ícone nativo, nenhum item duplicado |
| M02 | Alternar cada toggle em Configurações e reabrir o Explorer | Itens correspondentes aparecem/desaparecem imediatamente |
| M03 | Alternar cascata ↔ flat, pt-BR ↔ en-US | Nenhuma chave órfã; rótulos atualizados sem reiniciar o Explorer |
| M04 | `--test-archive` em ZIP íntegro | "Teste concluído", N itens, barra 100%, velocidade > 0, sem senha pedida |
| M05 | `--test-archive` em ZIP com CRC corrompido | Falha com lista de entradas corrompidas; sem travamento |
| M06 | `--test-archive` em 7z criptografado | Solicita senha; senha correta conclui; Cancelar encerra limpo; máx. 3 tentativas |
| M07 | `--crc-sha` em arquivo conhecido (`123456789`) e arquivo grande (≥ 200 MB) | Valores conferem com ferramenta de referência; progresso responsivo; cancelamento funciona |
| M08 | `--compress-dialog`: ZIP/7z com senha; TAR/GZ sem senha | Arquivo criado e reaberto; senha bloqueada para TAR/GZ com aviso claro |
| M09 | `--compress-email-zip` / `-7z` e checkbox "Enviar por e-mail" | Compressão termina antes de abrir e-mail; UI não congela; fallback `mailto:` + Explorer se não houver MAPI |
| M10 | Cancelar/fechar janelas novas durante operação | Sem processo órfão, sem exceção, sem arquivo parcial |
| M11 | Instalador sobre versão v1.0.1 | Menu/registro migrados sem duplicidade; desinstalação limpa as chaves `HKCU\Software\Classes` |

### Fase 3 — Hardening de release — ✅ CONCLUÍDA
1. ✅ **README** atualizado: badge v1.1.0, 246 testes, novas seções (Teste de integridade, CRC SHA, Compactar/e-mail), 6 novos verbos CLI e menu de 12 comandos.
2. ✅ **Versão v1.1.0** sincronizada em `Directory.Build.props` (Assembly/File 1.1.0.0), `zipbuddy_setup.iss`, `Package.appxmanifest` e `LocalizationTests` (agora aceita ≥ 1.1).
3. ✅ **Scripts corrigidos**: caminho winget `z\ZipBuddy\ZipBuddy` em `generate_winget_manifest.ps1` e `verify_release_candidate.ps1` (antes apontavam para `f\Zip Buddy\Zip Buddy`); versões default 1.1.0; gerador agora emite também `locale.pt-BR.yaml`; tabela de hashes de `prepare_github_release.ps1` corrigida (variáveis antes literais).
4. ✅ **Artefatos gerados e validados**:
   - `ZipBuddySetup-x64-v1.1.0.exe` — 165,40 MB — SHA256 `6633CD41…D1F67`
   - `ZipBuddy.UI.exe` (portable/launcher) — 130,96 MB — SHA256 `7F2061F8…A2002`
   - `ZipBuddy-v1.1.0-windows-x64-portable.zip` — 90,08 MB — SHA256 `C74DF806…CD010A`
   - `manifest.json` assinado RSA-SHA256 (auto-teste de assinatura OK)
   - Manifests winget v1.1.0 en-US + pt-BR com hash casando
5. ✅ `verify_release_candidate.ps1 -Version 1.1.0`: **100% PASS** (hash, winget validate, instalação silenciosa em sandbox, desinstalação limpa).
6. ✅ Notas de release em `packaging/RELEASE_NOTES_v1.1.0.md` com a tabela de hashes real.
7. ✅ `validate_phase2.ps1`: A02–A06 PASS, A06i/A07 INFO, M01–M11 PENDING (esperado — dependem de intervenção humana).

### Fase 4 — Go / No-Go
- **Go** se: 0 erros de build, 100% dos testes, todas as linhas M01–M11 aprovadas, nenhum achado B1–B7 em aberto, artefatos assinados/verificados.
- **No-Go** se: qualquer item 🔴 em aberto, regressão de menu/i18n, ou falha em M02/M09/M11 (são os de maior impacto ao usuário).

---

## 3.1 Fase 5 — Experiência do wizard e integração explícita — ✅ CONCLUÍDA

**Decisões fechadas com o usuário:**
- Checkboxes **nativos** do Inno na página final (sem botões Pascal Script, sem `TNewStaticText`).
- Item 1: `★ Adicionar ao menu de contexto do Windows (recomendado)` / `★ Add to Windows context menu (recommended)` — primeiro da lista, **marcado na primeira instalação** (`checkedonce`), destaque via `★` + "recomendado".
- Item 2: `Abrir Configurações de aplicativos padrão` / `Open Default Apps settings` — desmarcado por padrão (limitação do Windows: `UserChoice` não pode ser definido por código).
- Desinstalação: remoção do menu **automática por padrão** (já implementada e validada), sem checkbox — mantém os processos separados.
- Menu instalado: **cascata (12 itens)**, com a preferência persistida no `settings.json`.

| # | Arquivo | Mudança |
|:-:|:---|:---|
| 1 | `Core/Services/ContextMenuDefinitions.cs`, `Infrastructure/Settings/AppSettings.cs` | `DefaultUseCascadingMenu = true` (alinhar portable e instalado) |
| 2 | `Tests/ContextMenuDefinitionsTests.cs`, `Tests/SettingsTests.cs` | Atualizar 4 asserções de default (`false` → `true`): `ContextMenuDefinitionsTests` + `SettingsTests` linhas 160, 1160 e a de shared default |
| 3 | `UI/App.xaml.cs` | Novo verbo `--register-context-menu [cascata\|flat]`: carrega settings, grava `EnableContextMenu=true` e `UseCascadingContextMenu`, chama `RegisterContextMenu(...)` com todos os toggles e `isPortuguese`, depois encerra |
| 4 | `packaging/installer/zipbuddy_setup.iss` | `[CustomMessages]` pt-BR/en-US para os 2 rótulos; reordenar `[Run]`: (a) adicionar ao menu (`postinstall runasoriginaluser checkedonce`), (b) default apps (`postinstall shellexec skipifsilent unchecked`), (c) launch app; adicionar entrada **somente para silencioso** (`Check: WizardSilent`, `runhidden runasoriginaluser`) para registrar o menu também via winget/instalação silenciosa |
| 5 | `README.md`, `packaging/RELEASE_NOTES_v1.1.0.md`, `qa/PHASE2_CHECKLIST.md` | Documentar as opções do wizard, o default cascata e a ressalva de que checkboxes `postinstall` são ignorados em silencioso (coberto pela entrada `WizardSilent`) |
| 6 | Rebuild/verificação | `dotnet build/test` → `build_installer.ps1 -Version 1.1.0` → `generate_winget_manifest.ps1` → `prepare_github_release.ps1 -SkipBuild` → `verify_release_candidate.ps1` → `measure_install_flow.ps1` → atualizar hashes das notas |

**Ajustes feitos durante a implementação:**
- `checkedonce` **não é flag de `[Run]`** (é de `[Tasks]`) — removido; o checkbox do menu vem marcado por padrão em toda instalação (re-registrar é idempotente).
- `WizardSilent` foi trocado por `skipifnotsilent`, flag nativa e documentada.
- O `.iss` ganhou **BOM UTF-8** para os rótulos com `★` e acentos pt-BR serem lidos corretamente.
- **Bug encontrado:** os verbos helper travavam o instalador/desinstalador. Causa raiz dupla: (1) `Application.Current.Exit()` não encerra o WinUI quando nenhuma Window foi criada; (2) o launcher esperava `WaitForMainWindow()` para um verbo que nunca cria janela, exibindo MessageBox modal. Corrigido com `Environment.Exit(0)` no app e detecção de helper verb no launcher (`WaitForExit`, sem espera de janela). Também incluí o timestamp do launcher no marcador de versão para evitar reaproveitar binário antigo após rebuild com a mesma versão.

**Validações da Fase 5:**
- `.iss` compila com `CustomMessage.pt-BR`, `postinstall runasoriginaluser` e `skipifnotsilent`.
- `measure_install_flow.ps1`: **4/4 chaves HKCU presentes após instalação silenciosa** e **0/4 após desinstalação**; exe removido; instalação 8–12 s, desinstalação ~2 s.
- `verify_release_candidate.ps1 -Version 1.1.0`: 100% PASS (hash, winget validate, instalação/desinstalação silenciosa) — sem travamento.
- 246/246 testes, incluindo os 4 defaults ajustados para cascata.
- Pendente de validação humana (M01/M11): aparência do checkbox na tela final em instalação interativa.

---

## 4. Riscos e mitigação

| Risco | Mitigação |
|:---|:---|
| Registro do Explorer varia entre builds do Windows | Validar M01/M03 em Windows 10 e 11; usar apenas `MUIVerb` + valor padrão + `Icon`, sem `Extended`/APIs não documentadas |
| MAPI ausente ou cliente mal configurado | Fallback `mailto:` + `/select`; nunca propagar exceção para a janela de progresso |
| Hash CRC-64 com variante diferente da esperada pelo usuário | Documentar explicitamente **CRC-64/XZ (ECMA-182 reflectado)** e exibir vetor conhecido na documentação |
| Janela de progresso com arquivo de 10.000+ entradas | Manter progresso em `DispatcherQueue.TryEnqueue` e medir tempo de teste no benchmark |
| Regressão de i18n | Gate automatizado: todos os catálogos com paridade en-US/pt-BR antes do merge |

---

## 5. Entregáveis do plano

- [x] `PRE_PRODUCTION_PLAN.md` (este documento)
- [x] Correções B1–B8 implementadas com testes
- [x] Suíte 246 testes, 100% verde
- [x] `packaging/validate_phase2.ps1` + `qa/PHASE2_CHECKLIST.md` (ferramentas da Fase 2)
- [x] Limpeza de chaves HKCU do menu na desinstalação (`--unregister-context-menu` + `[UninstallRun]`), validada por `packaging/measure_install_flow.ps1` (0/4 resíduos)
- [x] Fase 5: wizard com checkbox destacado para adicionar o menu, fallback em silencioso, padrão cascata no app e no instalador
- [ ] Matriz M01–M11 preenchida com evidências (screenshots/logs) — **Fase 4**
- [x] README + notas de release v1.1.0 atualizadas
- [x] Artefatos RC gerados e validados localmente (dist/)
- [ ] Artefatos RC publicados no GitHub Releases (pre-release) para homologação — **Fase 4**

---

## 6. Observações sobre o estado atual

- Os arquivos do WIP **não estão commitados**; recomenda-se um commit atômico por feature (F1, F2, F3+F4, F6+F7) **após** as correções, para facilitar bisect e rollback.
- O README ainda anuncia **121 testes**, mas a suíte já tem **201** — atualizar junto com a release.
- Nenhuma alteração de `Directory.Build.props`/manifests foi feita no WIP; a versão ainda é 1.0.0/1.0.1, portanto o bump é obrigatório antes de publicar.
