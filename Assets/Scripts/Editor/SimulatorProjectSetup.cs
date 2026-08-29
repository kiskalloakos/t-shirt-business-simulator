using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class SimulatorProjectSetup
{
    private const string PipelinePath = "Assets/Settings/SimulatorURP.asset";
    private const string ScenePath = "Assets/Scenes/ShopFloor.unity";

    [MenuItem("Simulator/Set Up URP and Shop Floor")]
    public static void SetUp()
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (pipeline == null)
        {
            pipeline = UniversalRenderPipelineAsset.Create();
            AssetDatabase.CreateAsset(pipeline, PipelinePath);
            var renderer = pipeline.rendererDataList[0];
            if (renderer != null)
                AssetDatabase.AddObjectToAsset(renderer, pipeline);
        }

        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        AssetDatabase.SaveAssets();

        CreateShopFloor();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Simulator setup complete: URP is active and ShopFloor.unity is ready.");
    }

    private static void CreateShopFloor()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var world = new GameObject("T-Shirt Business Simulator");
        CreateBox("Floor", new Vector3(0, -0.25f, 0), new Vector3(18, 0.5f, 14), new Color(0.08f, 0.11f, 0.16f), world.transform);
        CreateBox("BackWall", new Vector3(0, 3.5f, 5.5f), new Vector3(18, 7, 0.4f), new Color(0.12f, 0.17f, 0.23f), world.transform);

        var display = new GameObject("T-Shirt Display");
        display.transform.SetParent(world.transform);
        CreateCylinder("Pedestal", new Vector3(0, 0.5f, 0), new Vector3(2.2f, 1f, 2.2f), new Color(0.16f, 0.22f, 0.31f), display.transform);
        CreateBox("T-Shirt Body", new Vector3(0, 2.65f, 0), new Vector3(2.5f, 2.8f, 0.35f), new Color(0.13f, 0.72f, 0.9f), display.transform);
        CreateBox("Left Sleeve", new Vector3(-1.65f, 3.25f, 0), new Vector3(1.0f, 1.1f, 0.35f), new Color(0.13f, 0.72f, 0.9f), display.transform, new Vector3(0, 0, 28));
        CreateBox("Right Sleeve", new Vector3(1.65f, 3.25f, 0), new Vector3(1.0f, 1.1f, 0.35f), new Color(0.13f, 0.72f, 0.9f), display.transform, new Vector3(0, 0, -28));
        CreateCylinder("Neck", new Vector3(0, 3.85f, -0.2f), new Vector3(0.75f, 0.15f, 0.75f), new Color(0.04f, 0.09f, 0.13f), display.transform);

        var keyLight = new GameObject("Key Light").AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.intensity = 1.6f;
        keyLight.color = new Color(0.8f, 0.92f, 1f);
        keyLight.transform.rotation = Quaternion.Euler(45, -30, 0);

        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(8f, 5.5f, -10f);
        camera.transform.LookAt(new Vector3(0, 2.2f, 0));
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.04f, 0.07f);

        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static void CreateBox(string name, Vector3 position, Vector3 scale, Color color, Transform parent, Vector3 rotation = default)
    {
        var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = name;
        item.transform.SetParent(parent);
        item.transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
        item.transform.localScale = scale;
        Paint(item, color);
    }

    private static void CreateCylinder(string name, Vector3 position, Vector3 scale, Color color, Transform parent)
    {
        var item = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        item.name = name;
        item.transform.SetParent(parent);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        Paint(item, color);
    }

    private static void Paint(GameObject item, Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        item.GetComponent<Renderer>().sharedMaterial = material;
    }
}
