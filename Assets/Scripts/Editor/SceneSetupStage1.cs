using Game.Core;
using Game.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.EditorTools
{
    public static class SceneSetupStage1
    {
        private const string ScenePath = "Assets/Scenes/Stage1.unity";
        private const string MaterialFolder = "Assets/Materials";
        private const string GroundMaterialPath = MaterialFolder + "/Ground.mat";
        private const string SkyProfilePath = "Assets/Settings/SkyandFogSettingsProfile.asset";

        [MenuItem("Game/Setup Stage1 Scene")]
        public static void Setup()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var sun = SetupSun();
            SetupSkyVolume();
            var ground = SetupGround();

            var player = SetupPlayer();
            var camera = player.GetComponentInChildren<Camera>();
            var sunDriver = ground.AddComponent<SunLightDriver>();
            sunDriver.SetSun(sun);

            var hudGo = new GameObject("DebugTimeHud");
            hudGo.AddComponent<DebugTimeHud>();

            var gm = new GameObject("GameManager");
            gm.AddComponent<WorldTime>();
            gm.AddComponent<GameManager>();

            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"Stage1 scene saved to {ScenePath}. Sun: {sun.name}, Camera: {camera.name}");
        }

        private static Light SetupSun()
        {
            var light = Object.FindFirstObjectByType<Light>();
            if (light == null)
            {
                var go = new GameObject("Directional Light");
                light = go.AddComponent<Light>();
            }

            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.9568f, 0.8392f);
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var hdLight = light.GetComponent<HDAdditionalLightData>();
            if (hdLight == null)
                hdLight = light.gameObject.AddComponent<HDAdditionalLightData>();

            hdLight.lightUnit = LightUnit.Lux;
            hdLight.intensity = 130000f;

            light.gameObject.name = "Sun";
            return light;
        }

        private static void SetupSkyVolume()
        {
            var volumeGo = new GameObject("Global Volume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SkyProfilePath);
            if (profile != null)
            {
                volume.sharedProfile = profile;
            }
            else
            {
                Debug.LogError($"Sky profile not found at {SkyProfilePath}. Assign a Volume profile manually.");
            }
        }

        private static GameObject SetupGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(50f, 1f, 50f);

            var material = LoadOrCreateGroundMaterial();
            ground.GetComponent<MeshRenderer>().sharedMaterial = material;
            return ground;
        }

        private static Material LoadOrCreateGroundMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            if (existing != null)
                return existing;

            var shader = Shader.Find("HDRP/Lit");
            if (shader == null)
            {
                Debug.LogWarning("HDRP/Lit shader not found, using default material.");
                return new Material(Shader.Find("Standard"));
            }

            var material = new Material(shader);
            material.SetColor("_BaseColor", new Color(0.28f, 0.42f, 0.2f));

            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder("Assets", "Materials");

            AssetDatabase.CreateAsset(material, GroundMaterialPath);
            return material;
        }

        private static GameObject SetupPlayer()
        {
            var player = new GameObject("Player");

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            player.transform.position = new Vector3(0f, 1.1f, 0f);

            var cameraHolder = new GameObject("CameraHolder");
            cameraHolder.transform.SetParent(player.transform, false);
            cameraHolder.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            var mainCamera = Camera.main;
            GameObject cameraGo;
            if (mainCamera != null)
            {
                cameraGo = mainCamera.gameObject;
            }
            else
            {
                cameraGo = new GameObject("Main Camera", typeof(Camera));
                cameraGo.tag = "MainCamera";
            }

            cameraGo.transform.SetParent(cameraHolder.transform, false);
            cameraGo.transform.localPosition = Vector3.zero;
            cameraGo.transform.localRotation = Quaternion.identity;

            var camera = cameraGo.GetComponent<Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;

            if (cameraGo.GetComponent<AudioListener>() == null)
                cameraGo.AddComponent<AudioListener>();

            if (cameraGo.GetComponent<HDAdditionalCameraData>() == null)
                cameraGo.AddComponent<HDAdditionalCameraData>();

            player.AddComponent<PlayerController>();
            return player;
        }
    }
}
