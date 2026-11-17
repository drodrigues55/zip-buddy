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

Token GitHub (fine-grained) com **Contents: Read and write** + **Pull requests: Read and write** no repositório `microsoft/winget-pkgs`:

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

## Estado

- [x] Schema 1.28.0 aplicado e validado localmente
- [x] CLA aceito
- [ ] PR `ZipBuddy.ZipBuddy version 1.1.0` submetido
- [ ] Aprovação de moderador
- [ ] `winget install ZipBuddy.ZipBuddy` funcionando
