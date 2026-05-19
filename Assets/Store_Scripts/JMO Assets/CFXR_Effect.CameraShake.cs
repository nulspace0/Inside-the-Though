// Stub file — CameraShake class was missing from the Cartoon FX Remaster package.
// This restores compilation without breaking existing scene references.

using UnityEngine;

namespace CartoonFX
{
    public partial class CFXR_Effect
    {
        [System.Serializable]
        public class CameraShake
        {
            public static bool editorPreview;

            public bool enabled;
            public bool isShaking { get; private set; }

            public void fetchCameras() { }
            public void animate(float time) { }
            public void StartShake() { }
            public void StopShake() { }
        }
    }
}
