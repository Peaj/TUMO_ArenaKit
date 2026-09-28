# Spickzettel: Arena-Baukasten

## Steuerung

| | Laufen | Springen | Schießen |
|---|---|---|---|
| **Spieler 1** (Tastatur links) | A / D | W | linke Shift-Taste ⇧ |
| **Spieler 2** (Tastatur rechts) | ← / → | ↑ | rechte Wahltaste ⌥ (alt) |
| **Gamepad** | Stick | A | X |

- Durch eine Plattform nach unten fallen: **runter + springen**
- Waffen hebst du auf, indem du hineinläufst. Ist die Munition leer, fällt die Waffe runter.
- **H** zeigt die Hilfe, **R** startet das Level neu.
- ⚠️ Beim Spielen **nie die Cmd-Taste ⌘** drücken! Cmd+Q beendet Unity, Cmd+W schließt Fenster.

## Unity in einer Minute

| Fenster | Wofür |
|---|---|
| **Scene** | Hier baust du dein Level |
| **Game** | So sieht es beim Spielen aus |
| **Hierarchy** | Alle Objekte in deinem Level |
| **Inspector** | Einstellungen des ausgewählten Objekts |
| **Project** | Alle Dateien (Prefabs, Bilder, Tiles ...) |

| Taste | Was passiert |
|---|---|
| **Q** | Ansicht verschieben (Hand) |
| **W** | Objekt verschieben |
| **E** | Objekt drehen |
| **R** | Objekt skalieren |
| **F** | Zum ausgewählten Objekt springen |
| **Scrollrad** | Zoomen |
| **⌘ + P** | Play (Spiel starten/stoppen) |
| **⌘ + S** | Speichern, bitte oft! |
| **⌘ + Z** | Rückgängig |
| **⌘ + D** | Objekt verdoppeln |

⚠️ **Alles, was du im Play-Modus änderst, ist danach wieder weg!** Erst Play stoppen, dann ändern.

## 1. Dein Level anlegen

1. Menü **TUMO > Neues Level erstellen...**
2. Deinen Vornamen und einen Namen für dein Level eintragen und auf **Level erstellen** klicken.
3. Jetzt hast du einen eigenen Ordner **Assets/Levels/DeinName**. **Arbeite nur in diesem Ordner!**
4. Später öffnest du dein Level wieder mit **TUMO > Mein Level öffnen**.

## 2. Malen mit der Tile Palette

1. **Window > 2D > Tile Palette** öffnen.
2. Oben eine Palette wählen: **1 Ground**, **2 Platforms**, **3 Hazards** oder **4 Decoration**.
3. **Wichtig:** Bei **Active Tilemap** die passende Ebene auswählen:

| Ebene | Was ist das? |
|---|---|
| **Ground** | Fester Boden und Wände |
| **Platforms** | Von unten kann man durchspringen |
| **Hazards** | Tödlich: Lava und Stacheln |
| **Back Decoration** | Deko hinter den Spielern |
| **Front Decoration** | Deko vor den Spielern |

4. Pinsel **B** malt, Radierer **D** löscht (oder Shift + Klick), **U** füllt ein Rechteck.

Tipp: Die Boden-Tiles setzen Ränder und Ecken automatisch. Du malst einfach drauflos.

## 3. Bausteine einbauen

Ziehe Prefabs aus **Assets/Kit/Prefabs** in deine Szene. Die Einstellungen änderst du im **Inspector**.

| Baustein | Was er macht |
|---|---|
| **SpawnPoint** | Startpunkt für Spieler 1-4. Bei *Start Weapon* bekommt der Spieler gleich eine Waffe |
| **Pickups/...** | Waffe zum Aufheben (*Pickup_Random* = Zufallswaffe) |
| **WeaponCrate** | Kiste mit Waffe drin, zum Aufschießen |
| **Crate / BrickWall** | Kaputtbare Kiste bzw. Mauer |
| **JumpPad** | Sprungfeder. Dreh sie, dann wird sie zum Seiten-Booster |
| **MovingPlatform / Elevator** | Bewegte Plattform. Die gelbe Linie zeigt, wohin sie fährt (*Offset*) |
| **TeleporterPair** | Zwei Portale, die verbunden sind |
| **Mine / TNTBlock** | Explodiert bei Berührung bzw. wenn man draufschießt |
| **Spikes / Saw** | Tödlich |
| **RisingLava** | Nach 30 Sekunden steigt die Lava! |
| **Door / LockBlock** | Versperrt den Weg, bis ein Event sie öffnet |
| **Lever / Button** | Schalter: an- und ausschalten durch Berühren oder Schießen |
| **Key** | Schlüssel zum Einsammeln |
| **TriggerZone** | Unsichtbare Zone. Löst etwas aus, wenn jemand hineinläuft |
| **Timer / Counter** | "Nach X Sekunden ..." und "Nach N Mal ..." |
| **Spawner** | Erzeugt immer wieder etwas, z.B. Bomben oder Bälle |
| **FinishFlag** | Wer zuerst hier ist, gewinnt |
| **ScoreZone** | König des Hügels: Wer allein drin steht, bekommt Punkte |
| **Coin / Gem** | Punkte zum Einsammeln |
| **Ball + Goal** | Ballspiele: Ball im Tor gibt einen Punkt |
| **NameSign** | Schild mit deinem Namen |

## Die Kamera

Die Kamera zeigt immer alle Spieler und zoomt automatisch rein und raus. **Dein Level darf größer sein als der Bildschirm**: Male einfach weiter, die Kamera erkennt die Größe an den Tiles auf *Ground* und *Platforms*.

Einstellungen findest du im Objekt **CameraRig**:

| Einstellung | Bedeutung |
|---|---|
| **Min Size** | So nah zoomt die Kamera höchstens heran |
| **Max Size** | So weit zoomt sie höchstens heraus |
| **Framing** | Wie viel Platz um die Spieler bleibt |
| **Shake Strength** | Wie stark Explosionen wackeln (0 = gar nicht) |

## 4. Die Spielregeln (Level Rules)

Klicke in der Hierarchy auf **Level Rules**:

| Einstellung | Bedeutung |
|---|---|
| **Level Name / Built By** | Wird am Anfang groß angezeigt |
| **Last Player Standing Wins** | Wer als Letzter lebt, gewinnt die Runde |
| **Points To Win** | So viele Punkte braucht man zum Sieg |
| **Kills Give Points** | Jeder Treffer, der tötet, gibt einen Punkt |
| **Time Limit** | Sekunden bis zum Rundenende (0 = kein Limit) |
| **Player Prefab** | Dein eigener Spieler (liegt schon in deinem Ordner) |
| **Player Health** | Leben aller Spieler (0 = normal, 1000 = fast unverwundbar) |
| **Respawn Players** | Tote Spieler kommen zurück |
| **Gravity** | 1 = normal, 0.3 = Mond, 2 = schwer |

## 5. Dinge verbinden mit Events

Viele Bausteine haben unten im Inspector **Events** (z.B. *On Turned On*). So verbindest du sie:

1. Beim Event auf **+** klicken.
2. Das Objekt, das reagieren soll, aus der Hierarchy in das leere Feld ziehen.
3. Rechts die Funktion auswählen, z.B. **Door > Open ()**.

Die Zahl bei Events ist die **Spielernummer**. Wählst du eine Funktion unter **Dynamic int** (z.B. *LevelRules > AddPoint*), bekommt genau der Spieler den Punkt, der den Baustein ausgelöst hat.

## 6. Rezepte für eigene Spielideen

**🚪 Tür mit Hebel**
- Door und Lever einbauen.
- Lever > *On Turned On* → Door > **Open**
- Lever > *On Turned Off* → Door > **Close**

**🔑 Schlüssel-Tür**
- Key und LockBlock einbauen.
- Key > *On Collected* → LockBlock > **Open**

**🌋 Sudden Death**
- RisingLava ins Level ziehen. Beim *Timer* stellst du ein, nach wie vielen Sekunden die Lava steigt.

**🏁 Parkour-Rennen**
- Level Rules: **Last Player Standing Wins** aus, **Respawn Players** an.
- Eine **FinishFlag** ans Ziel stellen, dazu Sägen, Stacheln und bewegte Plattformen.

**👑 König des Hügels**
- Level Rules: **Points To Win** = 10, **Respawn Players** an.
- Eine **ScoreZone** auf einen gefährlichen Platz stellen.

**🪙 Münzjagd**
- Level Rules: **Points To Win** = 10, **Respawn Players** an, **Last Player Standing Wins** aus.
- Viele **Coins** verteilen, dazu Waffen, damit man sich gegenseitig stören kann.

**🏐 Raketen-Volleyball**
- Schau dir das Level *Demo_RocketVolleyball* an.
- **Ball** und je ein **Goal** auf den Boden jeder Hälfte legen.
- Beim *Goal* ist **Point For Player** der Spieler auf der anderen Seite.
- Level Rules: **Points To Win** = 5, **Player Health** = 1000.

**⚽ Fußball**
- Wie Volleyball, aber das *Goal* steht an der Wand.
- **Point For Player** = 0, dann bekommt der Spieler den Punkt, der den Ball zuletzt berührt hat.

**🔫 Eigene Waffe**
1. Rechtsklick auf eine Waffe in *Assets/Kit/Prefabs/Weapons* > **Create > Prefab Variant**.
2. Die Variante in **deinen Ordner** schieben und umbenennen.
3. Werte ändern: *Projectiles Per Shot*, *Spread*, *Fire Delay*, *Recoil*, *Ammo* (-1 = unendlich) ...
4. Neues Aussehen: Zieh ein Bild aus **Assets/Kit/Art/Weapons** (z.B. *Blaster*, *WaterGun*, *HandCannon*) in das Feld *Sprite* des *Sprite Renderer*. Dann schiebst du das Kind-Objekt **Muzzle** an die Mündung, denn dort kommen die Schüsse raus. Bilder für passende Geschosse (Laser, Wassertropfen, Kanonenkugel) liegen in *Weapons/Projectiles*. Setz eines in eine Prefab-Variante von *Bullet* ein und trag sie bei *Projectile* ein.
5. Zum Benutzen: ein Pickup (z.B. *Pickup_Bazooka*) ins Level ziehen und bei *Weapon* deine Waffe eintragen, oder deine Waffe beim *SpawnPoint* als *Start Weapon* eintragen.

**🏃 Eigener Spieler**
- Öffne *DeinNamePlayer* in deinem Ordner und ändere:

| Einstellung | Bedeutung |
|---|---|
| **Max Speed** | Wie schnell der Spieler läuft |
| **Braking** | Wie schnell er stehen bleibt (0 = rutscht wie auf Eis) |
| **Jump Height** | Sprunghöhe in Tiles, wenn man Springen gedrückt hält |
| **Min Jump Height** | Sprunghöhe, wenn man nur kurz tippt |
| **Fall Gravity** | Wie schnell er nach dem Sprung wieder runterfällt (2 = doppelt so schnell, wie bei Mario) |
| **Extra Jumps** | Sprünge in der Luft (1 = Doppelsprung) |
| **Health** | Wie viel er aushält |

## 7. Abgeben

1. **Built By** in den Level Rules ausfüllen, das Schild zeigt deinen Namen.
2. Speichern (**⌘ + S**).
3. **TUMO > Level exportieren**. Die Datei **DeinName.unitypackage** liegt dann auf dem Schreibtisch.
4. Gib die Datei deiner Lehrerin oder deinem Lehrer. Im Finale spielen alle Level nacheinander! 🎉

## Regeln, damit das Finale klappt

- ✅ Nur in deinem Ordner **Assets/Levels/DeinName** arbeiten.
- ✅ Einstellungen an den Objekten **in deiner Szene** ändern, nicht die Originale in *Assets/Kit*.
- ✅ Eigene Waffen und Spieler als **Prefab-Variante in deinem Ordner** anlegen.
- ✅ Eigene Scripts beginnen mit deinem Namen, z.B. *LenaTrampolin.cs*.
