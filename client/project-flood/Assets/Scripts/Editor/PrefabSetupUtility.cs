using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Core.UI;
using Game.OutGame.Lobby;

namespace Game.Editor
{
    public static class PrefabSetupUtility
    {
        [MenuItem("Tools/Project Flood/Generate UI Prefabs")]
        public static void GeneratePrefabs()
        {
            // Create target directory if it doesn't exist
            string folderPath = "Assets/Resources/Prefabs/UI";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                // Create parent folders if needed
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
                }
                AssetDatabase.CreateFolder("Assets/Resources/Prefabs", "UI");
            }

            GenerateTutorialOverlay(folderPath);
            GenerateChapterChest(folderPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", "UI Prefabs (TutorialOverlay, ChapterChest) generated successfully in Assets/Resources/Prefabs/UI!", "OK");
        }

        private static void GenerateTutorialOverlay(string folderPath)
        {
            string prefabPath = $"{folderPath}/TutorialOverlay.prefab";
            
            // Create temporary GameObject hierarchy
            GameObject root = new GameObject("TutorialOverlay", typeof(RectTransform));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(1080, 1920);

            // Fullscreen Dismiss Button
            GameObject dismissGo = new GameObject("FullscreenDismissButton", typeof(RectTransform), typeof(Image), typeof(Button));
            dismissGo.transform.SetParent(root.transform, false);
            RectTransform dismissRt = dismissGo.GetComponent<RectTransform>();
            dismissRt.anchorMin = Vector2.zero;
            dismissRt.anchorMax = Vector2.one;
            dismissRt.sizeDelta = Vector2.zero;
            Image dismissImg = dismissGo.GetComponent<Image>();
            dismissImg.color = new Color(0, 0, 0, 0.4f); // Semi-transparent black blocker
            Button dismissBtn = dismissGo.GetComponent<Button>();

            // Spotlight Cutout
            GameObject spotlightGo = new GameObject("SpotlightCutout", typeof(RectTransform), typeof(Image));
            spotlightGo.transform.SetParent(root.transform, false);
            RectTransform spotlightRt = spotlightGo.GetComponent<RectTransform>();
            spotlightRt.sizeDelta = new Vector2(150, 150);
            Image spotlightImg = spotlightGo.GetComponent<Image>();
            spotlightImg.color = new Color(1, 1, 1, 0.1f); // subtle inner tint

            // Spotlight Glow (pulse outline)
            GameObject glowGo = new GameObject("SpotlightGlow", typeof(RectTransform), typeof(Image));
            glowGo.transform.SetParent(spotlightGo.transform, false);
            RectTransform glowRt = glowGo.GetComponent<RectTransform>();
            glowRt.anchorMin = Vector2.zero;
            glowRt.anchorMax = Vector2.one;
            glowRt.sizeDelta = new Vector2(20, 20); // slightly larger
            Image glowImg = glowGo.GetComponent<Image>();
            glowImg.color = new Color(1, 0.9f, 0.4f, 0.8f);

            // Tooltip Bubble
            GameObject bubbleGo = new GameObject("TooltipBubble", typeof(RectTransform), typeof(Image));
            bubbleGo.transform.SetParent(root.transform, false);
            RectTransform bubbleRt = bubbleGo.GetComponent<RectTransform>();
            bubbleRt.sizeDelta = new Vector2(800, 300);
            Image bubbleImg = bubbleGo.GetComponent<Image>();
            bubbleImg.color = new Color(0.1f, 0.15f, 0.25f, 0.95f); // Deep dark blue card

            // Tooltip Text
            GameObject textGo = new GameObject("TooltipText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(bubbleGo.transform, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = new Vector2(-40, -40); // padding
            TextMeshProUGUI textTmp = textGo.GetComponent<TextMeshProUGUI>();
            textTmp.fontSize = 32;
            textTmp.alignment = TextAlignmentOptions.Center;
            textTmp.color = Color.white;
            textTmp.text = "Tutorial Message";

            // Character Avatar (Floodie)
            GameObject avatarGo = new GameObject("CharacterAvatar", typeof(RectTransform), typeof(Image));
            avatarGo.transform.SetParent(bubbleGo.transform, false);
            RectTransform avatarRt = avatarGo.GetComponent<RectTransform>();
            avatarRt.anchorMin = new Vector2(0, 0.5f);
            avatarRt.anchorMax = new Vector2(0, 0.5f);
            avatarRt.pivot = new Vector2(0.5f, 0.5f);
            avatarRt.anchoredPosition = new Vector2(-60, 0);
            avatarRt.sizeDelta = new Vector2(150, 150);
            Image avatarImg = avatarGo.GetComponent<Image>();
            avatarImg.color = Color.cyan; // default color indicator

            // Finger Overlay
            GameObject fingerGo = new GameObject("FingerOverlay", typeof(RectTransform), typeof(Image));
            fingerGo.transform.SetParent(root.transform, false);
            RectTransform fingerRt = fingerGo.GetComponent<RectTransform>();
            fingerRt.sizeDelta = new Vector2(100, 100);
            Image fingerImg = fingerGo.GetComponent<Image>();
            fingerImg.color = new Color(1, 0.3f, 0.3f, 0.9f); // red indicator
            
            // Add script to root
            TutorialOverlay overlayScript = root.AddComponent<TutorialOverlay>();

            // Setup Serialized Fields
            SerializedObject so = new SerializedObject(overlayScript);
            so.FindProperty("_spotlightCutout").objectReferenceValue = spotlightRt;
            so.FindProperty("_spotlightGlow").objectReferenceValue = glowImg;
            so.FindProperty("_tooltipBubble").objectReferenceValue = bubbleRt;
            so.FindProperty("_tooltipText").objectReferenceValue = textTmp;
            so.FindProperty("_fingerOverlay").objectReferenceValue = fingerRt;
            so.FindProperty("_characterAvatar").objectReferenceValue = avatarImg;
            so.FindProperty("_fullscreenDismissButton").objectReferenceValue = dismissBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save Prefab
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        private static void GenerateChapterChest(string folderPath)
        {
            string prefabPath = $"{folderPath}/ChapterChest.prefab";

            // Create temporary GameObject hierarchy
            GameObject root = new GameObject("ChapterChest", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(ChapterChestView));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(120, 120);

            Image chestImg = root.GetComponent<Image>();
            chestImg.color = Color.white;
            Button chestBtn = root.GetComponent<Button>();
            CanvasGroup chestCg = root.GetComponent<CanvasGroup>();
            ChapterChestView chestView = root.GetComponent<ChapterChestView>();

            // Glow Effect
            GameObject glowGo = new GameObject("GlowEffect", typeof(RectTransform), typeof(Image), typeof(UIPulseGlowEffect));
            glowGo.transform.SetParent(root.transform, false);
            RectTransform glowRt = glowGo.GetComponent<RectTransform>();
            glowRt.sizeDelta = new Vector2(180, 180);
            
            Image glowImg = glowGo.GetComponent<Image>();
            glowImg.color = new Color(1, 0.85f, 0.3f, 0.5f); // Gold glow

            // Dynamic Material with Shader
            Shader glowShader = Shader.Find("UI/PulseGlow");
            if (glowShader != null)
            {
                Material glowMat = new Material(glowShader);
                glowMat.name = "PulseGlowMaterial";
                AssetDatabase.CreateAsset(glowMat, $"{folderPath}/PulseGlowMaterial.mat");
                glowImg.material = glowMat;
            }

            // Status Text
            GameObject textGo = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(root.transform, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.5f, 0);
            textRt.anchorMax = new Vector2(0.5f, 0);
            textRt.pivot = new Vector2(0.5f, 1f);
            textRt.anchoredPosition = new Vector2(0, -10);
            textRt.sizeDelta = new Vector2(150, 40);

            TextMeshProUGUI textTmp = textGo.GetComponent<TextMeshProUGUI>();
            textTmp.fontSize = 24;
            textTmp.alignment = TextAlignmentOptions.Center;
            textTmp.color = Color.yellow;
            textTmp.text = "Locked";

            // Bind Serialized Fields on ChapterChestView
            SerializedObject so = new SerializedObject(chestView);
            so.FindProperty("_chestImage").objectReferenceValue = chestImg;
            so.FindProperty("_statusText").objectReferenceValue = textTmp;
            so.FindProperty("_button").objectReferenceValue = chestBtn;
            so.FindProperty("_glowEffect").objectReferenceValue = glowGo;
            so.FindProperty("_canvasGroup").objectReferenceValue = chestCg;

            // Optional: load default Unity sprites or placeholders if needed,
            // otherwise user can easily drag and drop their own sprites in inspector.
            
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save Prefab
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }
    }
}
