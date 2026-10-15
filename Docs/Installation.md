# Arena-Baukasten: Installation auf den Macs

Anleitung für die IT. Ziel: Auf jedem Mac liegt das Unity-Projekt **TUMO_ArenaKit** und ist im Unity Hub eingetragen. Zeitbedarf: ca. 5 Minuten pro Mac, mit dem optionalen Test (Schritt 4) ca. 10 Minuten. Mehrere Macs können parallel laufen.

- Projekt: <https://github.com/Peaj/TUMO_ArenaKit> (öffentlich, kein GitHub-Konto nötig)
- Unity: **6000.3.25f1** (Unity 6.3 LTS)
- Größe: ca. 15 MB, nach dem ersten Öffnen ca. 1–2 GB (Unity-Cache im Ordner `Library`)

## 1. Voraussetzungen prüfen

Auf jedem Mac, im Benutzerkonto, mit dem die Teens arbeiten:

1. **Unity Hub** ist installiert und angemeldet (Lizenz aktiv).
2. **Unity 6000.3.25f1** ist im Hub unter *Installs* vorhanden. Falls nicht: *Install Editor* > *Archive* > Version 6000.3.25f1. Zusätzliche Module (Build Support) sind **nicht** nötig.
3. **git** ist vorhanden. Im Terminal (Programme > Dienstprogramme > Terminal):
   ```bash
   git --version
   ```
   Kommt eine Versionsnummer, passt alles. Erscheint stattdessen ein Fenster „Befehlszeilenentwickler-Tools installieren“: auf **Installieren** klicken (dauert ein paar Minuten, braucht evtl. Admin-Rechte). Alternativ GitHub Desktop installieren, das bringt git mit.

## 2. Projekt herunterladen

Im Terminal diesen einen Befehl ausführen:

```bash
git clone https://github.com/Peaj/TUMO_ArenaKit.git ~/Documents/TUMO_ArenaKit
```

Das Projekt liegt dann unter **Dokumente > TUMO_ArenaKit**. Ein anderer Ort geht auch, dann den Pfad am Ende des Befehls anpassen.

## 3. In Unity Hub eintragen

1. Unity Hub > **Projects** > **Add** > **Add project from disk** > den Ordner `Dokumente/TUMO_ArenaKit` wählen.
2. In der Liste muss bei *Editor Version* **6000.3.25f1** stehen.

Damit ist der Mac fertig.

## 4. Einmal öffnen und testen (optional, empfohlen)

Beim allerersten Öffnen importiert Unity alle Dateien, das dauert einige Minuten. Wird das hier schon erledigt, startet das Projekt am ersten Workshoptag sofort, und Probleme fallen vorher auf. Ohne diesen Schritt passiert der Import eben am ersten Tag, während die Teens schon anfangen.

1. Im Unity Hub auf das Projekt klicken und warten, bis Unity offen ist. Ein Hinweis auf ein „experimentelles Paket“ (`com.unity.pipeline`) ist in Ordnung.
2. Kurzer Test:
   - Oben in der Menüleiste gibt es das Menü **TUMO**.
   - Im Fenster *Project*: `Assets/Levels/Demo_Arena/Demo_Arena.unity` doppelklicken, dann oben auf **Play** (▶).
   - Beide Spielfiguren bewegen sich: Spieler 1 mit **A / D / W**, Spieler 2 mit den **Pfeiltasten**. Dann Play wieder ausschalten.
   - Menü **TUMO > Spickzettel öffnen**: Der Spickzettel öffnet sich im Browser.
   - Im Browser oben auf **Feedback-Box**, einen Testeintrag abschicken, die Seite neu laden (Cmd+R): Der Eintrag muss noch da sein. Dann den Eintrag mit 🗑️ löschen.
3. Unity beenden (Cmd+Q).

**Schneller bei vielen Macs:** Ist ein Mac fertig importiert, kann der ganze Ordner `TUMO_ArenaKit` (inklusive `Library`) auf die anderen Macs kopiert werden, z.B. per USB-Stick, AirDrop oder Netzlaufwerk. Dann entfällt dort der lange Import. Danach dort trotzdem Schritt 3 (im Hub eintragen) machen.

## 5. Aktualisieren und reparieren

Im Projektordner liegt **`Kit aktualisieren.command`**:

1. Unity beenden.
2. `Kit aktualisieren.command` doppelklicken. Ein Terminalfenster zeigt, was passiert.

Das Skript holt die neueste Version von GitHub und ersetzt damit die Dateien des Baukastens. Damit lassen sich auch Dateien reparieren, die Teens aus Versehen gelöscht haben (z.B. „Missing Prefab“ im Level). Kit-Dateien, die ein Teen verändert hat (z.B. ein Prefab direkt geändert statt eine Variante gemacht), listet das Skript auf: **Enter** behält die Änderungen (beim Exportieren kommen dann Kopien davon in den Level-Ordner), **z + Enter** stellt die Originale wieder her. Scripts werden immer ersetzt.

Es funktioniert mit einer git-Kopie **und** mit einem entpackten ZIP-Download von GitHub.

**Bleibt erhalten:** die Level der Teens in `Assets/Levels/<Name>`, ihre Level-Liste, der Unity-Cache und die exportierten `.unitypackage`-Dateien auf dem Schreibtisch. Eigene Dateien, die Teens in `Assets/Kit` gespeichert haben (z.B. eine Waffen-Variante), werden vorher in ihren Ordner verschoben: `Assets/Levels/<Name>/Gerettet`.

**ZIP-Installation mit altem Projektstand** (dort kann das alte `Kit aktualisieren.command` noch kein ZIP): einmal im Terminal ausführen, das Skript sucht das Projekt selbst (Schreibtisch, Dokumente, Benutzerordner):

```bash
curl -fsSL https://www.peaj.de/TUMO_ArenaKit/Tools/kit-update.sh | bash
```

Danach klappt auf diesem Mac auch der Doppelklick.

Falls macOS das Skript beim Doppelklick nicht startet:
- „kann nicht geöffnet werden“: Rechtsklick auf die Datei > **Öffnen** > **Öffnen**.
- „keine Berechtigung“: im Terminal `chmod +x ~/Documents/TUMO_ArenaKit/*.command`
- Oder einfach den `curl`-Befehl oben benutzen.

⚠️ **Niemals `git clean` verwenden!** Das würde die Level der Teens löschen, weil sie nicht in git sind.

⚠️ Auf dem **Lehrer-Mac** am letzten Tag nach „Arcade vorbereiten“ nicht mehr aktualisieren, sonst ist die Level-Liste für das Finale wieder zurückgesetzt.

**Im Level selbst kaputt** (Kamera gelöscht, Spiel schwarz, Tilemap verzerrt): in Unity **TUMO > Level reparieren**.

## Probleme?

| Meldung | Lösung |
|---|---|
| `destination path ... already exists` | Das Projekt ist schon da. Zum Auffrischen Schritt 5 benutzen. |
| `Could not resolve host: github.com` | Keine Internetverbindung oder GitHub gesperrt. Ordner von einem anderen Mac kopieren (siehe „Schneller bei vielen Macs“). |
| Hub zeigt eine andere Editor-Version | Unity 6000.3.25f1 installieren (Schritt 1) und im Hub beim Projekt auswählen. |
| Unity startet im **Safe Mode** | Nicht weiterarbeiten, Pascal Bescheid geben. |
| Menü **TUMO** fehlt | Unity ist noch beim Importieren: kurz warten. Sonst Unity neu starten. |

Bei Fragen: Pascal
