# Plano de Implementação: Adequação de Estrutura, Higienização de Tamanho e Publicação no GitHub Releases

> **Versão Alvo:** 1.1.0 (atual) — o plano original visava 1.0.0 GA  
> **Status:** Concluído ✅ — release publicada em 16/11/2026: https://github.com/drodrigues55/zip-buddy/releases/tag/v1.1.0  
> **Objetivo:** Adequar a estrutura de pastas do projeto, isolar arquivos binários pesados (> 100 MB) para não violar as restrições de push do GitHub, higienizar o repositório Git e automatizar a geração do kit oficial de release para publicação no GitHub Releases e Winget.  
> **Nota histórica:** as seções abaixo descrevem o processo original (v1.0.0). Os comandos e caminhos já foram atualizados para v1.1.0, que é a versão efetivamente publicada.

---

## 1. Diagnóstico e Auditoria Atual

### A. Violações de Limite de Arquivo do GitHub (Hard Limit de 100 MB)
O GitHub rejeita commits que contenham arquivos individuais superiores a **100 MB** (`remote: error: GH001: Large files detected`). **Situação atual (v1.1.0):** todos os binários estão protegidos; a raiz contém apenas o instalador e ele é ignorado pelo Git.

| Arquivo / Localização | Tamanho atual | Situação no Git | Resultado |
|---|---|---|---|
| `ZipBuddySetup-x64-v1.1.0.exe` (raiz) | 83,87 MB | Ignorado (`/*.exe`) | ✅ Seguro |
| `dist\ZipBuddySetup-x64-v1.1.0.exe` | 83,87 MB | Ignorado (`dist/`) | ✅ Seguro |
| `dist\release\*` (5 assets) | ~174 MB no total | Ignorado (`dist/`) | ✅ Publicados como assets da release |
| `src\ZipBuddy.*\bin\...` | — | Ignorado por `.gitignore` | ✅ Seguro |
| `.packages\...` (NuGet cache) | — | Ignorado por `.gitignore` | ✅ Seguro |

Aliases legados `MyUnzip.exe` / `MyUnzipUpdater.exe` foram removidos na v1.1.0; o instalador caiu de ~165 MB para 83,87 MB.

### B. Princípio Fundamental de Arquitetura de Repositório
* **Git Repository (Código):** Deve conter **apenas** código-fonte, configurações, scripts, manifestos e documentação (~5 MB a 10 MB total).
* **GitHub Releases (Binários):** Local oficial para onde os executáveis pesados (`.exe`, `.zip`) devem ser enviados como **Release Assets** anexados (o GitHub permite arquivos de até **2 GB** por anexo de release).

---

## 2. Etapas de Execução

```
 ┌────────────────────────────────────────────────────────────────────────┐
 │                      FLUXO DE ADEQUAÇÃO E RELEASE                      │
 └───────────────────────────────────┬────────────────────────────────────┘
                                     │
         ┌───────────────────────────┼───────────────────────────┐
         ▼                           ▼                           ▼
┌──────────────────┐        ┌──────────────────┐        ┌──────────────────┐
│   FASE 1:        │        │   FASE 2:        │        │   FASE 3:        │
│ Higienização do  │        │ Kit de Release   │        │ Publicação       │
│ Repositório Git  │        │ Automatizado     │        │ Oficial          │
│                  │        │                  │        │                  │
│ • Ignorar *.exe  │        │ • Script Único   │        │ • Commit & Push  │
│   na raiz        │        │ • Setup 1.0.0    │        │ • GitHub Release │
│ • Ignorar dist/  │        │ • Portable .zip  │        │   com Anexos     │
│ • Limpeza temp   │        │ • Hashes SHA-256 │        │ • Submissão      │
│ • Git < 10 MB    │        │ • Manifest RSA   │        │   Winget 1.0.0   │
└──────────────────┘        └──────────────────┘        └──────────────────┘
```

---

## FASE 1: Higienização e Adequação do Git

### 1.1 Atualização das Regras do `.gitignore`
Adicionar regras explícitas para garantir que nenhum executável compilado gerado na raiz ou na pasta de distribuição entre no histórico de commits:
```gitignore
# Root build executables & aliases
/*.exe
dist/
*.temp.config
```

### 1.2 Limpeza de Arquivos Temporários
* Remover `.nuget.temp.config` (redundante com `NuGet/NuGet.Config`).
* Manter os `.exe` na raiz e em `dist/` apenas como artefatos locais para uso/testes, porém 100% invisíveis ao `git add .`.

---

## FASE 2: Geração do Kit Oficial de Release (Release Assets)

Criar o script unificado `packaging/prepare_github_release.ps1` que prepara e organiza todos os artefatos em `dist/release/`:

1. **Instalador Oficial:**
   * `ZipBuddySetup-x64-v1.0.0.exe` (~162 MB)
   * `ZipBuddySetup-x64-v1.0.0.exe.sha256`
2. **Pacote Portátil Oficial (Standalone):**
   * Compactar `ZipBuddy.UI.exe`, `ZipBuddyUpdater.exe` e `THIRD-PARTY-NOTICES.md` em `ZipBuddy-v1.0.0-windows-x64-portable.zip` (~40-50 MB comprimido).
   * `ZipBuddy-v1.0.0-windows-x64-portable.zip.sha256`
3. **Manifesto do Auto-Updater:**
   * `manifest.json` assinado com RSA-SHA256 para permitir que o updater automático detecte e valide a v1.0.0.
4. **Relatório de Verificação:**
   * Tabela com todos os hashes SHA-256 calculados pronta para colar nas notas de lançamento do GitHub.

---

## FASE 3: Publicação no GitHub e Distribuição Pública

### 3.1 Subir o Código Limpo para o Repositório
No terminal:
```powershell
git status  # Deve mostrar apenas código-fonte, sem arquivos > 100 MB
git add .
git commit -m "build(release): publish Zip Buddy v1.1.0"
git branch -M main
git remote add origin https://github.com/drodrigues55/zip-buddy.git  # se ainda não configurado
git push -u origin main
```

### 3.2 Criar a Release no GitHub com os Binários Anexados
Existem duas opções práticas:

* **Opção A (Via Navegador / Interface Web):**
  1. Acessar `https://github.com/drodrigues55/zip-buddy/releases/new`.
  2. Escolher a tag: `v1.1.0`.
  3. Título: `Zip Buddy v1.1.0 — Integrity, Checksums and E-mail Workflows`.
  4. Descrição: Colar o conteúdo de `packaging\RELEASE_NOTES_v1.1.0.md`.
  5. Arrastar os arquivos de `dist/release/`:
     * `ZipBuddySetup-x64-v1.1.0.exe`
     * `ZipBuddySetup-x64-v1.1.0.exe.sha256`
     * `ZipBuddy-v1.1.0-windows-x64-portable.zip`
     * `ZipBuddy-v1.1.0-windows-x64-portable.zip.sha256`
     * `manifest.json`
  6. Clicar em **Publish release**.

* **Opção B (Via Terminal com GitHub CLI `gh`):**
  ```powershell
  powershell -ExecutionPolicy Bypass -File packaging\publish_pre_release.ps1 -Execute
  # equivalente: gh release create v1.1.0 --title "..." --notes-file packaging\RELEASE_NOTES_v1.1.0.md --prerelease dist\release\*
  ```

### 3.3 Submissão ao Windows Package Manager (Winget)
Com o instalador hospedado na URL pública oficial do GitHub Releases:
```powershell
wingetcreate submit packaging\winget\manifests\z\ZipBuddy\ZipBuddy\1.1.0
```

---

## 4. Checklist de Validação

- [x] `.gitignore` bloqueia `*.exe` na raiz e diretório `dist/`
- [x] `git status` não lista nenhum arquivo acima de 100 MB
- [x] Tamanho total do código versionado é inferior a 15 MB
- [x] `dist/release/` contém o instalador, o pacote portátil, hashes e `manifest.json` (5 assets)
- [x] Manifestos do Winget v1.1.0 contêm o hash exato do instalador (`B4FB84F9…`)
- [x] Todos os **246** testes automatizados passam com 100% de sucesso
- [x] Release `v1.1.0` publicada com os 5 assets e as notas de release
