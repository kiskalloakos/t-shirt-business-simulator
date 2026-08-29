using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class SimulatorProjectSetup
{
    private const string PipelinePath = "Assets/Settings/SimulatorURP.asset";
    private const string ScenePath = "Assets/Scenes/GarageDay1.unity";
    private const string OldScenePath = "Assets/Scenes/ShopFloor.unity";
    private const string MaterialsPath = "Assets/Materials";

    private static readonly Dictionary<string, Material> Materials = new();

    [MenuItem("Simulator/Build Playable Day 1 Garage")]
    public static void SetUp()
    {
        ConfigureUrp();
        CreateGarageDay1();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(OldScenePath) != null)
            AssetDatabase.DeleteAsset(OldScenePath);

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Day 1 garage is ready. Press Play to test the complete order loop.");
    }

    private static void ConfigureUrp()
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

        pipeline.shadowDistance = 35f;
        pipeline.msaaSampleCount = 2;
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        AssetDatabase.SaveAssets();
    }

    private static void CreateGarageDay1()
    {
        Materials.Clear();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var world = new GameObject("Garage Workshop");

        BuildGarageShell(world.transform);
        BuildOrderDesk(world.transform);
        BuildShirtStorage(world.transform);
        BuildScreenSetup(world.transform);
        BuildSubmissionDesk(world.transform);
        BuildPress(world.transform);
        BuildLighting(world.transform);
        BuildPlayer();
        new GameObject("Day 1 Game").AddComponent<Day1Game>();

        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static void BuildGarageShell(Transform parent)
    {
        Material concrete = GetMaterial("Concrete", new Color(0.23f, 0.25f, 0.27f), 0f, 0.18f);
        Material darkMetal = GetMaterial("DarkMetal", new Color(0.075f, 0.10f, 0.13f), 0.65f, 0.34f);
        Material steel = GetMaterial("Steel", new Color(0.28f, 0.32f, 0.35f), 0.75f, 0.4f);
        Material warmWood = GetMaterial("WarmWood", new Color(0.34f, 0.19f, 0.085f), 0f, 0.22f);

        CreateBox("Concrete Floor", new Vector3(0, -0.15f, 0), new Vector3(14, 0.3f, 11), concrete, parent);
        CreateBox("Back Corrugated Wall", new Vector3(0, 3f, 5.35f), new Vector3(14, 6f, 0.25f), darkMetal, parent);
        CreateBox("Left Corrugated Wall", new Vector3(-6.9f, 3f, 0), new Vector3(0.25f, 6f, 11), darkMetal, parent);
        CreateBox("Right Corrugated Wall", new Vector3(6.9f, 3f, 0), new Vector3(0.25f, 6f, 11), darkMetal, parent);
        CreateBox("Front Beam", new Vector3(0, 5.5f, -5.35f), new Vector3(14, 0.25f, 0.25f), steel, parent);
        CreateBox("Back Beam", new Vector3(0, 5.5f, 5.1f), new Vector3(14, 0.25f, 0.25f), steel, parent);
        CreateBox("Left Roof Beam", new Vector3(-4.4f, 5.5f, 0), new Vector3(0.2f, 0.2f, 10.4f), steel, parent);
        CreateBox("Right Roof Beam", new Vector3(4.4f, 5.5f, 0), new Vector3(0.2f, 0.2f, 10.4f), steel, parent);
        CreateBox("Garage Door", new Vector3(0, 2.5f, 5.17f), new Vector3(5.8f, 4.8f, 0.12f), steel, parent);
        CreateBox("Welcome Mat", new Vector3(0, 0.025f, -4.65f), new Vector3(2.2f, 0.05f, 1.0f), warmWood, parent);
    }

    private static void BuildOrderDesk(Transform parent)
    {
        var desk = new GameObject("Order Desk");
        desk.transform.SetParent(parent);
        Material wood = GetMaterial("Worktop", new Color(0.40f, 0.25f, 0.12f), 0f, 0.24f);
        Material cream = GetMaterial("Paper", new Color(0.92f, 0.86f, 0.68f), 0f, 0.1f);
        Material steel = GetMaterial("Steel", new Color(0.28f, 0.32f, 0.35f), 0.75f, 0.4f);

        CreateTable(Vector3.zero, desk.transform, wood, steel);
        GameObject clipboard = CreateBox("Day 1 Order Clipboard", new Vector3(0, 1.08f, 0), new Vector3(0.55f, 0.04f, 0.75f), cream, desk.transform);
        clipboard.AddComponent<WorkflowStation>().Configure(WorkflowStation.StationType.OrderClipboard);
        desk.transform.position = new Vector3(-4.8f, 0, -3.6f);
        CreateSign("ORDERS", new Vector3(0, 2.1f, 0), Quaternion.identity, desk.transform);
    }

    private static void BuildShirtStorage(Transform parent)
    {
        var storage = new GameObject("T-Shirt Storage");
        storage.transform.SetParent(parent);
        storage.transform.position = new Vector3(5.5f, 0, 1.4f);
        Material shelf = GetMaterial("ShelfBlue", new Color(0.08f, 0.26f, 0.48f), 0.35f, 0.32f);
        Material shirt = GetMaterial("NavyShirt", new Color(0.035f, 0.09f, 0.18f), 0f, 0.25f);

        CreateBox("Left Post", new Vector3(-0.85f, 1.45f, 0), new Vector3(0.12f, 2.9f, 0.75f), shelf, storage.transform);
        CreateBox("Right Post", new Vector3(0.85f, 1.45f, 0), new Vector3(0.12f, 2.9f, 0.75f), shelf, storage.transform);
        for (int i = 0; i < 4; i++)
            CreateBox($"Shelf {i + 1}", new Vector3(0, 0.25f + i * 0.82f, 0), new Vector3(1.8f, 0.1f, 0.85f), shelf, storage.transform);
        for (int i = 0; i < 3; i++)
            CreateBox($"Folded Shirt {i + 1}", new Vector3(0, 0.39f + i * 0.82f, -0.02f), new Vector3(1.15f, 0.16f, 0.62f), shirt, storage.transform);

        storage.AddComponent<WorkflowStation>().Configure(WorkflowStation.StationType.ShirtStorage);
        CreateSign("SHIRTS", new Vector3(0, 3.45f, 0), Quaternion.Euler(0, -90, 0), storage.transform);
    }

    private static void BuildScreenSetup(Transform parent)
    {
        var setup = new GameObject("Screen and Ink Setup");
        setup.transform.SetParent(parent);
        setup.transform.position = new Vector3(-5.1f, 0, 2.5f);
        Material wood = GetMaterial("Worktop", new Color(0.40f, 0.25f, 0.12f), 0f, 0.24f);
        Material steel = GetMaterial("Steel", new Color(0.28f, 0.32f, 0.35f), 0.75f, 0.4f);
        Material ink = GetMaterial("CreamInk", new Color(0.94f, 0.82f, 0.48f), 0f, 0.55f);
        Material red = GetMaterial("MachineRed", new Color(0.52f, 0.065f, 0.04f), 0.62f, 0.35f);

        CreateTable(Vector3.zero, setup.transform, wood, steel);
        var kit = new GameObject("Prepared Order Screen Kit");
        kit.transform.SetParent(setup.transform, false);
        CreateCylinder("Cream Ink Container", new Vector3(-0.58f, 1.24f, 0), new Vector3(0.32f, 0.24f, 0.32f), ink, kit.transform);
        CreateBox("Ready Red Screen", new Vector3(0.42f, 1.24f, 0), new Vector3(1.45f, 0.10f, 0.92f), red, kit.transform);
        CreateBox("Order Stencil", new Vector3(0.42f, 1.305f, 0), new Vector3(0.62f, 0.025f, 0.42f), ink, kit.transform);
        setup.AddComponent<WorkflowStation>().Configure(WorkflowStation.StationType.ScreenSetup, kit);
        CreateSign("SCREEN + INK", new Vector3(0, 2.15f, 0), Quaternion.identity, setup.transform);
    }

    private static void BuildSubmissionDesk(Transform parent)
    {
        var desk = new GameObject("Submission Desk");
        desk.transform.SetParent(parent);
        desk.transform.position = new Vector3(4.6f, 0, -3.6f);
        Material wood = GetMaterial("Worktop", new Color(0.40f, 0.25f, 0.12f), 0f, 0.24f);
        Material steel = GetMaterial("Steel", new Color(0.28f, 0.32f, 0.35f), 0.75f, 0.4f);
        CreateTable(Vector3.zero, desk.transform, wood, steel);
        desk.AddComponent<WorkflowStation>().Configure(WorkflowStation.StationType.SubmissionDesk);
        CreateSign("FINISHED", new Vector3(0, 2.15f, 0), Quaternion.identity, desk.transform);
    }

    private static void BuildPress(Transform parent)
    {
        var press = new GameObject("Manual Screen-Printing Press");
        press.transform.SetParent(parent);
        press.transform.position = new Vector3(0, 0, 1.0f);
        Material machine = GetMaterial("MachineRed", new Color(0.52f, 0.065f, 0.04f), 0.62f, 0.35f);
        Material dark = GetMaterial("MachineDark", new Color(0.04f, 0.045f, 0.055f), 0.55f, 0.28f);
        Material platen = GetMaterial("Platen", new Color(0.68f, 0.58f, 0.38f), 0f, 0.22f);
        Material shirtMat = GetMaterial("NavyShirt", new Color(0.035f, 0.09f, 0.18f), 0f, 0.25f);
        Material ink = GetMaterial("CreamInk", new Color(0.94f, 0.82f, 0.48f), 0f, 0.55f);
        Material screenMat = GetTransparentMaterial("ScreenMeshTransparent", new Color(0.05f, 0.48f, 0.62f, 0.27f));
        Material skin = GetMaterial("Hands", new Color(0.74f, 0.45f, 0.31f), 0f, 0.35f);

        CreateBox("Base", new Vector3(0, 0.18f, 0), new Vector3(2.3f, 0.35f, 2.0f), machine, press.transform);
        CreateCylinder("Center Post", new Vector3(0, 0.72f, 0.65f), new Vector3(0.25f, 0.65f, 0.25f), machine, press.transform);
        CreateBox("Arm", new Vector3(0, 1.22f, 0), new Vector3(0.3f, 0.22f, 1.5f), dark, press.transform);
        CreateBox("Platen", new Vector3(0, 1.02f, -0.95f), new Vector3(2.05f, 0.13f, 2.25f), platen, press.transform);

        var shirt = new GameObject("Shirt on Platen");
        shirt.transform.SetParent(press.transform, false);
        shirt.transform.localPosition = new Vector3(0, 1.105f, -0.95f);
        CreateBox("Shirt Body", Vector3.zero, new Vector3(1.55f, 0.035f, 1.85f), shirtMat, shirt.transform);
        CreateBox("Left Sleeve", new Vector3(-0.98f, 0, 0.46f), new Vector3(0.55f, 0.035f, 0.72f), shirtMat, shirt.transform);
        CreateBox("Right Sleeve", new Vector3(0.98f, 0, 0.46f), new Vector3(0.55f, 0.035f, 0.72f), shirtMat, shirt.transform);
        GameObject print = CreateBox("Printed Design", new Vector3(0, 0.028f, -0.15f), new Vector3(0.62f, 0.025f, 0.42f), ink, shirt.transform);

        var frame = new GameObject("Movable Screen Frame");
        frame.transform.SetParent(press.transform);
        frame.transform.localPosition = new Vector3(0, 1.28f, -0.95f);
        CreateBox("Frame Top", new Vector3(0, 0, 0.78f), new Vector3(2.15f, 0.10f, 0.12f), machine, frame.transform);
        CreateBox("Frame Bottom", new Vector3(0, 0, -0.78f), new Vector3(2.15f, 0.10f, 0.12f), machine, frame.transform);
        CreateBox("Frame Left", new Vector3(-1.02f, 0, 0), new Vector3(0.12f, 0.10f, 1.65f), machine, frame.transform);
        CreateBox("Frame Right", new Vector3(1.02f, 0, 0), new Vector3(0.12f, 0.10f, 1.65f), machine, frame.transform);
        GameObject mesh = CreateBox("Transparent Screen Mesh", Vector3.zero, new Vector3(1.92f, 0.018f, 1.42f), screenMat, frame.transform);
        CreateBox("Order Stencil Visible In Mesh", new Vector3(0, 0.018f, -0.10f), new Vector3(0.62f, 0.012f, 0.42f), ink, frame.transform);
        GameObject inkPass = CreateBox("Ink Spreading Under Squeegee", new Vector3(0, 0.045f, 0.43f), new Vector3(1.72f, 0.018f, 0.015f), ink, frame.transform);

        var toolRig = new GameObject("Squeegee and Hands");
        toolRig.transform.SetParent(frame.transform, false);
        toolRig.transform.localPosition = new Vector3(0, 0.12f, 0.43f);
        CreateBox("Squeegee Blade", Vector3.zero, new Vector3(1.45f, 0.16f, 0.16f), dark, toolRig.transform);
        CreateBox("Squeegee Handle", new Vector3(0, 0.20f, 0), new Vector3(1.25f, 0.25f, 0.22f), material: machine, parent: toolRig.transform);
        CreateSphere("Left Hand", new Vector3(-0.42f, 0.34f, 0), new Vector3(0.22f, 0.18f, 0.24f), skin, toolRig.transform);
        CreateSphere("Right Hand", new Vector3(0.42f, 0.34f, 0), new Vector3(0.22f, 0.18f, 0.24f), skin, toolRig.transform);
        var focusPose = new GameObject("Press Camera Pose").transform;
        focusPose.SetParent(press.transform);
        focusPose.localPosition = new Vector3(0f, 3.25f, -3.30f);
        focusPose.LookAt(press.transform.TransformPoint(new Vector3(0f, 1.12f, -0.82f)));

        var station = press.AddComponent<ScreenPrintStation>();
        station.Configure(frame.transform, toolRig.transform, focusPose, inkPass.transform,
            mesh.GetComponent<Renderer>(), shirt, print.GetComponent<Renderer>());
        CreateSign("PRESS", new Vector3(0, 2.35f, 0.75f), Quaternion.identity, press.transform);
    }

    private static void BuildPlayer()
    {
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0, 0.05f, -4f);
        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.32f;
        controller.center = new Vector3(0, 0.9f, 0);

        var cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(player.transform);
        cameraObject.transform.localPosition = new Vector3(0, 1.62f, 0);
        var camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.fieldOfView = 70f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.045f, 0.065f, 0.085f);
        cameraObject.AddComponent<AudioListener>();

        player.AddComponent<FirstPersonController>();
        player.AddComponent<PlayerInteractor>();
    }

    private static void BuildLighting(Transform parent)
    {
        var sun = new GameObject("Soft Daylight").AddComponent<Light>();
        sun.transform.SetParent(parent);
        sun.type = LightType.Directional;
        sun.intensity = 1.15f;
        sun.color = new Color(0.82f, 0.9f, 1f);
        sun.transform.rotation = Quaternion.Euler(48f, -28f, 0);
        sun.shadows = LightShadows.Soft;

        for (int i = -1; i <= 1; i++)
        {
            var lamp = new GameObject($"Garage Lamp {i + 2}").AddComponent<Light>();
            lamp.transform.SetParent(parent);
            lamp.type = LightType.Point;
            lamp.range = 8f;
            lamp.intensity = 5.5f;
            lamp.color = new Color(1f, 0.76f, 0.48f);
            lamp.transform.position = new Vector3(i * 4f, 4.8f, 0);
            lamp.shadows = LightShadows.None;
        }
    }

    private static void CreateTable(Vector3 position, Transform parent, Material top, Material legs)
    {
        CreateBox("Tabletop", position + new Vector3(0, 0.95f, 0), new Vector3(2.4f, 0.18f, 1.15f), top, parent);
        CreateBox("Leg FL", position + new Vector3(-1f, 0.45f, -0.42f), new Vector3(0.12f, 0.9f, 0.12f), legs, parent);
        CreateBox("Leg FR", position + new Vector3(1f, 0.45f, -0.42f), new Vector3(0.12f, 0.9f, 0.12f), legs, parent);
        CreateBox("Leg BL", position + new Vector3(-1f, 0.45f, 0.42f), new Vector3(0.12f, 0.9f, 0.12f), legs, parent);
        CreateBox("Leg BR", position + new Vector3(1f, 0.45f, 0.42f), new Vector3(0.12f, 0.9f, 0.12f), legs, parent);
    }

    private static GameObject CreateBox(string name, Vector3 localPosition, Vector3 scale, Material material, Transform parent)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = localPosition;
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        return item;
    }

    private static GameObject CreateCylinder(string name, Vector3 localPosition, Vector3 scale, Material material, Transform parent)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = localPosition;
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        return item;
    }

    private static GameObject CreateSphere(string name, Vector3 localPosition, Vector3 scale, Material material, Transform parent)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = localPosition;
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        return item;
    }

    private static void CreateSign(string text, Vector3 localPosition, Quaternion localRotation, Transform parent)
    {
        var sign = new GameObject($"{text} Sign");
        sign.transform.SetParent(parent, false);
        sign.transform.localPosition = localPosition;
        sign.transform.localRotation = localRotation;
        var mesh = sign.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = 48;
        mesh.characterSize = 0.08f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = new Color(1f, 0.78f, 0.28f);
    }

    private static Material GetMaterial(string name, Color color, float metallic, float smoothness)
    {
        if (Materials.TryGetValue(name, out Material cached))
            return cached;

        string path = $"{MaterialsPath}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
        }

        Materials[name] = material;
        return material;
    }

    private static Material GetTransparentMaterial(string name, Color color)
    {
        Material material = GetMaterial(name, color, 0f, 0.28f);
        material.color = color;
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetColor("_BaseColor", color);
        material.SetFloat("_AlphaClip", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }
}
