using System.Linq;
using pigbrain.core.Inspector;
using TMPro;
using UnityEngine;

namespace pigbrain.core.Localization
{
    [InlineButton(nameof(SetTMP))]
    public class LocalizationText : MonoBehaviour
    {
        [SerializeField][InlineScriptableObject][ReadOnly(nameof(isReadOnly))] LocalizationData data;
        [SerializeField][PopupString(nameof(popup))] string key;
        [SerializeField] TMP_Text tmp;
        LocalizationObject localizationObject => GetComponentInParent<LocalizationObject>();

        bool isReadOnly => !(useData == data && data != null);
        internal LocalizationData useData => data ? data : localizationObject ? localizationObject.data : null;
        string[] popup => useData ? useData.kvLookup.Keys.ToArray() : new string[0];

        void OnValidate() => tmp = GetComponent<TMP_Text>();

        void Start() => SetTMP();
        internal void SetTMP() { if (tmp && useData) tmp.text = useData[key]; }
    }
}
