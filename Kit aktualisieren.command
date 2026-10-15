#!/bin/bash
# Arena-Baukasten aktualisieren und reparieren (macOS: Doppelklick).
# Die eigentliche Arbeit macht Tools/kit-update.sh. Es funktioniert mit einer git-Kopie
# und mit einem entpackten ZIP-Download. Die Level der Teens bleiben erhalten.

DIR="$(cd "$(dirname "$0")" && pwd)"
SCRIPT="$DIR/Tools/kit-update.sh"
URL="https://www.peaj.de/TUMO_ArenaKit/Tools/kit-update.sh"

# Run a copy of the newest script (the update replaces Tools/kit-update.sh itself).
# Without internet the local one is used.
COPY="$(mktemp)"
if ! curl -fsSL "$URL" -o "$COPY" 2>/dev/null || [ ! -s "$COPY" ]; then
  if [ ! -f "$SCRIPT" ]; then
    echo "Tools/kit-update.sh fehlt und GitHub ist nicht erreichbar. Internet prüfen und im Terminal ausführen:"
    echo "  curl -fsSL $URL | bash"
    read -r -p "Enter drücken zum Schließen ..."
    exit 1
  fi
  cp "$SCRIPT" "$COPY"
fi
bash "$COPY" "$DIR"
rm -f "$COPY"
