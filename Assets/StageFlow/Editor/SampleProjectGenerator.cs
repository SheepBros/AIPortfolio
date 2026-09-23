using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace StageFlow.Editor
{
    public static class SampleProjectGenerator
    {
        public const string Root = "Assets/StageFlow";
        public const string ScenePath = Root + "/Scenes/StageFlowDemo.unity";

        [MenuItem("Tools/StageFlow/Create Sample Assets")]
        public static void CreateSampleAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before creating sample assets.");
            // 배치 프로세스의 빈 초기 씬만 재사용한다. 사용자 씬은 저장하거나 교체하지 않는다.
            var previous = SceneManager.GetActiveScene();
            var useBatchScene = Application.isBatchMode && SceneManager.sceneCount == 1 &&
                string.IsNullOrEmpty(previous.path) && !previous.isDirty && previous.rootCount == 0;
            if (!useBatchScene)
                for (var i = 0; i < SceneManager.sceneCount; i++)
                    if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                        throw new InvalidOperationException("Save untitled scenes before generating assets. No open scene was changed.");
            foreach (var folder in new[] { "Materials", "Prefabs", "Data", "Scenes" })
                EnsureFolder(Root + "/" + folder);

            var scratch = useBatchScene ? previous :
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scratch);
            try
            {
                var blue = MaterialAt("CubeBlue", new Color(0.12f, 0.55f, 0.95f));
                var orange = MaterialAt("CapsuleOrange", new Color(1f, 0.45f, 0.12f));
                var ground = MaterialAt("Ground", new Color(0.22f, 0.27f, 0.32f));
                var arrival = MaterialAt("Arrival", new Color(0.2f, 0.85f, 0.48f));
                var cube = PrefabAt("CubeEnemy", PrimitiveType.Cube, blue);
                var capsule = PrefabAt("CapsuleEnemy", PrimitiveType.Capsule, orange);
                var scout = EnemyAt("Scout", "scout", "Cube Scout", 2.8f, cube);
                var walker = EnemyAt("Walker", "walker", "Capsule Walker", 1.9f, capsule);
                var stageOne = StageAt("StageOne", "stage-one", "Stage One (5)", scout, 3, walker, 2);
                var stageTwo = StageAt("StageTwo", "stage-two", "Stage Two (8)", walker, 4, scout, 4);

                if (!File.Exists(ScenePath))
                {
                    if (File.Exists(ScenePath + ".meta"))
                        throw new InvalidOperationException("Orphan scene meta found; restore or review it first: " + ScenePath);
                    var floor = Primitive("Floor", PrimitiveType.Cube, ground);
                    floor.transform.position = new Vector3(0, -0.3f, 0);
                    floor.transform.localScale = new Vector3(14, 0.5f, 5);
                    var spawn = new GameObject("Spawn Point").transform;
                    spawn.position = new Vector3(-5, 0.6f, 0);
                    var target = new GameObject("Destination").transform;
                    target.position = new Vector3(5, 0.6f, 0);
                    var marker = Primitive("Arrival Marker", PrimitiveType.Cube, arrival);
                    marker.transform.position = new Vector3(5, 0.03f, 0);
                    marker.transform.localScale = new Vector3(0.3f, 0.08f, 3);
                    var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                    camera.tag = "MainCamera";
                    camera.transform.position = new Vector3(-1, 10, -15);
                    camera.transform.LookAt(new Vector3(-1, 0, 0));
                    var view = camera.GetComponent<Camera>();
                    view.orthographic = true;
                    view.orthographicSize = 7.5f;
                    view.clearFlags = CameraClearFlags.SolidColor;
                    view.backgroundColor = new Color(0.055f, 0.075f, 0.11f);
                    var light = new GameObject("Directional Light", typeof(Light));
                    light.transform.rotation = Quaternion.Euler(50, -30, 0);
                    light.GetComponent<Light>().type = LightType.Directional;
                    light.GetComponent<Light>().intensity = 1.5f;
                    var controller = new GameObject("StageFlow", typeof(StageRunner), typeof(StageFlowPanel));
                    controller.GetComponent<StageRunner>().Configure(new[] { stageOne, stageTwo }, spawn, target);
                    if (!EditorSceneManager.SaveScene(scratch, ScenePath))
                        throw new IOException("Could not save " + ScenePath);
                }
                Debug.Log("StageFlow sample assets ready: " + ScenePath + ". Existing assets were preserved.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (!useBatchScene) EditorSceneManager.CloseScene(scratch, true);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static T Existing<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            if (File.Exists(path) || File.Exists(path + ".meta"))
                throw new InvalidOperationException("Existing or unimported file requires review: " + path);
            return null;
        }

        private static Material MaterialAt(string name, Color color)
        {
            var path = Root + "/Materials/" + name + ".mat";
            var existing = Existing<Material>(path);
            if (existing != null) return existing;
            var pipeline = GraphicsSettings.currentRenderPipeline;
            var shaderName = pipeline == null ? "Standard" :
                pipeline.GetType().Name.Contains("Universal") ? "Universal Render Pipeline/Lit" :
                pipeline.GetType().Name.Contains("HDRender") ? "HDRP/Lit" : null;
            var shader = shaderName == null ? null : Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("No supported shader for current render pipeline.");
            var material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Material material)
        {
            var instance = GameObject.CreatePrimitive(type);
            instance.name = name;
            UnityEngine.Object.DestroyImmediate(instance.GetComponent<Collider>());
            instance.GetComponent<Renderer>().sharedMaterial = material;
            return instance;
        }

        private static GameObject PrefabAt(string name, PrimitiveType type, Material material)
        {
            var path = Root + "/Prefabs/" + name + ".prefab";
            var existing = Existing<GameObject>(path);
            if (existing != null) return existing;
            var instance = Primitive(name, type, material);
            try
            {
                if (type == PrimitiveType.Capsule) instance.transform.localScale = new Vector3(0.7f, 0.6f, 0.7f);
                instance.AddComponent<EnemyMover>();
                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
                if (prefab == null) throw new IOException("Could not save " + path);
                return prefab;
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static EnemyDefinition EnemyAt(string name, string id, string label, float speed, GameObject prefab)
        {
            var path = Root + "/Data/" + name + ".asset";
            var existing = Existing<EnemyDefinition>(path);
            if (existing != null) return existing;
            var data = ScriptableObject.CreateInstance<EnemyDefinition>();
            data.Id = id; data.DisplayName = label; data.MoveSpeed = speed; data.Prefab = prefab;
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static StageDefinition StageAt(string name, string id, string label,
            EnemyDefinition first, int firstCount, EnemyDefinition second, int secondCount)
        {
            var path = Root + "/Data/" + name + ".asset";
            var existing = Existing<StageDefinition>(path);
            if (existing != null) return existing;
            var data = ScriptableObject.CreateInstance<StageDefinition>();
            data.Id = id; data.DisplayName = label; data.SpawnInterval = 0.6f;
            data.Entries = new[] { new SpawnEntry { Enemy = first, Count = firstCount },
                new SpawnEntry { Enemy = second, Count = secondCount } };
            AssetDatabase.CreateAsset(data, path);
            return data;
        }
    }
}
