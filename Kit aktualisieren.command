#!/bin/bash
# Arena-Baukasten aktualisieren (macOS: Doppelklick).
# Holt die neueste Version des Kits von GitHub und setzt alle Kit-Dateien darauf zurück.
# Die Level der Teens (Assets/Levels/<Name>, von git nicht verfolgt) bleiben erhalten.

BRANCH="main"
cd "$(dirname "$0")" || exit 1

pause() {
  echo
  read -r -p "Enter drücken zum Schließen ..."
}

echo "=== Arena-Baukasten aktualisieren ==="
echo "Ordner: $(pwd)"
echo

if ! command -v git >/dev/null 2>&1 || ! git rev-parse --git-dir >/dev/null 2>&1; then
  echo "FEHLER: git fehlt oder dieser Ordner ist keine git-Kopie des Projekts."
  pause
  exit 1
fi

while pgrep -xq Unity; do
  echo "Unity läuft noch. Bitte in Unity speichern (Cmd+S), Unity beenden und dann Enter drücken."
  read -r
done

echo "Lade die neueste Version von GitHub ..."
if ! git fetch origin "$BRANCH"; then
  echo
  echo "FEHLER: Keine Verbindung zu GitHub. Internet prüfen und nochmal versuchen."
  pause
  exit 1
fi

CHANGED=$(git status --short --untracked-files=no)
if [ -n "$CHANGED" ]; then
  echo
  echo "Diese Kit-Dateien wurden auf diesem Mac verändert und werden zurückgesetzt:"
  echo "$CHANGED"
fi

git reset --hard "origin/$BRANCH"

echo
echo "Fertig! Version: $(git log -1 --format='%h  %s  (%cd)' --date=short)"
echo "Die Level der Teens in Assets/Levels sind unverändert."
pause
