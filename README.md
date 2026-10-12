# TUMO Arena Kit

A Duck-Game-style local multiplayer arena kit for the TUMO game development holiday workshop.
Students don't program a game from scratch. They get a working 2D arena shooter and build their own level and game mode from building blocks (tiles, prefabs and UnityEvents).
At the end, all levels are merged into one "arcade" project that plays one level after another.

- Unity **6000.3.25f1** (6.3 LTS), URP 2D, new Input System, Cinemachine 3.1
- Based on `Games-Academy/BirdGame_GD1_25` (bean man character, bazooka, force-based movement)
- Tiles and props: [Kenney New Platformer Pack](https://kenney.nl/assets/new-platformer-pack) (CC0)

Workshop material in German: interactive web pages in `Docs/` that ship with the project, so they're on every student Mac. They work offline straight from the file system (fonts are bundled). Students open them with `TUMO > Spickzettel öffnen` / `TUMO > Workshop-Material öffnen`.
- [Docs/index.html](Docs/index.html): start page
- [Docs/Spickzettel.html](Docs/Spickzettel.html): cheat sheet with a building block search. Printing it gives a one-page, nearly black-and-white quick reference to keep next to the keyboard (also as [Markdown](Docs/Spickzettel.md))
- [Docs/Challenges.html](Docs/Challenges.html): challenge card deck (draw, filter by level, mark as done)
- [Docs/Remix.html](Docs/Remix.html): remix dice for a game idea (mode × twist × theme)
- [Docs/Spielidee.html](Docs/Spielidee.html): game idea sheet with a paintable level sketch
- [Docs/Feedback.html](Docs/Feedback.html): feedback box; playtesters leave their feedback on the builder's Mac
- [Docs/Workshop.html](Docs/Workshop.html): for the teacher. 3-day time plan with a live timer (click a block to jump there, projector view, short timer for the playtest rotation), arcade finale, material and preparation

Progress, the game idea and feedback are saved in the browser's local storage of that Mac. That suits the 3-day holiday workshop (same Mac every day). The daily backup of the level itself is the exported `.unitypackage` uploaded to Google Classroom.

## Quick start

1. Open the project with Unity 6000.3.25f1 (the first import takes a few minutes).
2. Open `Assets/Levels/Demo_Arena/Demo_Arena.unity` and press Play.
3. After every round the next level of the build list loads (Demo_Arena → Demo_RocketVolleyball → ...).
4. To try every building block, use `TUMO > Alle Bausteine ansehen`. It opens `Assets/Kit/Scenes/Showcase.unity`, where all blocks stand in one row with labels:
   - Only player 1 plays (`Level Rules > Max Players` = 1) and starts on the left. The camera follows them through the whole row.
   - Respawn is on and nobody wins.
   - The event examples are already wired: lever → door, button → door, timer → door, 3 coins → counter → door, key → lock block, trigger zone → spawner, lever → rising lava.

| | Move | Jump | Fire | Drop through a platform |
|---|---|---|---|---|
| Player 1 (keyboard left) | A / D | W | Left Shift (or F) | S + W |
| Player 2 (keyboard right) | ← / → | ↑ | Right Option/Alt (or Right Shift, Enter) | ↓ + ↑ |
| Gamepads (players 3-4) | Stick / D-pad | A | X, RT or RB | Down + A |

Teacher keys during play: **H** help and key test, **N** next level, **R** restart level, **K** number of keyboard players (2 / 1 / 0), **0** reset scores.

## Workshop workflow

### Students

1. `TUMO > Neues Level erstellen...` asks for a first name and a level name. It creates:
   - `Assets/Levels/<Name>/<Name>.unity`, a copy of the template arena
   - `Assets/Levels/<Name>/<Name>Player.prefab`, a player variant to tune speed, jump, health...
   - The student's name in `Level Rules > Built By`, which is shown when the level starts
   - A build list containing only this scene, so after each round it simply restarts
2. Build:
   - Paint with the Tile Palette (`Window > 2D > Tile Palette`). There are 4 palettes (Ground, Platforms, Hazards, Decoration) for the 5 tilemaps in the scene.
   - Drag in prefabs from `Assets/Kit/Prefabs`.
   - Change values on the objects in the scene.
   - Connect things with the events in the Inspector.
3. `TUMO > Level exportieren` saves `<Name>.unitypackage` to the desktop. Before that it checks:
   - that a name is set
   - whether kit files were changed (those changes would not be exported)
   - whether files outside the student's folder are used
   - whether their own script names start with the student name

Rules for students (they make the merge work):
- Only work inside your own folder `Assets/Levels/<Name>`.
- Don't change the originals in `Assets/Kit`. Change the objects in your scene instead, or make a prefab variant in your own folder.
- Your own scripts start with your name (`LenaTrampolin.cs`), otherwise two students' classes clash.

### Teacher: the arcade finale

1. Collect all `.unitypackage` files in one folder (USB stick or network share).
2. In the teacher project: `TUMO > Lehrer > Level-Pakete importieren...` and choose the folder.
3. `TUMO > Lehrer > Arcade vorbereiten` puts all level scenes into the build list (demos optional) and opens the first one. Press Play.
4. **N** skips a broken level. **K** switches to 0 keyboard players, so 4 gamepads = 4 players.

Try the export and import once with two test levels before the workshop.

## Distributing the project to the Macs

- Copy the project **without** the `Library` folder to every Mac and open it once before day 1 (first import takes a few minutes).
- Alternatively, set up one Mac completely and copy that project folder including `Library` to the other Macs. `Library` can be reused on the same OS and Unity version.
- Every Mac needs Unity 6000.3.25f1 with the same packages. The kit checksum (`ProjectSettings/ArenaKitManifest.txt`) belongs to the project and must be copied too.

## Keyboards and gamepads on the Macs

- **Keyboard ghosting.** Apple Magic Keyboards report about 6 keys at once, and some combinations can ghost.
  - Both fire keys are modifier keys (Shift, Option). Modifiers are sent separately from the 6 normal keys, so two players each holding run + jump + fire is 4 normal keys + 2 modifiers.
  - **Test this on site:** press **H** during play. The help screen lists every key that is pressed at that moment. Let two students hold all their keys at the same time.
  - If a combination fails, use the alternative keys or change the bindings in `Assets/Kit/Input/ArenaControls.inputactions`.
- **Keys to avoid on macOS:**
  - Cmd+W, Cmd+Q and Cmd+P are editor shortcuts. Tell students not to touch Cmd while playing.
  - Ctrl + arrow keys switch desktops (Mission Control). The kit does not use Ctrl on the Mac side.
- **Xbox Wireless Controllers:**
  - The Series X|S controllers and the Xbox One S controller with Bluetooth (model 1708) pair through the macOS Bluetooth settings.
  - Unity detects them automatically (`XboxGamepadMacOSWireless`). Older Xbox One controllers without Bluetooth do not work on a Mac.
  - Every connected gamepad adds a player (max. 4). Keyboard players come first.

## Camera

Every level contains the **CameraRig** prefab (`Assets/Kit/Prefabs/Rules/CameraRig.prefab`). Like in Duck Game, it:
- always keeps all living players on screen and zooms in and out to fit them
- zooms in on the winner at the end of a round
- works on every screen shape (16:9, 16:10, 4:3, 21:9, ...)

How it works:
- **Following:** players are added to a Cinemachine Target Group when they spawn and removed when they die. Balls are followed too.
- **Nobody alive:** the camera shows the whole level.
- **Framing:** Position Composer + Group Framing (zoom only) + Confiner2D, all on one Cinemachine camera.
- **Level bounds:** with *Auto Bounds*, the bounds are recalculated from the painted **Ground** and **Platforms** tiles (plus padding), in the editor as well. Students can build levels of any size and never have to set up the camera.
- **Zoom limit:** the camera zooms out at most until the whole level is visible. If the screen shape doesn't match the level, a bit of background shows above/below or left/right. Players never leave the screen.
- **Background:** it always fills the screen (`CameraBackground`).
- **Shake:** explosions shake the camera (Cinemachine Impulse). *Shake Strength* 0 turns it off.

Settings on the CameraRig (`ArenaCamera`):

| Setting | Meaning |
|---|---|
| Min Size / Max Size | Closest and widest zoom (half the visible height, in tiles) |
| Framing | How much of the screen the players fill |
| Smoothing | Camera damping |
| Auto Bounds / Bounds Padding | Automatic level bounds from the tiles |
| Bounds | Manual bounds box, used when *Auto Bounds* is off |
| Shake Strength | Explosion shake |

## Building blocks

All scripts are in the `ArenaKit` namespace (`Assets/Kit/Scripts`). Everything a student sets up has English field names, tooltips and events. The in-game text is German.

### Triggers and actions

The blocks are compositions of small scripts, so students can recombine them:
- **Triggers** (`Scripts/Triggers`) notice that something happened and fire UnityEvents.
- **Actions** (`Scripts/Actions`) are the methods students pick in those events.

Actions work on **whoever triggered the event**. While a trigger's event runs, `TriggeredBy` remembers the thing and the player behind it. So `TriggerZone.OnEnter → ForcePush.Push()` launches exactly what entered the zone, with no "Dynamic" parameters to set up. This context passes down a chain: in TriggerZone → Switch → Counter → Explosive, the explosion still credits the player who walked into the zone.

| Trigger | Events | Fires when |
|---|---|---|
| `TriggerZone` | `OnEnter(int)`, `OnExit(int)` | Something enters or leaves. Settings: `ReactsTo` (players, projectiles, balls, any physics object, everything), `OnlyForPlayer`, `OnlyOnce`, `Cooldown`, and `RepeatWhileInside`, which fires `OnEnter` again every X s, e.g. for lava |
| `HitSensor` | `OnHit(int)` | Shot or caught in an explosion |
| `Breakable` | `OnDamaged(int)`, `OnBroken(int)` | Took damage / health is used up. `RemoveWhenBroken` off keeps it alive, e.g. for an `Explosive` |
| `Switch` | `OnTurnedOn(int)`, `OnTurnedOff(int)` | Its state changes (`Toggle`, `TurnOn`, `TurnOff`, `AutoResetTime`) |
| `Timer` | `OnFinished` | After X seconds, optionally repeating |
| `Counter` | `OnReached`, `OnCountChanged(int)` | The count reaches its target |

| Action | Methods | Works on |
|---|---|---|
| `Explosive` | `Explode()` | Itself: fuse with blinking, explosion, then removed or respawned. Kill credit goes to the triggering player |
| `ForcePush` | `Push()` | Whoever triggered it: velocity along the object's up direction (green arrow) |
| `Damager` | `Hurt()`, `Kill()` | Whoever triggered it: players, and breakables (lava breaks crates) |
| `Teleporter` | `Teleport()` | Whoever triggered it: beamed to `Target`, speed kept |
| `Score` | `AddPoints()`, `RemovePoints()`, `WinRound()`, `SendToStart()` | The triggering player (for balls: who touched it last), or a fixed `ForPlayer` |
| `Disappear` | `Hide()`, `Show()` | Itself, optionally coming back after `RespawnTime` |
| `Remover` | `RemoveObject()`, `RemoveSelf()` | Whoever triggered it (crates, dropped weapons; balls go back to their start), or itself |
| `Spawner` | `Spawn()` | Spawns prefabs at its position, also on an interval |
| `Door` | `Open()`, `Close()`, `Toggle()` | Itself |
| `Mover` | `MoveToEnd()`, `MoveToStart()`, `Toggle()`, `StartMoving()`, `StopMoving()` | Itself, between start and start + `Offset`. Players ride along |
| `SwapSprite` | `Show()`, `ShowFor(float)`, `Restore()` | Its renderer, optionally blinking |
| `PlaySound` | `Play()` | Plays a clip |

Visual helpers (`Scripts/Visuals`): `SpriteAnimation` (looping frames) and `Hover` (bobbing).

### Prefabs

| Prefab (Assets/Kit/Prefabs) | Made of | What it does |
|---|---|---|
| Rules/LevelRules | `LevelRules` | Level name, builder name, win rules (last player standing, points to win, kills give points, time limit), max players, respawn, player prefab and health, gravity. Methods for events: `AddPoint(int)`, `RemovePoint(int)`, `WinRound(int)`, `Draw()` |
| Rules/SpawnPoint | `SpawnPoint` | Start point for player 1-4, optional start weapon |
| Rules/FinishFlag | TriggerZone → Score.WinRound | First player to touch it wins the round |
| Rules/ScoreZone | `ScoreZone` | King of the hill: one point per second while alone inside |
| Rules/Coin, Gem | TriggerZone → PlaySound, Score.AddPoints, Disappear.Hide | Points for collecting, optional respawn |
| Rules/Goal + Rules/Ball | TriggerZone (balls) → Score.AddPoints, Remover.RemoveObject; `Ball` | The ball in a goal gives a point to `Score.ForPlayer`, or to whoever touched it last |
| Rules/KillZone | TriggerZone (everything) → Damager.Kill, Remover.RemoveObject | Kills players, removes objects and resets balls below the level |
| Rules/NameSign | `NameSign` | Sign with the builder's name |
| Rules/CameraRig | `ArenaCamera` | Duck Game camera, see [Camera](#camera) |
| Blocks/TriggerZone | `TriggerZone` | The bare trigger for students' own ideas |
| Blocks/Lever, Button | `Switch` ← TriggerZone + HitSensor | Flipped by touching or shooting it. The lever toggles; the button turns on and resets after 3 s |
| Blocks/Door, LockBlock | `Door` | Blocks the way until an event opens it |
| Blocks/Key | TriggerZone → PlaySound, Disappear.Hide | 0 points. Connect its `TriggerZone.OnEnter` to `LockBlock.Open` |
| Blocks/MovingPlatform, Elevator | `Mover` | Moving one-way platforms |
| Blocks/JumpPad | TriggerZone → ForcePush.Push, SwapSprite.ShowFor | Launches players and objects along its up direction (rotate it for a side booster) |
| Blocks/TeleporterPair | 2 × TriggerZone → Teleporter.Teleport | Two linked portals |
| Blocks/Mine | TriggerZone (OnEnter → SwapSprite.Show; **OnExit** → Explosive.Explode) + HitSensor → Explode | Stepping on it arms it (blinking light); stepping off or shooting it blows it up |
| Blocks/TNTBlock | Breakable (`RemoveWhenBroken` off) → Explosive.Explode | Any damage blows it up, so explosions chain |
| Blocks/Crate, BrickWall | `Breakable` | Breaks after enough damage |
| Blocks/WeaponCrate | Breakable → Spawner.Spawn | Drops a random weapon |
| Blocks/Spikes, Saw | TriggerZone → Damager.Hurt (+ `Mover`) | Kills on touch |
| Blocks/RisingLava | Timer → Mover.MoveToEnd; TriggerZone → Damager.Hurt, Remover.RemoveObject | Sudden death: the lava starts rising after 30 s |
| Blocks/Spawner | `Spawner` | Spawns prefabs on an interval or on command |
| Blocks/Timer, Counter | `Timer`, `Counter` | "After X seconds" / "after N times" events |
| Art/Weapons/* | sprites | 12 weapon sprites (the 4 kit weapons plus Revolver, Blaster, LaserRifle, SMG, SniperRifle, WaterGun, Flamethrower, HandCannon), 3 projectiles and the mine (`Props/Mine`, `Props/Mine_Armed`). The pivot is the grip, so swapping the sprite of a weapon variant keeps it in the hand. Drawn by `Tools/weapon_sprites.py` |
| Weapons/* | `Weapon` | Bazooka, Pistol, Shotgun, GrenadeLauncher, Minigun. Projectile, speed, projectiles per shot, spread, fire delay, automatic, recoil, ammo (the weapon drops when empty). It shoots where its `Muzzle` points, so rotating the weapon aims it (the GrenadeLauncher is rotated 25° up) |
| Projectiles/* | `Projectile` | Damage, explosion radius, push force, gravity, lifetime, explode on impact or bounce |
| Pickups/* | `WeaponPickup` | A weapon lying in the level (a fixed weapon or a random one), respawns |

The Hazards tilemap of every level has TriggerZone (everything) → Damager.Hurt.

Tilemaps in every level:
- **Back Decoration** and **Front Decoration**: no collision
- **Ground**: solid
- **Platforms**: one-way, you can jump through from below and drop through with down + jump
- **Hazards**: deadly, e.g. lava and spikes

Players always start without a weapon. Use a `SpawnPoint > Start Weapon` to hand one out right away.

## Development

- **Kit builder:** `TUMO > Lehrer > Kit neu bauen` (`Assets/Kit/Editor/KitBuilder.cs`) rebuilds everything from the art:
  - tiles, palettes, prefabs, weapons and the player
  - the template scene, the two demo levels and the showcase
  - the build list and the kit checksum

  Student levels are not touched. Batch mode:
  `Unity -batchmode -quit -projectPath . -executeMethod ArenaKit.EditorTools.KitBuilder.BuildAll`

  `KitBuilder.BuildShowcaseOnly` rebuilds just the showcase. When you add a new block prefab, also add it to `BuildShowcase`. The builder warns about any block that is missing there.
- **Kit checksum:** after changing kit files by hand, run `TUMO > Lehrer > Kit-Prüfsumme speichern` so the export check doesn't warn students about your changes.
- **Tests:** `Window > General > Test Runner > PlayMode`, 28 tests. They cover:
  - spawning, both keyboard halves, jumping, quick stop, variable jump height
  - pickups, bazooka kill and round end, empty weapons dropping, rocket rotation, grenade launcher aim
  - jump pad, explosion with crate drop, mine (arms on step, explodes on leaving), TNT chain, spikes and saw breaking crates
  - level rotation, volleyball goal
  - camera: follows living players, drops dead ones, zooms in on the winner, stays inside the level at 16:9, 16:10, 4:3 and 21:9
  - tile collision in every kit scene
  - showcase: lever, coins → counter, key, trigger zone → spawner, only player 1, camera follows along the whole row

  Batch mode:
  `Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter ArenaKit.Tests`
  Set `ARENA_PREVIEW_DIR` to also save camera screenshots.
- **Movement:** this is the BirdGame force-based running (`MoveForce`, `MaxSpeed`) with Mario-style jumping:
  - `Braking`: stops quickly on the ground when no direction is pressed (knockback from explosions only brakes gently)
  - `JumpHeight` / `MinJumpHeight` in tiles: holding jump jumps higher, a tap makes a short hop (stronger gravity after letting go)
  - `FallGravity`: faster falling after the highest point, capped by `MaxFallSpeed`
  - The rigidbody has no linear damping, so jump heights are exact. Sideways drag is applied by hand instead.

  The kit also adds:
  - FixedUpdate physics
  - wall-stick prevention
  - coyote time and jump buffering
  - optional extra jumps and air control

## Credits

- Tiles, props, backgrounds, sounds: Kenney, New Platformer Pack 1.1 (CC0)
- Weapon sprites (except the bazooka): drawn for this kit with `Tools/weapon_sprites.py` (`pip install pymupdf`, then `python Tools/weapon_sprites.py`). The grip and muzzle positions are written to `Assets/Kit/Art/Weapons/weapons.json`, and the kit builder uses them.
- Bean man, bazooka, rocket, explosion, player sounds, font: Unity's 2D Platformer sample project, as used in the Games Academy course. It is fine for the workshop. Check the Asset Store license before publishing builds publicly.
