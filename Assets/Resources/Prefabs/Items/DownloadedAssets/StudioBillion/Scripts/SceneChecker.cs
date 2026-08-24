#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;

public class SceneChecker : EditorWindow
{
    #region Variables
    public enum GameViewSizeType
    {
        AspectRatio, FixedResolution
    }
    #endregion
    #region Defaults
    [InitializeOnLoadMethod]
    public static void Init()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorPrefs.HasKey("SB_Init"))
            {
                Debug.Log("SB Scene Checker Initialized");
                EditorPrefs.SetInt("SB_Init", 1);
                if (EditorUtility.DisplayDialog("Hi there from Studio Billion", "Thank you for purchasing and using our asset. Do not forget to rate us :)", "Rate Us", "OK"))
                    Application.OpenURL("https://assetstore.unity.com/publishers/51907");

                SceneChecker editorWindow = GetWindow<SceneChecker>();
                editorWindow.Close();
                editorWindow.CheckScene();
            }
        };
    }
    void CheckScene()
    {
        CheckProjectColorSpace();
        CheckResolution();
    }
    #endregion
    #region Checkers
    void CheckResolution()
    {
        GameViewSizeGroupType platform = BuildTypeToGroupType();
        if (!SizeExists(platform, "SB_Showcase"))
            AddCustomSize(GameViewSizeType.FixedResolution, platform, 3000, 2000, "SB_Showcase");
        SetSize(FindSize(platform, "SB_Showcase"));
    }
    void CheckProjectColorSpace()
    {
        if (PlayerSettings.colorSpace == ColorSpace.Gamma)
            EditorUtility.DisplayDialog("Incorrect Color Space", "Scene lightning and models' colors are arrenged within Linear Color Space.\nYou are using Gamma. Colors would not probably looks same with the screenshots", "OK");
    }
    #endregion
    #region Reflection
    GameViewSizeGroupType BuildTypeToGroupType()
    {
        switch (EditorUserBuildSettings.activeBuildTarget)
        {
            case BuildTarget.StandaloneWindows:
                return GameViewSizeGroupType.Standalone;
            case BuildTarget.Android:
                return GameViewSizeGroupType.Android;
            case BuildTarget.iOS:
                return GameViewSizeGroupType.iOS;
            default:
                return GameViewSizeGroupType.Standalone;
        }
    }
    void AddCustomSize(GameViewSizeType viewSizeType, GameViewSizeGroupType sizeGroupType, int width, int height, string text)
    {
        var asm = typeof(Editor).Assembly;
        var sizesType = asm.GetType("UnityEditor.GameViewSizes");
        var getGroup = sizesType.GetMethod("GetGroup");
        getGroup.ReturnType.GetMethod("AddCustomSize").Invoke(getGroup.Invoke(typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null, null), new object[] { (int)sizeGroupType }), new object[] { asm.GetType("UnityEditor.GameViewSize").GetConstructor(new Type[] { asm.GetType("UnityEditor.GameViewSizeType"), typeof(int), typeof(int), typeof(string) }).Invoke(new object[] { (int)viewSizeType, width, height, text }) });
    }
    void SetSize(int index)
    {
        var gvWndType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        gvWndType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(EditorWindow.GetWindow(gvWndType), index, null);
        UpdateZoomAreaAndParent();
        SetGameViewScale();
    }

    void SetGameViewScale()
    {
        Type type = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var areaObj = type.GetField("m_ZoomArea", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(EditorWindow.GetWindow(type));
        areaObj.GetType().GetField("m_Scale", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(areaObj, new Vector2(0.1f, 0.1f));
    }
    bool SizeExists(GameViewSizeGroupType sizeGroupType, string text)
    {
        return FindSize(sizeGroupType, text) != -1;
    }
    int FindSize(GameViewSizeGroupType sizeGroupType, string text)
    {
        var group = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes").GetMethod("GetGroup").Invoke(typeof(ScriptableSingleton<>).MakeGenericType(typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes")).GetProperty("instance").GetValue(null, null), new object[] { (int)sizeGroupType });
        var displayTexts = group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null) as string[];
        for (int i = 0; i < displayTexts.Length; i++)
        {
            string display = displayTexts[i];
            int pren = display.IndexOf('(');
            if (pren != -1)
                display = display.Substring(0, pren - 1);
            if (display == text)
                return i;
        }
        return -1;
    }
    void UpdateZoomAreaAndParent()
    {
        var gvWndType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        gvWndType.GetMethod("UpdateZoomAreaAndParent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(EditorWindow.GetWindow(gvWndType), null);
    }
    #endregion
}
#endif