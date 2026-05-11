using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Deactivate : MonoBehaviour
{
    void Awake()
    {
        var children = GetComponentsInChildren<Deactivate>()
            .Where(a => a != this).ToHashSet();

        if (children.Count == 0) gameObject.SetActive(false);
        else StartCoroutine(WaitForChildren(children));
    }

    IEnumerator WaitForChildren(HashSet<Deactivate> children)
    {
        yield return new WaitUntil(() => children.All(c => c.didAwake));
        gameObject.SetActive(false);
    }
}
