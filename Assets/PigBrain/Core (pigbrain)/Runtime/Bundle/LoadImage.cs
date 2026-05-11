using pigbrain.core.Project;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace pigbrain.core.Bundle
{
    public class LoadImage : AddressableLoader<Sprite>
    // #if UNITY_EDITOR
    //         , IBuildStripper
    // #endif
    {
        [SerializeField] Image image;
        void OnValidate()
        {
            if (!image) image = GetComponentInChildren<Image>();
        }
        protected override void OnStartLoad(AsyncOperationHandle<Sprite> handle)
        {
            image.enabled = false;
        }
        protected override void OnLoaded(AsyncOperationHandle<Sprite> handle)
        {
            image.sprite = handle.Result;
            image.enabled = true;
        }
        // #if UNITY_EDITOR
        //         Sprite sprite;
        //         void IBuildStripper.Prebuild()
        //         {
        //             sprite = image.sprite;
        //             image.sprite = null;
        //         }

        //         void IBuildStripper.Postbuild() => image.sprite = sprite;
        // #endif
    }
}
