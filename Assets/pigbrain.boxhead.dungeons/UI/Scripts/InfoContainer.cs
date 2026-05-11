using System.Collections;
using pigbrain.core.Collections;
using pigbrain.core.UnityObject;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.game.Boxhead
{
    public class InfoContainer : MonoBehaviour
    {
        // [SerializeField] Image comboIndexImage;
        // [SerializeField] TMP_Text comboIndexText;

        // public void OnEnable() => StartCoroutine(Run());
        // public void OnDisable() => StopAllCoroutines();

        // IEnumerator Run()
        // {
        //     while (true)
        //     {
        //         UpdateComboIndex();
        //         yield return null;
        //     }
        // }

        // void UpdateComboIndex()
        // {
        //     float index = ComboManager.GetComboIndex();
        //     comboIndexImage.material.SetFloat("_Fill", index % 1);
        //     comboIndexText.text = $"{(int)index}";
        // }
    }
}