using System;
using System.Linq;
using ITKamianets.Engine.Audio;
using ITKamianets.Engine.Core;
using ITKamianets.Engine.Firebase;
using ITKamianets.Engine.Input;
using ITKamianets.Engine.Network;
using Newtonsoft.Json.Linq;
using Temnorid.Arena.Core;
using Temnorid.Arena.Hud;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Temnorid.Arena.Menus
{
    /// <summary>
    /// Everything around a match: the main menu (your name, play against bots, join a game,
    /// settings, quit), the pause menu (Esc / Start: resume, settings, leave), and settings
    /// (volumes, mouse sensitivity, invert look) saved per player. With a Firebase config it signs
    /// the player in, keeps their name in their profile and adds each match's kills and deaths to
    /// their saved stats. Builds itself in code; add one to the arena scene.
    /// </summary>
    public class ArenaMenus : MonoBehaviour
    {
        private const string LogCategory = "Menus";
        private const string NameKey = "temnorid-arena.name";
        private const string SensitivityKey = "input.pointerDegreesPerPixel";
        private const string InvertKey = "input.invertY";
        private const string StatsKey = "deathmatch";

        [Tooltip("google-services.json of the game's Firebase project. Empty: play without Firebase.")]
        [SerializeField] private TextAsset firebaseConfig;

        [SerializeField] private string defaultAddress = "127.0.0.1";

        private Canvas _canvas;
        private GameObject _main;
        private GameObject _pause;
        private GameObject _settings;
        private GameObject _returnTo;
        private TMP_InputField _name;
        private TMP_InputField _address;
        private TMP_Text _status;

        private EngineFirebase _firebase;
        private bool _inMatch;

        [Serializable]
        private class Stats
        {
            public int matches;
            public int kills;
            public int deaths;
        }

        private void Start()
        {
            ApplySavedInputSettings();
            Build();
            ShowMain();
            SignIn();
        }

        private void Update()
        {
            var session = NetworkSession.Current;
            var inMatch = session != null && session.Role != NetworkRole.Offline;
            if (inMatch != _inMatch)
            {
                _inMatch = inMatch;
                if (inMatch)
                {
                    HideAll();
                }
                else
                {
                    ShowMain();
                }
            }

            if (_inMatch && EngineInput.Default.UI.Pause.WasPressedThisFrame())
            {
                if (_pause.activeSelf || _settings.activeSelf)
                {
                    HideAll();
                }
                else
                {
                    Show(_pause);
                }
            }
        }

        // ---- Actions ----

        private void PlayVsBots()
        {
            SaveName();
            var session = NetworkSession.Current;
            if (session == null)
            {
                _status.text = "No NetworkSession in the scene.";
                return;
            }

            session.Mode = ArenaEvents.Mode;
            session.DisplayName = PlayerName;
            if (!session.StartHost())
            {
                _status.text = "Could not start the match.";
            }
        }

        private void Join()
        {
            SaveName();
            var session = NetworkSession.Current;
            if (session == null)
            {
                return;
            }

            session.Mode = ArenaEvents.Mode;
            session.DisplayName = PlayerName;
            var address = string.IsNullOrWhiteSpace(_address.text) ? defaultAddress : _address.text.Trim();
            _status.text = session.StartClient(address) ? $"Joining {address}..." : $"Could not join {address}.";
        }

        private async void Leave()
        {
            await SaveMatchStats();
            NetworkSession.Current?.Stop();

            // A fresh arena scene for the next match.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---- Firebase ----

        private async void SignIn()
        {
            if (firebaseConfig == null)
            {
                return;
            }

            try
            {
                _firebase = EngineFirebase.Initialize(FirebaseConfig.FromGoogleServicesJson(firebaseConfig.text));
                await _firebase.Auth.SignInAsync();
                var profile = await _firebase.LoadProfileAsync();
                var saved = (string)profile?["displayName"];
                if (!string.IsNullOrEmpty(saved) && _name != null && string.IsNullOrEmpty(EngineStorage.Current.GetString(NameKey)))
                {
                    _name.text = saved;
                }

                var stats = await _firebase.LoadProgressAsync(StatsKey, new Stats());
                _status.text = $"Signed in. {stats.matches} matches, {stats.kills} kills.";
            }
            catch (Exception exception)
            {
                EngineLog.Warning(LogCategory, $"Firebase unavailable: {exception.Message}");
                _status.text = "Playing offline (no Firebase).";
                _firebase = null;
            }
        }

        private async System.Threading.Tasks.Task SaveMatchStats()
        {
            var match = ArenaMatch.Current;
            var local = Combatant.All.FirstOrDefault(c => c.IsOwner && !c.IsBot);
            if (_firebase == null || !_firebase.Auth.IsSignedIn || match == null || local == null)
            {
                return;
            }

            try
            {
                var stats = await _firebase.LoadProgressAsync(StatsKey, new Stats());
                foreach (var score in match.Scores)
                {
                    if (score.Id.ToString() == local.Id)
                    {
                        stats.kills += score.Kills;
                        stats.deaths += score.Deaths;
                    }
                }

                stats.matches++;
                await _firebase.SaveProgressAsync(StatsKey, stats);
            }
            catch (Exception exception)
            {
                EngineLog.Warning(LogCategory, $"Could not save stats: {exception.Message}");
            }
        }

        private void SaveName()
        {
            EngineStorage.Current.SetString(NameKey, PlayerName);
            EngineStorage.Current.Save();
            if (_firebase != null && _firebase.Auth.IsSignedIn)
            {
                _ = _firebase.SaveProfileAsync(new JObject { ["displayName"] = PlayerName });
            }
        }

        private string PlayerName => string.IsNullOrWhiteSpace(_name.text) ? "Player" : _name.text.Trim();

        // ---- Settings ----

        private static void ApplySavedInputSettings()
        {
            var storage = EngineStorage.Current;
            var input = EngineInput.Default;
            input.PointerDegreesPerPixel = storage.GetFloat(SensitivityKey, input.PointerDegreesPerPixel);
            input.InvertY = storage.GetBool(InvertKey);
        }

        private static void SetSensitivity(float slider)
        {
            // Slider 0..1 maps to 0.02..0.3 degrees per pixel; the middle is the engine default.
            var value = Mathf.Lerp(0.02f, 0.3f, slider);
            EngineInput.Default.PointerDegreesPerPixel = value;
            EngineStorage.Current.SetFloat(SensitivityKey, value);
            EngineStorage.Current.Save();
        }

        // ---- Screens ----

        private void Build()
        {
            _canvas = ArenaUi.Canvas("Arena Menus", 20, transform);

            _main = Screen("Main menu", true);
            var title = ArenaUi.Box("Title", _main.transform, new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 140f));
            ArenaUi.Label(title, "TEMNORID ARENA", 84f, ArenaUi.Accent, TMPro.TextAlignmentOptions.Center);
            var column = ArenaUi.Column(_main.transform, new Vector2(0.5f, 0.42f), Vector2.zero);
            _name = ArenaUi.Field(column, "Your name", EngineStorage.Current.GetString(NameKey, "Player"));
            ArenaUi.Button(column, "Play vs bots", PlayVsBots);
            _address = ArenaUi.Field(column, "Server address", defaultAddress);
            ArenaUi.Button(column, "Join", Join);
            ArenaUi.Button(column, "Settings", () => Show(_settings));
            ArenaUi.Button(column, "Quit", Quit);
            var statusRect = ArenaUi.Box("Status", _main.transform, new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 50f));
            _status = ArenaUi.Label(statusRect, string.Empty, 24f, ArenaUi.Muted, TMPro.TextAlignmentOptions.Center);

            _pause = Screen("Pause menu", true);
            var pauseColumn = ArenaUi.Column(_pause.transform, new Vector2(0.5f, 0.5f), Vector2.zero);
            ArenaUi.Button(pauseColumn, "Resume", HideAll);
            ArenaUi.Button(pauseColumn, "Settings", () => Show(_settings));
            ArenaUi.Button(pauseColumn, "Leave match", Leave);

            _settings = Screen("Settings", true);
            var audio = EngineAudio.Default;
            var settingsColumn = ArenaUi.Column(_settings.transform, new Vector2(0.5f, 0.5f), Vector2.zero);
            ArenaUi.Slider(settingsColumn, "Master volume", audio.MasterVolume, audio.SetMasterVolume);
            ArenaUi.Slider(settingsColumn, "Effects volume", audio.GetVolume(SoundGroup.Effects), v => audio.SetVolume(SoundGroup.Effects, v));
            ArenaUi.Slider(settingsColumn, "Music volume", audio.GetVolume(SoundGroup.Music), v => audio.SetVolume(SoundGroup.Music, v));
            var sensitivity = Mathf.InverseLerp(0.02f, 0.3f, EngineInput.Default.PointerDegreesPerPixel);
            ArenaUi.Slider(settingsColumn, "Mouse sensitivity", sensitivity, SetSensitivity);
            UnityEngine.UI.Button invert = null;
            invert = ArenaUi.Button(settingsColumn, InvertLabel(), () =>
            {
                var on = !EngineInput.Default.InvertY;
                EngineInput.Default.InvertY = on;
                EngineStorage.Current.SetBool(InvertKey, on);
                EngineStorage.Current.Save();
                invert.GetComponentInChildren<TMP_Text>().text = InvertLabel();
            });
            ArenaUi.Button(settingsColumn, "Back", () => Show(_returnTo != null ? _returnTo : _main));
        }

        private static string InvertLabel()
        {
            return EngineInput.Default.InvertY ? "Invert look: on" : "Invert look: off";
        }

        private GameObject Screen(string name, bool dim)
        {
            var rect = ArenaUi.Rect(name, _canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            if (dim)
            {
                ArenaUi.Image(rect, new Color(0.02f, 0.03f, 0.05f, 0.86f));
            }

            return rect.gameObject;
        }

        private void ShowMain()
        {
            Show(_main);
        }

        private void Show(GameObject screen)
        {
            if (screen != _settings)
            {
                _returnTo = screen;
            }

            _main.SetActive(screen == _main);
            _pause.SetActive(screen == _pause);
            _settings.SetActive(screen == _settings);

            // A menu takes the mouse; gameplay input waits.
            EngineInput.Default.PlayerEnabled = false;
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void HideAll()
        {
            _main.SetActive(false);
            _pause.SetActive(false);
            _settings.SetActive(false);
            EngineInput.Default.PlayerEnabled = _inMatch;
        }
    }
}
