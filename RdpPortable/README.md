# RDP Portable

Applicazione ex-novo ispirata a Terminals, dedicata esclusivamente al protocollo **RDP**, di tipo **portable** (nessuna installazione, dati accanto all'eseguibile).

Costruita con **WPF (.NET 8)** e il controllo client **MSTSC ActiveX** (`mstscax.dll`) embedded nella finestra — lo stesso motore usato dal vecchio Terminals, con supporto completo a: RD Gateway (TS Gateway), redirect (clipboard, dischi, stampanti, porte, smart card, dispositivi), NLA/CredSSP, color depth, dimensioni desktop, performance flags, programmi di avvio.

## Struttura

```
RdpPortable/
├── RdpPortable.sln
├── RdpPortable.Core/            # netstandard2.0 - logica pura, testabile ovunque
│   ├── Models/                  # RdpConnection, TsGatewaySettings, enum
│   ├── Storage/JsonConnectionStore.cs   # persistenza JSON (portable)
│   ├── Import/TerminalsXmlImporter.cs   # import export XML del vecchio Terminals
│   └── Export/RdpFileExporter.cs        # generazione file .rdp (mstsc.exe)
├── RdpPortable.App/             # net8.0-windows - UI WPF
│   ├── MainWindow.xaml(.cs)     # lista connessioni + host RDP embedded
│   ├── ConnectionEditorWindow   # editor completo connessione
│   ├── RdpClientHost.cs         # wrapper MSTSC ActiveX (gateway, redirect, NLA)
│   └── ViewModels/
└── RdpPortable.Tests/           # xunit (importer, store, exporter)
```

## Portable

- Nessuna scrittura su registry o AppData: i dati risiedono in `Data\connections.json` accanto all'eseguibile (chiavetta USB-friendly).
- Publish single-file:

```powershell
dotnet publish RdpPortable.App -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

> Nota: `AxInterop.MSTSCLib.dll` e `MSTSCLib.dll` restano file separati accanto all'eseguibile (interop COM richiesti da `mstscax.dll`, presente in ogni Windows).

## Import dal vecchio Terminals

Bottone **Importa** nella finestra principale → selezionare il file XML esportato da Terminals (`ExportImport.ExportXML`):

- i preferiti con `<protocol>RDP</protocol>` vengono importati con tutte le opzioni mappate (server, porta, credenziali, tag, note, desktop size, colori, redirect, suoni, TS Gateway, NLA, performance flags);
- i preferiti con altri protocolli (SSH, VNC, HTTP, ICA, VMRC...) vengono ignorati e riportati nel riepilogo.

## Test

```bash
dotnet test RdpPortable.Tests
```

I test coprono importer XML (incluso skip di protocolli non-RDP), round-trip dello store JSON e generazione file `.rdp`.
