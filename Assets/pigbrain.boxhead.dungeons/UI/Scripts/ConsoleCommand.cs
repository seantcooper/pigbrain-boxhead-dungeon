#pragma warning disable UDR0001
using UnityEngine;
using System.Linq;
using pigbrain.game.Boxhead.Environment;
using pigbrain.core.Collections;
using UnityEngine.Scripting;
using pigbrain.game.Boxhead.Statistic;
using pigbrain.core.UnityObject;
using static UnityEngine.Object;
using pigbrain.core.Analysis;

namespace pigbrain.game.Boxhead.UI
{

    #region Test
    [Preserve]
    public class Test : Console.Command
    {
        public override string Help(Console console) => "Test";
        public override (Status, string) Parse(Console console, string[] parts)
        {
            if (!ActivePlayer.Instance.player) return (Status.Error, "No player");
            ActivePlayer.Instance.player.GetComponent<Health>().maxDamage = 10000;
            ActivePlayer.Instance.player.GetComponentsInChildren<Weapon>().ForEach(w => w.gameObject.SetActive(false));
            return (Status.Success, "");
        }
    }
    #endregion

    #region Invincible
    [Preserve]
    public class Invincible : Console.Command
    {
        public override string Help(Console console) => "Invincible - no damage to the players";
        public override (Status, string) Parse(Console console, string[] parts)
        {
            if (!BootStrap.Instance) return (Status.Error, "Scene is not loaded!");
            ActiveRoom.Instance.player.GetComponent<Health>().enabled = !ActiveRoom.Instance.player.GetComponent<Health>().enabled;
            return (Status.Success, "");
        }
    }
    #endregion

    #region CooperTeam
    [Preserve]
    public class CooperTeam : Console.Command
    {
        public override string Help(Console console) => "CooperTeam";
        public override (Status, string) Parse(Console console, string[] parts)
        {
            if (!BootStrap.Instance) return (Status.Error, "Scene is not loaded!");
            var weapons = Catalog.Query.GetAllObjects().Values
                .Select(o => o as GameObject)
                .Where(o => o is GameObject go && go.GetComponent<Weapon>());

            for (int i = 0; i < 5; i++)
            {
                foreach (var w in weapons)
                {
                    w.Instantiate(ActiveRoom.Instance.player.transform);
                    w.transform.ResetLocal();
                }
            }
            StatsCatalog.Session.SetValue(Stat.Money, 999);
            StatsCatalog.Session.SetValue(Stat.Exp, 100000);

            var soldier = Catalog.Q<GameObject>("soldier");
            for (int i = 0; i < 10; i++)
                soldier.Instantiate();



            return (Status.Success, "");
        }
    }
    #endregion

    #region Dev
    [Preserve]
    public class Dev : Console.Command
    {
        public override string Help(Console console) => GetType().Name;
        public override (Status, string) Parse(Console console, string[] parts)
        {
            // fallback: find in scene by name
            var profile = FindObjectsByType<RectTransform>(FindObjectsInactive.Include)
                .FirstOrDefault(o => o.name == "Debug Profile");

            if (!profile) return (Status.Error, "Not Found");
            profile.gameObject.SetActive(!profile.gameObject.activeSelf);
            return (Status.Success, "");
        }
    }
    #endregion

    #region Money
    [Preserve]
    public class Money : Console.Command
    {
        public override string Help(Console console) => "Money <int>, i.e. Money 999";
        public override (Status, string) Parse(Console console, string[] parts)
        {
            if (parts.Length != 1) return (Status.Error, "Should have 1 parameter!");
            if (!int.TryParse(parts[0], out int intValue)) return (Status.Error, "Parameter should ne int!");
            StatsCatalog.Session.SetValue(Stat.Money, intValue);
            return (Status.Success, "");
        }
    }
    #endregion

    #region FPS
    [Preserve]
    public class FPS : Console.Command
    {
        public override string Help(Console console) => "FPS <int>, i.e. FP3 30";
        public override (Status, string) Parse(Console console, string[] parts)
        {
            if (parts.Length != 1) return (Status.Error, "Should have 1 parameter!");
            if (!int.TryParse(parts[0], out int fps)) return (Status.Error, "Parameter should be integer!");
            RuntimePerformance.SetFPS(fps);
            return (Status.Success, "");
        }
    }
    #endregion

    #region Level
    [Preserve]
    public class Level : Console.Command
    {
        public override string Help(Console console) => "Level Complete | <int>";
        public override (Status, string) Parse(Console console, string[] parts)
        {
            if (parts.Length != 1) return (Status.Error, "Should have 1 parameter, e.g Level 17, Level Complete ");
            if (!BootStrap.Instance) return (Status.Error, "Scene is not loaded!");

            switch (parts[0].ToUpper())
            {
                case "COMPLETE":
                    ActiveRoom.Instance.SetLevelComplete();
                    return (Status.Success, "");

                default:
                    if (!int.TryParse(parts[0], out int level))
                        return (Status.Error, "Unknown parameter, e.g Level 17 ");
                    ActiveRoom.Instance.SetLevel(level);
                    return (Status.Success, "");
            }
        }
    }
    #endregion

    #region Kill
    [Preserve]
    public class Kill : Console.Command
    {
        public override string Help(Console console) => "Kill Player | Enemy";
        public override (Status, string) Parse(Console console, string[] parts)
        {
            if (parts.Length != 1) return (Status.Error, "Should have 1 parameter!");
            if (!BootStrap.Instance) return (Status.Error, "Scene is not loaded!");

            switch (parts[0].ToUpper())
            {
                case "PLAYER":
                    KillType<Player>();
                    return (Status.Success, "");

                case "ENEMY":
                    KillType<Enemy>();
                    return (Status.Success, "");

                default: return (Status.Error, "Unknown parameter, e.g Level 17 ");
            }
        }


        void KillType<T>() where T : MonoBehaviour => FindObjectsByType<T>()
            .Select(t => t.GetComponent<Health>())
            .Where(h => h)
            .ForEach(h => h.ApplyDamage(null, h.maxDamage));
    }
    #endregion

    #region Add
    [Preserve]
    public class Add : Console.Command
    {
        public override string Help(Console console) =>
            "Add " + string.Join(" | ", Catalog.Query.GetAllObjects().Select(kv => kv.Key));

        public override (Status, string) Parse(Console console, string[] parts)
        {
            if (parts.Length != 1) return (Status.Error, "Should have 1 parameter!");
            if (!BootStrap.Instance) return (Status.Error, "No Dungeon running");
            if (Catalog.Query.Find<GameObject>(parts[0]) is GameObject go)
            {
                go.Instantiate(ActiveRoom.Instance.player.transform);
                return (Status.Success, "");
            }
            // foreach (var kv in Catalog.Find<Weapon>())
            // {
            //     if (kv.Key.Equals(parts[0], StringComparison.OrdinalIgnoreCase))
            //     {
            //         kv.Value.Instantiate(ActiveRoom.Instance.player.transform);
            //         return (Status.Success, "");
            //     }
            // }
            return (Status.Error, $"Object '{parts[0]}' was not found!");
        }
    }
    #endregion

    #region Demo
    [Preserve]
    public class Demo : Console.Command
    {
        public override string Help(Console console) => "Demo";
        public override (Status, string) Parse(Console console, string[] parts)
        {
            if (!BootStrap.Instance) return (Status.Error, "Scene is not loaded!");
            FindObjectsByType<Player>().FirstOrDefault(p => !p.aiControl).StartDemo();
            return (Status.Success, "");
        }
    }
    #endregion

    #region System
    [Preserve]
    [Console.Name("SYSTEM")]
    public class Sys : Console.Command
    {
        public override string Help(Console console) => "";

        public override (Status, string) Parse(Console console, string[] parts)
        {
            // if (parts.Length > 0) return (Status.Error, "Should have at least 1 parameter!");

            switch (parts[0])
            {
                case "SCALE":
                    // if (parts.Length != 2) return (Status.Error, "Should have 2 parameter!");
                    if (!float.TryParse(parts[1], out float scale)) return (Status.Error, "Scale requires integer!");
                    RuntimePerformance.SetDynamicScale(scale);
                    return (Status.Success, "");
                default:
                    return (Status.Error, "No such command!");
            }
        }
    }
    #endregion
}