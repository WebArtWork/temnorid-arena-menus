# Temnorid Arena: Menus

`temnorid-arena-menus` is the Temnorid Arena the menus around a match: start, find a match, lobby, settings, and results.

It is a game package: it holds Temnorid Arena's own rules and content, built on the **IT-Kamianets 3D Engine** packages.

### Logic

Everything a player sees outside a match. Sign-in and saved profiles come from `3d-unity-firebase`; finding a match and the lobby run through `3d-unity-network`; settings change volume groups in `3d-unity-audio` and bindings in `3d-unity-input`; the results screen shows the match result from `temnorid-arena-core`.

It is built with Unity UI (uGUI and TextMeshPro).

### Status

Version 0.1, uGUI + TextMeshPro built in code.

* Main menu: your name, **Play vs bots** (hosts a match), **Join** by address, Settings, Quit.
* Pause menu (Esc / Start in a match): Resume, Settings, **Leave match** (saves stats, back to the main menu).
* Settings: master, effects and music volume (`3d-unity-audio`, saved), mouse sensitivity and invert look (`EngineInput`, saved in `EngineStorage`).
* Firebase (with the game's `google-services.json`): signs in anonymously, keeps your name in your profile, adds each match's kills and deaths to `progress/deathmatch`; plays offline when Firebase is unavailable.

Checked in Play mode: sign-in against the real project, Play vs bots, Leave match saving the stats (1 match) and returning to the menu.

### Roadmap

* Start screen
* Sign-in
* Find a match
* Lobby: teams and ready
* Settings: audio, controls, graphics
* Results screen
* Player profile and stats
