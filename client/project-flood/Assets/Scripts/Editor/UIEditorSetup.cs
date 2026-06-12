#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using Game.Core;
using Game.Core.UI;
using Game.InGame.View;
using Game.OutGame.Boot;
using Game.OutGame.Lobby;
using Game.OutGame.Settings;
using Game.Utils;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.StringIds;

namespace Game.Editor
{
    /// <summary>
    /// Tools/UI Setup — one-shot editor scripts.
    /// Generates base UI prefabs under Assets/UI/Prefabs/Base/ using premium casual/puzzle styling.
    /// Safe to re-run; outputs are overwritten without affecting scenes or final variants.
    /// </summary>
    public static class UIEditorSetup
    {
        // Directory configurations
        private const string PrefabRoot      = "Assets/Resources/Prefabs/UI"; // Destination for UIManager Popups (Final Variants)
        private const string PrefabBase      = "Assets/UI/Prefabs/Base";      // Destination for Code-Generated Skeletons
        private const string BaseCommonPath  = "Assets/UI/Prefabs/Base/Common";
        private const string BaseScenesPath  = "Assets/UI/Prefabs/Base/Scenes";
        private const string PrefabFinal     = "Assets/UI/Prefabs/Final";     // Destination for Scene Canvases (Final Variants)

        // Premium Candy / Casual Style Color Palette
        static Color UI_BG_DEEP  => Hex("2A1635"); // Deep grape purple (cozy & high-end frame outline)
        static Color UI_BG_MID   => Hex("4D235D"); // Warm plum purple (main body panel fill)
        static Color UI_PRIMARY  => Hex("FF4D79"); // Vibrant strawberry pink (tabs and secondary buttons)
        static Color UI_CTA      => Hex("FFC700"); // Sunny amber yellow (primary buttons that pop)
        static Color UI_SUCCESS  => Hex("2ED573"); // Lime mint green (success states and play CTA)
        static Color UI_DANGER   => Hex("FF4757"); // Coral alert red (danger and close elements)
        static Color UI_TEXT     => Hex("FFFFFF"); // Crisp white text
        static Color UI_BORDER   => Hex("FF9F00"); // Orange-yellow accent highlights
        static Color DIM         => new Color(0.08f, 0.07f, 0.15f, 0.50f); // Immersive, soft 50% opacity dark navy backdrop

        public enum TextCategory
        {
            Normal,
            Header,
            Button
        }

        // ════════════════════════════════════════════════════════════════
        //  MENU
        // ════════════════════════════════════════════════════════════════

        [MenuItem("Tools/UI Setup/1 - Create All Prefabs", false, 100)]
        static void CreateAllPrefabs()
        {
            EnsureDirs();
            CreateConfirmDialog();
            CreateToast();
            CreateLoadingOverlay();
            CreateNetworkError();
            CreateRewardPopup();
            CreateReLoginView();
            CreateStageInfoPopup();
            CreateResultOverlay();
            CreateFailOverlay();
            CreatePausePopup();
            CreateSettingsPanel();
            CreateAccountPopup();
            CreateAccountRestartPopup();
            CreateAccountConflictPopup();
            CreateStageNodeView();
            CreateStaminaPopup();
            CreateTutorialOverlay();
            CreateChapterChest();
            CreateRankingItemPrefab();
            CreateItemBuyConfirmPopup();
            
            // Generate Scenes as well
            SetupBoot();
            SetupLobby();
            SetupInGame();

            // Map cell and socket sprites
            MapCellAndSocketSprites();
            
            AssetDatabase.Refresh();
            Debug.Log("[UIEditorSetup] All base popups & scenes created successfully.");

            // Open Boot scene upon successful completion
            string bootScenePath = "Assets/Scenes/Boot.unity";
            if (File.Exists(bootScenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(bootScenePath);
                Debug.Log("[UIEditorSetup] Successfully opened Boot.unity scene.");
            }
            else
            {
                Debug.LogError("[UIEditorSetup] Boot.unity scene not found!");
            }
        }

        [MenuItem("Tools/UI Setup/Prefabs/ConfirmDialog",  false, 110)]
        static void CreateConfirmDialogSingle()  { EnsureDirs(); CreateConfirmDialog();  AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/Toast",           false, 111)]
        static void CreateToastSingle()          { EnsureDirs(); CreateToast();          AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/LoadingOverlay",  false, 112)]
        static void CreateLoadingOverlaySingle() { EnsureDirs(); CreateLoadingOverlay(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/NetworkError",    false, 113)]
        static void CreateNetworkErrorSingle()   { EnsureDirs(); CreateNetworkError();   AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/RewardPopup",     false, 114)]
        static void CreateRewardPopupSingle()    { EnsureDirs(); CreateRewardPopup();    AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/ReLoginView",     false, 115)]
        static void CreateReLoginViewSingle()    { EnsureDirs(); CreateReLoginView();    AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/StageInfoPopup",  false, 116)]
        static void CreateStageInfoPopupSingle() { EnsureDirs(); CreateStageInfoPopup(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/ResultOverlay",   false, 117)]
        static void CreateResultOverlaySingle()  { EnsureDirs(); CreateResultOverlay();  AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/FailOverlay",     false, 118)]
        static void CreateFailOverlaySingle()    { EnsureDirs(); CreateFailOverlay();    AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/PausePopup",      false, 119)]
        static void CreatePausePopupSingle()     { EnsureDirs(); CreatePausePopup();     AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/SettingsPanel",   false, 120)]
        static void CreateSettingsPanelSingle()  { EnsureDirs(); CreateSettingsPanel();  AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/AccountPopup",    false, 121)]
        static void CreateAccountPopupSingle()   { EnsureDirs(); CreateAccountPopup();   AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/StageNodeView",    false, 122)]
        static void CreateStageNodeViewSingle()  { EnsureDirs(); CreateStageNodeView();  AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/StaminaPopup",     false, 126)]
        static void CreateStaminaPopupSingle()   { EnsureDirs(); CreateStaminaPopup();   AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/TutorialOverlay",  false, 127)]
        static void CreateTutorialOverlaySingle() { EnsureDirs(); CreateTutorialOverlay(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/ChapterChest",     false, 128)]
        static void CreateChapterChestSingle()    { EnsureDirs(); CreateChapterChest();    AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/AccountRestartPopup", false, 135)]
        static void CreateAccountRestartPopupSingle() { EnsureDirs(); CreateAccountRestartPopup(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/AccountConflictPopup", false, 136)]
        static void CreateAccountConflictPopupSingle() { EnsureDirs(); CreateAccountConflictPopup(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/ItemBuyConfirmPopup", false, 137)]
        static void CreateItemBuyConfirmPopupSingle() { EnsureDirs(); CreateItemBuyConfirmPopup(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/BootCanvas",       false, 123)]
        static void CreateBootCanvasSingle()     { EnsureDirs(); SetupBoot();            AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/LobbyCanvas",      false, 124)]
        static void CreateLobbyCanvasSingle()    { EnsureDirs(); SetupLobby();           AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/InGameCanvas",     false, 125)]
        static void CreateInGameCanvasSingle()   { EnsureDirs(); SetupInGame();          AssetDatabase.Refresh(); }

        static void SetupBoot()
        {
            var canvas = CreateTempCanvas("Canvas_Scene");
            var content = Child(canvas, "Content");
            Stretch(content);
            TMP(content, "LogoText", Center(0, 200, 600, 120), 48, UI_TEXT, "PROJECT FLOOD", AppTitle, TextCategory.Header);

            // Animated loader indicator representing spinner
            var loaderGo = Child(content, "LoaderIcon");
            Fixed(loaderGo, new Vector2(0, -100), new Vector2(100, 100));
            Img(loaderGo, UI_CTA);
            var loaderAnim = Comp<UIIconIdleAnimator>(loaderGo);
            loaderAnim.Configure(UIIconIdleAnimator.AnimationType.Rotate, 3f, 45f); // Spin loader

            TMP(content, "SpinnerText", Center(0, -200, 600, 80), 24, UI_TEXT, "Loading...", BootLoading, TextCategory.Normal);

            SaveScenePrefab(canvas, "Boot");
        }

        static void SetupLobby()
        {
            var canvas = CreateTempCanvas("Canvas_Scene");
            canvas.AddComponent<LobbyView>();
            var resMap = LoadDynamicResourceMap();

            // SafeAreaRoot — fills Screen.safeArea
            var safeRoot = Child(canvas, "SafeAreaRoot");
            Stretch(safeRoot);
            Comp<SafeAreaHandler>(safeRoot);

            // Header — top 180px (Taller for casual layered styling)
            var header = Child(safeRoot, "Header");
            TopStrip(header, 180);
            Img(header, UI_BG_DEEP);
            var hv = Comp<HeaderView>(header);
            
            // Avatar Button (Square layout with C# idle floating, adjusted to width 96)
            var avatarBtn = Btn(header, "AvatarButton", new Vector2(-450, 0), new Vector2(96, 96), UI_BG_MID, "");
            var avatarIconImg = avatarBtn.transform.Find("Visual/Icon")?.GetComponent<Image>();
            if (avatarIconImg != null && resMap.TryGetValue("ui_avatar_default", out string avp))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(avp);
                if (spr != null) { avatarIconImg.sprite = spr; avatarIconImg.preserveAspect = true; }
            }

            // Settings Button (Matching Avatar Button on the right, width 96)
            var settingsBtn = Btn(header, "SettingsButton", new Vector2(450, 0), new Vector2(96, 96), UI_BG_MID, "");
            var settingsIconImg = settingsBtn.transform.Find("Visual/Icon")?.GetComponent<Image>();
            if (settingsIconImg != null && resMap.TryGetValue("ui_settings_icon", out string setip))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(setip);
                if (spr != null) { settingsIconImg.sprite = spr; settingsIconImg.preserveAspect = true; }
            }

            // Gold Container - Pill layout with gold border (adjusted position and width)
            var goldContainer = Child(header, "GoldContainer");
            Fixed(goldContainer, new Vector2(150, 0), new Vector2(280, 96));
            Img(goldContainer, UI_BG_MID);
            
            var goldBorder = Child(goldContainer, "Border");
            Stretch(goldBorder);
            var goldBorderImg = Img(goldBorder, Hex("2B003B"));
            goldBorder.transform.SetAsFirstSibling();
            goldBorderImg.rectTransform.offsetMin = new Vector2(-4, -4);
            goldBorderImg.rectTransform.offsetMax = new Vector2(4, 4);

            // Animated Gold Icon
            var goldIcon = Child(goldContainer, "Icon");
            Fixed(goldIcon, new Vector2(-85, 0), new Vector2(80, 80));
            var goldIconImg = Img(goldIcon, UI_CTA);
            if (resMap.TryGetValue("ui_gold_icon", out string gip))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(gip);
                if (spr != null) { goldIconImg.sprite = spr; goldIconImg.preserveAspect = true; }
            }
            var goldIconAnim = Comp<UIIconIdleAnimator>(goldIcon);
            goldIconAnim.Configure(UIIconIdleAnimator.AnimationType.GlowSweep, 2.2f, 12f);

            var goldText = TMP(goldContainer, "GoldText", Center(35, 0, 150, 70), 22, UI_CTA, "0", null, TextCategory.Normal);
            var goldCsf = Comp<ContentSizeFitter>(goldText.gameObject);
            goldCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var goldNumAnim = Comp<UINumberChange>(goldText.gameObject);
            var soGoldAnim = new SerializedObject(goldNumAnim);
            soGoldAnim.FindProperty("_formatString").stringValue = "{0:N0}";
            soGoldAnim.ApplyModifiedProperties();

            // Stamina Panel — Pill layout with dark border, heart icon with count number overlaid + timer next to it (adjusted position and width)
            var staminaPanel = Child(header, "StaminaPanel");
            Fixed(staminaPanel, new Vector2(-150, 0), new Vector2(280, 96));
            var staminaBgImg = Img(staminaPanel, UI_BG_MID);
            
            var staminaBorder = Child(staminaPanel, "Border");
            Stretch(staminaBorder);
            var staminaBorderImg = Img(staminaBorder, Hex("2B003B"));
            staminaBorder.transform.SetAsFirstSibling();
            staminaBorderImg.rectTransform.offsetMin = new Vector2(-4, -4);
            staminaBorderImg.rectTransform.offsetMax = new Vector2(4, 4);

            var staminaBtn = Comp<Button>(staminaPanel);
            staminaBtn.targetGraphic = staminaBgImg;
            Comp<UIButtonAnimator>(staminaPanel);

            var heartIcon = Child(staminaPanel, "HeartIcon");
            Fixed(heartIcon, new Vector2(-85, 0), new Vector2(80, 80));
            var heartIconImg = Img(heartIcon, UI_PRIMARY);
            if (resMap.TryGetValue("stamina_heart", out string shp))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(shp);
                if (spr != null) { heartIconImg.sprite = spr; heartIconImg.preserveAspect = true; }
            }
            Comp<UIIconIdleAnimator>(heartIcon).Configure(UIIconIdleAnimator.AnimationType.GlowSweep, 2.4f, 12f);

            // Count number overlaid on heart icon
            var staminaText = TMP(heartIcon, "CountText", Center(0, 0, 80, 80), 28, UI_TEXT, "5", null, TextCategory.Header);
            Comp<UINumberChange>(staminaText.gameObject);

            // Timer or MAX label to the right of the heart (adjusted center offset)
            var staminaTimerText = TMP(staminaPanel, "TimerText", Center(35, 0, 150, 70), 22, UI_CTA, "MAX", StaminaMax, TextCategory.Normal);

            // BottomNavBar — bottom 160px, HorizontalLayoutGroup for tab distribution
            var navBar = Child(safeRoot, "BottomNavBar");
            BottomStrip(navBar, 160);
            Img(navBar, UI_BG_DEEP);
            var bnv = Comp<BottomNavBarView>(navBar);
            var navHlg = Comp<HorizontalLayoutGroup>(navBar);
            navHlg.childAlignment      = TextAnchor.MiddleCenter;
            navHlg.childForceExpandWidth  = true;
            navHlg.childForceExpandHeight = true;
            navHlg.padding = new RectOffset(0, 0, 0, 0);
            navHlg.spacing = 0;

            var shopBtn    = BtnNavTab(navBar, "ShopButton",    UI_BG_MID, "Shop", NavShop);
            var homeBtn    = BtnNavTab(navBar, "HomeButton",    UI_BG_MID, "Home", NavHome);
            var rankBtn    = BtnNavTab(navBar, "RankingButton", UI_BG_MID, "Rank", NavRanking);
            if (resMap.TryGetValue("nav_shop",    out string nsp)) { var s = AssetDatabase.LoadAssetAtPath<Sprite>(nsp); if (s != null) { var i = shopBtn.transform.Find("Visual/Icon")?.GetComponent<Image>(); if (i != null) i.sprite = s; } }
            if (resMap.TryGetValue("nav_home",    out string nhp)) { var s = AssetDatabase.LoadAssetAtPath<Sprite>(nhp); if (s != null) { var i = homeBtn.transform.Find("Visual/Icon")?.GetComponent<Image>(); if (i != null) i.sprite = s; } }
            if (resMap.TryGetValue("nav_ranking", out string nrp)) { var s = AssetDatabase.LoadAssetAtPath<Sprite>(nrp); if (s != null) { var i = rankBtn.transform.Find("Visual/Icon")?.GetComponent<Image>(); if (i != null) i.sprite = s; } }

            // Tab content area — fills between header and nav
            var tabContent = Child(safeRoot, "TabContent");
            PaddedStretch(tabContent, 180, 160);

            // HomeTab — ScrollRect with curved zigzag map path
            var homeTab = Child(tabContent, "HomeTab"); Stretch(homeTab);
            var htv = Comp<HomeTabView>(homeTab);
            if (!homeTab.TryGetComponent<ScrollRect>(out var scrollRect))
                scrollRect = homeTab.AddComponent<ScrollRect>();
            scrollRect.horizontal = false; scrollRect.vertical = true;
            
            var viewportGo = Child(homeTab, "Viewport"); Stretch(viewportGo);
            Comp<RectMask2D>(viewportGo);
            
            var contentGo = Child(viewportGo, "Content");
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 3000);
            scrollRect.viewport = viewportGo.GetComponent<RectTransform>();
            scrollRect.content  = contentRt;

            // Generate stage node placeholders in a beautiful curved S-shape path
            for (int i = 1; i <= 12; i++)
            {
                float angle = i * 1.3f;
                float x = Mathf.Sin(angle) * 260f; // Curved pathway sine-wave
                float y = -180f - (i - 1) * 230f; // Scrolling downwards
                
                var nodeGo = Child(contentGo, $"StageNode_{i}");
                Fixed(nodeGo, new Vector2(x, y), new Vector2(130f, 130f));
                Img(nodeGo, UI_PRIMARY);
                
                var btn = Comp<Button>(nodeGo);
                Comp<UIButtonAnimator>(nodeGo);
                var anim = Comp<UIIconIdleAnimator>(nodeGo);
                anim.Configure(UIIconIdleAnimator.AnimationType.Float, 1.8f, 6f); // floating nodes
                
                TMP(nodeGo, "Label", Center(0, 0, 130, 130), 22, UI_TEXT, i.ToString(), null, TextCategory.Button);
            }

            var nodeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/StageNodeView.prefab");
            if (nodeAsset == null)
                nodeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BaseCommonPath + "/StageNodeView.prefab");

            var soHtv = new SerializedObject(htv);
            soHtv.FindProperty("_scrollRect").objectReferenceValue = scrollRect;
            if (resMap.TryGetValue("toast_success", out string tsPath))
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(tsPath);
                if (s != null)
                {
                    soHtv.FindProperty("_guideOrbSprite").objectReferenceValue = s;
                }
            }
            soHtv.ApplyModifiedProperties();

            var shopTab = Child(tabContent, "ShopTab");  Stretch(shopTab); shopTab.SetActive(false);
            var rankingTab = Child(tabContent, "RankingTab"); Stretch(rankingTab); rankingTab.SetActive(false);
            var rankingView = Comp<RankingTabView>(rankingTab);
            var starsTab = Btn(rankingTab, "StarsTabButton", new Vector2(-230, 700), new Vector2(300, 80), UI_PRIMARY, "Stars", LobbyRankingTabStars);
            var maxStageTab = Btn(rankingTab, "MaxStageTabButton", new Vector2(230, 700), new Vector2(300, 80), UI_BG_MID, "Max Stage", LobbyRankingTabMaxStage);
            
            var rankTitle = TMP(rankingTab, "TitleText", Center(0, 580, 760, 70), 30, UI_CTA, "Star Ranking", LobbyRankingStarsTitle, TextCategory.Header);
            var myRank = TMP(rankingTab, "MyRankText", Center(0, 490, 760, 80), 24, UI_TEXT, "My Rank: -", LobbyRankingMyRankEmpty, TextCategory.Normal);
            var entries = TMP(rankingTab, "EntriesText", Center(0, -190, 820, 1220), 20, UI_TEXT, "Ranking unavailable", LobbyRankingUnavailable, TextCategory.Normal);
            entries.alignment = TextAlignmentOptions.TopLeft;

            // Generate VirtualizedScrollRect hierarchy
            var scrollRectGo = Child(rankingTab, "VirtualizedScrollRect");
            var srRt = RT(scrollRectGo);
            srRt.anchorMin = new Vector2(0.5f, 0f);
            srRt.anchorMax = new Vector2(0.5f, 1f);
            srRt.pivot = new Vector2(0.5f, 0.5f);
            srRt.sizeDelta = new Vector2(820, -380); // height margin top = 380, bottom = 0
            srRt.anchoredPosition = new Vector2(0, -190); // y center offset = -190

            var rankScrollRect = Comp<ScrollRect>(scrollRectGo);
            rankScrollRect.horizontal = false;
            rankScrollRect.vertical = true;

            var rankViewportGo = Child(scrollRectGo, "Viewport");
            Stretch(rankViewportGo);
            Comp<RectMask2D>(rankViewportGo);
            rankScrollRect.viewport = rankViewportGo.GetComponent<RectTransform>();

            var rankContentGo = Child(rankViewportGo, "Content");
            var rankContentRt = rankContentGo.GetComponent<RectTransform>();
            rankContentRt.anchorMin = new Vector2(0, 1);
            rankContentRt.anchorMax = new Vector2(1, 1);
            rankContentRt.pivot = new Vector2(0.5f, 1);
            rankContentRt.sizeDelta = new Vector2(0, 0);
            rankScrollRect.content = rankContentRt;

            var vScroll = Comp<VirtualizedScrollRect>(scrollRectGo);

            // Assign the prefab asset dynamically
            var prefabPath = $"{BaseCommonPath}/RankingItemPrefab.prefab";
            var itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var soVScroll = new SerializedObject(vScroll);
            if (itemPrefab != null)
            {
                soVScroll.FindProperty("_itemPrefab").objectReferenceValue = itemPrefab.GetComponent<RectTransform>();
            }
            else
            {
                Debug.LogWarning($"[UIEditorSetup] RankingItemPrefab not found at {prefabPath}! Run 'Create All Prefabs' to generate it.");
            }
            soVScroll.FindProperty("_itemHeight").floatValue = 90f;
            soVScroll.FindProperty("_spacing").floatValue = 6f;
            soVScroll.ApplyModifiedProperties();

            // Pinned player ranking item at the bottom
            var myRankPin = Child(rankingTab, "MyRankPin");
            Fixed(myRankPin, new Vector2(0, -700), new Vector2(820, 90));
            Img(myRankPin, UI_BG_MID);

            var pinBorder = Child(myRankPin, "Border");
            Stretch(pinBorder);
            var pinBorderImg = Img(pinBorder, Hex("2B003B"));
            pinBorder.transform.SetAsFirstSibling();
            pinBorderImg.rectTransform.offsetMin = new Vector2(-4, -4);
            pinBorderImg.rectTransform.offsetMax = new Vector2(4, 4);

            BuildRankingItemHierarchy(myRankPin, resMap);

            // Wire LobbyView refs
            var soLobby = new SerializedObject(canvas.GetComponent<LobbyView>());
            soLobby.FindProperty("_header").objectReferenceValue      = hv;
            soLobby.FindProperty("_navBar").objectReferenceValue      = bnv;
            soLobby.FindProperty("_homeTabRoot").objectReferenceValue = homeTab;
            soLobby.FindProperty("_shopTabRoot").objectReferenceValue = shopTab;
            soLobby.FindProperty("_rankingTabRoot").objectReferenceValue = rankingTab;
            soLobby.FindProperty("_rankingTabView").objectReferenceValue = rankingView;
            soLobby.ApplyModifiedProperties();

            var soRanking = new SerializedObject(rankingView);
            soRanking.FindProperty("_starsTabButton").objectReferenceValue = starsTab.GetComponent<Button>();
            soRanking.FindProperty("_maxStageTabButton").objectReferenceValue = maxStageTab.GetComponent<Button>();
            soRanking.FindProperty("_titleText").objectReferenceValue = rankTitle;
            soRanking.FindProperty("_myRankText").objectReferenceValue = myRank;
            soRanking.FindProperty("_entriesText").objectReferenceValue = entries;
            soRanking.FindProperty("_virtualizedScrollRect").objectReferenceValue = vScroll;

            // Wire MyRankPin elements
            soRanking.FindProperty("_myRankPin").objectReferenceValue = myRankPin;
            soRanking.FindProperty("_myRankPinRankText").objectReferenceValue = myRankPin.transform.Find("RankText")?.GetComponent<TMP_Text>();
            soRanking.FindProperty("_myRankPinAvatarIcon").objectReferenceValue = myRankPin.transform.Find("AvatarIcon")?.GetComponent<Image>();
            soRanking.FindProperty("_myRankPinNameText").objectReferenceValue = myRankPin.transform.Find("NameText")?.GetComponent<TMP_Text>();
            soRanking.FindProperty("_myRankPinScoreIcon").objectReferenceValue = myRankPin.transform.Find("ScoreIcon")?.GetComponent<Image>();
            soRanking.FindProperty("_myRankPinScoreText").objectReferenceValue = myRankPin.transform.Find("ScoreText")?.GetComponent<TMP_Text>();

            // Populate avatarSprites list in RankingTabView
            var avatarSpritesProp = soRanking.FindProperty("_avatarSprites");
            avatarSpritesProp.ClearArray();
            var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));
            var avatarCsvPath = Path.Combine(repoRoot, "shared/datas/avatar/avatar.csv");
            if (File.Exists(avatarCsvPath))
            {
                var lines = File.ReadAllLines(avatarCsvPath);
                int count = 0;
                for (int i = 4; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    var cols = line.Split(',');
                    if (cols.Length >= 2)
                    {
                        if (int.TryParse(cols[0].Trim(), out int avatarId))
                        {
                            string resKey = cols[1].Trim();
                            if (resMap.TryGetValue(resKey, out string spritePath))
                            {
                                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                                if (sprite != null)
                                {
                                    avatarSpritesProp.InsertArrayElementAtIndex(count);
                                    var element = avatarSpritesProp.GetArrayElementAtIndex(count);
                                    element.FindPropertyRelative("avatarId").intValue = avatarId;
                                    element.FindPropertyRelative("resourceName").stringValue = resKey;
                                    element.FindPropertyRelative("sprite").objectReferenceValue = sprite;
                                    count++;
                                }
                            }
                        }
                    }
                }
            }

            // Populate Star and Stage icons on RankingTabView using specified resource keys
            string starKey = soRanking.FindProperty("_starResourceKey").stringValue;
            if (string.IsNullOrEmpty(starKey)) starKey = "star_filled";
            string stageKey = soRanking.FindProperty("_stageResourceKey").stringValue;
            if (string.IsNullOrEmpty(stageKey)) stageKey = "nav_home";

            if (resMap.TryGetValue(starKey, out string starPath))
            {
                var starSprite = AssetDatabase.LoadAssetAtPath<Sprite>(starPath);
                soRanking.FindProperty("_starSprite").objectReferenceValue = starSprite;
            }
            if (resMap.TryGetValue(stageKey, out string stagePath))
            {
                var stageSprite = AssetDatabase.LoadAssetAtPath<Sprite>(stagePath);
                soRanking.FindProperty("_stageSprite").objectReferenceValue = stageSprite;
            }

            soRanking.FindProperty("_activeTabColor").colorValue = UI_PRIMARY;
            soRanking.FindProperty("_inactiveTabColor").colorValue = UI_BG_MID;
            soRanking.ApplyModifiedProperties();

            // Wire BottomNavBarView
            var soNav = new SerializedObject(bnv);
            soNav.FindProperty("_shopButton").objectReferenceValue       = shopBtn.GetComponent<Button>();
            soNav.FindProperty("_homeButton").objectReferenceValue       = homeBtn.GetComponent<Button>();
            soNav.FindProperty("_rankingButton").objectReferenceValue    = rankBtn.GetComponent<Button>();
            soNav.FindProperty("_shopHighlight").objectReferenceValue    = shopBtn.transform.Find("Visual/Icon").GetComponent<Image>();
            soNav.FindProperty("_homeHighlight").objectReferenceValue    = homeBtn.transform.Find("Visual/Icon").GetComponent<Image>();
            soNav.FindProperty("_rankingHighlight").objectReferenceValue = rankBtn.transform.Find("Visual/Icon").GetComponent<Image>();
            soNav.ApplyModifiedProperties();

            // Wire HeaderView
            var soHeader = new SerializedObject(hv);
            soHeader.FindProperty("_avatarButton").objectReferenceValue     = avatarBtn.GetComponent<Button>();
            soHeader.FindProperty("_settingsButton").objectReferenceValue   = settingsBtn.GetComponent<Button>();
            soHeader.FindProperty("_goldText").objectReferenceValue          = goldText;
            soHeader.FindProperty("_staminaText").objectReferenceValue       = staminaText;
            soHeader.FindProperty("_staminaTimerText").objectReferenceValue  = staminaTimerText;
            soHeader.FindProperty("_staminaButton").objectReferenceValue     = staminaBtn;
            soHeader.ApplyModifiedProperties();

            SaveScenePrefab(canvas, "Lobby");
        }

        static void SetupInGame()
        {
            var canvas = CreateTempCanvas("Canvas_Scene");
            Comp<UIScreenShake>(canvas);
            var resMapInGame = LoadDynamicResourceMap();

            // HUD — top 240px fixed area (generous for casual game feel)
            var hud = Child(canvas, "HUD");
            TopStrip(hud, 240);
            Img(hud, new Color(0, 0, 0, 0)); // Transparent container
            var hudView = Comp<HUDView>(hud);

            // Pause button — top-left square button
            var pauseBtn = Btn(hud, "PauseButton", new Vector2(-460, -90), new Vector2(100, 100), UI_PRIMARY, "");
            var pauseIconImg = pauseBtn.transform.Find("Visual/Icon")?.GetComponent<Image>();
            if (pauseIconImg != null && resMapInGame.TryGetValue("ui_pause_icon", out string pip))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(pip);
                if (spr != null) { pauseIconImg.sprite = spr; pauseIconImg.preserveAspect = true; }
            }

            // StageInfo — shows stage number; background colored by DifficultyStyle at runtime; SkullBadge for Hard
            var stageInfo = Child(hud, "StageInfo");
            Fixed(stageInfo, new Vector2(-290f, -90f), new Vector2(160f, 80f));
            var stageInfoImg = Img(stageInfo, Color.clear);

            var stageNumTxt = TMP(stageInfo, "StageNumberText", Center(0, 0, 140, 70), 28, UI_TEXT, "1", null, TextCategory.Header);
            stageNumTxt.alignment = TextAlignmentOptions.Center;

            // Skull badge nested inside (Hard only)
            var hudSkull = Child(stageInfo, "SkullBadge");
            Fixed(hudSkull, new Vector2(55f, 10f), new Vector2(30f, 30f));
            var hudSkullImg = Img(hudSkull, Color.white);
            hudSkull.SetActive(false);

            // Turns HUD bubble — top-center circular container
            var turnsBubble = Child(hud, "TurnsBubble");
            Fixed(turnsBubble, new Vector2(0, -90), new Vector2(180, 180));
            Img(turnsBubble, UI_BG_MID);
            
            // Double layered round border for turns bubble
            var turnsBorder = Child(turnsBubble, "Border");
            Stretch(turnsBorder);
            var borderImg = Img(turnsBorder, UI_BORDER);
            turnsBorder.transform.SetAsFirstSibling();
            var turnsRt = borderImg.rectTransform;
            turnsRt.offsetMin = new Vector2(-8, -8);
            turnsRt.offsetMax = new Vector2(8, 8);
            
            var turnsTxt = TMP(turnsBubble, "TurnsText", Center(0, 0, 160, 100), 36, UI_TEXT, "20", null, TextCategory.Header);
            // TurnsLabel removed — icon + integer is sufficient

            // Progress container — Horizontal: [CellIcon][Count][★1][★2][★3]
            var progressContainer = Child(hud, "ProgressContainer");
            Fixed(progressContainer, new Vector2(230, -90), new Vector2(380, 110));
            Img(progressContainer, UI_BG_DEEP);

            var progHlg = Comp<HorizontalLayoutGroup>(progressContainer);
            progHlg.childAlignment       = TextAnchor.MiddleLeft;
            progHlg.childForceExpandWidth  = false;
            progHlg.childForceExpandHeight = false;
            progHlg.spacing  = 8f;
            progHlg.padding  = new RectOffset(14, 14, 10, 10);

            // CellIcon — no Shadow
            var cellIcon = Child(progressContainer, "CellIcon");
            RT(cellIcon).sizeDelta = new Vector2(70f, 70f);
            var cellIconLe = Comp<LayoutElement>(cellIcon);
            cellIconLe.preferredWidth  = cellIconLe.minWidth  = 70f;
            cellIconLe.preferredHeight = cellIconLe.minHeight = 70f;
            var cellIconImg = Img(cellIcon, UI_PRIMARY);
            if (resMapInGame.TryGetValue("ui_cell_icon", out string cip))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(cip);
                if (spr != null) { cellIconImg.sprite = spr; cellIconImg.preserveAspect = true; }
            }
            Comp<UIIconIdleAnimator>(cellIcon).Configure(UIIconIdleAnimator.AnimationType.Float, 2f, 6f);

            // RemainingText
            var remainingTxt = TMP(progressContainer, "RemainingText", Center(0, 0, 70, 80), 36, UI_TEXT, "0", null, TextCategory.Header);
            var remainLe = Comp<LayoutElement>(remainingTxt.gameObject);
            remainLe.preferredWidth  = remainLe.minWidth  = 70f;
            remainLe.preferredHeight = remainLe.minHeight = 80f;
            Comp<UINumberChange>(remainingTxt.gameObject);
            Comp<UINumberChange>(turnsTxt.gameObject);

            // Stars — direct sprite swap by HUDView.RefreshStars(); no Fill child needed
            var star1Go = Child(progressContainer, "Star1");
            RT(star1Go).sizeDelta = new Vector2(54f, 54f);
            var star1Le = Comp<LayoutElement>(star1Go);
            star1Le.preferredWidth = star1Le.minWidth = 54f; star1Le.preferredHeight = star1Le.minHeight = 54f;
            var star1Img = Img(star1Go, Color.white); star1Img.preserveAspect = true;

            var star2Go = Child(progressContainer, "Star2");
            RT(star2Go).sizeDelta = new Vector2(54f, 54f);
            var star2Le = Comp<LayoutElement>(star2Go);
            star2Le.preferredWidth = star2Le.minWidth = 54f; star2Le.preferredHeight = star2Le.minHeight = 54f;
            var star2Img = Img(star2Go, Color.white); star2Img.preserveAspect = true;

            var star3Go = Child(progressContainer, "Star3");
            RT(star3Go).sizeDelta = new Vector2(54f, 54f);
            var star3Le = Comp<LayoutElement>(star3Go);
            star3Le.preferredWidth = star3Le.minWidth = 54f; star3Le.preferredHeight = star3Le.minHeight = 54f;
            var star3Img = Img(star3Go, Color.white); star3Img.preserveAspect = true;

            var starFilledSpr = resMapInGame.TryGetValue("star_filled", out string sfp) ? AssetDatabase.LoadAssetAtPath<Sprite>(sfp) : null;
            var starEmptySpr  = resMapInGame.TryGetValue("star_empty",  out string sep) ? AssetDatabase.LoadAssetAtPath<Sprite>(sep)  : null;
            foreach (var sImg in new[] { star1Img, star2Img, star3Img })
                if (starEmptySpr != null) sImg.sprite = starEmptySpr;

            // Wire HUDView
            var soHud = new SerializedObject(hudView);
            soHud.FindProperty("_pauseButton").objectReferenceValue   = pauseBtn.GetComponent<Button>();
            soHud.FindProperty("_turnsText").objectReferenceValue     = turnsTxt;
            soHud.FindProperty("_remainingText").objectReferenceValue = remainingTxt;
            var starImagesArr = soHud.FindProperty("_starImages");
            starImagesArr.arraySize = 3;
            starImagesArr.GetArrayElementAtIndex(0).objectReferenceValue = star1Img;
            starImagesArr.GetArrayElementAtIndex(1).objectReferenceValue = star2Img;
            starImagesArr.GetArrayElementAtIndex(2).objectReferenceValue = star3Img;
            soHud.FindProperty("_starFilled").objectReferenceValue   = starFilledSpr;
            soHud.FindProperty("_starEmpty").objectReferenceValue    = starEmptySpr;
            soHud.FindProperty("_turnsBorder").objectReferenceValue    = borderImg;
            soHud.FindProperty("_stageInfoBg").objectReferenceValue    = stageInfoImg;
            soHud.FindProperty("_stageText").objectReferenceValue      = stageNumTxt;
            soHud.FindProperty("_skullBadge").objectReferenceValue     = hudSkull;
            soHud.ApplyModifiedProperties();

            // Load skull sprite for HUD badge
            if (resMapInGame.TryGetValue("ui_hard_skull", out string hudSkulPath))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(hudSkulPath);
                if (spr != null) { hudSkullImg.sprite = spr; hudSkullImg.preserveAspect = true; }
            }

            // BoardContainer (anchor for world-space board)
            var board = Child(canvas, "BoardContainer"); Stretch(board);

            // Load dynamic resources map
            var resMap = LoadDynamicResourceMap();

            Sprite dragSprite = null;
            if (resMap.TryGetValue("ui_drag_pointer", out string dragPath))
            {
                dragSprite = AssetDatabase.LoadAssetAtPath<Sprite>(dragPath);
            }

            // RowShiftOverlay Generation (placed before SafeAreaRoot so ItemTray is drawn on top)
            var overlayGo = Child(canvas, "RowShiftOverlay");
            Stretch(overlayGo);
            var overlayImg = Img(overlayGo, new Color(0f, 0f, 0f, 0.45f)); // Semi-transparent black overlay
            overlayImg.raycastTarget = true;
            overlayGo.SetActive(false);

            var pointerGo = Child(overlayGo, "Pointer");
            Fixed(pointerGo, Vector2.zero, new Vector2(100f, 100f));
            var pointerImg = Img(pointerGo, Color.white);
            pointerGo.SetActive(false);

            var lineGo = Child(overlayGo, "DragLine");
            var lineRt = RT(lineGo);
            lineRt.anchorMin = new Vector2(0.5f, 0.5f);
            lineRt.anchorMax = new Vector2(0.5f, 0.5f);
            lineRt.pivot = new Vector2(0.5f, 0.5f);
            lineRt.sizeDelta = new Vector2(0f, 16f); // 16px thickness
            var lineImg = Img(lineGo, Color.white);
            if (dragSprite != null)
            {
                lineImg.sprite = dragSprite;
            }
            lineImg.type = Image.Type.Sliced;
            lineImg.color = UI_PRIMARY; // Strawberry pink drag path line!
            lineGo.SetActive(false);

            var overlayView = Comp<RowShiftOverlayView>(overlayGo);

            var soOverlay = new SerializedObject(overlayView);
            soOverlay.FindProperty("_pointerImage").objectReferenceValue = pointerImg;
            soOverlay.FindProperty("_dragLineRect").objectReferenceValue = lineRt;
            if (dragSprite != null)
            {
                soOverlay.FindProperty("_pointerSprite").objectReferenceValue = dragSprite;
            }
            soOverlay.ApplyModifiedProperties();

            // ItemTray UI Generation
            var safeRoot = Child(canvas, "SafeAreaRoot");
            Stretch(safeRoot);
            Comp<SafeAreaHandler>(safeRoot);

            var trayGo = Child(safeRoot, "ItemTray");
            BottomStrip(trayGo, 180f);
            Img(trayGo, UI_BG_DEEP);
            var trayView = Comp<ItemTrayView>(trayGo);

            var hlg = Comp<HorizontalLayoutGroup>(trayGo);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 30f;
            hlg.padding = new RectOffset(10, 10, 10, 10);

            var bombSlot       = CreateSlotHelper(trayGo, "BombSlot");
            var hRocketSlot    = CreateSlotHelper(trayGo, "HRocketSlot");
            var colorSweepSlot = CreateSlotHelper(trayGo, "ColorSweepSlot");
            var rowShiftSlot   = CreateSlotHelper(trayGo, "RowShiftSlot");
            var cellSwapSlot   = CreateSlotHelper(trayGo, "CellSwapSlot");

            // Assign item icons using resMap
            AssignItemIcon(bombSlot, "item_bomb", resMap);
            AssignItemIcon(hRocketSlot, "item_h_rocket", resMap);
            AssignItemIcon(colorSweepSlot, "item_color_sweep", resMap);
            AssignItemIcon(rowShiftSlot, "item_row_shift", resMap);
            AssignItemIcon(cellSwapSlot, "item_cell_swap", resMap);

            var soTray = new SerializedObject(trayView);
            soTray.FindProperty("_bombSlot").objectReferenceValue       = bombSlot;
            soTray.FindProperty("_hRocketSlot").objectReferenceValue    = hRocketSlot;
            soTray.FindProperty("_colorSweepSlot").objectReferenceValue = colorSweepSlot;
            soTray.FindProperty("_rowShiftSlot").objectReferenceValue   = rowShiftSlot;
            soTray.FindProperty("_cellSwapSlot").objectReferenceValue   = cellSwapSlot;
            soTray.ApplyModifiedProperties();

            SaveScenePrefab(canvas, "InGame");
        }

        static ItemSlotView CreateSlotHelper(GameObject parent, string slotName)
        {
            var go = Child(parent, slotName);
            var rt = RT(go);
            rt.sizeDelta = new Vector2(140f, 140f);

            var le = Comp<LayoutElement>(go);
            le.minWidth = le.preferredWidth = 140f;
            le.minHeight = le.preferredHeight = 140f;

            var visual = Child(go, "Visual");
            Stretch(visual);
            var bgImg = Img(visual, UI_BG_MID);

            var btn = Comp<Button>(go);
            btn.targetGraphic = bgImg;
            Comp<UIButtonAnimator>(go);

            var highlight = Child(visual, "SelectedHighlight");
            Stretch(highlight);
            var hlImg = Img(highlight, UI_BORDER);
            hlImg.rectTransform.offsetMin = new Vector2(-4, -4);
            hlImg.rectTransform.offsetMax = new Vector2(4, 4);
            highlight.SetActive(false);

            var icon = Child(visual, "Icon");
            Fixed(icon, Vector2.zero, new Vector2(90f, 90f));
            var iconImg = Img(icon, Color.white);
            iconImg.preserveAspect = true;

            var countText = TMP(visual, "CountText", Center(30, -35, 70, 45), 16, UI_CTA, "0", null, TextCategory.Normal);
            countText.alignment = TextAlignmentOptions.BottomRight;

            var cg = Comp<CanvasGroup>(go);

            var slotView = Comp<ItemSlotView>(go);
            var so = new SerializedObject(slotView);
            so.FindProperty("_button").objectReferenceValue = btn;
            so.FindProperty("_countText").objectReferenceValue = countText;
            so.FindProperty("_selectedHighlight").objectReferenceValue = highlight;
            so.FindProperty("_canvasGroup").objectReferenceValue = cg;
            so.FindProperty("_icon").objectReferenceValue = iconImg;
            so.ApplyModifiedProperties();

            return slotView;
        }

        // ════════════════════════════════════════════════════════════════
        //  PREFAB BUILDERS
        // ════════════════════════════════════════════════════════════════

        static void CreateConfirmDialog()
        {
            var root = FullScreen("ConfirmDialogView");
            Comp<ConfirmDialogView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var backdrop = Btn(root, "Backdrop", Vector2.zero, new Vector2(1080, 1920), DIM, "", shadowAlpha: 200f / 255f);
            Stretch(backdrop);

            var panel = Panel(root, "Panel", new Vector2(900, 500), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Confirm", CommonBtnConfirm);
            var body  = TMP(panel, "BodyText", Center(0, -10, 800, 120), 18, UI_TEXT, "Are you sure?", null, TextCategory.Normal);
            body.enableWordWrapping = true;
            var bodyCsf = Comp<ContentSizeFitter>(body.gameObject);
            bodyCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            var cancel = Btn(panel, "CancelButton",  new Vector2(-200, -150), new Vector2(320, 90), UI_BG_DEEP, "Cancel", CommonBtnCancel);
            var confirm= Btn(panel, "ConfirmButton", new Vector2( 200, -150), new Vector2(320, 90), UI_PRIMARY, "Confirm", CommonBtnConfirm);

            var so = new SerializedObject(root.GetComponent<ConfirmDialogView>());
            so.FindProperty("_titleText").objectReferenceValue         = title;
            so.FindProperty("_bodyText").objectReferenceValue          = body;
            so.FindProperty("_cancelLabel").objectReferenceValue       = cancel.transform.Find("Visual/Label").GetComponent<TMP_Text>();
            so.FindProperty("_confirmLabel").objectReferenceValue      = confirm.transform.Find("Visual/Label").GetComponent<TMP_Text>();
            so.FindProperty("_cancelButton").objectReferenceValue      = cancel.GetComponent<Button>();
            so.FindProperty("_confirmButton").objectReferenceValue     = confirm.GetComponent<Button>();
            so.FindProperty("_backdropButton").objectReferenceValue    = backdrop.GetComponent<Button>();
            so.FindProperty("_confirmButtonImage").objectReferenceValue = confirm.transform.Find("Visual").GetComponent<Image>();
            so.ApplyModifiedProperties();

            Save(root, "ConfirmDialogView");
        }

        static void CreateItemBuyConfirmPopup()
        {
            var root = FullScreen("ItemBuyConfirmPopupView");
            Comp<ItemBuyConfirmPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var backdrop = Btn(root, "Backdrop", Vector2.zero, new Vector2(1080, 1920), DIM, "", shadowAlpha: 200f / 255f);
            Stretch(backdrop);

            var panel = Panel(root, "Panel", new Vector2(700, 720), UI_BG_MID);

            RibbonTitle(panel, "TitleText", "Buy Item", PopupBuyTitle);

            var iconGo = Child(panel, "Icon");
            Fixed(iconGo, new Vector2(0, 175), new Vector2(110, 110));
            var iconImg = Img(iconGo, Color.white);
            iconImg.preserveAspect = true;

            var nameText = TMP(panel, "NameText", Center(0, 80, 600, 55), 22, UI_TEXT, "Item Name", null, TextCategory.Header);

            var descText = TMP(panel, "DescText", Center(0, 10, 580, 52), 15, new Color(0.75f, 0.75f, 0.75f, 1f), "Item description", null, TextCategory.Normal);
            descText.enableWordWrapping = true;
            descText.alignment = TextAlignmentOptions.Center;

            var priceText = TMP(panel, "PriceText", Center(0, -67, 600, 50), 22, UI_CTA, "Cost: 100 Gold", null, TextCategory.Normal);
            priceText.alignment = TextAlignmentOptions.Center;

            var ownedGoldText = TMP(panel, "OwnedGoldText", Center(0, -127, 600, 50), 18, new Color(0.65f, 0.65f, 0.65f, 1f), "Owned: 350 Gold", null, TextCategory.Normal);
            ownedGoldText.alignment = TextAlignmentOptions.Center;

            var cancel  = Btn(panel, "CancelButton",  new Vector2(-170, -270), new Vector2(280, 88), UI_BG_DEEP, "Cancel", CommonBtnCancel);
            var confirm = Btn(panel, "ConfirmButton",  new Vector2( 170, -270), new Vector2(280, 88), UI_CTA,     "Buy",   CommonBtnConfirm);

            var so = new SerializedObject(root.GetComponent<ItemBuyConfirmPopupView>());
            so.FindProperty("_icon").objectReferenceValue           = iconImg;
            so.FindProperty("_nameText").objectReferenceValue       = nameText;
            so.FindProperty("_descText").objectReferenceValue       = descText;
            so.FindProperty("_priceText").objectReferenceValue      = priceText;
            so.FindProperty("_ownedGoldText").objectReferenceValue  = ownedGoldText;
            so.FindProperty("_cancelButton").objectReferenceValue   = cancel.GetComponent<Button>();
            so.FindProperty("_confirmButton").objectReferenceValue  = confirm.GetComponent<Button>();
            so.FindProperty("_backdropButton").objectReferenceValue = backdrop.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, "ItemBuyConfirmPopupView");
        }

        static void CreateToast()
        {
            var root = new GameObject("ToastView");
            root.AddComponent<RectTransform>();
            TopStrip(root, 120);
            var rt = root.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -260); // sit below HUD/header
            
            // Border background for toast
            var border = Child(root, "Border");
            Stretch(border);
            var borderImg = Img(border, UI_BORDER);
            border.transform.SetAsFirstSibling();
            borderImg.rectTransform.offsetMin = new Vector2(-4, -4);
            borderImg.rectTransform.offsetMax = new Vector2(4, 4);
            
            Img(root, UI_BG_MID); Comp<ToastView>(root); Comp<CanvasGroup>(root);
            var msgTxt = TMP(root, "MessageText", Center(0, 0, 900, 80), 18, UI_TEXT, "Notification", null, TextCategory.Normal);
            msgTxt.overflowMode = TextOverflowModes.Ellipsis;
            msgTxt.enableWordWrapping = false;

            Save(root, "ToastView");
        }

        static void CreateLoadingOverlay()
        {
            var root = FullScreen("LoadingOverlayView");
            Img(root, DIM);
            Comp<LoadingOverlayView>(root);
            
            var spinner = Child(root, "Spinner"); Fixed(spinner, Vector2.zero, new Vector2(120, 120));
            Img(spinner, UI_CTA);
            var loaderAnim = Comp<UIIconIdleAnimator>(spinner);
            loaderAnim.Configure(UIIconIdleAnimator.AnimationType.Rotate, 3f, 45f);

            var msgTxt = TMP(root, "MessageText", Center(0, -140, 600, 60), 20, UI_TEXT, "Loading...", null, TextCategory.Normal);

            var so = new SerializedObject(root.GetComponent<LoadingOverlayView>());
            so.FindProperty("_messageText").objectReferenceValue = msgTxt;
            so.ApplyModifiedProperties();

            Save(root, "LoadingOverlayView");
        }

        static void CreateNetworkError()
        {
            var root = FullScreen("NetworkErrorView");
            Img(root, DIM); Comp<NetworkErrorView>(root);

            var panel = Panel(root, "Panel", new Vector2(800, 420), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Network Error", PopupNetworkErrorTitle);
            var msg   = TMP(panel, "MessageText", Center(0, -10, 680, 150), 20, UI_TEXT, "Check your network connection.", ErrorNetworkCheck, TextCategory.Normal);
            var retry = Btn(panel, "RetryButton", new Vector2(0, -140), new Vector2(320, 90), UI_PRIMARY, "Retry", CommonBtnRetry);

            var so = new SerializedObject(root.GetComponent<NetworkErrorView>());
            so.FindProperty("_messageText").objectReferenceValue = msg;
            so.FindProperty("_retryButton").objectReferenceValue = retry.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, "NetworkErrorView");
        }

        static void CreateRewardPopup()
        {
            // Build RewardItemCell prefab first (background + icon + quantity badge)
            var cellGo = new GameObject("RewardItemCell");
            RT(cellGo).sizeDelta = new Vector2(160, 160);
            Img(cellGo, UI_BG_DEEP);

            var cellIcon = Child(cellGo, "Icon");
            Fixed(cellIcon, new Vector2(0, 10), new Vector2(100, 100));
            var cellIconImg = Img(cellIcon, Color.white);
            cellIconImg.preserveAspect = true;

            var qtyGo = Child(cellGo, "Quantity");
            var qtyRt = RT(qtyGo);
            qtyRt.anchorMin = new Vector2(1, 0);
            qtyRt.anchorMax = new Vector2(1, 0);
            qtyRt.pivot     = new Vector2(1, 0);
            qtyRt.anchoredPosition = new Vector2(-6, 8);
            qtyRt.sizeDelta        = new Vector2(80, 36);
            var qtyTmp = qtyGo.AddComponent<TextMeshProUGUI>();
            qtyTmp.enableAutoSizing  = true;
            qtyTmp.fontSizeMin       = 24f;
            qtyTmp.fontSizeMax       = 36f;
            qtyTmp.fontSize          = 30f;
            qtyTmp.color             = UI_CTA;
            qtyTmp.alignment         = TextAlignmentOptions.BottomRight;
            qtyTmp.text              = "× 1";
            qtyTmp.enableWordWrapping = false;
            Comp<LocalizedText>(qtyGo);
            Comp<UITextStyle>(qtyGo).ApplyStyle();

            string cellPrefabPath = $"{BaseCommonPath}/RewardItemCell.prefab";
            PrefabUtility.SaveAsPrefabAsset(cellGo, cellPrefabPath);
            Object.DestroyImmediate(cellGo);
            var cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cellPrefabPath);

            // Build the popup
            var root = FullScreen("RewardPopupView");
            Img(root, DIM); Comp<RewardPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel = Panel(root, "Panel", new Vector2(720, 780), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Reward!", PopupRewardTitle);

            var items = Child(panel, "ItemContainer");
            Fixed(items, new Vector2(0, 20), new Vector2(400, 400));
            var glg = items.AddComponent<GridLayoutGroup>();
            glg.cellSize         = new Vector2(170, 170);
            glg.spacing          = new Vector2(20, 20);
            glg.padding          = new RectOffset(20, 20, 20, 20);
            glg.constraint       = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount  = 2;
            glg.childAlignment   = TextAnchor.UpperCenter;

            var ok = Btn(panel, "OkButton", new Vector2(0, -280), new Vector2(300, 90), UI_PRIMARY, "OK", CommonBtnOk);

            var so = new SerializedObject(root.GetComponent<RewardPopupView>());
            so.FindProperty("_itemContainer").objectReferenceValue = items.transform;
            so.FindProperty("_itemRowPrefab").objectReferenceValue = cellPrefab;
            so.FindProperty("_okButton").objectReferenceValue      = ok.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, "RewardPopupView");
        }

        static void CreateReLoginView()
        {
            var root = FullScreen("ReLoginView");
            Img(root, UI_BG_DEEP); Comp<ReLoginView>(root);

            var panel   = Panel(root, "Panel", new Vector2(900, 520), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Session Expired", PopupReloginTitle);
            
            var relogin = Btn(panel, "ReLoginButton",         new Vector2(0,  10), new Vector2(500, 90), UI_PRIMARY, "Re-login",          PopupReloginBtnRelogin);
            var guest   = Btn(panel, "ContinueAsGuestButton", new Vector2(0, -110), new Vector2(500, 80), UI_BG_DEEP, "Continue as Guest", PopupReloginBtnGuest);

            var so = new SerializedObject(root.GetComponent<ReLoginView>());
            so.FindProperty("_reLoginButton").objectReferenceValue         = relogin.GetComponent<Button>();
            so.FindProperty("_continueAsGuestButton").objectReferenceValue = guest.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, "ReLoginView");
        }

        static void CreateStageInfoPopup()
        {
            var root = FullScreen("StageInfoPopupView");
            Img(root, DIM); Comp<StageInfoPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var backdrop = Btn(root, "Backdrop", Vector2.zero, new Vector2(1080, 1920), new Color(0, 0, 0, 0), "", shadowAlpha: 200f / 255f);
            Stretch(backdrop);

            var panel = Panel(root, "Panel", new Vector2(700, 760), UI_BG_MID);
            var title = RibbonTitle(panel, "StageTitleText", "Stage 1", PopupStageInfoTitle);
            var ribbonImg = title.transform.parent.GetComponent<Image>();

            var best  = TMP(panel, "BestRecordText", Center(0, 195, 600, 55), 20, UI_TEXT, "Best: 2 Stars", null, TextCategory.Normal);
            var bestCsf = Comp<ContentSizeFitter>(best.gameObject);
            bestCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 3 star placeholders — empty always visible, Fill child shown per bestStars
            var starsRoot = Child(panel, "Stars"); Fixed(starsRoot, new Vector2(0, 90), new Vector2(450, 130));
            var hlg = starsRoot.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            var s0 = StarGO(starsRoot, "Star0", 110f); var s1 = StarGO(starsRoot, "Star1", 110f); var s2 = StarGO(starsRoot, "Star2", 110f);

            // Section label ribbon — "보유 아이템" header above ItemContainer
            var sectionRibbon = Child(panel, "ItemSectionLabel_Ribbon");
            Fixed(sectionRibbon, new Vector2(0, -10), new Vector2(260, 38));
            Img(sectionRibbon, Hex("2B1040"));
            var sectionShadow = Child(sectionRibbon, "Shadow");
            Fixed(sectionShadow, new Vector2(0, -4), new Vector2(260, 38));
            Img(sectionShadow, Hex("2B003B"));
            sectionShadow.transform.SetAsFirstSibling();
            TMP(sectionRibbon, "ItemSectionLabel", Center(0, 0, 240, 34), 16, UI_TEXT, "Owned Items", PopupStageInfoOwnedItems, TextCategory.Normal);

            // Item container — HorizontalLayoutGroup; booster items
            var itemContainer = Child(panel, "ItemContainer");
            Fixed(itemContainer, new Vector2(0, -110), new Vector2(420, 148));
            Img(itemContainer, Hex("1A0D2E")); // subtle dark bg to visually group item section
            var itemContainerGroup = Comp<CanvasGroup>(itemContainer);
            var itemHlg = itemContainer.AddComponent<HorizontalLayoutGroup>();
            itemHlg.spacing = 20f;
            itemHlg.childAlignment = TextAnchor.MiddleCenter;
            itemHlg.childForceExpandWidth  = false;
            itemHlg.childForceExpandHeight = false;
            itemHlg.childControlWidth  = false;
            itemHlg.childControlHeight = false;
            itemHlg.padding = new RectOffset(10, 10, 10, 10);

            var toggleRow = ItemToggleRow(itemContainer, "ExtraTurnsToggleRow", Vector2.zero, "Add Turns", ItemNameAddTurn);
            var countTmp = TMP(toggleRow, "CountText", Center(0, -38, 120, 26), 16, UI_CTA, "×0", null, TextCategory.Normal);

            var play = Btn(panel, "PlayButton", new Vector2(0, -285), new Vector2(400, 80), UI_CTA, "Play", CommonBtnPlay);

            var so = new SerializedObject(root.GetComponent<StageInfoPopupView>());
            so.FindProperty("_stageTitle").objectReferenceValue       = title;
            so.FindProperty("_bestRecord").objectReferenceValue       = best;
            so.FindProperty("_ribbonImage").objectReferenceValue      = ribbonImg;
            so.FindProperty("_playButton").objectReferenceValue       = play.GetComponent<Button>();
            so.FindProperty("_backdropButton").objectReferenceValue   = backdrop.GetComponent<Button>();
            so.FindProperty("_extraTurnsToggle").objectReferenceValue = toggleRow.GetComponent<Toggle>();
            so.FindProperty("_extraTurnsStateIcon").objectReferenceValue =
                toggleRow.transform.Find("StateIndicator")?.GetComponent<Image>();
            so.FindProperty("_itemCountText").objectReferenceValue    = countTmp;
            so.FindProperty("_itemContainerGroup").objectReferenceValue = itemContainerGroup;
            var starsArr = so.FindProperty("_bestStarFills");
            starsArr.arraySize = 3;
            starsArr.GetArrayElementAtIndex(0).objectReferenceValue = s0.transform.Find("Fill").gameObject;
            starsArr.GetArrayElementAtIndex(1).objectReferenceValue = s1.transform.Find("Fill").gameObject;
            starsArr.GetArrayElementAtIndex(2).objectReferenceValue = s2.transform.Find("Fill").gameObject;

            var resMap = LoadDynamicResourceMap();
            var starEmpty  = resMap.TryGetValue("star_empty",  out string ep) ? AssetDatabase.LoadAssetAtPath<Sprite>(ep)  : null;
            var starFilled = resMap.TryGetValue("star_filled", out string fp) ? AssetDatabase.LoadAssetAtPath<Sprite>(fp) : null;
            var addTurnSpr = resMap.TryGetValue("item_add_turn", out string atp) ? AssetDatabase.LoadAssetAtPath<Sprite>(atp) : null;
            if (resMap.TryGetValue("ui_toggle_on",  out string tonPath))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(tonPath);
                if (spr != null) so.FindProperty("_toggleOnSprite").objectReferenceValue = spr;
            }
            if (resMap.TryGetValue("ui_toggle_off", out string toffPath))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(toffPath);
                if (spr != null) so.FindProperty("_toggleOffSprite").objectReferenceValue = spr;
            }
            so.ApplyModifiedProperties();

            foreach (var star in new[] { s0, s1, s2 })
            {
                if (starEmpty  != null) { star.GetComponent<Image>().sprite = starEmpty; }
                var fillImg = star.transform.Find("Fill")?.GetComponent<Image>();
                if (fillImg != null && starFilled != null) fillImg.sprite = starFilled;
            }
            var extraIcon = toggleRow.GetComponent<Image>();
            if (extraIcon != null && addTurnSpr != null) extraIcon.sprite = addTurnSpr;

            Save(root, "StageInfoPopupView");
        }

        static void CreateResultOverlay()
        {
            var root = FullScreen("ResultOverlayView");
            Img(root, DIM); Comp<ResultOverlayView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel = Panel(root, "Panel", new Vector2(900, 900), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Stage Clear!", PopupResultTitle);

            var ratio  = TMP(panel, "RatioText", Center(0,   65, 700, 60), 22, UI_TEXT, "Cleared: 90%", null, TextCategory.Normal);
            var turns  = TMP(panel, "TurnsText", Center(0,  -20, 700, 60), 22, UI_TEXT, "Turns: 12/20", null, TextCategory.Normal);
            var rank   = TMP(panel, "RankText",  Center(0, -105, 700, 55), 20, UI_CTA,  "",             null, TextCategory.Normal);

            var goldRow = Child(panel, "GoldRow"); Fixed(goldRow, new Vector2(0, -185), new Vector2(700, 60));
            var goldTxt = TMP(goldRow, "GoldText", Center(0, 0, 700, 60), 24, UI_CTA, "+120 Gold", null, TextCategory.Normal);

            // Stars — UIStarPop drives Fill children sequentially left-to-right with punch animation
            var starsRoot = Child(panel, "Stars"); Fixed(starsRoot, new Vector2(0, 240), new Vector2(600, 160));
            var hlg = starsRoot.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            var s0 = StarGO(starsRoot, "Star0", 110f); var s1 = StarGO(starsRoot, "Star1", 110f); var s2 = StarGO(starsRoot, "Star2", 110f);

            var retry = Btn(panel, "RetryButton", new Vector2(-270, -330), new Vector2(230, 90), UI_BG_DEEP, "Retry", CommonBtnRetry);
            var next  = Btn(panel, "NextButton",  new Vector2(   0, -330), new Vector2(230, 90), UI_PRIMARY,  "Next",  CommonBtnNext);
            var map   = Btn(panel, "MapButton",   new Vector2( 270, -330), new Vector2(230, 90), UI_BG_DEEP,  "Map",   CommonBtnMap);

            var so = new SerializedObject(root.GetComponent<ResultOverlayView>());
            so.FindProperty("_titleText").objectReferenceValue  = title;
            so.FindProperty("_ratioText").objectReferenceValue  = ratio;
            so.FindProperty("_turnsText").objectReferenceValue  = turns;
            so.FindProperty("_rankText").objectReferenceValue   = rank;
            so.FindProperty("_goldText").objectReferenceValue   = goldTxt;
            so.FindProperty("_goldRow").objectReferenceValue    = goldRow;
            so.FindProperty("_retryButton").objectReferenceValue = retry.GetComponent<Button>();
            so.FindProperty("_nextButton").objectReferenceValue  = next.GetComponent<Button>();
            so.FindProperty("_mapButton").objectReferenceValue   = map.GetComponent<Button>();
            var starsArr = so.FindProperty("_starObjects");
            starsArr.arraySize = 3;
            starsArr.GetArrayElementAtIndex(0).objectReferenceValue = s0;
            starsArr.GetArrayElementAtIndex(1).objectReferenceValue = s1;
            starsArr.GetArrayElementAtIndex(2).objectReferenceValue = s2;
            so.ApplyModifiedProperties();

            var resMap = LoadDynamicResourceMap();
            var starEmpty  = resMap.TryGetValue("star_empty",  out string ep) ? AssetDatabase.LoadAssetAtPath<Sprite>(ep)  : null;
            var starFilled = resMap.TryGetValue("star_filled", out string fp) ? AssetDatabase.LoadAssetAtPath<Sprite>(fp) : null;
            foreach (var star in new[] { s0, s1, s2 })
            {
                if (starEmpty  != null) { star.GetComponent<Image>().sprite = starEmpty; }
                var fillImg = star.transform.Find("Fill")?.GetComponent<Image>();
                if (fillImg != null && starFilled != null) fillImg.sprite = starFilled;
            }

            Save(root, "ResultOverlayView");
        }

        static void CreateFailOverlay()
        {
            var root = FullScreen("FailOverlayView");
            Img(root, DIM); Comp<FailOverlayView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel = Panel(root, "Panel", new Vector2(700, 780), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Just a bit more!", PopupFailTitle);

            var contLabel = TMP(panel, "ContinueLabel", Center(0,  170, 600, 60), 24, UI_TEXT, "+3 Turns", null, TextCategory.Normal);
            var costTxt   = TMP(panel, "CostText",       Center(0,   85, 600, 55), 22, UI_CTA,  "Cost: 150", null, TextCategory.Normal);
            var ownedTxt  = TMP(panel, "OwnedGoldText",  Center(0,    5, 600, 55), 20, UI_TEXT, "Gold: 320", null, TextCategory.Normal);

            var limitTxt = TMP(panel, "ReviveLimitText", Center(165, -70, 280, 40), 14, UI_TEXT, "Remaining Revives: 3", null, TextCategory.Normal);
            var adBtn   = Btn(panel, "WatchAdButton",  new Vector2( 165, -165), new Vector2(280, 95), UI_PRIMARY, "Watch Ad", PopupFailWatchAd);
            var contBtn = Btn(panel, "ContinueButton", new Vector2(-165, -165), new Vector2(280, 95), UI_CTA,     "Continue", PopupFailBtnContinue);
            var forfBtn = Btn(panel, "ForfeitButton",  new Vector2(   0, -285), new Vector2(280, 95), UI_BG_DEEP, "Give Up",  PopupFailBtnForfeit);

            var so = new SerializedObject(root.GetComponent<FailOverlayView>());
            so.FindProperty("_continueLabel").objectReferenceValue  = contLabel;
            so.FindProperty("_costText").objectReferenceValue       = costTxt;
            so.FindProperty("_ownedGoldText").objectReferenceValue  = ownedTxt;
            so.FindProperty("_continueButton").objectReferenceValue = contBtn.GetComponent<Button>();
            so.FindProperty("_forfeitButton").objectReferenceValue  = forfBtn.GetComponent<Button>();
            so.FindProperty("_watchAdButton").objectReferenceValue  = adBtn.GetComponent<Button>();
            so.FindProperty("_reviveLimitText").objectReferenceValue = limitTxt;
            so.ApplyModifiedProperties();

            Save(root, "FailOverlayView");
        }

        static void CreatePausePopup()
        {
            var root = FullScreen("PausePopupView");
            Img(root, DIM); Comp<PausePopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel = Panel(root, "Panel", new Vector2(600, 600), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Paused", PopupPauseTitle);

            var resume  = Btn(panel, "ResumeButton",      new Vector2(0,  120), new Vector2(480, 90), UI_PRIMARY, "Resume",       PopupPauseBtnResume);
            var restart = Btn(panel, "RestartButton",     new Vector2(0,   10), new Vector2(480, 90), UI_BG_DEEP, "Restart",      PopupPauseBtnRestart);
            var settings= Btn(panel, "SettingsButton",    new Vector2(0, -100), new Vector2(480, 90), UI_BG_DEEP, "Settings",     CommonSettings);
            var select  = Btn(panel, "StageSelectButton", new Vector2(0, -210), new Vector2(480, 90), UI_BG_DEEP, "Stage Select", PopupPauseBtnStageSelect);

            var so = new SerializedObject(root.GetComponent<PausePopupView>());
            so.FindProperty("_resumeButton").objectReferenceValue      = resume.GetComponent<Button>();
            so.FindProperty("_restartButton").objectReferenceValue     = restart.GetComponent<Button>();
            so.FindProperty("_settingsButton").objectReferenceValue    = settings.GetComponent<Button>();
            so.FindProperty("_stageSelectButton").objectReferenceValue = select.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, "PausePopupView");
        }

        static void CreateSettingsPanel()
        {
            var root = FullScreen("SettingsPanelView");
            Img(root, DIM); Comp<SettingsPanelView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var backdrop = Btn(root, "Backdrop", Vector2.zero, new Vector2(1080, 1920), new Color(0,0,0,0), "", shadowAlpha: 200f / 255f);
            Stretch(backdrop);

            // Bottom-sheet: taller panel so dropdown popup opens within screen bounds
            var border = Child(root, "Border");
            BottomStrip(border, 1050);
            Img(border, Hex("2B003B"));

            var panel = Child(border, "InnerPanel");
            BottomStrip(panel, 1030);
            Img(panel, UI_BG_MID);

            var title = RibbonTitle(panel, "TitleText", "Settings", CommonSettings);

            var bgmRow    = SoundRow(panel,  "BGMRow",         new Vector2(0,  370), "BGM",          PopupSettingsBgm);
            var sfxRow    = SoundRow(panel,  "SFXRow",         new Vector2(0,  265), "SFX",          PopupSettingsSfx);
            var shakeRow  = ToggleRow(panel, "ScreenShakeRow", new Vector2(0,  160), "Screen Shake", PopupSettingsScreenShake);
            var hapticRow = ToggleRow(panel, "HapticRow",      new Vector2(0,   65), "Haptic",       PopupSettingsHaptic);

            var langDropdown = LanguageDropdownRow(panel, "LanguageRow", new Vector2(0, -45), "Language", PopupSettingsLanguage);
            var verTxt = TMP(panel, "VersionText", Center(0, -175, 600, 50), 16, UI_TEXT, "v1.0.0", null, TextCategory.Normal);

            var so = new SerializedObject(root.GetComponent<SettingsPanelView>());
            so.FindProperty("_bgmToggle").objectReferenceValue         = bgmRow.transform.Find("Toggle").GetComponent<Toggle>();
            so.FindProperty("_sfxToggle").objectReferenceValue         = sfxRow.transform.Find("Toggle").GetComponent<Toggle>();
            so.FindProperty("_bgmSlider").objectReferenceValue         = bgmRow.transform.Find("Slider").GetComponent<Slider>();
            so.FindProperty("_sfxSlider").objectReferenceValue         = sfxRow.transform.Find("Slider").GetComponent<Slider>();
            so.FindProperty("_screenShakeToggle").objectReferenceValue = shakeRow.GetComponentInChildren<Toggle>();
            so.FindProperty("_hapticToggle").objectReferenceValue      = hapticRow.GetComponentInChildren<Toggle>();
            so.FindProperty("_langDropdown").objectReferenceValue      = langDropdown;
            so.FindProperty("_backdropButton").objectReferenceValue    = backdrop.GetComponent<Button>();
            so.FindProperty("_versionText").objectReferenceValue       = verTxt;
            so.ApplyModifiedProperties();

            Save(root, "SettingsPanelView");
        }

        static void CreateAccountPopup()
        {
            var root = FullScreen("AccountPopupView");
            Img(root, DIM); Comp<AccountPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel = Panel(root, "Panel", new Vector2(850, 1200), UI_BG_MID);
            var uidTxt = RibbonTitle(panel, "UserIdText", "Guest", CommonGuest);

            // Tabs setup (y = 505)
            var avatarTabBtn = Btn(panel, "AvatarTabButton", new Vector2(-200, 505), new Vector2(320, 75), UI_PRIMARY, "Avatars", PopupAccountTabAvatars);
            var themeTabBtn = Btn(panel, "BoardThemeTabButton", new Vector2(200, 505), new Vector2(320, 75), UI_BG_MID, "Board Skins", PopupAccountTabBoardSkins);

            // 1. Nickname Input Area (grouped in NicknameArea)
            var nicknameAreaGo = Child(panel, "NicknameArea");
            Fixed(nicknameAreaGo, new Vector2(0, 380), new Vector2(800, 160));
            
            var nickLabel = TMP(nicknameAreaGo, "NicknameLabel", Center(0, 45, 750, 50), 20, UI_TEXT, "Nickname", PopupAccountLabelNickname, TextCategory.Normal);

            var inputFieldGo = Child(nicknameAreaGo, "DisplayNameInput");
            Fixed(inputFieldGo, new Vector2(-100, -30), new Vector2(500, 80));
            var inputImg = inputFieldGo.AddComponent<Image>();
            inputImg.color = UI_BG_DEEP;
            var inputField = inputFieldGo.AddComponent<TMP_InputField>();

            var textAreaGo = Child(inputFieldGo, "TextArea");
            Stretch(textAreaGo);
            var textAreaRt = RT(textAreaGo);
            textAreaRt.offsetMin = new Vector2(15, 10);
            textAreaRt.offsetMax = new Vector2(-15, -10);
            textAreaGo.AddComponent<RectMask2D>();

            var textComponentGo = Child(textAreaGo, "TextComponent");
            Stretch(textComponentGo);
            var textTmp = textComponentGo.AddComponent<TextMeshProUGUI>();
            textTmp.fontSize = 28f;
            textTmp.color = UI_TEXT;
            textTmp.alignment = TextAlignmentOptions.Left;

            var placeholderGo = Child(textAreaGo, "Placeholder");
            Stretch(placeholderGo);
            var placeholderTmp = placeholderGo.AddComponent<TextMeshProUGUI>();
            placeholderTmp.fontSize = 28f;
            placeholderTmp.color = new Color(UI_TEXT.r, UI_TEXT.g, UI_TEXT.b, 0.5f);
            placeholderTmp.text = "Enter nickname...";
            placeholderTmp.alignment = TextAlignmentOptions.Left;
            var placeholderLt = Comp<LocalizedText>(placeholderGo);
            var soPlaceholder = new SerializedObject(placeholderLt);
            soPlaceholder.FindProperty("_stringId").stringValue = PopupAccountPlaceholderNickname;
            soPlaceholder.ApplyModifiedProperties();

            inputField.textViewport = textAreaRt;
            inputField.textComponent = textTmp;
            inputField.placeholder = placeholderTmp;

            var saveBtn = Btn(nicknameAreaGo, "SaveNicknameButton", new Vector2(250, -30), new Vector2(160, 80), UI_CTA, "Save", PopupAccountBtnSave);

            // 2. Avatar & Theme Grid Area
            var gridLabelTxt = TMP(panel, "GridLabelText", Center(0, 220, 750, 50), 20, UI_TEXT, "Choose Avatar", PopupAccountLabelSelectAvatar, TextCategory.Normal);

            var scrollViewGo = Child(panel, "AvatarScrollView");
            Fixed(scrollViewGo, new Vector2(0, -60), new Vector2(750, 420));
            var scrollRect = scrollViewGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var viewportGo = Child(scrollViewGo, "Viewport");
            Stretch(viewportGo);
            viewportGo.AddComponent<RectMask2D>();
            var viewportImg = viewportGo.AddComponent<Image>();
            viewportImg.color = new Color(0, 0, 0, 0.01f);
            scrollRect.viewport = RT(viewportGo);

            var contentGo = Child(viewportGo, "Content");
            var contentRt = RT(contentGo);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 420);
            scrollRect.content = contentRt;

            var grid = contentGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(130, 130);
            grid.spacing = new Vector2(12, 12);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(15, 15, 15, 15);

            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Template Slot for Grid
            var templateGo = Child(contentGo, "AvatarSlotTemplate");
            RT(templateGo).sizeDelta = new Vector2(130, 130);
            var slotBg = templateGo.AddComponent<Image>();
            slotBg.color = UI_BG_DEEP;
            var templateBtn = templateGo.AddComponent<Button>();
            templateBtn.targetGraphic = slotBg;
            Comp<UIButtonAnimator>(templateGo);

            var visualGo = Child(templateGo, "Visual");
            Stretch(visualGo);

            var iconGo = Child(visualGo, "Icon");
            Fixed(iconGo, Vector2.zero, new Vector2(90, 90));
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.preserveAspect = true;

            var hlGo = Child(visualGo, "SelectedHighlight");
            Stretch(hlGo);
            var hlImg = hlGo.AddComponent<Image>();
            hlImg.color = UI_BORDER;
            RT(hlGo).offsetMin = new Vector2(-4, -4);
            RT(hlGo).offsetMax = new Vector2(4, 4);
            hlGo.SetActive(false);

            var lockGo = Child(visualGo, "LockOverlay");
            Stretch(lockGo);
            var lockBg = lockGo.AddComponent<Image>();
            lockBg.color = new Color(0, 0, 0, 0.6f);

            var lockIconGo = Child(lockGo, "LockIcon");
            Fixed(lockIconGo, new Vector2(0, 15), new Vector2(40, 40));
            var lockIconImg = lockIconGo.AddComponent<Image>();
            lockIconImg.color = Color.white;
            lockIconImg.preserveAspect = true;

            var costTextGo = Child(lockGo, "CostText");
            Fixed(costTextGo, new Vector2(0, -35), new Vector2(120, 35));
            var costTxt = costTextGo.AddComponent<TextMeshProUGUI>();
            costTxt.fontSize = 20f;
            costTxt.color = UI_CTA;
            costTxt.alignment = TextAlignmentOptions.Center;
            costTxt.text = "0";

            lockGo.SetActive(false);

            // 3. Platform Account Buttons
            var linkBtn = Btn(panel, "LinkAccountButton",   new Vector2(0, -340), new Vector2(600, 85), UI_PRIMARY, "Link Account",   PopupAccountBtnLink);
            var swBtn   = Btn(panel, "SwitchAccountButton", new Vector2(0, -340), new Vector2(600, 85), UI_PRIMARY, "Switch Account", PopupAccountBtnSwitch);
            var clsBtn  = Btn(panel, "CloseButton",         new Vector2(0, -450), new Vector2(260, 75), UI_BG_DEEP, "Close",          CommonBtnClose);

            // 4. Map Avatar Sprites
            var resMap = LoadDynamicResourceMap();
            var avatarCsvPath = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "../../..")), "shared/datas/avatar/avatar.csv");
            var avatarMappings = new List<AccountPopupView.AvatarSpriteMapping>();

            if (File.Exists(avatarCsvPath))
            {
                var lines = File.ReadAllLines(avatarCsvPath);
                for (int i = 4; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    var cols = line.Split(',');
                    if (cols.Length >= 2)
                    {
                        if (int.TryParse(cols[0].Trim(), out int avatarId))
                        {
                            string resourceName = cols[1].Trim();
                            Sprite sprite = null;
                            if (resMap.TryGetValue(resourceName, out string spritePath))
                            {
                                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                            }
                            
                            avatarMappings.Add(new AccountPopupView.AvatarSpriteMapping
                            {
                                avatarId = avatarId,
                                resourceName = resourceName,
                                sprite = sprite
                            });
                        }
                    }
                }
            }

            // Map Board Theme Sprites
            var themeCsvPath = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "../../..")), "shared/datas/board_theme/board_theme.csv");
            var themeMappings = new List<AccountPopupView.BoardThemeSpriteMapping>();

            if (File.Exists(themeCsvPath))
            {
                var lines = File.ReadAllLines(themeCsvPath);
                for (int i = 4; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    var cols = line.Split(',');
                    if (cols.Length >= 2)
                    {
                        if (int.TryParse(cols[0].Trim(), out int themeId))
                        {
                            string resourceName = cols[1].Trim();
                            Sprite borderSprite = null;
                            Sprite socketSprite = null;
                            if (resMap.TryGetValue($"{resourceName}_border", out string borderPath))
                            {
                                borderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(borderPath);
                            }
                            if (resMap.TryGetValue($"{resourceName}_socket", out string socketPath))
                            {
                                socketSprite = AssetDatabase.LoadAssetAtPath<Sprite>(socketPath);
                            }
                            
                            themeMappings.Add(new AccountPopupView.BoardThemeSpriteMapping
                            {
                                themeId = themeId,
                                resourceName = resourceName,
                                borderSprite = borderSprite,
                                socketSprite = socketSprite
                            });
                        }
                    }
                }
            }

            if (resMap.TryGetValue("ui_lock_icon", out string lockIconPath))
            {
                var lockIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(lockIconPath);
                if (lockIconSprite != null)
                {
                    lockIconImg.sprite = lockIconSprite;
                    EditorUtility.SetDirty(lockIconImg);
                }
            }

            // 5. Serialize Object Wiring
            var so = new SerializedObject(root.GetComponent<AccountPopupView>());
            so.FindProperty("_userIdText").objectReferenceValue          = uidTxt;
            so.FindProperty("_linkAccountButton").objectReferenceValue   = linkBtn.GetComponent<Button>();
            so.FindProperty("_switchAccountButton").objectReferenceValue = swBtn.GetComponent<Button>();
            so.FindProperty("_closeButton").objectReferenceValue         = clsBtn.GetComponent<Button>();

            so.FindProperty("_displayNameInput").objectReferenceValue    = inputField;
            so.FindProperty("_saveNicknameButton").objectReferenceValue   = saveBtn.GetComponent<Button>();
            so.FindProperty("_avatarGridParent").objectReferenceValue     = contentRt;
            so.FindProperty("_avatarSlotTemplate").objectReferenceValue   = templateGo;

            so.FindProperty("_avatarTabButton").objectReferenceValue     = avatarTabBtn.GetComponent<Button>();
            so.FindProperty("_boardThemeTabButton").objectReferenceValue  = themeTabBtn.GetComponent<Button>();
            so.FindProperty("_nicknameArea").objectReferenceValue         = nicknameAreaGo;
            so.FindProperty("_gridLabelText").objectReferenceValue        = gridLabelTxt;

            var avatarSpritesProp = so.FindProperty("_avatarSprites");
            avatarSpritesProp.ClearArray();
            avatarSpritesProp.arraySize = avatarMappings.Count;
            for (int i = 0; i < avatarMappings.Count; i++)
            {
                var elem = avatarSpritesProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("avatarId").intValue = avatarMappings[i].avatarId;
                elem.FindPropertyRelative("resourceName").stringValue = avatarMappings[i].resourceName;
                elem.FindPropertyRelative("sprite").objectReferenceValue = avatarMappings[i].sprite;
            }

            var themeSpritesProp = so.FindProperty("_boardThemeSprites");
            themeSpritesProp.ClearArray();
            themeSpritesProp.arraySize = themeMappings.Count;
            for (int i = 0; i < themeMappings.Count; i++)
            {
                var elem = themeSpritesProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("themeId").intValue = themeMappings[i].themeId;
                elem.FindPropertyRelative("resourceName").stringValue = themeMappings[i].resourceName;
                elem.FindPropertyRelative("borderSprite").objectReferenceValue = themeMappings[i].borderSprite;
                elem.FindPropertyRelative("socketSprite").objectReferenceValue = themeMappings[i].socketSprite;
            }

            so.ApplyModifiedProperties();

            Save(root, "AccountPopupView");
        }

        static void CreateStageNodeView()
        {
            var root = new GameObject("StageNodeView");
            var rt = RT(root);
            rt.sizeDelta = new Vector2(130f, 130f);
            
            var snv = Comp<StageNodeView>(root);
            Comp<UIButtonAnimator>(root);

            // Pulse Ring
            var pulseRing = Child(root, "PulseRing");
            Fixed(pulseRing, Vector2.zero, new Vector2(150f, 150f));
            Img(pulseRing, UI_CTA);
            Comp<UIScalePulse>(pulseRing);
            pulseRing.SetActive(false);

            // Difficulty outline (colored at runtime by DifficultyStyle; hidden for Easy)
            var diffOutline = Child(root, "DifficultyOutline");
            Fixed(diffOutline, Vector2.zero, new Vector2(148f, 148f));
            var diffOutlineImg = Img(diffOutline, Color.white);
            diffOutline.SetActive(false);

            // Visual background border
            var visual = Child(root, "Visual");
            Stretch(visual);
            var borderImg = Img(visual, Color.white);
            
            // Inner circle
            var inner = Child(visual, "Inner");
            Fixed(inner, Vector2.zero, new Vector2(110f, 110f));
            Img(inner, UI_PRIMARY);
            
            // Label
            var stageLabel = TMP(inner, "StageLabel", Center(0, 0, 110, 110), 22, UI_TEXT, "1", null, TextCategory.Button);
            
            // Stars container — empty sprite always visible; Fill child shown per earned stars
            var starsRoot = Child(root, "Stars");
            Fixed(starsRoot, new Vector2(0f, -60f), new Vector2(110f, 32f));
            var hlg = starsRoot.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            var s0 = StarGO(starsRoot, "Star0", 30f);
            var s1 = StarGO(starsRoot, "Star1", 30f);
            var s2 = StarGO(starsRoot, "Star2", 30f);

            // Lock Overlay
            var lockOverlay = Child(root, "LockOverlay");
            Stretch(lockOverlay);
            Img(lockOverlay, new Color(0f, 0f, 0f, 0f));
            var lockIcon = Child(lockOverlay, "LockIcon");
            Fixed(lockIcon, Vector2.zero, new Vector2(40f, 40f));
            Img(lockIcon, Color.white);
            lockOverlay.SetActive(false);

            // Skull badge (Hard only — top-right corner of 130x130 root)
            var skullBadge = Child(root, "SkullBadge");
            Fixed(skullBadge, new Vector2(45f, 45f), new Vector2(40f, 40f));
            var skullBadgeImg = Img(skullBadge, Color.white);
            skullBadge.SetActive(false);

            // Button
            var btn = Comp<Button>(root);
            btn.targetGraphic = borderImg;

            // Wire StageNodeView properties
            var so = new SerializedObject(snv);
            so.FindProperty("_stageLabel").objectReferenceValue       = stageLabel;
            so.FindProperty("_lockOverlay").objectReferenceValue      = lockOverlay;
            var pulseRingProp = so.FindProperty("_pulseRing");
            if (pulseRingProp != null) pulseRingProp.objectReferenceValue = pulseRing;
            so.FindProperty("_button").objectReferenceValue           = btn;
            so.FindProperty("_border").objectReferenceValue           = borderImg;
            so.FindProperty("_difficultyOutline").objectReferenceValue = diffOutlineImg;
            so.FindProperty("_skullIcon").objectReferenceValue        = skullBadge;

            var starFillsArr = so.FindProperty("_starFills");
            starFillsArr.arraySize = 3;
            starFillsArr.GetArrayElementAtIndex(0).objectReferenceValue = s0.transform.Find("Fill").gameObject;
            starFillsArr.GetArrayElementAtIndex(1).objectReferenceValue = s1.transform.Find("Fill").gameObject;
            starFillsArr.GetArrayElementAtIndex(2).objectReferenceValue = s2.transform.Find("Fill").gameObject;
            so.ApplyModifiedProperties();

            var resMap = LoadDynamicResourceMap();
            var starEmpty  = resMap.TryGetValue("star_empty",  out string ep) ? AssetDatabase.LoadAssetAtPath<Sprite>(ep)  : null;
            var starFilled = resMap.TryGetValue("star_filled", out string fp) ? AssetDatabase.LoadAssetAtPath<Sprite>(fp) : null;
            var lockSpr    = resMap.TryGetValue("ui_lock_icon", out string lp) ? AssetDatabase.LoadAssetAtPath<Sprite>(lp) : null;
            foreach (var star in new[] { s0, s1, s2 })
            {
                if (starEmpty  != null) { star.GetComponent<Image>().sprite = starEmpty; }
                var fillImg = star.transform.Find("Fill")?.GetComponent<Image>();
                if (fillImg != null && starFilled != null) fillImg.sprite = starFilled;
            }
            var lockImgComp = lockIcon.GetComponent<Image>();
            if (lockImgComp != null && lockSpr != null) { lockImgComp.sprite = lockSpr; lockImgComp.preserveAspect = true; }
            var skullSpr = resMap.TryGetValue("ui_hard_skull", out string skulp) ? AssetDatabase.LoadAssetAtPath<Sprite>(skulp) : null;
            if (skullSpr != null) { skullBadgeImg.sprite = skullSpr; skullBadgeImg.preserveAspect = true; }

            Save(root, "StageNodeView");
        }

        static void CreateStaminaPopup()
        {
            var root = FullScreen("StaminaPopupView");
            Img(root, DIM);
            Comp<StaminaPopupView>(root);
            Comp<UIPanelAppear>(root);
            Comp<CanvasGroup>(root);

            // Transparent backdrop for tap-to-close
            var backdrop = Child(root, "Backdrop"); Stretch(backdrop);
            var backdropBtn = Comp<Button>(backdrop);
            backdropBtn.targetGraphic = Img(backdrop, new Color(0, 0, 0, 0));

            var panel = Panel(root, "Panel", new Vector2(600, 560), UI_BG_MID);
            RibbonTitle(panel, "TitleText", "Lives", PopupStaminaTitle);

            // Large heart icon: pulsing with count number overlaid
            var heartDisplay = Child(panel, "HeartDisplay");
            Fixed(heartDisplay, new Vector2(0, 80), new Vector2(140, 140));
            Img(heartDisplay, UI_PRIMARY);
            Comp<UIIconIdleAnimator>(heartDisplay).Configure(UIIconIdleAnimator.AnimationType.GlowSweep, 2.4f, 15f);

            var countText = TMP(heartDisplay, "CountText", Center(0, 0, 140, 140), 56, UI_TEXT, "5", null, TextCategory.Header);

            // Timer / MAX label
            var timerText = TMP(panel, "TimerText", Center(0, -30, 500, 64), 26, UI_CTA, "MAX", StaminaMax, TextCategory.Normal);

            // Watch Ad button (+1 life) — CanvasGroup so we can dim at MAX
            var watchAdGo = Child(panel, "WatchAdButton");
            Fixed(watchAdGo, new Vector2(0, -130), new Vector2(440, 90));
            Img(watchAdGo, UI_SUCCESS);
            var watchAdBtn = Comp<Button>(watchAdGo);
            Comp<UIButtonAnimator>(watchAdGo);
            var watchAdCg = Comp<CanvasGroup>(watchAdGo);
            TMP(watchAdGo, "Label", Center(0, 0, 440, 90), 24, UI_TEXT, "Watch Ad (+1)", PopupStaminaWatchAd, TextCategory.Button);

            // Close button
            var closeGo = Btn(panel, "CloseButton", new Vector2(0, -238), new Vector2(260, 75), UI_BG_DEEP, "Close", CommonBtnClose);

            // Wire all fields
            var so = new SerializedObject(root.GetComponent<StaminaPopupView>());
            so.FindProperty("_countText").objectReferenceValue          = countText;
            so.FindProperty("_timerText").objectReferenceValue          = timerText;
            so.FindProperty("_watchAdButton").objectReferenceValue      = watchAdBtn;
            so.FindProperty("_watchAdButtonGroup").objectReferenceValue = watchAdCg;
            so.FindProperty("_closeButton").objectReferenceValue        = closeGo.GetComponent<Button>();
            so.FindProperty("_backdropButton").objectReferenceValue     = backdropBtn;
            so.ApplyModifiedProperties();

            Save(root, "StaminaPopupView");
        }

        static void CreateTutorialOverlay()
        {
            var root = FullScreen("TutorialOverlay");
            var overlayScript = Comp<TutorialOverlay>(root);

            // DimLayer — always-visible full-screen dim; stays active for all steps (including blocking).
            // raycastTarget=true blocks EventSystem interaction (HUD buttons etc.) during blocking steps.
            var dimGo = Child(root, "DimLayer");
            Stretch(dimGo);
            var dimImg = Img(dimGo, new Color(0.05f, 0.05f, 0.12f, 0.88f));

            // Fullscreen Dismiss Button — transparent hit area for tap-to-advance on non-blocking steps.
            // Has no background; DimLayer above provides the visual dim.
            var dismissGo = Child(root, "FullscreenDismissButton");
            Stretch(dismissGo);
            if (!dismissGo.TryGetComponent<Button>(out var dismissBtn)) dismissBtn = dismissGo.AddComponent<Button>();
            var dismissImg = Img(dismissGo, new Color(0, 0, 0, 0));
            dismissBtn.targetGraphic = dismissImg;
            Comp<UIButtonAnimator>(dismissGo);

            // Spotlight Cutout
            var spotlight = Child(root, "SpotlightCutout");
            Fixed(spotlight, Vector2.zero, new Vector2(150, 150));
            Img(spotlight, new Color(1, 1, 1, 0.0f)); // transparent: dim cutout perception only

            // Spotlight Glow
            var glow = Child(spotlight, "SpotlightGlow");
            Stretch(glow);
            var glowRt = RT(glow);
            glowRt.offsetMin = new Vector2(-10, -10);
            glowRt.offsetMax = new Vector2(10, 10);
            var glowImg = Img(glow, new Color(1, 0.9f, 0.4f, 0.08f)); // subtle border; runtime AnimateGlowPulse drives alpha

            // Tooltip Bubble
            var bubble = Child(root, "TooltipBubble");
            Fixed(bubble, Vector2.zero, new Vector2(800, 300));
            Img(bubble, new Color(0.1f, 0.15f, 0.25f, 0.95f));

            // Tooltip Text
            var textGo = Child(bubble, "TooltipText");
            Stretch(textGo);
            var textRt = RT(textGo);
            textRt.offsetMin = new Vector2(20, 20);
            textRt.offsetMax = new Vector2(-20, -20);
            var textTmp = textGo.GetComponent<TextMeshProUGUI>();
            if (textTmp == null) textTmp = textGo.AddComponent<TextMeshProUGUI>();
            textTmp.fontSizeMin = 24f;
            textTmp.fontSizeMax = 36f;
            textTmp.fontSize = 32f;
            textTmp.color = Color.white;
            textTmp.text = "Tutorial Message";
            textTmp.alignment = TextAlignmentOptions.Center;
            textTmp.enableWordWrapping = true;
            Comp<LocalizedText>(textGo); // font-only: runtime sets text via TutorialOverlay

            // Character Avatar (Floodie)
            var avatar = Child(bubble, "CharacterAvatar");
            var avatarRt = RT(avatar);
            avatarRt.anchorMin = new Vector2(0, 0.5f);
            avatarRt.anchorMax = new Vector2(0, 0.5f);
            avatarRt.pivot = new Vector2(0.5f, 0.5f);
            avatarRt.anchoredPosition = new Vector2(-60, 0);
            avatarRt.sizeDelta = new Vector2(150, 150);
            var avatarImg = Img(avatar, Color.cyan);

            // Finger Overlay
            var finger = Child(root, "FingerOverlay");
            Fixed(finger, Vector2.zero, new Vector2(100, 100));
            Img(finger, new Color(1, 0.3f, 0.3f, 0.9f));

            // Wire serialized fields
            var so = new SerializedObject(overlayScript);
            so.FindProperty("_dimLayer").objectReferenceValue = dimImg;
            so.FindProperty("_spotlightCutout").objectReferenceValue = spotlight.GetComponent<RectTransform>();
            so.FindProperty("_spotlightGlow").objectReferenceValue = glowImg;
            so.FindProperty("_tooltipBubble").objectReferenceValue = bubble.GetComponent<RectTransform>();
            so.FindProperty("_tooltipText").objectReferenceValue = textTmp;
            so.FindProperty("_fingerOverlay").objectReferenceValue = finger.GetComponent<RectTransform>();
            so.FindProperty("_characterAvatar").objectReferenceValue = avatarImg;
            so.FindProperty("_fullscreenDismissButton").objectReferenceValue = dismissBtn;
            so.ApplyModifiedProperties();

            Save(root, "TutorialOverlay");
        }

        static void CreateChapterChest()
        {
            var root = new GameObject("ChapterChest");
            var rootRt = RT(root);
            rootRt.sizeDelta = new Vector2(120, 120);
            
            var chestImg = Img(root, Color.white);
            chestImg.preserveAspect = true;
            var chestBtn = Comp<Button>(root);
            var chestCg = Comp<CanvasGroup>(root);
            var chestView = Comp<ChapterChestView>(root);
            
            // Glow Effect
            var glow = Child(root, "GlowEffect");
            var glowRt = RT(glow);
            glowRt.sizeDelta = new Vector2(180, 180);
            var glowImg = Img(glow, new Color(1, 0.85f, 0.3f, 0.5f));
            Comp<UIPulseGlowEffect>(glow);
            
            // Dynamic Material with Shader
            Shader glowShader = Shader.Find("UI/PulseGlow");
            if (glowShader != null)
            {
                string matDir = "Assets/Resources/Prefabs/UI";
                MkDir(matDir);
                string matPath = $"{matDir}/PulseGlowMaterial.mat";
                Material glowMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (glowMat == null)
                {
                    glowMat = new Material(glowShader);
                    glowMat.name = "PulseGlowMaterial";
                    AssetDatabase.CreateAsset(glowMat, matPath);
                }
                glowImg.material = glowMat;
            }
            
            // Status Text
            var statusTextGo = Child(root, "StatusText");
            var textRt = RT(statusTextGo);
            textRt.anchorMin = new Vector2(0.5f, 0);
            textRt.anchorMax = new Vector2(0.5f, 0);
            textRt.pivot = new Vector2(0.5f, 1f);
            textRt.anchoredPosition = new Vector2(0, -10);
            textRt.sizeDelta = new Vector2(150, 40);
            
            var textTmp = statusTextGo.GetComponent<TextMeshProUGUI>();
            if (textTmp == null) textTmp = statusTextGo.AddComponent<TextMeshProUGUI>();
            textTmp.fontSizeMin = 18f;
            textTmp.fontSizeMax = 28f;
            textTmp.fontSize = 24f;
            textTmp.alignment = TextAlignmentOptions.Center;
            textTmp.color = Color.yellow;
            textTmp.text = "Locked";
            textTmp.enableWordWrapping = true;
            var chestStatusLt = Comp<LocalizedText>(statusTextGo);
            var soChestStatusLt = new SerializedObject(chestStatusLt);
            soChestStatusLt.FindProperty("_stringId").stringValue = ChapterLocked;
            soChestStatusLt.ApplyModifiedProperties();

            // Sparkle Particles
            var sparkleGo = Child(root, "SparkleParticles");
            var sparkleRt = RT(sparkleGo);
            sparkleRt.anchorMin = sparkleRt.anchorMax = new Vector2(0.5f, 0.5f);
            sparkleRt.pivot = new Vector2(0.5f, 0.5f);
            sparkleRt.anchoredPosition = Vector2.zero;
            sparkleRt.sizeDelta = Vector2.zero;

            var ps = sparkleGo.AddComponent<ParticleSystem>();
            var psRenderer = sparkleGo.GetComponent<ParticleSystemRenderer>();

            var psMain = ps.main;
            psMain.loop = true;
            psMain.duration = 1f;
            psMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            psMain.startSpeed = new ParticleSystem.MinMaxCurve(60f, 140f);
            psMain.startSize = new ParticleSystem.MinMaxCurve(5f, 14f);
            psMain.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.92f, 0.3f, 1f),
                new Color(1f, 0.55f, 0.1f, 0.9f));
            psMain.maxParticles = 40;
            psMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            psMain.gravityModifier = 0f;

            var psEmission = ps.emission;
            psEmission.rateOverTime = 20f;

            var psShape = ps.shape;
            psShape.enabled = true;
            psShape.shapeType = ParticleSystemShapeType.Circle;
            psShape.radius = 0.4f;

            psRenderer.sortingLayerName = "UI";
            psRenderer.sortingOrder = 15;
            psRenderer.material = new Material(Shader.Find("Sprites/Default"));
            sparkleGo.SetActive(false);

            // Bind Serialized Fields on ChapterChestView
            var so = new SerializedObject(chestView);
            so.FindProperty("_chestImage").objectReferenceValue = chestImg;
            so.FindProperty("_statusText").objectReferenceValue = textTmp;
            so.FindProperty("_button").objectReferenceValue = chestBtn;
            so.FindProperty("_glowEffect").objectReferenceValue = glow;
            so.FindProperty("_sparkleParticles").objectReferenceValue = ps;
            so.FindProperty("_canvasGroup").objectReferenceValue = chestCg;
            so.ApplyModifiedProperties();
            
            Save(root, "ChapterChest");
        }

        // ════════════════════════════════════════════════════════════════
        //  LAYOUT HELPERS
        // ════════════════════════════════════════════════════════════════

        static void Stretch(GameObject go)
        {
            var rt = RT(go);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        // Top-anchored fixed-height strip (pivot top-center)
        static void TopStrip(GameObject go, float h)
        {
            var rt = RT(go);
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, h); rt.anchoredPosition = Vector2.zero;
        }

        // Bottom-anchored fixed-height strip (pivot bottom-center)
        static void BottomStrip(GameObject go, float h)
        {
            var rt = RT(go);
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot     = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(0, h); rt.anchoredPosition = Vector2.zero;
        }

        // Stretch with uniform padding from each edge (using offsetMin/offsetMax)
        static void PaddedStretch(GameObject go, float topPad, float bottomPad)
        {
            var rt = RT(go);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0, bottomPad);
            rt.offsetMax = new Vector2(0, -topPad);
        }

        // Fixed-size, center-pivoted, offset from parent center
        static void Fixed(GameObject go, Vector2 pos, Vector2 size)
        {
            var rt = RT(go);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
        }

        // Center-pivot fixed rect helper for TMP
        static Rect Center(float x, float y, float w, float h) =>
            new Rect(x, y, w, h);

        // ════════════════════════════════════════════════════════════════
        //  COMPONENT / OBJECT HELPERS
        // ════════════════════════════════════════════════════════════════

        static GameObject FullScreen(string name)
        {
            var go = new GameObject(name); go.AddComponent<RectTransform>(); Stretch(go); return go;
        }

        static GameObject Panel(GameObject parent, string name, Vector2 size, Color color)
        {
            var border = Child(parent, name); Fixed(border, Vector2.zero, size + new Vector2(24f, 24f));
            Img(border, Hex("2B003B")); // Thick dark border outline shadow
            
            var panel = Child(border, "InnerPanel"); Stretch(panel);
            var rt = RT(panel);
            rt.offsetMin = new Vector2(12f, 12f);
            rt.offsetMax = new Vector2(-12f, -12f);
            Img(panel, color);
            return panel;
        }

        static TMP_Text RibbonTitle(GameObject panel, string name, string text, string stringId = null)
        {
            var panelRt = RT(panel);
            float panelW = panelRt.sizeDelta.x;
            float panelH = panelRt.sizeDelta.y;

            // If anchors are stretched, sizeDelta contains margins rather than absolute size.
            // In this case, read the parent border's size which is fixed in pixels.
            if (panelRt.anchorMin == Vector2.zero && panelRt.anchorMax == Vector2.one)
            {
                var parentRt = panel.transform.parent.GetComponent<RectTransform>();
                if (parentRt != null)
                {
                    panelW = parentRt.sizeDelta.x - 24f; // subtract border margins
                    panelH = parentRt.sizeDelta.y - 24f;
                }
            }

            // Safe fallback bounds
            if (panelW <= 0f) panelW = 800f;
            if (panelH <= 0f) panelH = 500f;

            // Ribbon base banner (uses UI_CTA)
            var ribbon = Child(panel, name + "_Ribbon");
            Fixed(ribbon, new Vector2(0f, panelH * 0.5f), new Vector2(panelW * 0.85f, 100f));
            Img(ribbon, UI_CTA);
            
            // Shadow under the ribbon banner
            var ribbonShadow = Child(ribbon, "Shadow");
            Fixed(ribbonShadow, new Vector2(0f, -6f), new Vector2(panelW * 0.85f, 100f));
            Img(ribbonShadow, Hex("2B003B"));
            ribbonShadow.transform.SetAsFirstSibling();
            
            var tmp = TMP(ribbon, "Text", new Rect(0, 0, panelW * 0.8f, 80f), 28, UI_TEXT, text, stringId, TextCategory.Header);
            return tmp;
        }

        // Button with label child
        static GameObject Btn(GameObject parent, string name, Vector2 pos, Vector2 size, Color color, string label, string labelStringId = null, float shadowAlpha = 1f)
        {
            var go = Child(parent, name); Fixed(go, pos, size);
            
            // Shadow underlay for 3D look
            var shadowGo = Child(go, "Shadow");
            Fixed(shadowGo, new Vector2(0, -8f), size);
            var shadowColor = Hex("2B003B"); shadowColor.a = shadowAlpha;
            Img(shadowGo, shadowColor);
            
            // Visual top layer
            var visualGo = Child(go, "Visual");
            Stretch(visualGo);
            var img = Img(visualGo, color);
            
            if (!go.TryGetComponent<Button>(out var btn)) btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            Comp<UIButtonAnimator>(go);
            
            // Check if button is square
            bool isSquare = Mathf.Approximately(size.x, size.y);
            if (isSquare)
            {
                var iconGo = Child(visualGo, "Icon");
                Fixed(iconGo, Vector2.zero, size * 0.75f);
                Img(iconGo, Color.white);
                var animator = Comp<UIIconIdleAnimator>(iconGo);
                animator.Configure(UIIconIdleAnimator.AnimationType.Float, 2f, 5f);
            }
            else if (!string.IsNullOrEmpty(label))
            {
                TMP(visualGo, "Label", Center(0, 0, size.x, size.y), 20, UI_TEXT, label, labelStringId, TextCategory.Button);
            }
            return go;
        }

        // Button for use inside LayoutGroup — uses LayoutElement
        static GameObject BtnHlg(GameObject parent, string name, Color color, string label, string labelStringId = null)
        {
            var go = Child(parent, name);
            var rt = RT(go);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var le = Comp<LayoutElement>(go);
            le.flexibleWidth  = 1;
            le.preferredHeight = 100;
            
            // Shadow underlay for 3D look
            var shadowGo = Child(go, "Shadow");
            var shadowRt = RT(shadowGo);
            shadowRt.anchorMin = Vector2.zero; shadowRt.anchorMax = Vector2.one;
            shadowRt.offsetMin = new Vector2(0, -8f); shadowRt.offsetMax = new Vector2(0, -8f);
            Img(shadowGo, Hex("2B003B"));
            
            // Visual top layer
            var visualGo = Child(go, "Visual");
            Stretch(visualGo);
            var img = Img(visualGo, color);
            
            if (!go.TryGetComponent<Button>(out var btn)) btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            Comp<UIButtonAnimator>(go);
            
            TMP(visualGo, "Label", Center(0, 0, 180, 80), 18, UI_TEXT, label, labelStringId, TextCategory.Button);
            return go;
        }

        static string StripEmojis(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in input)
            {
                int val = (int)c;
                if (val >= 0x2600 && val <= 0x27BF) continue;
                if (val >= 0xD800 && val <= 0xDFFF) continue;
                sb.Append(c);
            }
            return sb.ToString().Replace("👤", "").Replace("🪙", "").Replace("⏸", "").Replace("★", "").Trim();
        }

        static TMP_Text TMP(GameObject parent, string name, Rect rect, int size, Color color, string text, string stringId = null, TextCategory category = TextCategory.Normal)
        {
            var go = Child(parent, name);
            Fixed(go, new Vector2(rect.x, rect.y), new Vector2(rect.width, rect.height));
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
            
            // Auto Font Sizing configuration
            tmp.enableAutoSizing = true;
            switch (category)
            {
                case TextCategory.Header:
                    tmp.fontSizeMin = 48f;
                    tmp.fontSizeMax = 72f;
                    break;
                case TextCategory.Button:
                    tmp.fontSizeMin = 40f;
                    tmp.fontSizeMax = 56f;
                    break;
                case TextCategory.Normal:
                default:
                    tmp.fontSizeMin = 32f;
                    tmp.fontSizeMax = 44f;
                    break;
            }
            tmp.fontSize  = tmp.fontSizeMax;
            tmp.color     = color;
            tmp.text      = StripEmojis(text);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            
            var lt = Comp<LocalizedText>(go);
            if (!string.IsNullOrEmpty(stringId))
            {
                var soLt = new SerializedObject(lt);
                soLt.FindProperty("_stringId").stringValue = stringId;
                soLt.ApplyModifiedProperties();
            }
            
            var style = Comp<UITextStyle>(go);
            style.ApplyStyle();
            
            return tmp;
        }

        static Image Img(GameObject go, Color color)
        {
            if (!go.TryGetComponent<Image>(out var img)) img = go.AddComponent<Image>();
            img.color = color; return img;
        }

        static T Comp<T>(GameObject go) where T : Component
        {
            if (!go.TryGetComponent<T>(out var c)) c = go.AddComponent<T>(); return c;
        }

        static GameObject Child(GameObject parent, string childName)
        {
            var t = parent.transform.Find(childName);
            if (t != null) return t.gameObject;
            var go = new GameObject(childName);
            go.AddComponent<RectTransform>();
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static RectTransform RT(GameObject go)
        {
            if (!go.TryGetComponent<RectTransform>(out var rt)) rt = go.AddComponent<RectTransform>();
            return rt;
        }

        static GameObject StarGO(GameObject parent, string name, float size = 70f)
        {
            var go = Child(parent, name);
            RT(go).sizeDelta = new Vector2(size, size);
            var le = Comp<LayoutElement>(go);
            le.minWidth = le.preferredWidth = size;
            le.minHeight = le.preferredHeight = size;
            var emptyImg = Img(go, Color.white);
            emptyImg.preserveAspect = true;
            var fill = Child(go, "Fill");
            Stretch(fill);
            var fillImg = Img(fill, Color.white);
            fillImg.preserveAspect = true;
            fill.SetActive(false);
            return go;
        }

        static GameObject ToggleRow(GameObject parent, string rowName, Vector2 pos, string label, string labelStringId = null)
        {
            var row = Child(parent, rowName); Fixed(row, pos, new Vector2(800, 80));
            TMP(row, "Label",  Center(-220, 0, 400, 60), 22, UI_TEXT, label, labelStringId, TextCategory.Normal);

            var tgo = Child(row, "Toggle"); Fixed(tgo, new Vector2(280, 0), new Vector2(100, 60));
            if (!tgo.TryGetComponent<Toggle>(out var toggle)) toggle = tgo.AddComponent<Toggle>();
            var bg  = Child(tgo, "Background"); Fixed(bg,   Vector2.zero, new Vector2(100, 60)); Img(bg, UI_BG_DEEP);
            var chk = Child(tgo, "Checkmark");  Fixed(chk,  Vector2.zero, new Vector2(50, 50)); Img(chk, UI_CTA);
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic       = chk.GetComponent<Image>();
            toggle.isOn          = true;
            return row;
        }

        static GameObject SoundRow(GameObject parent, string rowName, Vector2 pos, string label, string labelStringId = null)
        {
            var row = Child(parent, rowName); Fixed(row, pos, new Vector2(800, 80));
            TMP(row, "Label", Center(-260, 0, 240, 60), 22, UI_TEXT, label, labelStringId, TextCategory.Normal);

            var tgo = Child(row, "Toggle"); Fixed(tgo, new Vector2(-60, 0), new Vector2(80, 50));
            var toggle = tgo.AddComponent<Toggle>();
            var bg  = Child(tgo, "Background"); Fixed(bg,   Vector2.zero, new Vector2(80, 50)); Img(bg, UI_BG_DEEP);
            var chk = Child(tgo, "Checkmark");  Fixed(chk,  Vector2.zero, new Vector2(40, 40)); Img(chk, UI_CTA);
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic       = chk.GetComponent<Image>();
            toggle.isOn          = true;

            var sgo = Child(row, "Slider"); Fixed(sgo, new Vector2(200, 0), new Vector2(380, 40));
            var slider = sgo.AddComponent<Slider>();

            var sbg = Child(sgo, "Background"); Stretch(sbg); Img(sbg, UI_BG_DEEP);
            var sbgRt = RT(sbg);
            sbgRt.offsetMin = new Vector2(0, 15);
            sbgRt.offsetMax = new Vector2(0, -15);

            var fillArea = Child(sgo, "Fill Area"); Stretch(fillArea);
            var fillAreaRt = RT(fillArea);
            fillAreaRt.offsetMin = new Vector2(5, 0);
            fillAreaRt.offsetMax = new Vector2(-15, 0);

            var fill = Child(fillArea, "Fill");
            var fillRt = RT(fill);
            fillRt.anchorMin = new Vector2(0, 0.25f);
            fillRt.anchorMax = new Vector2(0.5f, 0.75f);
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            Img(fill, UI_PRIMARY);

            var handleArea = Child(sgo, "Handle Slide Area"); Stretch(handleArea);
            var handleAreaRt = RT(handleArea);
            handleAreaRt.offsetMin = new Vector2(10, 0);
            handleAreaRt.offsetMax = new Vector2(-10, 0);

            var handle = Child(handleArea, "Handle");
            var handleRt = RT(handle);
            handleRt.anchorMin = new Vector2(0.5f, 0);
            handleRt.anchorMax = new Vector2(0.5f, 1);
            handleRt.sizeDelta = new Vector2(30, 30);
            handleRt.anchoredPosition = Vector2.zero;
            Img(handle, UI_CTA);

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            return row;
        }

        static GameObject LanguageRow(GameObject parent, string rowName, Vector2 pos, string label, string labelStringId, out GameObject koBtn, out GameObject enBtn, out GameObject jaBtn)
        {
            var row = Child(parent, rowName); Fixed(row, pos, new Vector2(800, 80));
            TMP(row, "Label", Center(-260, 0, 240, 60), 22, UI_TEXT, label, labelStringId, TextCategory.Normal);

            var container = Child(row, "Buttons"); Fixed(container, new Vector2(170, 0), new Vector2(440, 70));
            var hlg = container.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            koBtn = BtnHlg(container, "KOButton", UI_PRIMARY, "KO");
            enBtn = BtnHlg(container, "ENButton", UI_BG_DEEP, "EN");
            jaBtn = BtnHlg(container, "JAButton", UI_BG_DEEP, "JA");

            return row;
        }

        static TMP_Dropdown LanguageDropdownRow(GameObject parent, string rowName, Vector2 pos, string label, string labelStringId)
        {
            var row = Child(parent, rowName);
            Fixed(row, pos, new Vector2(800, 80));
            TMP(row, "Label", Center(-250, 0, 240, 60), 22, UI_TEXT, label, labelStringId, TextCategory.Normal);

            // Dropdown container
            var dropGo = Child(row, "Dropdown");
            Fixed(dropGo, new Vector2(190, 0), new Vector2(360, 70));
            var dropBg = Img(dropGo, UI_BG_DEEP);

            // Caption — stretched rect; -70 right inset keeps text clear of the arrow
            var captionGo = Child(dropGo, "Label");
            var captionRt = RT(captionGo);
            captionRt.anchorMin = Vector2.zero;
            captionRt.anchorMax = Vector2.one;
            captionRt.offsetMin = new Vector2(15, 5);
            captionRt.offsetMax = new Vector2(-70, -5);
            var captionTmp = captionGo.AddComponent<TextMeshProUGUI>();
            captionTmp.enableAutoSizing   = true;
            captionTmp.fontSizeMin        = 28f;
            captionTmp.fontSizeMax        = 38f;
            captionTmp.fontSize           = 38f;
            captionTmp.color              = UI_TEXT;
            captionTmp.alignment          = TextAlignmentOptions.Left;
            captionTmp.overflowMode       = TextOverflowModes.Ellipsis;
            captionTmp.enableWordWrapping = false;
            captionTmp.text               = "한국어";
            Comp<LocalizedText>(captionGo);   // _stringId empty → font-only mode
            Comp<UITextStyle>(captionGo).ApplyStyle();

            // Arrow — sits in the right 70px zone
            var arrowGo = Child(dropGo, "Arrow");
            Fixed(arrowGo, new Vector2(155, 0), new Vector2(32, 32));
            Img(arrowGo, UI_TEXT);

            // Template (inactive; TMP_Dropdown repositions at runtime)
            var templateGo = Child(dropGo, "Template");
            var templateRt = RT(templateGo);
            templateRt.anchorMin        = new Vector2(0, 1);
            templateRt.anchorMax        = new Vector2(1, 1);
            templateRt.pivot            = new Vector2(0.5f, 0f);
            templateRt.anchoredPosition = new Vector2(0, 4);
            templateRt.sizeDelta        = new Vector2(0, 160);
            Img(templateGo, UI_BG_MID);

            var scrollRect = templateGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical   = true;

            var viewportGo = Child(templateGo, "Viewport");
            Stretch(viewportGo);
            viewportGo.AddComponent<RectMask2D>();
            scrollRect.viewport = RT(viewportGo);

            var contentGo = Child(viewportGo, "Content");
            var contentRt = RT(contentGo);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot     = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 70);
            scrollRect.content  = contentRt;

            // Item template (one instance; TMP_Dropdown clones per option)
            var itemGo = Child(contentGo, "Item");
            var itemRt = RT(itemGo);
            itemRt.anchorMin = new Vector2(0, 0.5f);
            itemRt.anchorMax = new Vector2(1, 0.5f);
            itemRt.sizeDelta = new Vector2(0, 65);
            var itemToggle = itemGo.AddComponent<Toggle>();

            var itemBgGo  = Child(itemGo, "Item Background");
            Stretch(itemBgGo);
            var itemBgImg = Img(itemBgGo, UI_BG_DEEP);
            itemToggle.targetGraphic = itemBgImg;

            var itemChkGo = Child(itemGo, "Item Checkmark");
            Fixed(itemChkGo, new Vector2(140, 0), new Vector2(28, 28));  // right side
            Img(itemChkGo, UI_CTA);
            itemToggle.graphic = itemChkGo.GetComponent<Image>();

            var itemLabelGo  = Child(itemGo, "Item Label");
            var itemLabelRt  = RT(itemLabelGo);
            itemLabelRt.anchorMin = Vector2.zero;
            itemLabelRt.anchorMax = Vector2.one;
            itemLabelRt.offsetMin = new Vector2(20, 4);
            itemLabelRt.offsetMax = new Vector2(-50, -4);  // -50 clears right-side checkmark
            var itemLabelTmp = itemLabelGo.AddComponent<TextMeshProUGUI>();
            itemLabelTmp.enableAutoSizing   = true;
            itemLabelTmp.fontSizeMin        = 28f;
            itemLabelTmp.fontSizeMax        = 38f;
            itemLabelTmp.fontSize           = 38f;
            itemLabelTmp.color              = UI_TEXT;
            itemLabelTmp.alignment          = TextAlignmentOptions.Left;
            itemLabelTmp.overflowMode       = TextOverflowModes.Ellipsis;
            itemLabelTmp.enableWordWrapping = false;
            itemLabelTmp.text               = "Option";
            Comp<LocalizedText>(itemLabelGo);  // font-only
            Comp<UITextStyle>(itemLabelGo).ApplyStyle();

            templateGo.SetActive(false);

            var dropdown = dropGo.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = dropBg;
            dropdown.template      = templateRt;
            dropdown.captionText   = captionTmp;
            dropdown.itemText      = itemLabelTmp;

            return dropdown;
        }

        // Nav tab button — icon above label; highlight targets the icon Image (color-tinted)
        static GameObject BtnNavTab(GameObject parent, string name, Color color, string label, string labelStringId = null)
        {
            var go = Child(parent, name);
            var rt = RT(go);
            rt.sizeDelta = new Vector2(360f, 160f);
            var le = Comp<LayoutElement>(go);
            le.flexibleWidth = 1;
            le.preferredHeight = 160;

            var shadowGo = Child(go, "Shadow");
            var shadowRt = RT(shadowGo);
            shadowRt.anchorMin = Vector2.zero; shadowRt.anchorMax = Vector2.one;
            shadowRt.offsetMin = new Vector2(0, -8f); shadowRt.offsetMax = new Vector2(0, -8f);
            Img(shadowGo, Hex("2B003B"));

            var visualGo = Child(go, "Visual");
            Stretch(visualGo);
            var bgImg = Img(visualGo, color);

            if (!go.TryGetComponent<Button>(out var btn)) btn = go.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            Comp<UIButtonAnimator>(go);

            var iconGo = Child(visualGo, "Icon");
            Fixed(iconGo, new Vector2(0, 25), new Vector2(80, 80));
            var iconImg = Img(iconGo, UI_TEXT);
            iconImg.preserveAspect = true;

            TMP(visualGo, "Label", Center(0, -42, 240, 50), 22, UI_TEXT, label, labelStringId, TextCategory.Normal);
            return go;
        }

        static GameObject ItemToggleRow(GameObject parent, string rowName, Vector2 pos, string label, string labelStringId = null)
        {
            var row = Child(parent, rowName);
            Fixed(row, pos, new Vector2(128, 128));

            var le = Comp<LayoutElement>(row);
            le.preferredWidth = le.minWidth   = 128f;
            le.preferredHeight = le.minHeight = 128f;

            var iconImg = Img(row, Color.white);
            iconImg.preserveAspect = true;

            if (!row.TryGetComponent<Toggle>(out var toggle)) toggle = row.AddComponent<Toggle>();
            toggle.targetGraphic = iconImg;
            toggle.isOn = false;

            var chk = Child(row, "Checkmark");
            Fixed(chk, Vector2.zero, new Vector2(128, 128));
            var chkImg = Img(chk, new Color(1f, 0.92f, 0.3f, 0.55f));
            toggle.graphic = chkImg;

            // State indicator — bottom-right corner; sprite swapped by StageInfoPopupView.OnExtraTurnsToggled
            var stateGo = Child(row, "StateIndicator");
            var stateRt = RT(stateGo);
            stateRt.anchorMin = new Vector2(1f, 0f);
            stateRt.anchorMax = new Vector2(1f, 0f);
            stateRt.pivot = new Vector2(1f, 0f);
            stateRt.anchoredPosition = Vector2.zero;
            stateRt.sizeDelta = new Vector2(44f, 44f);
            var stateImg = Img(stateGo, Color.white);
            stateImg.preserveAspect = true;

            return row;
        }

        static void Save(GameObject go, string name)
        {
            EnsureDirs();
            string path = $"{BaseCommonPath}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[UIEditorSetup] Saved Base Popup → {path}");
        }

        private static void SaveScenePrefab(GameObject root, string sceneName)
        {
            string path = $"{BaseScenesPath}/{sceneName}/{sceneName}Canvas_Base.prefab";
            EnsureDirs();
            MkDir($"{BaseScenesPath}/{sceneName}");
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[UIEditorSetup] Saved Base Scene Prefab for {sceneName} → {path}");
        }

        static GameObject CreateTempCanvas(string canvasName)
        {
            var go = new GameObject(canvasName);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        static void EnsureDirs()
        {
            foreach (var path in new[] { PrefabRoot, PrefabBase, BaseCommonPath, BaseScenesPath, PrefabFinal })
                MkDir(path);
        }

        static void MkDir(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            var cur   = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out Color c); return c; }

        private static Dictionary<string, string> LoadDynamicResourceMap()
        {
            var map = new Dictionary<string, string>();
            var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));
            var csvPath = Path.Combine(repoRoot, "shared/datas/common/dynamic_resource.csv");
            if (!File.Exists(csvPath))
            {
                Debug.LogWarning($"[UIEditorSetup] dynamic_resource.csv not found at: {csvPath}");
                return map;
            }

            var lines = File.ReadAllLines(csvPath);
            for (int i = 4; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                var cols = line.Split(',');
                if (cols.Length >= 2)
                {
                    string key = cols[0].Trim();
                    string path = cols[1].Trim();
                    map[key] = path;
                }
            }
            return map;
        }

        private static void AssignItemIcon(ItemSlotView slot, string key, Dictionary<string, string> resMap)
        {
            if (slot == null || !resMap.TryGetValue(key, out string spritePath)) return;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null) return;

            var soSlot = new SerializedObject(slot);
            var iconProp = soSlot.FindProperty("_icon");
            if (iconProp != null && iconProp.objectReferenceValue != null)
            {
                var iconImg = iconProp.objectReferenceValue as Image;
                if (iconImg != null)
                {
                    iconImg.sprite = sprite;
                    iconImg.preserveAspect = true;
                    EditorUtility.SetDirty(iconImg);
                }
            }
        }

        private static void TryMapSprite(SerializedObject so, string propertyName, string key, Dictionary<string, string> resMap)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null) return;
            if (string.IsNullOrEmpty(key))
            {
                prop.objectReferenceValue = null;
                return;
            }
            if (resMap.TryGetValue(key, out string path))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                prop.objectReferenceValue = sprite;
            }
        }

        private static void TryMapImageSprite(SerializedObject so, string propertyName, string key, Dictionary<string, string> resMap)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null && prop.objectReferenceValue != null && resMap.TryGetValue(key, out string path))
            {
                var img = prop.objectReferenceValue as Image;
                if (img != null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    img.sprite = sprite;
                    img.preserveAspect = true;
                    EditorUtility.SetDirty(img);
                }
            }
        }

        [MenuItem("Tools/UI Setup/Inspect Sprites", false, 149)]
        public static void InspectSprites()
        {
            var resMap = LoadDynamicResourceMap();
            string[] keys = { "socket_default", "socket_0", "cell_0", "cell_basic" };
            foreach (var key in keys)
            {
                if (resMap.TryGetValue(key, out string path))
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null)
                    {
                        Debug.Log($"[Inspect] Key: {key}, Path: {path}, Name: {sprite.name}, Rect: {sprite.rect}, PPU: {sprite.pixelsPerUnit}, Bounds Size: {sprite.bounds.size}");
                    }
                    else
                    {
                        Debug.LogWarning($"[Inspect] Key: {key}, Path: {path} - Sprite is NULL!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[Inspect] Key: {key} not found in resMap");
                }
            }
        }

        [MenuItem("Tools/UI Setup/Map Cell and Socket Sprites", false, 150)]
        public static void MapCellAndSocketSprites()
        {
            var resMap = LoadDynamicResourceMap();

            // 1. Map Cell Sprites
            string cellViewPrefabPath = "Assets/Resources/Prefabs/Game/CellView.prefab";
            var cellViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cellViewPrefabPath);
            if (cellViewPrefab != null)
            {
                var root = PrefabUtility.LoadPrefabContents(cellViewPrefabPath);
                var cellView = root.GetComponent<CellView>();
                var baseRenderer = root.GetComponent<SpriteRenderer>();

                var overlayTransform = root.transform.Find("ProtectorOverlay");
                SpriteRenderer protectorOverlayRenderer = null;
                if (overlayTransform == null)
                {
                    var overlayGo = new GameObject("ProtectorOverlay");
                    overlayGo.transform.SetParent(root.transform, false);
                    protectorOverlayRenderer = overlayGo.AddComponent<SpriteRenderer>();
                    if (baseRenderer != null)
                    {
                        protectorOverlayRenderer.sortingLayerID = baseRenderer.sortingLayerID;
                        protectorOverlayRenderer.sortingOrder = baseRenderer.sortingOrder + 1;
                    }
                }
                else
                {
                    protectorOverlayRenderer = overlayTransform.GetComponent<SpriteRenderer>();
                    if (protectorOverlayRenderer == null)
                    {
                        protectorOverlayRenderer = overlayTransform.gameObject.AddComponent<SpriteRenderer>();
                    }
                }

                var soCell = new SerializedObject(cellView);
                soCell.FindProperty("_protectorOverlay").objectReferenceValue = protectorOverlayRenderer;

                var colorSpritesProp = soCell.FindProperty("_colorSprites");
                colorSpritesProp.arraySize = 16;
                for (int i = 0; i < 16; i++)
                {
                    string key = $"cell_{i}";
                    Sprite sprite = null;
                    if (resMap.TryGetValue(key, out string path))
                    {
                        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    }
                    colorSpritesProp.GetArrayElementAtIndex(i).objectReferenceValue = sprite;
                }

                // Map additional cell fields dynamically assigned in code branching
                TryMapSprite(soCell, "_basicSprite", null, resMap);
                TryMapSprite(soCell, "_obstacleSprite", "cell_obstacle", resMap);
                TryMapSprite(soCell, "_bombSprite", "item_bomb", resMap);
                TryMapSprite(soCell, "_hRocketSprite", "item_h_rocket", resMap);
                TryMapSprite(soCell, "_colorSweepSprite", "item_color_sweep", resMap);
                TryMapSprite(soCell, "_protectorSprite1", "cell_protector_1", resMap);
                TryMapSprite(soCell, "_protectorSprite2", "cell_protector_2", resMap);

                soCell.ApplyModifiedProperties();
                PrefabUtility.SaveAsPrefabAsset(root, cellViewPrefabPath);
                PrefabUtility.UnloadPrefabContents(root);
                Debug.Log("[UIEditorSetup] Successfully mapped cell sprites and updated ProtectorOverlay hierarchy in CellView.prefab");
            }
            else
            {
                Debug.LogError("[UIEditorSetup] CellView.prefab not found!");
            }

            // 2. Map Socket Sprites in InGame scene
            string scenePath = "Assets/Scenes/InGame.unity";
            if (File.Exists(scenePath))
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                var boardBg = Object.FindAnyObjectByType<BoardBackground>();
                if (boardBg != null)
                {
                    var soBg = new SerializedObject(boardBg);
                    var socketSpritesProp = soBg.FindProperty("_socketSprites");
                    
                    socketSpritesProp.arraySize = 16;
                    for (int i = 0; i < 16; i++)
                    {
                        string key = $"socket_{i}";
                        Sprite sprite = null;
                        if (resMap.TryGetValue(key, out string path))
                        {
                            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        }
                        socketSpritesProp.GetArrayElementAtIndex(i).objectReferenceValue = sprite;
                    }

                    if (resMap.TryGetValue("socket_default", out string defaultPath))
                    {
                        var defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>(defaultPath);
                        soBg.FindProperty("_defaultSocketSprite").objectReferenceValue = defaultSprite;
                    }
                    else if (resMap.TryGetValue("socket_0", out string fallbackPath))
                    {
                        var defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>(fallbackPath);
                        soBg.FindProperty("_defaultSocketSprite").objectReferenceValue = defaultSprite;
                    }
                    
                    soBg.ApplyModifiedProperties();
                    EditorUtility.SetDirty(boardBg);
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                    Debug.Log("[UIEditorSetup] Successfully mapped socket sprites to BoardBackground in InGame.unity scene");
                }
                else
                {
                    Debug.LogError("[UIEditorSetup] BoardBackground component not found in InGame scene!");
                }
            }
            else
            {
                Debug.LogError("[UIEditorSetup] InGame.unity scene not found!");
            }

            // 3. Map ChapterChest Sprites
            string[] chestPaths = {
                "Assets/Resources/Prefabs/UI/ChapterChest.prefab",
                "Assets/UI/Prefabs/Base/Common/ChapterChest.prefab"
            };
            foreach (var path in chestPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var chestView = prefab.GetComponent<ChapterChestView>();
                    if (chestView != null)
                    {
                        var soChest = new SerializedObject(chestView);
                        TryMapSprite(soChest, "_inactiveSprite", "chest_inactive", resMap);
                        TryMapSprite(soChest, "_activeSprite", "chest_active", resMap);
                        TryMapSprite(soChest, "_claimedSprite", "chest_claimed", resMap);
                        
                        // Set preserveAspect on the Image component of the chest
                        var chestImgProp = soChest.FindProperty("_chestImage");
                        if (chestImgProp != null && chestImgProp.objectReferenceValue != null)
                        {
                            var img = chestImgProp.objectReferenceValue as Image;
                            if (img != null)
                            {
                                img.preserveAspect = true;
                                EditorUtility.SetDirty(img);
                            }
                        }
                        
                        soChest.ApplyModifiedProperties();
                        EditorUtility.SetDirty(prefab);
                        Debug.Log($"[UIEditorSetup] Successfully mapped chest sprites to {path}");
                    }
                }
            }

            // 4. Map ToastView Sprites
            string[] toastPaths = {
                "Assets/Resources/Prefabs/UI/ToastView.prefab",
                "Assets/UI/Prefabs/Base/Common/ToastView.prefab"
            };
            foreach (var path in toastPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var toastView = prefab.GetComponent<ToastView>();
                    if (toastView != null)
                    {
                        var soToast = new SerializedObject(toastView);
                        TryMapSprite(soToast, "_warningIcon", "toast_warning", resMap);
                        TryMapSprite(soToast, "_successIcon", "toast_success", resMap);
                        TryMapSprite(soToast, "_errorIcon", "toast_error", resMap);
                        
                        // Set preserveAspect on the Image component of the toast icon
                        var toastImgProp = soToast.FindProperty("_iconImage");
                        if (toastImgProp != null && toastImgProp.objectReferenceValue != null)
                        {
                            var img = toastImgProp.objectReferenceValue as Image;
                            if (img != null)
                            {
                                img.preserveAspect = true;
                                EditorUtility.SetDirty(img);
                            }
                        }
                        
                        soToast.ApplyModifiedProperties();
                        EditorUtility.SetDirty(prefab);
                        Debug.Log($"[UIEditorSetup] Successfully mapped toast sprites to {path}");
                    }
                }
            }

            // 5. Map TutorialOverlay Guide Avatar
            string[] tutorialPaths = {
                "Assets/Resources/Prefabs/UI/TutorialOverlay.prefab",
                "Assets/UI/Prefabs/Base/Common/TutorialOverlay.prefab"
            };
            foreach (var path in tutorialPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var tutorialOverlay = prefab.GetComponent<TutorialOverlay>();
                    if (tutorialOverlay != null)
                    {
                        var soTut = new SerializedObject(tutorialOverlay);
                        TryMapImageSprite(soTut, "_characterAvatar", "guide_avatar", resMap);
                        soTut.ApplyModifiedProperties();
                        EditorUtility.SetDirty(prefab);
                        Debug.Log($"[UIEditorSetup] Successfully mapped guide avatar sprite to {path}");
                    }
                }
            }

            // 6. Map star and UI icon sprites on common prefabs
            MapStarAndIconSprites(resMap);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void MapHierarchyImageSprite(GameObject prefab, string childPath, string key, Dictionary<string, string> resMap)
        {
            if (prefab == null) return;
            var child = prefab.transform.Find(childPath);
            if (child == null) return;
            if (!resMap.TryGetValue(key, out string spritePath)) return;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null) return;
            var img = child.GetComponent<Image>();
            if (img == null) return;
            img.sprite = sprite;
            img.preserveAspect = true;
            EditorUtility.SetDirty(prefab);
        }

        private static void MapStarAndIconSprites(Dictionary<string, string> resMap)
        {
            string[] stageInfoPaths = {
                "Assets/Resources/Prefabs/UI/StageInfoPopupView.prefab",
                "Assets/UI/Prefabs/Base/Common/StageInfoPopupView.prefab"
            };
            string[] resultPaths = {
                "Assets/Resources/Prefabs/UI/ResultOverlayView.prefab",
                "Assets/UI/Prefabs/Base/Common/ResultOverlayView.prefab"
            };
            string[] stageNodePaths = {
                "Assets/Resources/Prefabs/UI/StageNodeView.prefab",
                "Assets/UI/Prefabs/Base/Common/StageNodeView.prefab"
            };

            for (int i = 0; i < 3; i++)
            {
                foreach (var path in stageInfoPaths)
                {
                    var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    MapHierarchyImageSprite(p, $"Panel/InnerPanel/Stars/Star{i}", "star_empty", resMap);
                    MapHierarchyImageSprite(p, $"Panel/InnerPanel/Stars/Star{i}/Fill", "star_filled", resMap);
                }
                foreach (var path in resultPaths)
                {
                    var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    MapHierarchyImageSprite(p, $"Panel/InnerPanel/Stars/Star{i}", "star_empty", resMap);
                    MapHierarchyImageSprite(p, $"Panel/InnerPanel/Stars/Star{i}/Fill", "star_filled", resMap);
                }
                foreach (var path in stageNodePaths)
                {
                    var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    MapHierarchyImageSprite(p, $"Stars/Star{i}", "star_empty", resMap);
                    MapHierarchyImageSprite(p, $"Stars/Star{i}/Fill", "star_filled", resMap);
                }
            }

            foreach (var path in stageInfoPaths)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                MapHierarchyImageSprite(p, "Panel/InnerPanel/ItemContainer/ExtraTurnsToggleRow", "item_add_turn", resMap);
            }
            foreach (var path in stageNodePaths)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                MapHierarchyImageSprite(p, "LockOverlay/LockIcon", "ui_lock_icon", resMap);
            }
        }

        private static void BuildRankingItemHierarchy(GameObject itemGo, Dictionary<string, string> resMap)
        {
            // RankText
            TMP(itemGo, "RankText", Center(-320, 0, 100, 60), 20, UI_CTA, "#1", null, TextCategory.Normal);

            // AvatarIcon: between RankText and NameText
            var avatarIcon = Child(itemGo, "AvatarIcon");
            Fixed(avatarIcon, new Vector2(-210, 0), new Vector2(64, 64));
            var avatarImg = Img(avatarIcon, Color.white);
            avatarImg.preserveAspect = true;
            if (resMap.TryGetValue("ui_avatar_default", out string avp))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(avp);
                if (spr != null) avatarImg.sprite = spr;
            }

            // NameText
            TMP(itemGo, "NameText", Center(-20, 0, 280, 60), 20, UI_TEXT, "Player Name", null, TextCategory.Normal);

            // ScoreIcon: immediately to the left of ScoreText
            var scoreIcon = Child(itemGo, "ScoreIcon");
            Fixed(scoreIcon, new Vector2(200, 0), new Vector2(48, 48));
            var scoreImg = Img(scoreIcon, Color.white);
            scoreImg.preserveAspect = true;
            if (resMap.TryGetValue("star_filled", out string sfp))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(sfp);
                if (spr != null) scoreImg.sprite = spr;
            }

            // ScoreText
            TMP(itemGo, "ScoreText", Center(300, 0, 120, 60), 20, UI_CTA, "100", null, TextCategory.Normal);
        }

        private static void CreateRankingItemPrefab()
        {
            var root = new GameObject("RankingItemPrefab");
            var rt = RT(root);
            rt.sizeDelta = new Vector2(820, 90);

            // Set anchors and pivot to top-center to align with VirtualizedScrollRect content mapping
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);

            Img(root, UI_BG_MID);

            var resMap = LoadDynamicResourceMap();
            BuildRankingItemHierarchy(root, resMap);

            Save(root, "RankingItemPrefab");
        }

        static void CreateAccountRestartPopup()
        {
            var root = FullScreen("AccountRestartPopupView");
            Comp<AccountRestartPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var backdrop = Btn(root, "Backdrop", Vector2.zero, new Vector2(1080, 1920), DIM, "", shadowAlpha: 200f / 255f);
            Stretch(backdrop);

            var panel = Panel(root, "Panel", new Vector2(700, 500), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Game Restart Required", PopupAccountRestartTitle);

            var body = TMP(panel, "BodyText", Center(0, 50, 580, 150), 20, UI_TEXT, "The game will now restart.", PopupAccountRestartBody, TextCategory.Normal);
            body.enableWordWrapping = true;
            var bodyCsf = Comp<ContentSizeFitter>(body.gameObject);
            bodyCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var confirmBtn = Btn(panel, "ConfirmButton", new Vector2(0, -140), new Vector2(500, 85), UI_PRIMARY, "Restart", PopupAccountRestartConfirm);

            var so = new SerializedObject(root.GetComponent<AccountRestartPopupView>());
            so.FindProperty("_titleText").objectReferenceValue   = title;
            so.FindProperty("_bodyText").objectReferenceValue    = body;
            so.FindProperty("_confirmButton").objectReferenceValue = confirmBtn.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, "AccountRestartPopupView");
        }

        static void CreateAccountConflictPopup()
        {
            var root = FullScreen("AccountConflictPopupView");
            Comp<AccountConflictPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var backdrop = Btn(root, "Backdrop", Vector2.zero, new Vector2(1080, 1920), DIM, "", shadowAlpha: 200f / 255f);
            Stretch(backdrop);

            var panel = Panel(root, "Panel", new Vector2(900, 1100), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Account Data Conflict", PopupAccountConflictTitle);

            var body = TMP(panel, "BodyText", Center(0, 370, 800, 100), 18, UI_TEXT, "Choose which data to keep.", PopupAccountConflictBody, TextCategory.Normal);
            body.enableWordWrapping = true;

            // Local save panel (left)
            var localPanel = Panel(panel, "LocalPanel", new Vector2(380, 550), UI_BG_DEEP);
            Fixed(localPanel.transform.parent.gameObject, new Vector2(-215, 80), new Vector2(380 + 24, 550 + 24));

            var localLabel      = TMP(localPanel, "LocalLabel",      Center(0, 240, 340, 60),  22, UI_TEXT, "Current Data",  PopupAccountConflictLocalLabel, TextCategory.Header);
            var localStageText  = TMP(localPanel, "LocalStageText",  Center(0, 150, 340, 55),  18, UI_TEXT, "Stage 0",       PopupAccountConflictStageFmt,   TextCategory.Normal);
            var localGoldText   = TMP(localPanel, "LocalGoldText",   Center(0,  80, 340, 55),  18, UI_TEXT, "Gold: 0",       PopupAccountConflictGoldFmt,    TextCategory.Normal);
            var localStarsText  = TMP(localPanel, "LocalStarsText",  Center(0,  10, 340, 55),  18, UI_TEXT, "★ 0",          PopupAccountConflictStarsFmt,   TextCategory.Normal);
            var localItemsText  = TMP(localPanel, "LocalItemsText",  Center(0, -60, 340, 55),  18, UI_TEXT, "Items: 0",      PopupAccountConflictItemsFmt,   TextCategory.Normal);
            var keepLocalBtn    = Btn(localPanel, "KeepLocalButton", new Vector2(0, -190), new Vector2(340, 75), UI_PRIMARY, "Keep Current", PopupAccountConflictBtnKeepLocal);

            // Cloud save panel (right)
            var cloudPanel = Panel(panel, "CloudPanel", new Vector2(380, 550), UI_BG_DEEP);
            Fixed(cloudPanel.transform.parent.gameObject, new Vector2(215, 80), new Vector2(380 + 24, 550 + 24));

            var cloudLabel      = TMP(cloudPanel, "CloudLabel",      Center(0, 240, 340, 60),  22, UI_TEXT, "Google Account Data", PopupAccountConflictCloudLabel,        TextCategory.Header);
            var cloudStageText  = TMP(cloudPanel, "CloudStageText",  Center(0, 150, 340, 55),  18, UI_TEXT, "Stage 0",             PopupAccountConflictStageFmt,          TextCategory.Normal);
            var cloudGoldText   = TMP(cloudPanel, "CloudGoldText",   Center(0,  80, 340, 55),  18, UI_TEXT, "Gold: 0",             PopupAccountConflictGoldFmt,           TextCategory.Normal);
            var cloudStarsText  = TMP(cloudPanel, "CloudStarsText",  Center(0,  10, 340, 55),  18, UI_TEXT, "★ 0",                PopupAccountConflictStarsFmt,          TextCategory.Normal);
            var cloudItemsText  = TMP(cloudPanel, "CloudItemsText",  Center(0, -60, 340, 55),  18, UI_TEXT, "Items: 0",            PopupAccountConflictItemsFmt,          TextCategory.Normal);
            var keepCloudBtn    = Btn(cloudPanel, "KeepCloudButton", new Vector2(0, -190), new Vector2(340, 75), UI_PRIMARY, "Use Google Data", PopupAccountConflictBtnKeepCloud);

            var cancelBtn = Btn(panel, "CancelButton", new Vector2(0, -470), new Vector2(300, 70), UI_BG_DEEP, "Cancel", CommonBtnCancel);

            var so = new SerializedObject(root.GetComponent<AccountConflictPopupView>());
            so.FindProperty("_titleText").objectReferenceValue      = title;
            so.FindProperty("_bodyText").objectReferenceValue       = body;
            so.FindProperty("_localLabel").objectReferenceValue     = localLabel;
            so.FindProperty("_localStageText").objectReferenceValue = localStageText;
            so.FindProperty("_localGoldText").objectReferenceValue  = localGoldText;
            so.FindProperty("_localStarsText").objectReferenceValue = localStarsText;
            so.FindProperty("_localItemsText").objectReferenceValue = localItemsText;
            so.FindProperty("_keepLocalButton").objectReferenceValue = keepLocalBtn.GetComponent<Button>();
            so.FindProperty("_cloudLabel").objectReferenceValue     = cloudLabel;
            so.FindProperty("_cloudStageText").objectReferenceValue = cloudStageText;
            so.FindProperty("_cloudGoldText").objectReferenceValue  = cloudGoldText;
            so.FindProperty("_cloudStarsText").objectReferenceValue = cloudStarsText;
            so.FindProperty("_cloudItemsText").objectReferenceValue = cloudItemsText;
            so.FindProperty("_keepCloudButton").objectReferenceValue = keepCloudBtn.GetComponent<Button>();
            so.FindProperty("_cancelButton").objectReferenceValue   = cancelBtn.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, "AccountConflictPopupView");
        }
    }
}
#endif
