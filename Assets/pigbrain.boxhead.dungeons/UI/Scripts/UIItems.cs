using System.Collections.Generic;
using pigbrain.core.Inspector;
using pigbrain.core.Populator;
using pigbrain.core.Project;
using pigbrain.core.Statistics;
using pigbrain.core.UnityObject;
using UnityEngine;

namespace pigbrain.game.Boxhead.UI
{
    // [ProjectInterface.Control(ProjectInterface.Filter.Project, true, -100)]
    [InlineButton(nameof(Populate))]

    public class UIItems : MonoBehaviour
    {
        [Populate.Types(typeof(generated.ItemCardbase.PrefabView))]
        public GameObject card;
        [Populate.Types(typeof(generated.CharacterIconBase.PrefabView), typeof(generated.WeaponIconBase.PrefabView))]
        public GameObject icon;
        public GameObject tutorial;

        static readonly HashSet<string> appearence = new();
        void Start()
        {
            if (tutorial)
            {
                if (TryGetComponent(out AssetIdentity id) && !Persistence.CurrentData.GetBool(id.GetGuid()))
                {
                    Persistence.CurrentData.SetBool(id.GetGuid(), true);
                    var inst = tutorial.Instantiate(transform);
                    inst.transform.SetLocalPositionAndRotation(default, Quaternion.identity);
                }
            }
        }

        [ProjectInterface.Button("Populate")]
        void Populate() => core.Populator.Populate.Parse(this);
    }
}

