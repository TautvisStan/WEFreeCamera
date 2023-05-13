using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using UnityEngine;
using BepInEx.Configuration;
//TODO fix scene transitions; controller display when character is not visible on main
namespace WEFreeCamera
{
    [BepInPlugin(PluginGuid, PluginName, PluginVer)]
    [HarmonyPatch]
    public class CustomCameraPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "GeeEM.WrestlingEmpire.WEFreeCamera";
        public const string PluginName = "WEFreeCamera";
        public const string PluginVer = "1.0.0";

        internal static ManualLogSource Log;
        internal readonly static Harmony Harmony = new(PluginGuid);

        internal static string PluginPath;
        internal static bool inFreeCamMode = false;
        internal static bool cameraLocked = false;
        internal static Camera ourCamera;
        internal static Camera currentMain;
        internal static Vector3? currentUserCameraPosition;
        internal static Quaternion? currentUserCameraRotation;
        public static ConfigEntry<double> configCameraMoveSpeed;
        internal static Vector3 previousMousePosition;
        internal static CustomCamera freeCamScript;
        public static ConfigEntry<KeyCode> configToggle;
        public static ConfigEntry<KeyCode> configLock;
        public static ConfigEntry<KeyCode> configSpeed;
        public static ConfigEntry<KeyCode> configLeft;
        public static ConfigEntry<KeyCode> configRight;
        public static ConfigEntry<KeyCode> configForwards;
        public static ConfigEntry<KeyCode> configBackwards;
        public static ConfigEntry<KeyCode> configUp;
        public static ConfigEntry<KeyCode> configDown;

        private void Awake()
        {
            CustomCameraPlugin.Log = base.Logger;
            PluginPath = Path.GetDirectoryName(Info.Location);
            configCameraMoveSpeed = Config.Bind("Camera",
                 "CameraSpeed",
                 (double)10,
                 "Camera move speed");
            configToggle = Config.Bind("Controls",
                 "CameraToggle",
                 KeyCode.O,
                 "Toggle the free camera on and off");
            configLock = Config.Bind("Controls",
                 "CameraLock",
                 KeyCode.L,
                 "Lock the free camera in place");
            configSpeed = Config.Bind("Controls",  
                 "SuperSpeed", 
                 KeyCode.RightShift, 
                 "Super speed");
            configLeft = Config.Bind("Controls",     
                 "MoveLeft", 
                 KeyCode.LeftArrow, 
                 "Move left"); 
            configRight = Config.Bind("Controls",
                 "MoveRight", 
                 KeyCode.RightArrow,
                 "Move Right");
            configForwards = Config.Bind("Controls",
                 "MoveForwards",  
                 KeyCode.UpArrow,
                 "Move forwards");
            configBackwards = Config.Bind("Controls",
                 "MoveBackwards",
                 KeyCode.DownArrow,
                 "Move backwards");
            configUp = Config.Bind("Controls",
                 "MoveUp",
                 KeyCode.Space,
                 "Move up");
            configDown = Config.Bind("Controls",
                 "MoveDown",
                 KeyCode.LeftControl,
                 "Move down");

        }

        private void OnEnable()
        {
            Harmony.PatchAll();
            Logger.LogInfo($"Loaded {PluginName}!");
        }

        private void OnDisable()
        {
            Harmony.UnpatchSelf();
            Logger.LogInfo($"Unloaded {PluginName}!");
        }
        static void BeginFreecam()
        {
            inFreeCamMode = true;

            previousMousePosition = Input.mousePosition;
            CacheMainCamera();
            SetupFreeCamera();
        }
        static void CacheMainCamera()
        {
            currentMain = Camera.main;
            if (currentMain)
            {
                if (currentUserCameraPosition == null)
                {
                    currentUserCameraPosition = currentMain.transform.position;
                    currentUserCameraRotation = currentMain.transform.rotation;
                }
            }
        }
        static void SetupFreeCamera()
        {
            if (!ourCamera)
            {
                ourCamera = new GameObject("Freecam").AddComponent<Camera>();
                ourCamera.gameObject.AddComponent<AudioListener>();
                ourCamera.gameObject.tag = "MainCamera";
                GameObject.DontDestroyOnLoad(ourCamera.gameObject);
                ourCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            if (!freeCamScript)
                freeCamScript = ourCamera.gameObject.AddComponent<CustomCamera>();

            ourCamera.transform.position = (Vector3)currentUserCameraPosition;
            ourCamera.transform.rotation = (Quaternion)currentUserCameraRotation;

            ourCamera.gameObject.SetActive(true);
            ourCamera.enabled = true;
            currentMain.GetComponent<AudioListener>().enabled = false;
        }
        internal static void EndFreecam()
        {
            inFreeCamMode = false;

            if (ourCamera)
                ourCamera.gameObject.SetActive(false);

            if (freeCamScript)
            {
                GameObject.Destroy(freeCamScript);
                freeCamScript = null;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(configToggle.Value) && inFreeCamMode == false)
            {
                BeginFreecam();
            }
            else if (Input.GetKeyDown(configToggle.Value) && inFreeCamMode == true)
            {
                EndFreecam();
            }
        }
    }
    //Patching character movement controls
    [HarmonyPatch(typeof(FMOKFGNFBEL), nameof(FMOKFGNFBEL.LCPJNDDEDOP))]
    public static class FMOKFGNFBEL_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(FMOKFGNFBEL __instance, ref float __result)
        {
            if (!CustomCameraPlugin.inFreeCamMode)
                return true;
            else
            {
                __result = CustomCameraPlugin.ourCamera.transform.eulerAngles.y + Mathf.Atan2(__instance.OIDEGGMJJPP, __instance.BHNNEGFENKO) * 57.29578f;
                return false;
            }
        }
    }
    //Patching label rotation
    [HarmonyPatch(typeof(CBGJILJIJIB), nameof(CBGJILJIJIB.JIKCOKCCPBE))]
    public static class CBGJILJIJIB_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(GameObject HJGHHNAKAPD)
        {
            if (!CustomCameraPlugin.inFreeCamMode)
                return true;
            else
            {
                Quaternion rotation = CustomCameraPlugin.ourCamera.transform.rotation;
                HJGHHNAKAPD.transform.LookAt(HJGHHNAKAPD.transform.position + rotation * Vector3.forward, rotation * Vector3.up);
                return false;
            }
        }
    }
    //Patching UI elements display 
    [HarmonyPatch(typeof(OEKDDHPAEIF))]
    public static class OEKDDHPAEIF_Patch
    {
        [HarmonyPatch(nameof(OEKDDHPAEIF.CHDIFHAKFLC))]
        [HarmonyPrefix]
        // Token: 0x060001AF RID: 431 RVA: 0x0009EB9C File Offset: 0x0009CD9C
        public static bool CHDIFHAKFLC(int DLMFHPGACEP, OEKDDHPAEIF __instance, ref float __result)
        {
            if (!CustomCameraPlugin.inFreeCamMode)
                return true;
            else
            {
                __result = 0f;
                if (__instance.PNINKKAAPBD[DLMFHPGACEP] != null)
                {
                    __result = CustomCameraPlugin.ourCamera.WorldToScreenPoint(__instance.PNINKKAAPBD[DLMFHPGACEP].transform.position).x;
                }
                
                return false;
            }
        }
        [HarmonyPatch(nameof(OEKDDHPAEIF.OHLBJLHHIHD))]
        [HarmonyPrefix]
        // Token: 0x060001B0 RID: 432 RVA: 0x0009EBD5 File Offset: 0x0009CDD5
        public static bool OHLBJLHHIHD(int DLMFHPGACEP, OEKDDHPAEIF __instance, ref float __result)
        {
            if (!CustomCameraPlugin.inFreeCamMode)
                return true;
            else
            {
                __result = 0f;
                if (__instance.PNINKKAAPBD[DLMFHPGACEP] != null)
                {
                    __result = CustomCameraPlugin.ourCamera.WorldToScreenPoint(__instance.PNINKKAAPBD[DLMFHPGACEP].transform.position).y;
                }
                
                return false;
            }
        }
        [HarmonyPatch(nameof(OEKDDHPAEIF.BGMAJCJLLPN))]
        [HarmonyPrefix]
        // Token: 0x060001B1 RID: 433 RVA: 0x0009EC0E File Offset: 0x0009CE0E
        public static bool BGMAJCJLLPN(int DLMFHPGACEP, OEKDDHPAEIF __instance, ref float __result)
        {
            if (!CustomCameraPlugin.inFreeCamMode)
                return true;
            else
            {
                __result = 0f;
                if (__instance.PNINKKAAPBD[DLMFHPGACEP] != null)
                {
                    __result = CustomCameraPlugin.ourCamera.WorldToScreenPoint(__instance.PNINKKAAPBD[DLMFHPGACEP].transform.position).z;
                }
                
                return false;
            }
        }
    }
    public class CustomCamera : MonoBehaviour
    {
        internal void Update()
        {
            if (CustomCameraPlugin.inFreeCamMode)
            {
                Transform transform = this.transform;

                CustomCameraPlugin.currentUserCameraPosition = transform.position;
                CustomCameraPlugin.currentUserCameraRotation = transform.rotation;
                if(Input.GetKeyDown(CustomCameraPlugin.configLock.Value))
                {
                    CustomCameraPlugin.cameraLocked = !CustomCameraPlugin.cameraLocked;
                }
                if (!CustomCameraPlugin.cameraLocked)
                {
                    float moveSpeed = (float)CustomCameraPlugin.configCameraMoveSpeed.Value * Time.deltaTime;
                    if (Input.GetKey(CustomCameraPlugin.configSpeed.Value))
                        moveSpeed *= 10f;

                    if (Input.GetKey(CustomCameraPlugin.configLeft.Value))
                        transform.position += transform.right * -1 * moveSpeed;

                    if (Input.GetKey(CustomCameraPlugin.configRight.Value))
                        transform.position += transform.right * moveSpeed;

                    if (Input.GetKey(CustomCameraPlugin.configForwards.Value))
                        transform.position += transform.forward * moveSpeed;

                    if (Input.GetKey(CustomCameraPlugin.configBackwards.Value))
                        transform.position += transform.forward * -1 * moveSpeed;

                    if (Input.GetKey(CustomCameraPlugin.configUp.Value))
                        transform.position += transform.up * moveSpeed;

                    if (Input.GetKey(CustomCameraPlugin.configDown.Value))
                        transform.position += transform.up * -1 * moveSpeed;

                    if (Input.GetMouseButton(1))
                    {
                        Vector3 mouseDelta = Input.mousePosition - CustomCameraPlugin.previousMousePosition;

                        float newRotationX = transform.localEulerAngles.y + mouseDelta.x * 0.3f;
                        float newRotationY = transform.localEulerAngles.x - mouseDelta.y * 0.3f;
                        transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
                    }

                    CustomCameraPlugin.previousMousePosition = Input.mousePosition;
                }
            }
        }
    }
}