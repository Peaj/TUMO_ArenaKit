# Learnings from the first run (October 2026)

Notes after day 1 of the first TUMO holiday workshop, plus ideas for the next run.
Small fixes were made during the workshop; the bigger items are for an overhaul before running it again.

## What happened

**Timing**
- The course runs 9:30–13:30 with a fixed 30-minute break at 11:30. The plan keeps relative times (0:00–4:00) with the break at 2:00.

**The Teens and the computer**
- Their first instinct was to change the level the moment they had it, before any explanation. They broke things:
  - deleted the CameraRig ("Why is my game black?")
  - scaled and skewed the tilemap layers with the scale tool
  - deleted kit prefab assets, leaving "Missing Prefab" in their level
- Mouse and keyboard were hard for many:
  - double-clicks came too slowly or with the mouse moving in between
  - in drag and drop they released early and lost the Inspector focus of the drop target
  - WASD and the arrow keys were unfamiliar; some had probably never used a keyboard for games
- Most of them play Roblox, Fortnite and free-to-play mobile games (touch or controller).

**The Unity workflow**
- The full Unity editor overwhelmed them on first use. Custom menus like "Neues Level erstellen" worked well, and more of them would help.
- Creating a custom weapon (prefab variant, sprite, muzzle, values, pickup) had too many steps and had to be explained several times. "Prefab variant" doesn't stick at this point.
- Assigning a weapon to a pickup was the biggest stumbling block: Teens dropped the weapon prefab itself into the level and wondered why they couldn't pick it up.
- The "Alle Bausteine" showcase wasn't shown on day 1. That may have been for the better: they would have started placing everything right away.

**Setup**
- The IT downloaded the GitHub ZIP and unpacked it on the desktop instead of following the git instructions. So there was no git repo, and `Kit aktualisieren.command` (git only at the time) couldn't update or reset anything.

## Changes made during the workshop
- **Break at the 2:00 mark** on all days.
- **Day 2 now starts with the game idea:** level design input (what each mode needs), a tour of the Bausteine scene, then remix dice and the game idea sheet. The level is built for that idea from the start, so nobody has to rebuild an arena on day 3 for a race or a ball game. No paper arena (it wasn't paper anymore anyway). Triggers and actions come after the break.
- **Day 3** is for mechanics and polish: plan, two build sprints with a test at the neighbour's Mac in between, feature freeze, finale.
- **Day 1, for the next run:** "Neues Level erstellen" and painting a few tiles come right after the Unity driver's licence. Then the Teens know how to change the level before they try on their own.
- **`Kit aktualisieren.command` works without git.** It downloads the ZIP and replaces the kit parts. Teen files found in `Assets/Kit` are moved to `Assets/Levels/<Name>/Gerettet`. On a Mac with an old ZIP copy, run it once from Terminal: `curl -fsSL https://www.peaj.de/TUMO_ArenaKit/Tools/kit-update.sh | bash`.
- **TUMO > Level reparieren** puts back what every level needs: the CameraRig, Level Rules, the global light and the KillZone. It also straightens a moved, scaled or skewed Grid and its tilemaps, and warns about missing prefabs and spawn points.
- **The cheat sheet now says:** weapons always go into the level through a pickup. It also points to "Level reparieren".

## Ideas for the next run

**Order of the first day**
- Start with painting tiles in their own level within the first 20–30 minutes. They'll change the level anyway, so let them do it the right way first.
- Add a short input warm-up. A tiny keyboard and mouse mini-game would do: walk, jump and shoot with both keyboard halves, double-click and drag a few objects. Gamepads could come out earlier for Teens who only know controllers.

**Protect what they shouldn't break**
- Hide or lock the CameraRig, the Grid transform and the Level Rules object in the Hierarchy (HideFlags or a locked parent). Or let "Level reparieren" run automatically when Play is pressed.
- Make kit assets read-only for the Teens: a pre-import hook or an editor warning when something in `Assets/Kit` is deleted or moved.

**Fewer Unity steps, more custom menus and tools**
- **Make the pickup and the weapon the same thing.** A weapon that lies in the level without an owner behaves as its own pickup, so dragging a weapon prefab into the level just works.
- A "Neue Waffe erstellen" window: pick a base weapon and a sprite, enter values. It creates the variant in the Teen's folder and puts a pickup into the level.
- More of these windows wherever a task needs several Unity steps (new player look, new projectile).
- Where possible, offer buttons instead of drag and drop, e.g. "+ Pickup" or "Ins Level setzen" in a kit window.

**Installation**
- Hand the IT a single command that installs, opens and verifies, or prepare one Mac and image or copy it. Check the install the day before.
- Since the update script also handles ZIP installs, either way of installing now works.

**Content**
- Their reference games are Roblox, Fortnite and mobile games. Examples and genre talk could start there.
