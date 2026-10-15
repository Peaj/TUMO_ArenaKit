#!/bin/bash
# Arena-Baukasten aktualisieren und reparieren (macOS).
#
# Holt die neueste Version des Kits von GitHub und ersetzt damit alle Kit-Dateien:
# gelöschte oder kaputte Prefabs, Scripts und Einstellungen sind danach wieder da.
# Die Level der Teens (Assets/Levels/<Name>) bleiben erhalten.
# Funktioniert mit einer git-Kopie und mit einem entpackten ZIP-Download.
#
# Start: Doppelklick auf "Kit aktualisieren.command" im Projektordner, oder im Terminal:
#   curl -fsSL https://www.peaj.de/TUMO_ArenaKit/Tools/kit-update.sh | bash

REPO="Peaj/TUMO_ArenaKit"
BRANCH="main"

# Read answers from the keyboard, also when this script comes in through "curl | bash".
ask() { read -r -p "$1" REPLY < /dev/tty; }
pause() { echo; ask "Enter drücken zum Schließen ..."; }
fail() { echo; echo "FEHLER: $1"; pause; exit 1; }
is_project() { [ -f "$1/ProjectSettings/ProjectVersion.txt" ] && [ -d "$1/Assets/Kit" ]; }

echo "=== Arena-Baukasten aktualisieren ==="

# --- Which project? Argument, or search on the Desktop, in Documents and in the home folder. ---
PROJECT="$1"
if [ -z "$PROJECT" ]; then
  FOUND=()
  for dir in "$HOME"/Desktop/* "$HOME"/Documents/* "$HOME"/*; do
    is_project "$dir" && FOUND+=("$dir")
  done
  if [ ${#FOUND[@]} -eq 0 ]; then
    fail "Kein Arena-Baukasten-Projekt gefunden (gesucht auf dem Schreibtisch, in Dokumente und im Benutzerordner)."
  elif [ ${#FOUND[@]} -eq 1 ]; then
    PROJECT="${FOUND[0]}"
  else
    echo "Mehrere Projekte gefunden:"
    for i in "${!FOUND[@]}"; do echo "  $((i + 1))) ${FOUND[$i]}"; done
    ask "Welches aktualisieren? Nummer eingeben: "
    PROJECT="${FOUND[$((REPLY - 1))]}"
  fi
fi
PROJECT="$(cd "$PROJECT" 2>/dev/null && pwd)"
is_project "$PROJECT" || fail "$1 ist kein Arena-Baukasten-Projekt."
echo "Projekt: $PROJECT"
echo

while pgrep -xq Unity; do
  ask "Unity läuft noch. Bitte in Unity speichern (Cmd+S), Unity beenden und dann Enter drücken."
done

# --- Kit files a Teen changed instead of making a variant (compared with the kit checksums). ---
# They are put back after the update, so the changes are not lost: "Level exportieren" then copies
# them into the Teen's own folder. Scripts are always replaced, an old script could break the kit.
DOWNLOAD=""  # set in the ZIP part; never use $TMP, it can be a system variable
KEEP="$(mktemp -d)"
trap 'rm -rf "$KEEP"; [ -n "$DOWNLOAD" ] && rm -rf "$DOWNLOAD"' EXIT
if command -v md5 >/dev/null 2>&1; then
  checksum() { LC_ALL=C tr -d '\r' < "$1" | md5 -q | tr a-f A-F; }
else
  checksum() { LC_ALL=C tr -d '\r' < "$1" | md5sum | cut -d' ' -f1 | tr a-f A-F; }
fi
KEPT=()
if [ -f "$PROJECT/ProjectSettings/ArenaKitManifest.txt" ]; then
  echo "Suche veränderte Kit-Dateien ..."
  while IFS=$'\t' read -r path hash; do
    case "$path" in *.cs|*.asmdef|*.asmref|*.dll|*.shader) continue ;; esac
    [ -f "$PROJECT/$path" ] || continue
    [ "$(checksum "$PROJECT/$path")" = "${hash%$'\r'}" ] && continue
    KEPT+=("$path")
  done < "$PROJECT/ProjectSettings/ArenaKitManifest.txt"
fi
if [ ${#KEPT[@]} -gt 0 ]; then
  echo
  echo "Diese Kit-Dateien wurden verändert:"
  printf '  %s
' "${KEPT[@]}"
  ask "Änderungen behalten (Enter) oder auf das Original zurücksetzen (z + Enter)? "
  case "$REPLY" in z|Z) KEPT=() ;; esac
fi
for path in "${KEPT[@]}"; do
  mkdir -p "$KEEP/$(dirname "$path")"
  cp -p "$PROJECT/$path" "$KEEP/$path"
done
restore_kept() {
  [ ${#KEPT[@]} -gt 0 ] || return 0
  cp -Rp "$KEEP/." "$PROJECT/"
  echo "${#KEPT[@]} veränderte Kit-Datei(en) behalten. Beim Exportieren kommen Kopien davon in den eigenen Level-Ordner."
}

# --- git copy: fetch and reset (untracked student folders stay). ---
if [ -d "$PROJECT/.git" ] && command -v git >/dev/null 2>&1; then
  cd "$PROJECT" || fail "Ordner nicht erreichbar."
  echo "Lade die neueste Version von GitHub (git) ..."
  git fetch origin "$BRANCH" || fail "Keine Verbindung zu GitHub. Internet prüfen und nochmal versuchen."
  # Keep the build list: it points to the Teen's own level.
  BUILD_LIST="$(mktemp)"
  cp -p ProjectSettings/EditorBuildSettings.asset "$BUILD_LIST" 2>/dev/null
  git reset --hard "origin/$BRANCH" || fail "Zurücksetzen hat nicht geklappt."
  [ -s "$BUILD_LIST" ] && cp -p "$BUILD_LIST" ProjectSettings/EditorBuildSettings.asset
  rm -f "$BUILD_LIST"
  restore_kept
  echo
  echo "Fertig! Version: $(git log -1 --format='%h  %s  (%cd)' --date=short)"
  echo "Die Level der Teens in Assets/Levels sind unverändert."
  pause
  exit 0
fi

# --- ZIP copy: download the newest ZIP and replace the kit parts with it. ---
DOWNLOAD="$(mktemp -d)"
echo "Lade die neueste Version von GitHub ..."
curl -fL --progress-bar "https://github.com/$REPO/archive/refs/heads/$BRANCH.zip" -o "$DOWNLOAD/kit.zip" \
  || fail "Keine Verbindung zu GitHub. Internet prüfen und nochmal versuchen."
unzip -q "$DOWNLOAD/kit.zip" -d "$DOWNLOAD/src" || fail "Der Download ist kaputt. Nochmal versuchen."
SRC="$(find "$DOWNLOAD/src" -mindepth 1 -maxdepth 1 -type d | head -n 1)"
[ -d "$SRC/Assets/Kit" ] || fail "Im Download fehlt der Ordner Assets/Kit."

# Makes the destination folder exactly like the source folder.
mirror() {
  mkdir -p "$2"
  if command -v rsync >/dev/null 2>&1; then
    rsync -a --delete "$1/" "$2/" || fail "Kopieren nach $2 hat nicht geklappt."
  else
    rm -rf "$2" && cp -R "$1" "$2" || fail "Kopieren nach $2 hat nicht geklappt."
  fi
}

# Files the Teens made inside Assets/Kit (e.g. a weapon variant that was never moved) would be deleted
# by the mirror. Move them into the Teen's own folder first. Together with their .meta file Unity keeps
# every reference to them.
STUDENTS=()
for dir in "$PROJECT"/Assets/Levels/*/; do
  name="$(basename "$dir")"
  case "$name" in Demo_*|Gerettet) ;; *) STUDENTS+=("$name") ;; esac
done
if [ ${#STUDENTS[@]} -eq 1 ]; then
  RESCUE="$PROJECT/Assets/Levels/${STUDENTS[0]}/Gerettet"
else
  RESCUE="$PROJECT/Assets/Levels/Gerettet"
fi
RESCUED=0
while IFS= read -r -d '' file; do
  relative="${file#"$PROJECT/Assets/Kit/"}"
  case "$relative" in *.meta) continue ;; esac
  [ -e "$SRC/Assets/Kit/$relative" ] && continue
  mkdir -p "$RESCUE/$(dirname "$relative")"
  mv "$file" "$RESCUE/$relative"
  [ -f "$file.meta" ] && mv "$file.meta" "$RESCUE/$relative.meta"
  echo "  gerettet: Assets/Kit/$relative"
  RESCUED=$((RESCUED + 1))
done < <(find "$PROJECT/Assets/Kit" -type f -print0)
if [ $RESCUED -gt 0 ]; then
  echo "$RESCUED eigene Datei(en) aus Assets/Kit verschoben nach ${RESCUE#"$PROJECT/"}"
  echo
fi

echo "Ersetze die Kit-Dateien ..."
mirror "$SRC/Assets/Kit" "$PROJECT/Assets/Kit"
cp -p "$SRC/Assets/Kit.meta" "$PROJECT/Assets/"
cp -p "$SRC/Assets/Levels.meta" "$PROJECT/Assets/" 2>/dev/null
for demo in "$SRC"/Assets/Levels/Demo_*; do
  [ -d "$demo" ] || continue
  name="$(basename "$demo")"
  mirror "$demo" "$PROJECT/Assets/Levels/$name"
  cp -p "$SRC/Assets/Levels/$name.meta" "$PROJECT/Assets/Levels/" 2>/dev/null
done
mirror "$SRC/Packages" "$PROJECT/Packages"
# Project settings, but keep the build list (it points to the Teen's own level).
for file in "$SRC"/ProjectSettings/*; do
  [ "$(basename "$file")" = "EditorBuildSettings.asset" ] && continue
  cp -p "$file" "$PROJECT/ProjectSettings/"
done
for folder in Docs Tools; do
  [ -d "$SRC/$folder" ] && mirror "$SRC/$folder" "$PROJECT/$folder"
done
for file in "$SRC"/* "$SRC"/.gitattributes "$SRC"/.gitignore "$SRC"/.nojekyll; do
  [ -f "$file" ] && cp -p "$file" "$PROJECT/"
done
chmod +x "$PROJECT"/*.command 2>/dev/null
restore_kept

echo
echo "Fertig! Das Kit ist auf dem neuesten Stand."
echo "Die Level der Teens in Assets/Levels sind unverändert."
echo "Beim nächsten Öffnen importiert Unity die neuen Dateien, das dauert kurz."
pause
