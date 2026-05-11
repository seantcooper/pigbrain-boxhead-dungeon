using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using UnityEngine;

namespace pigbrain.core.Localization
{
    [InlineButton(nameof(SetAllTMP))]
    public class LocalizationObject : MonoBehaviour
    {
        [SerializeField][InlineScriptableObject] internal LocalizationData data;
        void SetAllTMP()
        {
            GetComponentsInChildren<LocalizationText>(true).ForEach(t => t.SetTMP());
        }
    }

}
