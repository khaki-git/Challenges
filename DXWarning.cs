using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine;

namespace Challenges {
    public class DeleteLaterDebris: MonoBehaviour {
        private float timeAlive;
        public float timeToKill = 15;
        void Update() {
            timeAlive += Time.deltaTime;
            if (timeAlive >= timeToKill) Destroy(gameObject);
        }
    }
    public static class DXWarning {
        private static GameObject menuPopup;
        public static void Init(GameObject popup) {
            menuPopup = popup;
            SceneManager.sceneLoaded += (scene, mode) => {
                if (scene.name == "Title" && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan) {
                    var newPopup = Object.Instantiate(menuPopup, null, true);
                    newPopup.name = "MenuPopup";
                    newPopup.AddComponent < DeleteLaterDebris > ();
                }
            };
        }
    }
}