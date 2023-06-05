//Game version: 1.58
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;

namespace WEFreeCamera
{
    [BepInPlugin(PluginGuid, PluginName, PluginVer)]
    [HarmonyPatch]
    public class FreeCameraPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "GeeEM.WrestlingEmpire.WEFreeCamera";
        public const string PluginName = "WEFreeCamera";
        public const string PluginVer = "1.3.0";

        internal static ManualLogSource Log;
        internal readonly static Harmony Harmony = new(PluginGuid);

        internal static string PluginPath;
        internal static bool inFreeCamMode = false;
        internal static bool cameraLocked = false;
        internal static Camera ourCamera;
        internal static Camera currentMain;
        internal static Camera CAC = null;
        public static GameObject trackingAction = null;
        internal static List<MonoBehaviour> CACBehaviours = new List<MonoBehaviour>();
        internal static Vector3? currentUserCameraPosition;
        internal static Quaternion? currentUserCameraRotation;
        public static ConfigEntry<double> configCameraMoveSpeed;
        internal static Vector3 previousMousePosition;
        internal static CustomCamera freeCamScript;
        public static List<string> movementModes = new List<string>() { "Relative", "Static" };
        public static int currentMode = 0;
        public static ConfigEntry<KeyCode> configToggle;
        public static ConfigEntry<KeyCode> configLock;
        public static ConfigEntry<KeyCode> configSpeed;
        public static ConfigEntry<KeyCode> configMode;
        public static ConfigEntry<KeyCode> configLeft;
        public static ConfigEntry<KeyCode> configRight;
        public static ConfigEntry<KeyCode> configForwards;
        public static ConfigEntry<KeyCode> configBackwards;
        public static ConfigEntry<KeyCode> configUp;
        public static ConfigEntry<KeyCode> configDown;
        public static ConfigEntry<KeyCode> configTargeting;
        public static ConfigEntry<KeyCode> configIncreaseFoV;
        public static ConfigEntry<KeyCode> configDecreaseFoV;
        public static ConfigEntry<Vector3>[] savedPositions = new ConfigEntry<Vector3>[10];
        public static ConfigEntry<Quaternion>[] savedRotations = new ConfigEntry<Quaternion>[10];
        public static ConfigEntry<float>[] savedFoVs = new ConfigEntry<float>[10];

        private void Awake()
        {
            FreeCameraPlugin.Log = base.Logger;
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
            configMode = Config.Bind("Controls",
                 "MovementMode",
                 KeyCode.I,
                 "Toggle between camera movement modes");
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
            configTargeting = Config.Bind("Controls",
                 "TargetingMode",
                 KeyCode.Q,
                 "Sets the camera to target the game action. May not work if the main game camera is set to first person");
            configIncreaseFoV = Config.Bind("Controls",
                 "IncreaseFoV",
                 KeyCode.LeftBracket,
                 "Increases the free camera field of view");
            configDecreaseFoV = Config.Bind("Controls",
                 "DecreaseFoV",
                 KeyCode.RightBracket,
                 "Decreases the free camera field of view");
            SetPositions();
            Config.SaveOnConfigSet = true;
        }
        private void SetPositions()
        {
            for (int i = 0; i < 10; i++)
            {
                savedPositions[i] = Config.Bind("Positions", "Position" + i, new Vector3(), "Position " + i);
                savedRotations[i] = Config.Bind("Positions", "Rotation" + i, new Quaternion(), "Rotation " + i);
                savedFoVs[i] = Config.Bind("Positions", "FoV" + i, 50f, "FoV " + i);
            }
        }
        public static void SavePosition(int num, Vector3 pos, Quaternion rot, float fov)
        {
            savedPositions[num].Value = pos;
            savedRotations[num].Value = rot;
            savedFoVs[num].Value = fov;
        }
        public static void LoadPosition(int num, out Vector3 pos, out Quaternion rot, out float fov)
        {
            pos = savedPositions[num].Value;
            rot = savedRotations[num].Value;
            fov = savedFoVs[num].Value;
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
        internal void BeginFreecam()
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
        void SetupFreeCamera()
        {
            if (!ourCamera)
            {
                GameObject cam = GameObject.Find("CustomArenaCamera");
                if (!cam)
                {

                    ourCamera = Instantiate(currentMain);
                    ourCamera.name = "Freecam";
                    ourCamera.gameObject.tag = "MainCamera";
                    GameObject.DontDestroyOnLoad(ourCamera.gameObject);
                    ourCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }
                else
                {
                    cam.name = "CAC";
                    CAC = cam.GetComponent<Camera>();
                    ourCamera = Instantiate(CAC);
                    ourCamera.gameObject.name = "Freecam";
                    foreach (MonoBehaviour comp in CAC.GetComponents<MonoBehaviour>())
                    {
                        if (comp.enabled)
                        {
                            CACBehaviours.Add(comp);
                            comp.enabled = false;
                        }
                        CAC.enabled = false;
                    }
                    GameObject.DontDestroyOnLoad(ourCamera.gameObject);
                    ourCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }
            }

            if (!freeCamScript)
            {
                freeCamScript = ourCamera.gameObject.AddComponent<CustomCamera>();
            }


            ourCamera.transform.position = (Vector3)currentUserCameraPosition;
            ourCamera.transform.rotation = (Quaternion)currentUserCameraRotation;

            ourCamera.gameObject.SetActive(true);
            ourCamera.enabled = true;
            currentMain.GetComponent<AudioListener>().enabled = false;
            currentMain.GetComponent<Camera>().enabled = false;
        }
        internal static void EndFreecam()
        {
            inFreeCamMode = false;
            trackingAction = null;

            if (ourCamera)
            {
                GameObject.Destroy(ourCamera.gameObject);
                ourCamera = null;
            }
            if (freeCamScript)
            {
                GameObject.Destroy(freeCamScript);
                freeCamScript = null;
            }
            if (CAC)
            {
                CAC.gameObject.name = "CustomArenaCamera";
                CAC.enabled = true;
                foreach (MonoBehaviour comp in CACBehaviours)
                {
                    comp.enabled = true;
                }
            }
            CACBehaviours.Clear();
            if (currentMain)
            {
                currentMain.GetComponent<AudioListener>().enabled = true;
                currentMain.GetComponent<Camera>().enabled = true;
            }
        }

        private void Update()
        {
            if(Input.GetKeyDown(configMode.Value))
            {
                currentMode++;
                if (currentMode == movementModes.Count)
                    currentMode = 0;
            }
            if (Input.GetKeyDown(configTargeting.Value))
            {
                if (!trackingAction)
                {
                    trackingAction = GameObject.Find("Camera Focal Point");
                }
                else
                {
                    trackingAction = null;
                }
            }
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
    //Disabling the camera during scene switch
    [HarmonyPatch(typeof(DNDIEGNJOKN))]
    public static class DNDIEGNJOKN_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DNDIEGNJOKN.KGAMHBKDPCB))]
        public static void Prefix()
        {
            if (FreeCameraPlugin.inFreeCamMode)
            {
                FreeCameraPlugin.EndFreecam();
            }
        }
    }
    //Patching character movement controls
    [HarmonyPatch(typeof(KDOHFMKNHOB), nameof(KDOHFMKNHOB.CLPAMGDJAKN))]
    public static class KDOHFMKNHOB_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(KDOHFMKNHOB __instance, ref float __result)
        {
            if (!FreeCameraPlugin.inFreeCamMode)
                return true;
            else
            {
                __result = FreeCameraPlugin.ourCamera.transform.eulerAngles.y + Mathf.Atan2(__instance.IKGCLCIAOOL, __instance.ONCJKLOEGEF) * 57.29578f;
                return false;
            }
        }
    }
    //Patching match setup
    [HarmonyPatch(typeof(Scene_Match_Setup), nameof(Scene_Match_Setup.AdjustCamera))]
    public static class Scene_Match_Setup_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (FreeCameraPlugin.inFreeCamMode)
            {
                FreeCameraPlugin.ourCamera.transform.position = (Vector3)FreeCameraPlugin.currentUserCameraPosition;
                FreeCameraPlugin.ourCamera.transform.rotation = (Quaternion)FreeCameraPlugin.currentUserCameraRotation;
            }
        }
    }
    public class CustomCamera : MonoBehaviour
    {
        public Camera camera;
        private void Start()
        {
            camera = this.GetComponent<Camera>();
        }

        internal void Update()
        {
            if (FreeCameraPlugin.inFreeCamMode)
            {
                HandlePositionSaveLoad();
                if (Input.GetKey(FreeCameraPlugin.configIncreaseFoV.Value))
                {
                    camera.fieldOfView += 0.5f;
                }
                if (Input.GetKey(FreeCameraPlugin.configDecreaseFoV.Value))
                {
                    camera.fieldOfView -= 0.5f;
                }
                if (Input.GetKeyDown(FreeCameraPlugin.configLock.Value))
                {
                    FreeCameraPlugin.cameraLocked = !FreeCameraPlugin.cameraLocked;
                }
                if (FreeCameraPlugin.trackingAction)
                {
                    transform.LookAt(FreeCameraPlugin.trackingAction.transform);
                }
                if (!FreeCameraPlugin.cameraLocked)
                {
                    float moveSpeed = (float)FreeCameraPlugin.configCameraMoveSpeed.Value * Time.deltaTime;
                    if (Input.GetKey(FreeCameraPlugin.configSpeed.Value))
                        moveSpeed *= 10f;
                    switch (FreeCameraPlugin.movementModes[FreeCameraPlugin.currentMode])
                    {
                        case "Relative":
                            HandleMovementModeRelative(moveSpeed);
                            break;
                        case "Static":
                            HandleMovementModeStatic(moveSpeed);
                            break;
                    }
                    FreeCameraPlugin.currentUserCameraPosition = transform.position;
                    if (Input.GetMouseButton(1))
                    {
                        Vector3 mouseDelta = Input.mousePosition - FreeCameraPlugin.previousMousePosition;

                        float newRotationX = transform.localEulerAngles.y + mouseDelta.x * 0.3f;
                        float newRotationY = transform.localEulerAngles.x - mouseDelta.y * 0.3f;
                        transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);

                        FreeCameraPlugin.currentUserCameraRotation = transform.rotation;
                    }

                    FreeCameraPlugin.previousMousePosition = Input.mousePosition;
                }
            }
        }
        private void HandleMovementModeStatic(float moveSpeed)
        {
            if (Input.GetKey(FreeCameraPlugin.configLeft.Value))
            {
                transform.position += transform.right * -1 * moveSpeed;
            }

            if (Input.GetKey(FreeCameraPlugin.configRight.Value))
            {
                transform.position += transform.right * moveSpeed;
            }

            if (Input.GetKey(FreeCameraPlugin.configForwards.Value))
            {
                transform.position += Vector3.Normalize(new Vector3(transform.forward.x, 0, transform.forward.z)) * moveSpeed;
            }

            if (Input.GetKey(FreeCameraPlugin.configBackwards.Value))
            {
                transform.position += Vector3.Normalize(new Vector3(transform.forward.x, 0, transform.forward.z)) * -1 * moveSpeed;
            }

            if (Input.GetKey(FreeCameraPlugin.configUp.Value))
            {
                transform.position += Vector3.Normalize(new Vector3(0, 1, 0)) * moveSpeed;
            }

            if (Input.GetKey(FreeCameraPlugin.configDown.Value))
            {
                transform.position += Vector3.Normalize(new Vector3(0, 1, 0)) * -1 * moveSpeed;
            }
        }
        private void HandleMovementModeRelative(float moveSpeed)
        {
            if (Input.GetKey(FreeCameraPlugin.configLeft.Value))
            {
                transform.position += transform.right * -1 * moveSpeed;
            }

            if (Input.GetKey(FreeCameraPlugin.configRight.Value))
            {
                transform.position += transform.right * moveSpeed;
            }

            if (Input.GetKey(FreeCameraPlugin.configForwards.Value))
            {
                transform.position += transform.forward * moveSpeed;
            }

            if (Input.GetKey(FreeCameraPlugin.configBackwards.Value))
            {
                transform.position += transform.forward * -1 * moveSpeed;
            }

            if (Input.GetKey(FreeCameraPlugin.configUp.Value))
            {
                transform.position += transform.up * moveSpeed;
            }

            if (Input.GetKey(FreeCameraPlugin.configDown.Value))
            {
                transform.position += transform.up * -1 * moveSpeed;
            }
        }
        private void HandlePositionSaveLoad()
        {
            Vector3 pos;
            Quaternion rot;
            float fov;
            for (int i = 0; i < 10; i++)
            {
                KeyCode keyCode = (KeyCode)System.Enum.Parse(typeof(KeyCode), "Alpha" + i);
                if (Input.GetKey(keyCode) && Input.GetKey(KeyCode.BackQuote))
                {
                    FreeCameraPlugin.SavePosition(i, transform.position, transform.rotation, camera.fieldOfView);
                }
                else if(Input.GetKey(keyCode))
                {
                    FreeCameraPlugin.LoadPosition(i, out pos, out rot, out fov);
                    transform.position = pos;
                    transform.rotation = rot;
                    camera.fieldOfView = fov;
                    FreeCameraPlugin.currentUserCameraPosition = transform.position;
                    FreeCameraPlugin.currentUserCameraRotation = transform.rotation;
                }
            }
        }
    }
}