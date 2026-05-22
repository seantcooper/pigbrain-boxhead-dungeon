using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Analysis;
using pigbrain.core.Collections;
using pigbrain.core.Statistics;
using pigbrain.game.Boxhead.Environment;
using pigbrain.game.Boxhead.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.game.Boxhead.Creator
{
    public class DungeonCreator : MonoBehaviour
    {
        [SerializeField] RectTransform weaponSelection;
        [SerializeField] WeaponEditor weaponEditor;
        [SerializeField] Button exit;
        [SerializeField] Button play;
        [SerializeField] TMP_InputField code;
        [SerializeField] Button copy;

        [Header("Panels")]
        [SerializeField] EnemyEditor enemies;
        [SerializeField] WeaponEditor weapons;
        [SerializeField] LengthEditor length;
        [SerializeField] SquadEditor squad;
        [SerializeField] ThreatEditor threat;
        [SerializeField] SummaryEditor summary;

        CreatorEditor[] editors => new CreatorEditor[] { enemies, weapons, length, squad, threat };

        const byte Version = 1;
        string currentCode;

        public const string Bullet = " + ";

        internal void OnEditorChange(CreatorEditor _)
        {
            string code = Encode();
            UpdateCode(code);
        }

        void Awake()
        {
            exit.onClick.AddListener(() =>
            {
                DungeonSelector.Instance.dungeonCreator = null;
            });

            play.onClick.AddListener(() =>
            {
                var dungeon = GetLevelData();
                Persistence.CurrentData.Clear(dungeon.name);
                DungeonSelector.Instance.dungeonCreator = dungeon;
            });

            code.onSubmit.AddListener(OnInputSubmit);
            code.onEndEdit.AddListener(OnInputSubmit);
            code.onValidateInput += ValidateCharacter;
            copy.onClick.AddListener(OnCopy);
        }

        void OnCopy()
        {
            ValidateCode(code.text);
            JS.Copy(currentCode);
        }

        char ValidateCharacter(string text, int index, char c) =>
            Base62.IsValid(c) ? c : '\0';
        void OnInputSubmit(string value) => ValidateCode(value);
        void ValidateCode(string value)
        {
            if (IsValid(value)) UpdateCode(value);
            else code.SetTextWithoutNotify(currentCode);
        }
        void OnEnable()
        {
            var code = Persistence.CurrentData.GetString($"DungeonCreator", Encode());
            Decode(code);
            UpdateCode(code);
        }

        void UpdateCode(string code)
        {
            Persistence.CurrentData.SetString($"DungeonCreator", code);
            this.code.text = currentCode = code;
            summary.detail.text = string.Join("\n", editors.SelectMany(e => e.summary.Append("")));
            Debug.Log($"Editor Changing {code}");
        }

        #region Encode / Decode
        public string Encode() => Base62.Encode(ByteEncode());
        public void Decode(string code) => ByteDecode(Base62.Decode(code));
        public bool IsValid(string code) => IsValid(Base62.Decode(code));

        byte[] ByteEncode()
        {
            byte[] bytes = editors.SelectMany(e => e.Encode()).ToArray();
            return bytes.Prepend(ChkSum(bytes)).Prepend((byte)bytes.Length).Prepend(Version).ToArray();
        }

        byte ChkSum(byte[] bytes) => (byte)bytes.Sum(b => b);

        const int HeaderSize = 3;
        bool IsValid(byte[] bytes)
        {
            int index = 0;

            if (bytes.Length < HeaderSize)
            {
                Debug.LogError($"Not enough bytes {bytes.Length}?");
                return false;
            }

            int version = bytes[index++];
            int len = bytes[index++];
            byte chksum = bytes[index++];
            int v = ChkSum(bytes.Skip(HeaderSize).ToArray());

            if (Version != version)
            {
                Debug.LogError($"Version is wrong {Version} != {version}");
                return false;
            }
            if (len != bytes.Length - HeaderSize)
            {
                Debug.LogError($"Length is wrong {len} != {bytes.Length - HeaderSize}");
                return false;
            }
            if (v != chksum)
            {
                Debug.LogError($"Checksum is wrong {v} != {chksum}"); return false;
            }
            return true;
        }

        void ByteDecode(byte[] bytes)
        {
            if (!IsValid(bytes)) return;
            int index = 3;
            editors.ForEach(e => e.Decode(bytes, ref index));
        }
        #endregion

        #region Level Data
        LevelData GetLevelData()
        {
            var data = ScriptableObject.CreateInstance<LevelData>();
            data.traits = LevelData.Traits.None;
            data.title = data.name = currentCode;

            data.startWeapons = weapons.GetValue()
                .SelectMany(w => Enumerable.Repeat($"{w.index}", w.level + 1)).ToArray();

            data.roomCount = length.roomCount;
            data.soldiers = squad.soldierCount;

            data.seed = (uint)UnityEngine.Random.Range(1, 10000000);

            // difficulty
            data.money = 10;
            data.exp = 0;

            // room count?          
            data.roomSize = new(6, 15);

            int dindex = (int)threat.GetValue();

            var availableEnemies = enemies.GetValue().ToHashSet();

            bool activeZombies = availableEnemies.Contains(EnemyEditor.Indices.Zombie);
            bool activeRunners = availableEnemies.Contains(EnemyEditor.Indices.Runner);
            bool activeGhosts = availableEnemies.Contains(EnemyEditor.Indices.Ghost);
            bool activeTerrors = availableEnemies.Contains(EnemyEditor.Indices.Terror);
            bool activeDevils = availableEnemies.Contains(EnemyEditor.Indices.Devil);

            var zombie = this.zombie[dindex];
            var runner = this.runner[dindex];
            var ghost = this.ghost[dindex];
            var terror = this.terror[dindex];
            var devil = this.devil[dindex];

            data.levels = new();
            for (int i = 0; i < data.roomCount; i++)
            {
                float t = data.roomCount == 1 ? 1f : (float)i / (data.roomCount - 1);

                var items = new List<LevelData.Level.Item>();

                void AddItem(Enemy.Type type, (int start, int end, int active) d)
                {
                    items.Add(new LevelData.Level.Item
                    {
                        type = type,
                        total = Mathf.RoundToInt(Mathf.Lerp(d.start, d.end, t)),
                        count = Mathf.RoundToInt(Mathf.Lerp(d.active * 0.5f, d.active, t)),
                        level = enemyLevel[dindex],
                        interval = interval[dindex],
                        burstRate = burstRate[dindex]
                    });
                }

                if (activeZombies) AddItem(Enemy.Type.Zombie, zombie);
                if (activeRunners) AddItem(Enemy.Type.Runner, runner);
                if (activeGhosts) AddItem(Enemy.Type.Ghost, ghost);
                if (activeTerrors) AddItem(Enemy.Type.Terror, terror);
                if (activeDevils) AddItem(Enemy.Type.Devil, devil);

                var level = new LevelData.Level
                {
                    levelid = i + 1,
                    title = $"Level {i + 1}",
                    description = "Generated fromg the Dungeon Creator!",
                    loot = new string[0],
                    lockDoor = true,
                    items = items.ToArray()
                };
                data.levels.Add(level);
            }
            return data;
        }

        float[] burstRate = new[]
         {
            Mathf.Lerp(0.1f, 0.01f, 0 * 0.25f),
            Mathf.Lerp(0.1f, 0.01f, 1 * 0.25f),
            Mathf.Lerp(0.1f, 0.01f, 2 * 0.25f),
            Mathf.Lerp(0.1f, 0.01f, 3 * 0.25f),
            Mathf.Lerp(0.1f, 0.01f, 4 * 0.25f),
        };

        float[] interval = new[]
       {
            Mathf.Lerp(1, 0, 0 * 0.25f),
            Mathf.Lerp(1, 0, 1 * 0.25f),
            Mathf.Lerp(1, 0, 2 * 0.25f),
            Mathf.Lerp(1, 0, 3 * 0.25f),
            Mathf.Lerp(1, 0, 4 * 0.25f),
        };


        int[] enemyLevel = new[] { 1, 1, 2, 2, 3 };

        (int start, int end, int active)[] zombie = new[]
        {
            (start: 20, end: 100, active: 20),
            (start: 50, end: 250, active: 40),
            (start: 100, end: 500, active: 75),
            (start: 200, end: 1000, active: 125),
            (start: 400, end: 2000, active: 200),
        };

        (int start, int end, int active)[] runner = new[]
        {
            (start: 10, end: 50, active: 10),
            (start: 25, end: 125, active: 20),
            (start: 50, end: 250, active: 40),
            (start: 100, end: 500, active: 75),
            (start: 200, end: 1000, active: 125),
        };

        (int start, int end, int active)[] terror = new[]
        {
            (start: 8, end: 40, active: 8),
            (start: 20, end: 100, active: 16),
            (start: 40, end: 200, active: 32),
            (start: 80, end: 400, active: 64),
            (start: 160, end: 800, active: 100),
        };

        (int start, int end, int active)[] ghost = new[]
        {
            (start: 1, end: 5, active: 1),
            (start: 2, end: 10, active: 2),
            (start: 4, end: 20, active: 4),
            (start: 8, end: 40, active: 6),
            (start: 16, end: 80, active: 10),
        };

        (int start, int end, int active)[] devil = new[]
        {
            (start: 1, end: 5, active: 1),
            (start: 2, end: 10, active: 2),
            (start: 4, end: 20, active: 4),
            (start: 8, end: 40, active: 6),
            (start: 16, end: 80, active: 10),
        };
        #endregion
    }
}