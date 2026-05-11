using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Navigation;
using pigbrain.game.Boxhead.Statistic;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    public class Weapon : MonoBehaviour
    {
        const int MaxShotsPerFrame = 5;
        const int MaxShotsPerSecond = 200;

        [HeaderLine("Weapon")]
        public string displayName;
        [SerializeField] Trigger trigger = Trigger.Target;
        [SerializeField] Type type;
        [SerializeField] Shot shot;
        [SerializeField] Transform rotator;
        [SerializeField] Target.Score score = Target.Score.Fear | Target.Score.Threat | Target.Score.Angle;

        [HeaderLine("Stats")]
        [SerializeField][Range(0, 5)] float fireRate = 0.5f;
        [SerializeField][MinMaxRange(0, 20)] MinMaxFloat range = new(0, 10);
        [SerializeField][Range(0, 1000)] int ammo = 0;
        [SerializeField][ReadOnly] int ammoUsed = 0;
        [SerializeField][Range(0, 20)] int instances = 0;

        [HeaderLine("Emitters")]
        [SerializeField][FlattenObject] Transform[] emitters;

        int emitterIndex = 0;
        Targeting targeting;
        Health health;
        float triggerTime;

        void OnValidate()
        {
            if (string.IsNullOrEmpty(displayName))
                displayName = gameObject.GetDisplayName();
            if (trigger == 0) trigger = Trigger.Target;
        }

        void OnEnable() => ChangeValue();
        void OnDisable()
        {
            ChangeValue();
            RemoveInstances();
        }

        #region Accessors
        public int remainingAmmo => maxAmmo - ammoUsed;
        public int maxAmmo => Mathf.Max(0, ammo);
        public bool infiniteAmmo => maxAmmo == 0;
        public bool hasAmmo => !(ammo != 0 && ammoUsed >= ammo);
        public float maxFireRate => Mathf.Max(1f / MaxShotsPerSecond, fireRate);
        public float reloadUnitTime => Mathf.Clamp01(triggerTime / maxFireRate);

        public void SetMinRange(float min) => range.min = min;
        public void SetMaxRange(float max) => range.max = max;
        public float GetMaxRange() => range.max;
        public float GetFireRate() => fireRate;
        #endregion

        #region Level
        public int GetLevel() => TryGetComponent(out StatsController stats)
            ? stats.GetLevelIndex() : 0;

        public void LevelUp() => ChangeValue(() =>
        {
            if (TryGetComponent(out StatsController stats))
                stats.SetLevelIndex(stats.GetLevelIndex() + 1);
            if (!infiniteAmmo) ammoUsed = 0;
        });
        #endregion

        #region Events
        public void AddListener(Action<ChangeEvent> callback)
        {
            OnWeaponChanged += callback;
            ChangeValue();
        }
        public void RemoveListener(Action<ChangeEvent> callback) => OnWeaponChanged -= callback;

        void ChangeValue(Action valueChange = null)
        {
            changeEvent.weapon = this;
            changeEvent.lastAmmo = remainingAmmo;
            changeEvent.lastAmmo = remainingAmmo;
            changeEvent.lastLevel = GetLevel();
            valueChange?.Invoke();
            changeEvent.ammo = remainingAmmo;
            changeEvent.level = GetLevel();
            OnWeaponChanged?.Invoke(changeEvent);
        }

        ChangeEvent changeEvent;
        event Action<ChangeEvent> OnWeaponChanged;
        public struct ChangeEvent
        {
            public Weapon weapon;
            public int lastLevel, level;
            public int lastAmmo, ammo;
        }
        #endregion

        #region Update
        NavMapLayerFear fearLayer;

        void Initialize()
        {
            if (targeting) return;

            // triggerLastTime = Time.time;
            targeting = GetComponentInParent<Targeting>(true);
            if (trigger.HasFlag(Trigger.Threat)) fearLayer = NavMap.TryGetLayer<NavMapLayerFear>();
        }

        bool GetTarget(out Target target)
        {
            target = null;
            if (!targeting || !targeting.isActiveAndEnabled) return false;
            if (!targeting.GetBestTarget(range.min, range.max, out target, score)) return false;
            return true;
        }

        void Update()
        {
            if (TimeScale.IsPaused) return;
            Initialize();
            GetTarget(out Target result);
            UpdateRotator(result);
            TryFire(result);
        }

        void UpdateRotator(Target target)
        {
            if (!target || !rotator) return;
            rotator.rotation = target.GetRotation();
        }
        #endregion

        #region Trigger
        bool TryFire(Target target = null)
        {
            float rate = maxFireRate;
            triggerTime += Time.deltaTime;
            bool triggerState = TryTrigger_Target(target) || TryTrigger_Damage() || TryTrigger_Threat();
            for (int i = 0, n = Mathf.FloorToInt(triggerTime / rate); i < n && hasAmmo && hasInstance && triggerState; i++, triggerTime -= rate)
                CreateShot(target);
            if (!hasAmmo || !hasInstance) triggerTime = 0;
            else if (triggerTime > rate) triggerTime = rate;
            return true;
        }

        #endregion

        #region Triggers
        [Flags] enum Trigger { Target = 1 << 0, Threat = 1 << 1, Damage = 1 << 2, Other = 1 << 3, }
        bool TryTrigger_Target(Target target)
        {
            if (!trigger.HasFlag(Trigger.Target)) return false;
            if (!target) return false;
            if (!InRange(target.transform.position)) return false;
            if (target.transform.TryGetComponent(out Health health) && health.isDeadOrWillBe) return false;
            return true;

            bool InRange(Vector3 target) => type switch
            {
                Type.Placement => true, //range.InRange((target - transform.position).magnitude),
                Type.Projectile => range.InRange((target - transform.position).magnitude),
                _ => throw new Exception("Unknown type {type}!"),
            };
        }

        bool TryTrigger_Damage()
        {
            if (!trigger.HasFlag(Trigger.Damage)) return false;
            if (!health) return false;
            if (health.TryGetLastDamage(out Health.Damage d) && Time.time > d.time + 1) return false;
            return true;
        }

        bool TryTrigger_Threat()
        {
            if (!trigger.HasFlag(Trigger.Threat)) return false;
            if (!fearLayer) return false;
            if (fearLayer.GetThreat(transform.position) < 0.25f) return false;
            return true;
        }
        #endregion

        #region Instances
        readonly HashSet<Shot> population = new();
        HashSet<Shot> GetInstances()
        {
            population.RemoveWhere(i => !i || !i.isActiveAndEnabled);
            return population;
        }
        bool hasInstance => instances == 0 || GetInstances().Count < instances;
        void AddInstance(Shot shot) { if (instances > 0) population.Add(shot); }
        void RemoveInstances()
        {
            if (instances > 0)
            {
                GetInstances().ToArray().ForEach(s => s.Put());
                population.Clear();
            }
        }
        #endregion

        #region Create Shot
        void CreateShot(Target target)
        {
            Transform e = GetEmitter();
            var r = target ? (target.center - e.position).WithY(0).GetRotation() : Quaternion.identity; // e.rotation
            var inst = shot.CreateInstance(e.gameObject, target, e.position, r);
            GetComponent<StatsController>().TryApplyIndexTo(inst.gameObject);

            AddInstance(inst);

            if (!infiniteAmmo) ChangeValue(() => ammoUsed++);
            else ChangeValue();

        }

        Transform GetEmitter()
        {
            if (emitters != null && emitters.Length > 0)
                return emitters[(emitterIndex = Mathf.Max(0, emitterIndex + 1)) % emitters.Length];
            return transform;
        }
        #endregion

        enum Type { Projectile, Placement, }

        #region Gizmos
        void OnDrawGizmosSelected()
        {
            Vector3 s = Vector3.one * 0.1f;
            Gizmos.color = Color.green;
            if (emitters.IsNullOrEmpty())
                Gizmos.DrawCube(transform.position, s);
            else
                foreach (var e in emitters)
                    Gizmos.DrawCube(e.position, s);
        }
        #endregion
    }

    public interface IShot
    {
        void SetShotTarget(Transform transform);
    }
}
