using System.Collections;
using pigbrain.core.UnityObject;
using UnityEngine;

public class EnforceLandscape : MonoBehaviour
{
    [SerializeField] RectTransform content;
    float lastApsect;

    void Awake() => content.SetActive(false);

    IEnumerator Start()
    {
        yield return new WaitForSeconds(1);
        while (true)
        {
            Adjust();
            yield return null;
        }
    }

    void Adjust()
    {
        var aspect = Screen.width / Screen.height;
        if (lastApsect != aspect)
        {
            content.SetActive(aspect < 1);
            lastApsect = aspect;
        }
    }
}
