# Temnorid Arena: Menus

`temnorid-arena-menus` is the Temnorid Arena the menus around a match: start, find a match, lobby, settings, and results.

It is a game package: it holds Temnorid Arena's own rules and content, built on the **IT-Kamianets 3D Engine** packages.

### Logic

Everything a player sees outside a match. Sign-in and saved profiles come from `3d-unity-firebase`; finding a match and the lobby run through `3d-unity-network`; settings change volume groups in `3d-unity-audio` and bindings in `3d-unity-input`; the results screen shows the match result from `temnorid-arena-core`.

It is built with Unity UI (uGUI and TextMeshPro).

### Status

Version 0.1, uGUI + TextMeshPro built in code.

* Main menu: your name, **Play vs bots** (hosts a match), **Join** by address, Settings, Quit (not on iOS, where apps do not quit themselves).
* Pause menu (Esc / Start in a match): Resume, Settings, **Leave match** (saves stats, back to the main menu).
* Settings: master, effects and music volume (`3d-unity-audio`, saved), look sensitivity (mouse and touch) and invert look (`EngineInput`, saved in `EngineStorage`).
* VR: every screen is a panel 1.6 m ahead that comes up in front when opened, pointed at with either controller (trigger clicks); the menu button pauses. Settings show snap or smooth turning, the comfort vignette and the casting hand instead of look sensitivity and invert look. Checked with simulated headset and controllers: Play vs bots, pause, Settings, a toggle, Back, Resume, Leave match.
* Phones: 60 frames per second, the screen stays on during a match, the pause button on the touch HUD opens the pause menu.
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
