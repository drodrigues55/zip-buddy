# Zip Buddy v1.1.0 — Integrity, Checksums & E-mail Workflows 🚀

A versão **v1.1.0** adiciona ferramentas de verificação e novos fluxos de compactação ao Zip Buddy, além de um menu de contexto do Explorer reformulado, totalmente localizado em **pt-BR** e **en-US**.

---

## ✨ O que há de novo

### 🔎 Teste de integridade de arquivos compactados
- Novo verbo `Testar arquivo compactado` / `Test archive` (`--test-archive`) na janela de progresso dedicada.
- Cada entrada é lida em streaming e o **CRC32 calculado é comparado com o CRC armazenado** — arquivos corrompidos são relatados com a lista de entradas afetadas em "Ver mais detalhes".
- Velocidade em tempo real e ETA também são exibidos durante o teste.
- Arquivos criptografados solicitam senha; número máximo de **3 tentativas** antes de abortar com mensagem localizada.

### 🔢 CRC SHA
- Nova janela (`--crc-sha`, menu `CRC SHA`) com **CRC-32**, **CRC-64/XZ**, **SHA-1** e **SHA-256**.
- Botões de copiar individualmente e "Copiar Tudo"; cancelamento e progresso responsivos mesmo em arquivos de vários GB.
- > ⚠️ O CRC-64 foi corrigido nesta versão: versões anteriores usavam o polinômio em forma normal e produziam valores não padrão. O vetor de `"123456789"` agora é `995DC9BBDF1939FA` (CRC-64/XZ).

### 📧 Compactar e enviar por e-mail
- Diálogo `Adicionar ao arquivo compactado...` (`--compress-dialog`) com destino, formato (ZIP, 7Z, TAR, .tar.gz), nível de compressão e senha opcional.
- Validação de destino (vazio, inválido, igual à origem), confirmação de sobrescrita e bloqueio de senha/nível para formatos que não os suportam.
- Verbos `--compress-email-zip`, `--compress-email-7z` e `--compress-email-dialog`: o arquivo é criado e o cliente de e-mail padrão é aberto com o anexo via MAPI. O diálogo modal roda em thread STA dedicada, **sem congelar a janela de progresso**. Sem cliente MAPI, abre `mailto:` e seleciona o arquivo no Explorer.

### 🗂️ Menu de contexto do Explorer reformulado
- Até **12 comandos** em cascata para arquivos compactados e **7** para arquivos/pastas, com rótulos no idioma do sistema e ícone nativo.
- Todos os toggles de configuração são respeitados nos modos cascata e flat; chaves obsoletas do Registro são removidas ao desativar comandos ou trocar de idioma.

### 🛡️ Correções de qualidade (pré-produção)
- 246 testes automatizados, 100% aprovados.
- Contrato estruturado `OperationResult.RequiresPassword` substitui a detecção de senha por substring de erro.
- Validação de integridade usa a mesma implementação de CRC32 do calculador de hashes.
- Desinstalação agora remove também as chaves do menu de contexto escritas pelo app no HKCU (`--unregister-context-menu` via `[UninstallRun]`). Em instalações per-machine, usuários diferentes precisam executar `ZipBuddy.UI.exe --unregister-context-menu` uma vez em sua própria sessão para limpar seu HKCU.
- **Página final do instalador:** opção em destaque `★ Adicionar Zip Buddy ao menu de contexto do Windows (recomendado)`, marcada por padrão na primeira instalação, e `Abrir Configurações de aplicativos padrão` (o Windows não permite definir associações padrão por código). Instalações silenciosas/winget registram o menu automaticamente. O padrão do app agora é o **menu em cascata com 12 comandos**.
- **Identidade unificada:** os aliases legados `MyUnzip.exe` / `MyUnzipUpdater.exe` foram removidos de todo o produto — executáveis, updater, instalador, manifestos e documentação agora usam exclusivamente **Zip Buddy / ZipBuddy** e o publicador **Zip Buddy Open Source Team**. Como efeito colateral, o instalador caiu de ~165 MB para **~84 MB**.

---

## 📥 Como instalar

### Instalador oficial (recomendado)
Baixe **`ZipBuddySetup-x64-v1.1.0.exe`** e execute:
```powershell
ZipBuddySetup-x64-v1.1.0.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-
```

### Portátil
Extraia `ZipBuddy-v1.1.0-windows-x64-portable.zip` e execute `ZipBuddy.UI.exe`.

### Winget
```powershell
winget install ZipBuddy.ZipBuddy --version 1.1.0
```

---

## 🛡️ Tabela Oficial de Hashes (SHA-256)

Gerada por `packaging/prepare_github_release.ps1 -Version 1.1.0` em 16/11/2026.

| Arquivo | Tamanho | Checksum SHA-256 |
| :--- | :--- | :--- |
| `ZipBuddySetup-x64-v1.1.0.exe` | 83,87 MB | `B4FB84F91CB2B5E3E881788C76088F5E392DA9AC55ABC22CF0C467EF3BDF9300` |
| `ZipBuddy-v1.1.0-windows-x64-portable.zip` | 90,08 MB | `461494F65F902CB47F5B095BFDE4E57842051CB49279A541C7AA698F60951D9E` |
| `manifest.json` (assinado RSA-SHA256) | < 1 KB | `28A39C10AE8813F182F09E8498909538A0A5E0AF3CBE085F04D53D03EE82CE5E` |
