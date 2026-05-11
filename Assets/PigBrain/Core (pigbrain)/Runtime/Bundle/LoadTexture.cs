using System.Collections.Generic;
using pigbrain.core.Project;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace pigbrain.core.Bundle
{
    public class LoadTexture : AddressableLoader<Texture2D>
#if UNITY_EDITOR // Stripper
        , IBuildStripper
#endif
    {
        [SerializeField] RawImage image;
        void OnValidate()
        {
            if (!image) image = GetComponentInChildren<RawImage>();
        }

        protected override void OnStartLoad(AsyncOperationHandle<Texture2D> handle) =>
            image.enabled = false;

        protected override void OnLoaded(AsyncOperationHandle<Texture2D> handle)
        {
            image.texture = handle.Result;
            image.enabled = true;
        }

#if UNITY_EDITOR // Stripper
        IEnumerable<Object> IBuildStripper.Prebuild()
        {
            yield return image.texture;
            image.enabled = false;
            image.texture = null;
        }
        void IBuildStripper.Postbuild(UnityEngine.Object[] objects)
        {
            image.enabled = true;
            image.texture = objects[0] as Texture;
        }

#endif
    }
}
