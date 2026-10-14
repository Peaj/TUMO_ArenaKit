#!/bin/bash
# Arena-Baukasten aktualisieren und reparieren (macOS: Doppelklick).
# Die eigentliche Arbeit macht Tools/kit-update.sh. Es funktioniert mit einer git-Kopie
# und mit einem entpackten ZIP-Download. Die Level der Teens bleiben erhalten.

DIR="$(cd "$(dirname "$0")" && pwd)"
SCRIPT="$DIR/Tools/kit-update.sh"

if [ ! -f "$SCRIPT" ]; then
  echo "Tools/kit-update.sh fehlt. Im Terminal stattdessen ausführen:"
  echo "  curl -fsSL https://www.peaj.de/TUMO_ArenaKit/Tools/kit-update.sh | bash"
  read -r -p "Enter drücken zum Schließen ..."
  exit 1
fi

# Run a copy: the update replaces Tools/kit-update.sh itself.
COPY="$(mktemp)"
cp "$SCRIPT" "$COPY"
bash "$COPY" "$DIR"
rm -f "$COPY"
