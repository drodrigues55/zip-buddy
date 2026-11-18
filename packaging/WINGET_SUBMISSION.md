# Winget — diagnóstico e submissão (v1.1.0)

## Diagnóstico dos PRs anteriores (via GitHub API)

| PR | Identificador | Resultado | Motivo real |
|:--|:---|:---|:---|
| [#447753](https://github.com/microsoft/winget-pkgs/pull/447753) | `Firezip.Firezip` 1.0.0 | fechado 06/10/2026 | `LicenseUrl` apontava para `github.com/drodrigues55/firezip/...` → **404** (repo renomeado para `zip-buddy`). |
| [#447809](https://github.com/microsoft/winget-pkgs/pull/447809) | `Firezip.Firezip` 1.0.0 | fechado 16/11/2026 | **Manifest deprecado**: maintainer pediu schema `1.28.0` (usávamos `1.9.0`); fechado como *"Stale with ManifestVersion Deprecated"*. O pipeline até havia passado (`Azure-Pipeline-Passed`, `Validation-Completed`). |

CLA já foi aceito (comentário `@microsoft-github-policy-service agree`). A exigência de aprovação de moderador é padrão para PRs da comunidade.

**Não havia PR ativo para `ZipBuddy.ZipBuddy` 1.1.0.**

## Correção aplicada

- `packaging/generate_winget_manifest.ps1`: schema `1.9.0` → **`1.28.0`** nas 4 seções.
- Manifestos regenerados em `packaging/winget/manifests/z/ZipBuddy/ZipBuddy/1.1.0/`.
- `winget validate --manifest <dir>` → **Êxito na validação do manifesto** (winget 1.29.380).

## Submissão do novo PR

> **Importante:** use `wingetcreate >= 1.28.x`. A versão 1.12.13.0 rejeita os cabeçalhos de schema 1.28 com `The schema header URL does not match the expected pattern`.
> Atualize com `winget upgrade --id Microsoft.WingetCreate --exact`.

> **Importante:** use **Personal Access Token (classic)** com o escopo **`public_repo`**. Tokens **fine-grained** (`github_pat_...`) **não são suportados** pelo wingetcreate ([issue #595](https://github.com/microsoft/winget-create/issues/595)) e causam `ERRO: Resource not accessible by personal access token`. O token classic começa com `ghp_`.

Token GitHub **classic** com escopo `public_repo` (opcional `delete_repo` para limpar o fork se o PR falhar):

```powershell
# Armazenar o token uma vez (evita expor na linha de comando)
wingetcreate token --token SEU_TOKEN

# Usar CAMINHO ABSOLUTO (caminho relativo pode gerar "ERRO: O caminho não existe")
wingetcreate submit `
  --prtitle "ZipBuddy.ZipBuddy version 1.1.0" `
  --no-open `
  "C:\Users\DRODRIGUES\Documents\zip-buddy\packaging\winget\manifests\z\ZipBuddy\ZipBuddy\1.1.0"
```

Ou pelo site: https://github.com/microsoft/winget-pkgs/pulls → **New pull request**, base `master`, compare seu fork com os 4 YAMLs em `manifests/z/ZipBuddy/ZipBuddy/1.1.0/`.

**Título obrigatório:** `ZipBuddy.ZipBuddy version 1.1.0`

Checks a observar após abrir o PR:
1. `winget validate` / schema 1.28.0 — ✅ local.
2. URL do instalador e hash — o instalador está publicado em `releases/download/v1.1.0/...` com SHA-256 `B4FB84F9…`.
3. `LicenseUrl`/`PublisherUrl` — apontam para `github.com/drodrigues55/zip-buddy` (correto).
4. **SmartScreen**: o PR #447753 tomou flag de SmartScreen. O instalador não é assinado digitalmente; se o check recorrir, será necessário assinar o `.exe` com certificado de code signing (preferencialmente EV).

## Correções pós-validação — `Validation-Unattended-Failed` (17/11/2026)

O PR #482144 recebeu o label `Validation-Unattended-Failed`: *"failed to install without user input"*, mesmo com `Silent`/`SilentWithProgress` e `InstallerType: inno` corretos.

### Tentativa 1 — `ElevationRequirement: elevationRequired` (não resolveu)
Adicionamos `ElevationRequirement: elevationRequired` com `Scope: machine`. O bot revalidou (~2h depois) e **falhou novamente**.

### Tentativa 2 — escopo de usuário (aplicada, não resolveu)
O bot revalidou em 17/11 22:40 e **falhou novamente**, com o YAML correto no PR. Conclusão: o problema não é o manifesto.

### Tentativa 3 — remover `[Run]` que inicia a UI em instalação silenciosa (aplicada)
O `.iss` tinha uma entrada `skipifnotsilent` que executava o launcher (`--register-context-menu`) durante instalações silenciosas. Em sandbox sem sessão interativa, o WinUI pode exibir `MessageBox` modal → o validador interpreta como "user input". Entrada removida; instalador recompilado:

- Novo SHA-256 do instalador: `4EBD60716AB9FDC1F2E1202C28DEE2A684BF568C5AC9DC9F41E937521A21FA1A`
- Novo SHA-256 do portable: `55D2A754676C34853586C5268D35726647962360F413FF32683748808AF9EAFF`
- `manifest.json`: `957ED3551C3FDFBBF4B74274C9B628247CA10BFD390F091ED3F54DB938D4DF83`

⚠️ **Exige re-upload dos assets na release v1.1.0** (instalador, portable, `.sha256` e `manifest.json`) e atualização do hash no manifesto do PR.

### (histórico) Tentativa 2 detalhes — escopo de usuário
O instalador suporta `/CURRENTUSER` (validado localmente pelo `verify_release_candidate.ps1`, que instala e desinstala silenciosamente). Mudança **apenas no manifesto** — o binário/hash não muda:

```yaml
Scope: user
InstallerSwitches:
  Silent: /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /CURRENTUSER
  SilentWithProgress: /SILENT /SUPPRESSMSGBOXES /NORESTART /SP- /CURRENTUSER
```
Sem UAC, sem prompt — deve funcionar no sandbox de validação. `winget validate` local: **Êxito**.

> Nota: em YAML, comentários usam `#` (não `;`). Um `;` no here-string do gerador causou erro de scanner na primeira tentativa.

**Reenviar:** editar o YAML no branch do fork (`ZipBuddy.ZipBuddy-1.1.0-5d4c9790-4005-4943-8c6c-046dd5c5e6e5`) ou fechar o PR e submeter um novo sem `--replace` (o `--replace` só funciona para pacotes já mesclados).

## Estado

- [x] Schema 1.28.0 aplicado e validado localmente
- [x] `ElevationRequirement: elevationRequired` adicionado após `Validation-Unattended-Failed`
- [x] CLA aceito
- [x] wingetcreate atualizado para 1.28.1.0
- [x] Token classic (`public_repo`) armazenado via `wingetcreate token --store`
- [x] PR submetido: **https://github.com/microsoft/winget-pkgs/pull/482144** (17/11/2026 09:54 UTC, estado `open`)
- [ ] Validação automática (Azure pipeline) — monitorar via API
- [ ] Aprovação de moderador
- [ ] `winget install ZipBuddy.ZipBuddy` funcionando
