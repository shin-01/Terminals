# RDP on .NET 8 - Proof of Concept

Valida il **go/no-go** per la migrazione di Terminals a .NET 8:
l'hosting dell'ActiveX RDP (`mstscax.dll`) non è supportato da
WinForms moderno; questo PoC verifica la sostituzione con
l'interop COM di **MsRdpEx** (Devolutions), basata sul campione
ufficiale `MsRdpEx_App`.

## Stack

- `net8.0-windows`, WinForms
- `Devolutions.MsRdpEx` 2026.10.6 (NuGet, zero dipendenze, target
  net8.0-windows **e** net48 — la stessa libreria potrà essere usata
  anche prima della migrazione, su net48)
- `AxMSTSCLib.AxMsRdpClient9NotSafeForScripting`: il pacchetto
  include una AxInterop.MSTSCLib **legacy compilata per
  net8.0-windows** (via build targets, `MsRdpExComInterop=Legacy`),
  quindi il controllo AxHost classico funziona anche su .NET 8 —
  lo stesso modello di `Terminals.Plugins.Rdp`, senza riscritture

## Cosa validare (checklist)

- [ ] `dotnet build -c Release` (compila con .NET 8 SDK)
- [ ] Avvio su Windows con mstscax.dll
- [ ] Connect a un server RDP di test
- [ ] Rendering e input (mouse/tastiera)
- [ ] Eventi Connected/Disconnected
- [ ] Chiusura pulita (nessun leak di thread mstscax dopo N sessioni)
- [ ] Desktop size / fullscreen
- [ ] Credenziali (utente/password), CredSSp/NLA
- [ ] Gateway TS (richiede hook MsRdpEx: `AxHookEnabled`)

## Esecuzione

```
cd PoC/RdpNet8
dotnet run -- <server>
# oppure con credenziali da ambiente:
RDP_USERNAME=user RDP_PASSWORD=pass dotnet run -- server
```

## Impatto sulla migrazione

Se il PoC passa, `Terminals.Plugins.Rdp` può migrare sostituendo:
- `AxMsRdpClient6NotSafeForScripting` (AxHost, interop 2010)
  con `RdpView`/`AxMsRdpClient9NotSafeForScripting` (MsRdpEx)
- i riferimenti `AxInterop.MSTSCLib`/`MSTSCLib` vendored con il
  pacchetto `Devolutions.MsRdpEx`
e il resto della pipeline di configurazione (~30 proprietà) resta
concettualmente identico (le interfacce IMsRdp* sono le stesse).
