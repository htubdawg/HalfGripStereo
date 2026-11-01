# HalfGripStereo

A Half Grip (Steam app 4163270, Unity, Windows) mod: a CD stereo **built into every car's cockpit** (not a shop item, owner: it takes no
item slot). A head unit left of the steering wheel and a CD wallet on the sun visor, one disc per soundtrack song plus **MY**, the
player's own folder. Worked with the cockpit hand only. Current release: see `Stereo/mod.json`.

## Always be ready for a context clear

Sessions lose their context without warning (and the owner clears it on purpose between steps). This file is the only memory the
next session has, so after **every** step that changes the state of things, this file must already say everything needed to carry on:

- **Update Status in the same commit as the change** (not "later"): what changed, what was checked and how, what is untested, what
  waits on whom, the next step. An owner's report (screenshot, log, wish) goes in Status as soon as it arrives, with the causes found,
  before fixing.
- **Never leave work only in the session:** commit and push each finished step to the branch. No uncommitted edits at the end of a
  turn. Scratch renders, decompiled code and the cloned HalfGripTools are lost on a clear; anything worth keeping goes in the repo
  (`docs/`, this file).
- **Write down what was learned the hard way** (game facts, causes of bugs, the owner's wishes and decisions) under *Game facts* and
  *Owner's wishes*, so a fresh session doesn't repeat a mistake or re-ask a question.
- **A fresh session starts by:** reading this file, `git log --oneline -10` and `git status` on the branch, cloning and building
  HalfGripTools (see *Checking changes*), and reading the files named in Status before touching anything.
- Keep it short enough to read first thing; move settled history out of Status into one line each.

## How the mod runs

- A **source mod** for [HalfGripTools](https://github.com/4l3x4nd3r0/HalfGripTools) (BepInEx 5): the loader compiles `Stereo/src/*.cs`
  at game start (**C# 10**, netstandard2.1). What ships is the `Stereo/` folder, installed to `<game>\BepInEx\HalfGripMods\Stereo\`.
  Loader API: HalfGripTools' `docs/WRITING_MODS.md`, `docs/GAME_NOTES.md`. Game DLLs and scene dumps: private `htubdawg/HalfGripRefs`.
- Siblings used for reference: `htubdawg/HalfGripMiniMap` (the GPS: dash sprites, lit material copies, `Clickable` targets, the
  live-reloaded config) and `htubdawg/HalfGripHelicopter` (cockpit parts placed from the wheel's bounds).

## Layout of src/

| File | Role |
|---|---|
| `Mod.cs` | Config (live re-read for placement), patches, the persistent `Fitter` object |
| `Fitter.cs` | Twice a second: no stereo yet and a cockpit (`NSteeringWheel`'s parent, `CarInterior`) → adds a `Stereo` object with `Deck` + `StereoDash`. Skips scenes whose `TrackManager.playRaceMusic` is false (menu background race, credits) |
| `Deck.cs` | Playback and the session's state (static: disc, chosen, power, pause, track, position): one `AudioSource` on the Music mixer group; soundtrack discs loop their song, next/prev change disc; MY loads each song off the disk (`UnityWebRequestMultimedia`, streamed), next/prev change song; the display's lines |
| `Discs.cs` | The discs: the game's songs gathered from `Resources.Load` (race songs), every `MapDefinition.musicTrack/altMusicTrack` and loaded clips named `* OST`, at each scene load until all `Known` are found; duplicates (same samples) merged into one disc with aliases, logged; the MY folder |
| `RaceMusicPatch.cs` | Prefix on `TrackManager.PlayRaceMusic`: with a stereo, the game's pick (`PickRaceSong`, private) goes to `Deck.Go` and the game's song is not started |
| `StereoDash.cs` | Builds the head unit (right edge 0.15 left of the wheel's rim, centre 0.5 above the wheel's, at the wheel's depth; radius from the sprite, not the turning bounds), slanted by `Stereo.Slant` (each column lifted by whole pixels, `Art.Lifts`/`Art.Lift`; the 2x display lifted in casing-pixel steps so it meets the window), and the visor (camera viewport point `Visor.ScreenX/Y` projected onto the wheel's plane); sprites pivot bottom left, placed in art pixels (`UnitLocal`); cockpit pixel = median over cockpit sprites (fallback 0.05); lit casing = own copy of the wheel's material with `_BaseMap` set; the display unlit at 2x; keys = `StereoButton` (`Clickable`) on `InteriorSquareCollider`s at `sortingLayer` -100, 0.1 in front, a unit-wide one at -101 whose right click ejects (every key's right click too); the loose discs (`Loose`: sleeve → hand → slot / visor / fall → sleeve; `sticking` = disc half out of the slot, static) |
| `Hand.cs` | The game's `Grabbable` subclassed: `KnobGrab` (never moves; measures the hand's turn round its middle, `angle` follows so the grip point goes round; 270 degrees = 0..10), `SleeveGrab` (the wallet's sleeve; `grabRedirect` hands the Grabber a new loose disc), `DiscGrab` (follows the hand exactly, no tilt; falls with gravity in the cockpit's plane when let go; still while in the slot) |
| `Sounds.cs` | Borrowed clicks: `Grab1` (keys; quiet and high for each volume step), `Reload` at a higher pitch (disc in/out of the slot), `Grab2` (disc taken from a sleeve or the slot) |
| `Art.cs` | Pixel art, managed only (so `tools/preview` draws it): `Canvas`, 3x5 font, `HeadUnit` (64x28 dash px: knob, display window, slot row 23 x 20-53, eject, 4 keys, CD badge), `Lifts`/`Lift` (the slant), `Lcd` (amber on brown with ghost segments), `LooseDisc` (31 px, full-face label, number/MY, clipped below the slot), `VisorWallet` (overlapping sleeves cut in a curve, a 7 px crescent per disc, numbers under them; `DiscCorner`, `PocketPlace`), `DrawDisc`, `LabelFor` (map colours) |

## Rules that matter

- Not an item. Never add it to the shop or to loadouts (owner, 0.1.0).
- Any map: only the cockpit (`NSteeringWheel`, `Grabber.viewCamera`) and the music API are relied on.
- Pixel look: flat palette colours, point filtering, whole cockpit pixels; no smooth or rotated sprites.
- Logging: everything but warnings through `Mod.Info` (only with `Debug.VerboseLog`).
- C# 10, netstandard2.1, tabs, `///` summaries that explain why, try/catch + `LogWarning` round risky work.
- Bump `version` in `mod.json` each release, the README's version in step. A changed default needs a migration (BepInEx keeps old values).
- Work on a branch and open a PR; do not push to `main`.

## Game facts

- Music: `SoundController.PlayMusic2D` makes `2D_Music_<clip>` sources on the Music mixer group (`SoundController.MusicGroup`).
  `TrackManager.PlayRaceMusic` runs when the countdown ends; `PickRaceSong` = scene `song` (Resources name, tutorials) or
  `RunState.ActiveMapMusic` (map's `musicTrack`, or `altMusicTrack` drawn with `altMusicChance`) or one of `raceSongNames`
  (`Forest OST`, `Neon OST`, `Factory OST`, `Stadion OST`, in Resources).
- Songs in 0.418 (maps.txt): Stadium x2 → Stadion; Mountain Day/Night → Winter; Factory x2 → Factory; Forest x3 → Forest; Neon Tokyo
  → Neon, alt Neon 2. The owner says there are **5 songs, maybe 1 more from the trailer**: the 5 map songs, plus probably Neon 2.
  Neon 2 is a different recording (owner's 0.1.0 test: no duplicates).
  The main menu's `Menu` clip is left out.
- `PauseController` pauses/unpauses every `AudioSource` in the scene (our source too); `InteriorMath.Paused` is the flag.
- Cockpit (Forest dump): `CarInterior` (layer INTERIOR) with `InteriorCamera` (fov 60, perspective) at `Head` (0, 4.51, -9.8);
  `Steering` (-1.8, -0.36, 1.14) scale 5.5; mounts `SLOT1` (6.11,-0.34) `SLOT2` (8.38,-0.34) `SLOTPASSIVE` (2.19, 7.14) `SLOT3`
  (-5.49, 8.79, top left: an external controller item could meet the visor). On a 1080p screen one cockpit unit at the wheel's
  depth is about 86 px; a cockpit pixel (0.05) about 4.3 px. The main menu has no cockpit.
- **Grabbing** (decompiled `Grabber`, `Grabbable`): the hand hovers the `InteriorCollider` with the highest `sortingLayer`, then the
  nearest; on a press it takes `GetComponentInParent<Grabbable>()` (following its `grabRedirect`) and holds it while the button is
  down, feeding it the hand's point in the Grabbable's plane (its parent's xy for `movementPlane` forward: x right, y up);
  otherwise it clicks the `Clickable`. Right click goes to the held Grabbable, else the hovered Grabbable, else the Clickable.
  `Grabbable` is abstract (`StepPhysics`, `StepDirect`; `ApplyPose` writes the transform from `position`/`angle`); its `Awake` reads
  the pose from the transform, so set the local position before `AddComponent`. A destroyed held object is simply dropped.
  `keepPhysicalCursorClose` pulls the cursor back within `keepPhysicalCursorRange` of `GripPlanePoint()` (= `position` +
  `grabbedPoint` turned by `angle`). The game's own: `LinearObject`, `DoubleLinearObject`, `HingedObject` (turns its transform),
  `FallingObject` (2D gravity, stuck/break-off), `BeerBottle`.
- Hand sprites: under `MouseGrabber` (`Hand1_0`, ...), `SpriteLit` material; their sorting order is not in the dumps (the build logs it
  as `hand sorting a..b`; a loose disc draws at the wheel's order + 3).
- `MountSlot.ScriptOnly` puts an item's interior on the passive mount without occupying it (not used now; noted from the item idea).

## Owner's wishes

- The stereo left of the steering wheel; the CD holder on the sun visor (or on the dash above the stereo): one disc per OST song and
  one for the player's own songs from a folder.
- **Not an item** (said mid-way through 0.1.0): it goes in no item slot, so it is part of every car. The first draft was a PASSIVE
  shop item on the ScriptOnly mount; it was taken out before the first push.
- Songs: "only 5 tracks and potentially 1 other one that was used in the trailer" — don't count map references twice (several maps
  share one clip). The 0.1.0 test: the songs were **not** duplicates (Neon 2 is its own song); "everything seems to work fine".
- After 0.1.0 (screenshot with red/yellow lines): the radio must **match the angle of the dash** (the dash's top edge left of the wheel
  rises to the right, about 0.4 px up per px across on screen); the CDs at the **same scale as the stereo**; CDs **dragged and dropped**
  into the radio, with a sound; volume by **turning a dial**; **right click ejects**, the player grabs the disc and drops it back on the
  visor, or lets it fall and it puts itself back in the visor. The wallet like the owner's photo: sleeves **overlapping**, a crescent
  of each disc showing on its right.

## Checking changes

Repos side by side in `/home/user`: `HalfGripRefs` (capitals: pass `-p:Refs=/home/user/HalfGripRefs`), `HalfGripTools` (clone it;
build once against the refs).

- Compile (before every push):
  ```
  dotnet build <HalfGripTools>/HalfGripTools/HalfGripTools.csproj -c Release -p:GameDir=<halfgriprefs>/game
  tools/check.sh <halfgriprefs>/game <HalfGripTools>/dist/BepInEx/plugins/HalfGripTools/HalfGripTools.dll
  ```
- Pictures: `dotnet run -c Release --project tools/preview -p:Refs=<halfgriprefs> -- <out dir>` writes `unit.png`/`visor.png` (one
  pixel per cockpit pixel, for laying over a screenshot at ~4.3x on 1080p), `unit-4x.png`, `visor-4x.png`, `unit-slanted-4x.png` (slanted by the optional second argument, default 0.33, a disc half out; copy to `docs/unit-slanted.png`), `disc-4x.png`, `icon.png` (copy to
  `Stereo/icon.png`). `docs/mockup.png` is those laid over the owner's screenshot where `StereoDash` computes they go (by hand, PIL).
- Game code: `DOTNET_ROLL_FORWARD=Major ilspycmd -p -o <dir> <refs>/game/HalfGrip_Data/Managed/Assembly-CSharp.dll`.
- In game (the owner tests): copy `Stereo/` to `<game>\BepInEx\HalfGripMods\`, `Debug.VerboseLog=true`, read `BepInEx\LogOutput.log`
  (look for `found the song`, `same song as`, `put in the cockpit`, `head unit at`, `CD wallet with`, `the race starts`).

## Status (keep current)

- **0.1.0** tested by the owner (screenshot): works; songs not duplicates. Asks in *Owner's wishes* (after 0.1.0).
- **0.2.0** on `claude/laughing-darwin-uowk66` (pushed, **no PR yet**): head unit slanted (`Stereo.Slant`, default 0.33, a guess from
  the screenshot's dash edge; new key, no migration); discs 31 px (fit the 34 px slot) in an overlapping wallet (123x48 for 7 discs);
  drag a disc from its sleeve to the stereo (slides in, `Reload` sound), to the visor (back), elsewhere (falls, back in its sleeve once
  out of sight or after 4 s; can be caught while falling); right click anywhere on the stereo or the eject key: the disc rises half
  out of the slot (`sticking`, kept between races), grabbable; a disc dropped in while another plays swaps them; volume knob grabbed
  and turned (clockwise louder, 10 steps, a tick each). Checked: compiles (`tools/check.sh`), `tools/preview` (`docs/visor.png`,
  `docs/unit-slanted.png`), `docs/mockup.png` laid over the owner's 0.1.0 screenshot (PIL, by hand). **Not tested in game.**
- **Untested guesses:** the slant's size and direction; whether a loose disc draws under the hand (log line `hand sorting`); the knob's
  turning feel (`Grabber` cursor pull with `keepPhysicalCursorRange` = the knob's width); fall speed (600 px/s²).
- **Waiting on the owner:** test of 0.2.0 with `Debug.VerboseLog=true`: screenshot + log (`head unit at ... slant`, `taken from the
  visor`, `put in the slot`, `fell; back in the visor`). Check: drag in, eject, take, drop on visor, let fall; knob turn; the slant.
- **Open questions for the owner:** the sounds borrowed (`Grab1`, `Grab2`, `Reload`); should the music start before the countdown
  ends; the Open World (does it call `PlayRaceMusic`?); the Helicopter hides the wheel (the stereo is still placed from it).
- **Next step:** the owner's test; then a PR.
