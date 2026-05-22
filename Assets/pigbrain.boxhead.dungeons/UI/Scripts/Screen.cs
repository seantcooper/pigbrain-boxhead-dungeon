#pragma warning disable UDR0005
#pragma warning disable UDR0001
using System;
using System.Collections;
using pigbrain.core.Analytics;
using pigbrain.core.Collections;
using pigbrain.core.UnityObject;
using pigbrain.core.Audio;
using pigbrain.game.Boxhead.Environment;
using pigbrain.game.Boxhead.FiniteStateMachine;
using pigbrain.game.Boxhead.Statistic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static pigbrain.game.Boxhead.UI.ScreenUtility;
using pigbrain.core.Statistics;

namespace pigbrain.game.Boxhead.UI
{
    // base
    static class ScreenUtility
    {
        public static ScreenTransition Transition(RectTransform screen, ScreenTransition.Direction direction, Action complete = null)
        {
            if (screen.TryGetComponent(out ScreenTransition transition))
            {
                transition.Transition(direction, complete);
                return transition;
            }
            screen.gameObject.SetActive(direction == ScreenTransition.Direction.Enter);
            return null;
        }
        public static bool MouseClick() =>
            Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        public static bool WeaponFire() =>
            Game.Input.GamePlay.WeaponFire.WasPressedThisFrame();
    }

    public class Screen : FSM.State<Screens>
    {
        [SerializeField] protected RectTransform screen;
        protected ScreenTransition transition;
        public override void Awake() => screen.gameObject.SetActive(false);
        public override void Enter() => transition = Transition(screen, ScreenTransition.Direction.Enter);
        public override void Exit() => transition = Transition(screen, ScreenTransition.Direction.Exit);
        public override IEnumerator Hold() { yield return new WaitUntil(() => !transition || transition.transitioning == false); }
    }

    public class SubScreen : FSM.SubState<Screens>
    {
        [SerializeField] protected RectTransform screen;
        protected ScreenTransition transition;
        protected virtual (float scale, float duration) timeScale => (1, 0);
        TimeScale.Scope timeScaleScope;
        Coroutine timeFade;
        public override void Awake() => screen.gameObject.SetActive(false);
        public override void Enter()
        {
            transition = Transition(screen, ScreenTransition.Direction.Enter);
            if (timeScale.scale != 1)
                timeScaleScope = new TimeScale.Scope(fsm, timeScale.duration, timeScale.scale);
        }
        public override void Exit()
        {
            timeScaleScope.TryDispose();
            transition = Transition(screen, ScreenTransition.Direction.Exit);
        }
        public override IEnumerator Hold() { yield return new WaitUntil(() => !transition || transition.transitioning == false); }
    }

    #region Title
    [Serializable]
    public sealed class TitleScreen : Screen
    {
        [SerializeField] string[] autoload;
        [SerializeField] GameObject sitelocked;
        public override void Enter()
        {
            base.Enter();
            sitelocked.SetActive(false);
        }

        public override IEnumerator Run()
        {
            if (!Application.isEditor && !AnalyticsManager.IsValidSite)
            {
                sitelocked.SetActive(true);
                yield break;
            }
            int loadCount = 0;
            foreach (var key in autoload)
            {
                if (string.IsNullOrEmpty(key)) continue;
                Debug.Log($"Loading Scene: {key}");
                AssetLoader.Load(key, (s) => loadCount--);
                loadCount++;
            }
            yield return new WaitUntil(() => loadCount == 0);
            yield return new WaitForSeconds(1);

            if (Persistence.CurrentData.GetBool("SkipSelection", true))
            {
                Persistence.CurrentData.SetBool("SkipSelection", false);
                SetState(fsm.loading);
            }
            else SetState(fsm.main);
        }
    }
    #endregion

    #region Main
    [Serializable]
    public sealed class MainScreen : Screen
    {
        [SerializeField] Button start;
        [SerializeField] Button confirm;
        [SerializeField] Button resume;
        [SerializeField] Button creator;
        DungeonSelector selector => transform.GetComponentInChildren<DungeonSelector>(true);

        public Status status = Status.None;

        public enum Status { None, Start, Resume, Reset }

        public override void Start()
        {
            base.Start();
            selector.OnSelectionChanged += OnDungeonSelected;
            start.onClick.AddListener(() => status = Status.Start);
            resume.onClick.AddListener(() => status = Status.Resume);
            confirm.onClick.AddListener(() => status = Status.Reset);
            creator.onClick.AddListener(() => fsm.Interrupt(fsm.creator));
        }

        void OnDungeonSelected(LevelData dungeon) =>
            resume.interactable = BootStrap.HasValidSave(dungeon.name);

        public override void Enter()
        {
            base.Enter();
            status = Status.None;
            Persistence.Read();
            OnDungeonSelected(selector.GetSelectedDungeon());
        }

        public override IEnumerator Run()
        {
            while (true)
            {
                status = Status.None;
                yield return new WaitUntil(() => status != Status.None);

                if (status == Status.Start)
                {
                    if (resume.interactable)
                    {
                        confirm.SetActive(true);
                        yield return new WaitUntilTimeout(2, () => status != Status.Start);
                        confirm.SetActive(false);

                        if (status == Status.Start)
                            status = Status.None;

                        if (status == Status.Reset)
                        {
                            Persistence.CurrentData.Clear(selector.GetSelectedDungeon().name);
                            break;
                        }
                    }
                    else break;
                }

                if (status == Status.Resume)
                    break;
            }
            SetState(fsm.loading);
        }
    }

    #region Creator
    [Serializable]
    public sealed class CreatorScreen : Screen
    {
        [SerializeField] Button start;
        [SerializeField] Button back;

        public override void Start()
        {
            base.Start();
            start.onClick.AddListener(() => fsm.Interrupt(fsm.loading));
            back.onClick.AddListener(() => fsm.Interrupt(fsm.main));
        }

        // public override IEnumerator Run()
        // {
        //     yield break;
        // }
    }
    #endregion

    public class WaitForButtonClick : CustomYieldInstruction
    {
        Button button;
        InputAction inputAction;
        bool waiting = true;
        public WaitForButtonClick(Button button, InputAction inputAction = null)
        {
            this.button = button;
            this.inputAction = inputAction;
            if (inputAction != null)
            {
                inputAction.performed += OnInput;
                if (!inputAction.enabled) inputAction.Enable();
            }
            this.button.onClick.AddListener(OnClick);
        }

        void OnClick() => Stop();
        void OnInput(InputAction.CallbackContext ctx) => Stop();
        void Stop()
        {
            if (button) button.onClick.RemoveListener(OnClick);
            if (inputAction != null) inputAction.performed -= OnInput;
            waiting = false;
        }
        public override bool keepWaiting => !Game.Input.GamePlay.WeaponFire.WasPressedThisFrame() && waiting;
    }
    #endregion

    #region Loading
    [Serializable]
    public sealed class LoadingScreen : Screen
    {
        [SerializeField] string worldKey = "boxhead-dungeon01";
        [SerializeField] Image loading;

        string loadedKey;
        Scene? loaded;

        public override void Start() =>
            loading.material = new Material(loading.material);

        public override void Enter()
        {
            base.Enter();
            loaded = null;
            StatsCatalog.Instance.ResetRuntime();
            AssetLoader.Load(worldKey,
                progress: p =>
                    loading.material.SetFloat("_Fill", p),
                completed: (s) =>
                {
                    loaded = s;
                    loadedKey = worldKey;
                    SceneManager.SetActiveScene(s);
                });
        }

        public void Unload()
        {
            _ = AssetLoader.Unload(worldKey);
            loaded = null;
            loadedKey = null;
        }

        public override IEnumerator Run()
        {
            Debug.Log("Wait for Load");
            while (loaded == null || SceneManager.GetActiveScene() != loaded) yield return null;

            Debug.Log("Wait for BootStrap");
            yield return new WaitUntil(() => BootStrap.Instance);

            bool ready = false;
            BootStrap.Instance.OnComplete += () => ready = true;
            BootStrap.Instance.Activate();

            // wait for the player event
            while (!ready) yield return null;

            SetState(fsm.game);
        }
    }
    #endregion

    #region Game
    [Serializable]
    public sealed class GameScreen : Screen
    {
        [SerializeField] TMP_Text title;
        [SerializeField] Button bored;
        [SerializeField] EdgeMarkerPanel edgeMarkers;
        [SerializeField][Range(0, 15)] float boredDelay = 5;

        bool exiting, outro;
        Room currentRoom;
        float roomStartTime;
        Player player;

        Screen exitState => DungeonSelector.Instance.dungeonCreator ? fsm.creator : fsm.main;

        public override void Enter()
        {
            base.Enter();
            exiting = outro = false;
            fsm.pause.StartInput();
            fsm.wasted.ResetPrice();

            ActiveRoom.Instance.OnRoomStarted += OnRoomStarted;
            ActiveRoom.Instance.OnRoomCompleted += OnRoomCompleted;

            bored.gameObject.SetActive(false);
            bored.onClick.AddListener(OnBoredClick);
            RegisterPlayer();
            OnRoomStarted(ActiveRoom.Instance.GetCurrentRoom());
        }

        public override void Exit()
        {
            base.Exit();
            // StatsCatalog.Instance.ResetRuntime();
            fsm.pause.StopInput();
            fsm.loading.Unload();
        }

        void RegisterPlayer()
        {
            player = ActivePlayer.Instance.player;
            Debug.Log($"RegisterPlayer {ActivePlayer.Instance.player} {ActivePlayer.Instance.player.GetEntityId()}");
            screen.GetComponentInChildren<WeaponsContainer>().Bind(player);
            player.GetComponent<Health>().onDeath += () => exiting = true;
            edgeMarkers.target = player.transform;
        }

        void RespawnPlayer() => ActivePlayer.Instance.RespawnPlayer(() => RegisterPlayer());

        void OnRoomStarted(Room room)
        {
            if (!room || room.data.levelData == null) return;
            currentRoom = room;
            roomStartTime = Time.time;
            title.text = room.data.levelData.title;
        }

        void OnRoomCompleted(Room room)
        {
            bored.gameObject.SetActive(false);
            if (room.type == Room.Type.Final)
                outro = true;
        }

        void OnBoredClick() => ActiveRoom.Instance.SetLevelComplete();

        void ActivateBored()
        {
            if (currentRoom.state == Room.State.Complete) return;
            if (bored.isActiveAndEnabled) return;
            if (!currentRoom || currentRoom.level >= 5) return;
            if (Time.time < roomStartTime + boredDelay) return;
            bored.gameObject.SetActive(true);
        }

        Action adAction;
        public void RunAd(Action adAction) => this.adAction = adAction;

        public override IEnumerator Run()
        {
            if (outro)
            {
                yield return new WaitForSeconds(2);
                yield return fsm.outro.RunState();
                SetState(exitState);
                yield break;
            }

            // ActivateBored();
            if (fsm.pause.CanSet())
            {
                yield return fsm.pause.RunState();

                if (fsm.pause.status == PauseScreen.Status.Quit)
                {
                    yield return new WaitForSeconds(0.5f);
                    SetState(exitState);
                    yield break;
                }
            }

            if (adAction != null)
            {
                yield return fsm.ad.RunState();
                adAction?.Invoke();
                adAction = null;
            }

            if (exiting)
            {
                exiting = false;
                yield return new WaitForSeconds(1);
                yield return fsm.wasted.RunState();
                if (fsm.wasted.status == WastedScreen.Status.Quit) SetState(exitState);
                else RespawnPlayer();
            }
        }
    }
    #endregion

    #region Pause
    [Serializable]
    public sealed class PauseScreen : SubScreen
    {
        [SerializeField] Button pause;
        [SerializeField] Button quit;
        [SerializeField] internal Status status;

        InputAction input => Game.Input.GamePlay.Pause;
        protected override (float scale, float duration) timeScale => (0, 0.25f);
        bool pauseKey = false;
        internal void StartInput() => input.performed += OnInput;
        internal void StopInput() => input.performed -= OnInput;
        void OnInput(InputAction.CallbackContext ctx) => pauseKey = true;

        public override bool CanSet() => enabled && pauseKey;

        public override void Start()
        {
            quit.onClick.AddListener(() => status = Status.Quit);
            pause.onClick.AddListener(() => status = Status.Leave);
        }

        public override IEnumerator Run()
        {
            status = Status.None;
            using var _ = new AudioMixer.PauseScope();
            pauseKey = false;
            yield return new WaitUntil(() => status != Status.None || CanSet() || WeaponFire()); // || MouseClick()
            pauseKey = false;
        }
        internal enum Status { None, Leave, Quit }
    }
    #endregion

    #region Wasted
    [Serializable]
    public sealed class WastedScreen : SubScreen
    {
        [SerializeField] float countDown = 10;
        [SerializeField] int priceStart = 10;
        [SerializeField] int priceIncrement = 10;
        [SerializeField] Button continueAd, continuePrice, quit;
        InputAction input => Game.Input.GamePlay.WeaponFire;
        protected override (float scale, float duration) timeScale => (0, 1f);

        int price;
        internal Status status;

        internal void ResetPrice() => price = priceStart;

        public override void Start()
        {
            continueAd.onClick.AddListener(OnClickAd);
            continuePrice.onClick.AddListener(OnClickPrice);
            quit.onClick.AddListener(OnClickQuit);
        }

        public override void Enter()
        {
            base.Enter();
            continuePrice.interactable = fsm.money >= price;
            continuePrice.GetComponentInChildren<TMP_Text>().text = $"CONTINUE ({price})";
        }
        public override void Exit()
        {
            base.Exit();
            if (status == Status.PayPrice) price += priceIncrement;
        }

        public override IEnumerator Run()
        {
            for (status = Status.Waiting; ;)
            {
                switch (status)
                {
                    default:
                    case Status.Waiting: yield return null; break;
                    case Status.PayPrice: fsm.money.Set(fsm.money - price); yield break;
                    case Status.WatchAd:
                        yield return fsm.ad.RunState();
                        yield break;
                    case Status.Quit: yield break;
                }
            }
        }

        void OnClickAd() => status = Status.WatchAd;
        void OnClickPrice() => status = Status.PayPrice;
        void OnClickQuit() => status = Status.Quit;
        internal enum Status { Waiting, Quit, WatchAd, PayPrice, }
    }
    #endregion

    #region AD
    [Serializable]
    public sealed class ADScreen : SubScreen
    {
        [SerializeField] float timeout = 10;
        [SerializeField] Button @continue;
        InputAction input => Game.Input.GamePlay.WeaponFire;
        protected override (float scale, float duration) timeScale => (0, 0);

        bool leave = false;

        public override void Start() => @continue.onClick.AddListener(OnClick);

        public override IEnumerator Run()
        {
            leave = false;
            for (float time = Time.unscaledTime + timeout; Time.unscaledTime < time;)
            {
                int cd = Mathf.FloorToInt(Mathf.Clamp(time - Time.unscaledTime, 0, timeout));
                @continue.GetComponentInChildren<TMP_Text>().text = $"CONTINUE ({cd})";
                yield return null;
                if (leave) { leave = false; yield break; }
            }
        }
        void OnClick() => leave = true;
    }
    #endregion

    #region Outro
    [Serializable]
    public sealed class OutroScreen : SubScreen
    {
        [SerializeField] float timeout = 10;
        [SerializeField] Button @continue;

        protected override (float scale, float duration) timeScale => (0, 2);

        bool exit = false;
        public override void Start() => @continue.onClick.AddListener(() => exit = true);
        public override IEnumerator Run()
        {
            exit = false;
            DungeonSelector.UnlockAllAndSave();
            yield return new WaitUntil(() => exit);
            exit = false;
        }
        // void OnClick() => leave = true;
    }
    #endregion

}
