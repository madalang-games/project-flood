#if UNITY_EDITOR
using System.IO;
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
            CreateStageNodeView();
            CreateStaminaPopup();
            CreateTutorialOverlay();
            CreateChapterChest();
            
            // Generate Scenes as well
            SetupBoot();
            SetupLobby();
            SetupInGame();
            
            AssetDatabase.Refresh();
            Debug.Log("[UIEditorSetup] All base popups & scenes created successfully.");
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

            // SafeAreaRoot — fills Screen.safeArea
            var safeRoot = Child(canvas, "SafeAreaRoot");
            Stretch(safeRoot);
            Comp<SafeAreaHandler>(safeRoot);

            // Header — top 180px (Taller for casual layered styling)
            var header = Child(safeRoot, "Header");
            TopStrip(header, 180);
            Img(header, UI_BG_DEEP);
            var hv = Comp<HeaderView>(header);
            
            // Avatar Button (Square layout with C# idle floating)
            var avatarBtn = Btn(header, "AvatarButton", new Vector2(-440, -40), new Vector2(100, 100), UI_BG_MID, "");
            
            // Gold Container - Pill layout with gold border
            var goldContainer = Child(header, "GoldContainer");
            Fixed(goldContainer, new Vector2(340, -40), new Vector2(280, 80));
            Img(goldContainer, UI_BG_MID);
            
            var goldBorder = Child(goldContainer, "Border");
            Stretch(goldBorder);
            var goldBorderImg = Img(goldBorder, UI_BORDER);
            goldBorder.transform.SetAsFirstSibling();
            goldBorderImg.rectTransform.offsetMin = new Vector2(-4, -4);
            goldBorderImg.rectTransform.offsetMax = new Vector2(4, 4);

            // Animated Gold Icon
            var goldIcon = Child(goldContainer, "Icon");
            Fixed(goldIcon, new Vector2(-95, 0), new Vector2(50, 50));
            Img(goldIcon, UI_CTA);
            var goldIconAnim = Comp<UIIconIdleAnimator>(goldIcon);
            goldIconAnim.Configure(UIIconIdleAnimator.AnimationType.GlowSweep, 2.2f, 12f);

            var goldText = TMP(goldContainer, "GoldText", Center(30, 0, 160, 60), 22, UI_CTA, "0", null, TextCategory.Normal);
            var goldCsf = Comp<ContentSizeFitter>(goldText.gameObject);
            goldCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Stamina Panel — tappable, heart icon with count number on top + timer below
            var staminaPanel = Child(header, "StaminaPanel");
            Fixed(staminaPanel, new Vector2(-100, -40), new Vector2(180, 90));
            var staminaBtn = Comp<Button>(staminaPanel);
            staminaBtn.targetGraphic = Img(staminaPanel, new Color(0, 0, 0, 0)); // transparent hit area
            Comp<UIButtonAnimator>(staminaPanel);

            var heartIcon = Child(staminaPanel, "HeartIcon");
            Fixed(heartIcon, new Vector2(0, 12), new Vector2(62, 62));
            Img(heartIcon, UI_PRIMARY);
            Comp<UIIconIdleAnimator>(heartIcon).Configure(UIIconIdleAnimator.AnimationType.GlowSweep, 2.4f, 12f);

            // Count number overlaid on heart icon
            var staminaText = TMP(heartIcon, "CountText", Center(0, 0, 62, 62), 28, UI_TEXT, "5", null, TextCategory.Header);

            // Timer or MAX label below the heart
            var staminaTimerText = TMP(staminaPanel, "TimerText", Center(0, -32, 180, 36), 14, UI_CTA, "MAX", null, TextCategory.Normal);

            // BottomNavBar — bottom 140px, HorizontalLayoutGroup for tab distribution
            var navBar = Child(safeRoot, "BottomNavBar");
            BottomStrip(navBar, 140);
            Img(navBar, UI_BG_DEEP);
            var bnv = Comp<BottomNavBarView>(navBar);
            var navHlg = Comp<HorizontalLayoutGroup>(navBar);
            navHlg.childAlignment      = TextAnchor.MiddleCenter;
            navHlg.childForceExpandWidth  = true;
            navHlg.childForceExpandHeight = true;
            navHlg.padding = new RectOffset(20, 20, 20, 20);
            navHlg.spacing = 30;

            var shopBtn    = BtnHlg(navBar, "ShopButton",    UI_BG_MID, "Shop", NavShop);
            var homeBtn    = BtnHlg(navBar, "HomeButton",    UI_BG_MID, "Home", NavHome);
            var rankBtn    = BtnHlg(navBar, "RankingButton", UI_BG_MID, "Rank", NavRanking);

            // Tab content area — fills between header and nav
            var tabContent = Child(safeRoot, "TabContent");
            PaddedStretch(tabContent, 180, 140);

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
            soHtv.ApplyModifiedProperties();

            var shopTab = Child(tabContent, "ShopTab");  Stretch(shopTab); shopTab.SetActive(false);
            var rankingTab = Child(tabContent, "RankingTab"); Stretch(rankingTab); rankingTab.SetActive(false);
            var rankingView = Comp<RankingTabView>(rankingTab);
            var starsTab = Btn(rankingTab, "StarsTabButton", new Vector2(-230, 480), new Vector2(300, 80), UI_PRIMARY, "Stars", LobbyRankingTabStars);
            var maxStageTab = Btn(rankingTab, "MaxStageTabButton", new Vector2(230, 480), new Vector2(300, 80), UI_BG_MID, "Max Stage", LobbyRankingTabMaxStage);
            
            var rankTitle = TMP(rankingTab, "TitleText", Center(0, 360, 760, 70), 30, UI_CTA, "Star Ranking", null, TextCategory.Header);
            var myRank = TMP(rankingTab, "MyRankText", Center(0, 270, 760, 80), 24, UI_TEXT, "My Rank: -", null, TextCategory.Normal);
            var entries = TMP(rankingTab, "EntriesText", Center(0, -160, 820, 700), 20, UI_TEXT, "Ranking unavailable", null, TextCategory.Normal);
            entries.alignment = TextAlignmentOptions.TopLeft;

            // Generate VirtualizedScrollRect hierarchy
            var scrollRectGo = Child(rankingTab, "VirtualizedScrollRect");
            Fixed(scrollRectGo, new Vector2(0, -160), new Vector2(820, 700));
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

            var itemPrefabGo = Child(scrollRectGo, "RankingItemPrefab");
            Fixed(itemPrefabGo, new Vector2(0, 0), new Vector2(820, 80));
            Img(itemPrefabGo, UI_BG_MID);

            TMP(itemPrefabGo, "RankText", Center(-300, 0, 150, 60), 20, UI_CTA, "#1", null, TextCategory.Normal);
            TMP(itemPrefabGo, "NameText", Center(-50, 0, 300, 60), 20, UI_TEXT, "Player Name", null, TextCategory.Normal);
            TMP(itemPrefabGo, "ScoreText", Center(300, 0, 150, 60), 20, UI_CTA, "100", null, TextCategory.Normal);

            var soVScroll = new SerializedObject(vScroll);
            soVScroll.FindProperty("_itemPrefab").objectReferenceValue = itemPrefabGo.GetComponent<RectTransform>();
            soVScroll.FindProperty("_itemHeight").floatValue = 80f;
            soVScroll.FindProperty("_spacing").floatValue = 5f;
            soVScroll.ApplyModifiedProperties();

            itemPrefabGo.SetActive(false);

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
            soRanking.ApplyModifiedProperties();

            // Wire BottomNavBarView
            var soNav = new SerializedObject(bnv);
            soNav.FindProperty("_shopButton").objectReferenceValue       = shopBtn.GetComponent<Button>();
            soNav.FindProperty("_homeButton").objectReferenceValue       = homeBtn.GetComponent<Button>();
            soNav.FindProperty("_rankingButton").objectReferenceValue    = rankBtn.GetComponent<Button>();
            soNav.FindProperty("_shopHighlight").objectReferenceValue    = shopBtn.transform.Find("Visual").GetComponent<Image>();
            soNav.FindProperty("_homeHighlight").objectReferenceValue    = homeBtn.transform.Find("Visual").GetComponent<Image>();
            soNav.FindProperty("_rankingHighlight").objectReferenceValue = rankBtn.transform.Find("Visual").GetComponent<Image>();
            soNav.ApplyModifiedProperties();

            // Wire HeaderView
            var soHeader = new SerializedObject(hv);
            soHeader.FindProperty("_avatarButton").objectReferenceValue     = avatarBtn.GetComponent<Button>();
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

            // HUD — top 240px fixed area (generous for casual game feel)
            var hud = Child(canvas, "HUD");
            TopStrip(hud, 240);
            Img(hud, new Color(0, 0, 0, 0)); // Transparent container
            var hudView = Comp<HUDView>(hud);

            // Pause button — top-left square button
            var pauseBtn = Btn(hud, "PauseButton", new Vector2(-460, -90), new Vector2(100, 100), UI_PRIMARY, "");
            
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
            
            var turnsTxt = TMP(turnsBubble, "TurnsText", Center(0, 10, 160, 80), 36, UI_TEXT, "20", null, TextCategory.Header);
            TMP(turnsBubble, "TurnsLabel", Center(0, -50, 160, 40), 16, UI_TEXT, "TURNS", IngameTurnsLabel, TextCategory.Normal);

            // Progress bar container (Remaining Cells)
            var progressContainer = Child(hud, "ProgressContainer");
            Fixed(progressContainer, new Vector2(290, -90), new Vector2(200, 200));
            
            Img(progressContainer, UI_BG_DEEP);
            var progBorder = Child(progressContainer, "Border");
            Stretch(progBorder);
            var progBorderImg = Img(progBorder, UI_BORDER);
            progBorder.transform.SetAsFirstSibling();
            var progRt = progBorderImg.rectTransform;
            progRt.offsetMin = new Vector2(-8, -8);
            progRt.offsetMax = new Vector2(8, 8);

            var cellIcon = Child(progressContainer, "CellIcon");
            Fixed(cellIcon, new Vector2(0, 40), new Vector2(65, 65));
            Img(cellIcon, UI_PRIMARY);
            var iconShadow = Child(cellIcon, "Shadow");
            Fixed(iconShadow, new Vector2(0, -6), new Vector2(65, 65));
            Img(iconShadow, Hex("2B003B"));
            iconShadow.transform.SetAsFirstSibling();
            Comp<UIIconIdleAnimator>(cellIcon).Configure(UIIconIdleAnimator.AnimationType.Float, 2f, 6f);

            var remainingTxt = TMP(progressContainer, "RemainingText", Center(0, -40, 160, 80), 36, UI_TEXT, "0", null, TextCategory.Header);
            TMP(progressContainer, "RemainingLabel", Center(0, -85, 160, 40), 16, UI_TEXT, "CELLS", null, TextCategory.Normal);

            // Wire HUDView
            var soHud = new SerializedObject(hudView);
            soHud.FindProperty("_pauseButton").objectReferenceValue = pauseBtn.GetComponent<Button>();
            soHud.FindProperty("_turnsText").objectReferenceValue   = turnsTxt;
            soHud.FindProperty("_remainingText").objectReferenceValue = remainingTxt;
            soHud.ApplyModifiedProperties();

            // BoardContainer (anchor for world-space board)
            var board = Child(canvas, "BoardContainer"); Stretch(board);

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
            lineImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprite/cell/btn_46.png");
            lineImg.type = Image.Type.Sliced;
            lineImg.color = UI_PRIMARY; // Strawberry pink drag path line!
            lineGo.SetActive(false);

            var overlayView = Comp<RowShiftOverlayView>(overlayGo);

            var soOverlay = new SerializedObject(overlayView);
            soOverlay.FindProperty("_pointerImage").objectReferenceValue = pointerImg;
            soOverlay.FindProperty("_dragLineRect").objectReferenceValue = lineRt;
            var pointerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprite/cell/btn_46.png");
            if (pointerSprite != null)
            {
                soOverlay.FindProperty("_pointerSprite").objectReferenceValue = pointerSprite;
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
            Img(icon, Color.white);

            var countText = TMP(visual, "CountText", Center(30, -35, 70, 45), 16, UI_CTA, "0", null, TextCategory.Normal);
            countText.alignment = TextAlignmentOptions.BottomRight;

            var cg = Comp<CanvasGroup>(go);

            var slotView = Comp<ItemSlotView>(go);
            var so = new SerializedObject(slotView);
            so.FindProperty("_button").objectReferenceValue = btn;
            so.FindProperty("_countText").objectReferenceValue = countText;
            so.FindProperty("_selectedHighlight").objectReferenceValue = highlight;
            so.FindProperty("_canvasGroup").objectReferenceValue = cg;
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

            var backdrop = Btn(root, "Backdrop", Vector2.zero, new Vector2(1080, 1920), DIM, "");
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

        static void CreateToast()
        {
            var root = new GameObject("ToastView");
            root.AddComponent<RectTransform>();
            BottomStrip(root, 120);
            var rt = root.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 180); // sit above nav bar
            
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
            var title = RibbonTitle(panel, "TitleText", "Network Error", ErrorNetworkCheck);
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
            var root = FullScreen("RewardPopupView");
            Img(root, DIM); Comp<RewardPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel = Panel(root, "Panel", new Vector2(700, 640), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Reward!", PopupRewardTitle);

            var items = Child(panel, "ItemContainer"); Fixed(items, new Vector2(0, -20), new Vector2(600, 300));
            var vlg = items.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12; vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false; vlg.childControlWidth = false;

            var ok = Btn(panel, "OkButton", new Vector2(0, -230), new Vector2(300, 90), UI_PRIMARY, "OK", CommonBtnOk);

            var so = new SerializedObject(root.GetComponent<RewardPopupView>());
            so.FindProperty("_itemContainer").objectReferenceValue = items.transform;
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

            var backdrop = Btn(root, "Backdrop", Vector2.zero, new Vector2(1080, 1920), new Color(0, 0, 0, 0), "");
            Stretch(backdrop);

            var panel = Panel(root, "Panel", new Vector2(700, 420), UI_BG_MID);
            var title = RibbonTitle(panel, "StageTitleText", "Stage 1");
            
            var best  = TMP(panel, "BestRecordText", Center(0, 20, 600, 55), 20, UI_TEXT, "Best: 2 Stars", null, TextCategory.Normal);
            var bestCsf = Comp<ContentSizeFitter>(best.gameObject);
            bestCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 3 star placeholders
            var starsRoot = Child(panel, "Stars"); Fixed(starsRoot, new Vector2(0, -50), new Vector2(300, 60));
            var hlg = starsRoot.AddComponent<HorizontalLayoutGroup>(); hlg.spacing = 20; hlg.childAlignment = TextAnchor.MiddleCenter;
            var s0 = StarGO(starsRoot, "Star0"); var s1 = StarGO(starsRoot, "Star1"); var s2 = StarGO(starsRoot, "Star2");

            // Extra Turns Toggle Row (sitting between Stars and Play button)
            var toggleRow = ToggleRow(panel, "ExtraTurnsToggleRow", new Vector2(0, -100), "Add Turns", ItemNameAddTurn);
            Fixed(toggleRow, new Vector2(0, -105), new Vector2(500, 50));
            var labelTmp = toggleRow.transform.Find("Label").gameObject;
            Fixed(labelTmp, new Vector2(-100, 0), new Vector2(300, 50));
            var toggleGo = toggleRow.transform.Find("Toggle").gameObject;
            Fixed(toggleGo, new Vector2(180, 0), new Vector2(70, 40));
            var toggleBg = toggleGo.transform.Find("Background").gameObject;
            Fixed(toggleBg, Vector2.zero, new Vector2(70, 40));
            var toggleChk = toggleGo.transform.Find("Checkmark").gameObject;
            Fixed(toggleChk, Vector2.zero, new Vector2(30, 30));
            
            var toggleComp = toggleGo.GetComponent<Toggle>();
            toggleComp.isOn = false; // default off

            var play = Btn(panel, "PlayButton", new Vector2(0, -165), new Vector2(400, 80), UI_CTA, "Play", CommonBtnPlay);

            var so = new SerializedObject(root.GetComponent<StageInfoPopupView>());
            so.FindProperty("_stageTitle").objectReferenceValue     = title;
            so.FindProperty("_bestRecord").objectReferenceValue     = best;
            so.FindProperty("_playButton").objectReferenceValue     = play.GetComponent<Button>();
            so.FindProperty("_backdropButton").objectReferenceValue = backdrop.GetComponent<Button>();
            so.FindProperty("_extraTurnsToggle").objectReferenceValue = toggleComp;
            var starsArr = so.FindProperty("_bestStarFills");
            starsArr.arraySize = 3;
            starsArr.GetArrayElementAtIndex(0).objectReferenceValue = s0;
            starsArr.GetArrayElementAtIndex(1).objectReferenceValue = s1;
            starsArr.GetArrayElementAtIndex(2).objectReferenceValue = s2;
            so.ApplyModifiedProperties();

            Save(root, "StageInfoPopupView");
        }

        static void CreateResultOverlay()
        {
            var root = FullScreen("ResultOverlayView");
            Img(root, DIM); Comp<ResultOverlayView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel = Panel(root, "Panel", new Vector2(900, 840), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Stage Clear!", PopupResultTitle);
            
            var ratio  = TMP(panel, "RatioText", Center(0,   40, 700, 60), 22, UI_TEXT, "Cleared: 90%", null, TextCategory.Normal);
            var turns  = TMP(panel, "TurnsText", Center(0,  -20, 700, 60), 22, UI_TEXT, "Turns: 12/20", null, TextCategory.Normal);
            var rank   = TMP(panel, "RankText", Center(0,  -80, 700, 55), 20, UI_CTA, "", null, TextCategory.Normal);

            var goldRow = Child(panel, "GoldRow"); Fixed(goldRow, new Vector2(0, -140), new Vector2(700, 60));
            var goldTxt = TMP(goldRow, "GoldText", Center(0, 0, 700, 60), 24, UI_CTA, "+120 Gold", null, TextCategory.Normal);

            // Stars
            var starsRoot = Child(panel, "Stars"); Fixed(starsRoot, new Vector2(0, 160), new Vector2(400, 80));
            var hlg = starsRoot.AddComponent<HorizontalLayoutGroup>(); hlg.spacing = 20; hlg.childAlignment = TextAnchor.MiddleCenter;
            var s0 = StarGO(starsRoot, "Star0"); var s1 = StarGO(starsRoot, "Star1"); var s2 = StarGO(starsRoot, "Star2");

            // Add dance animation to reward stars
            Comp<UIIconIdleAnimator>(s0).Configure(UIIconIdleAnimator.AnimationType.Rotate, 2f, 15f);
            Comp<UIIconIdleAnimator>(s1).Configure(UIIconIdleAnimator.AnimationType.Float, 1.8f, 10f);
            Comp<UIIconIdleAnimator>(s2).Configure(UIIconIdleAnimator.AnimationType.Rotate, 2f, -15f);

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

            Save(root, "ResultOverlayView");
        }

        static void CreateFailOverlay()
        {
            var root = FullScreen("FailOverlayView");
            Img(root, DIM); Comp<FailOverlayView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel = Panel(root, "Panel", new Vector2(700, 620), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Just a bit more!", PopupFailTitle);
            
            var contLabel = TMP(panel, "ContinueLabel", Center(0,  80, 600, 60), 24, UI_TEXT, "+3 Turns", null, TextCategory.Normal);
            var costTxt   = TMP(panel, "CostText",       Center(0,  20, 600, 55), 22, UI_CTA,  "Cost: 150", null, TextCategory.Normal);
            var ownedTxt  = TMP(panel, "OwnedGoldText",  Center(0, -40, 600, 55), 20, UI_TEXT, "Gold: 320", null, TextCategory.Normal);

            var limitTxt = TMP(panel, "ReviveLimitText", Center(165, -95, 280, 40), 14, UI_TEXT, "Remaining Revives: 3", null, TextCategory.Normal);
            var adBtn   = Btn(panel, "WatchAdButton", new Vector2( 165, -160), new Vector2(280, 95), UI_PRIMARY, "Watch Ad", "");
            var contBtn = Btn(panel, "ContinueButton", new Vector2(-165, -160), new Vector2(280, 95), UI_CTA,    "Continue", PopupFailBtnContinue);
            var forfBtn = Btn(panel, "ForfeitButton",  new Vector2( 0, -250), new Vector2(280, 95), UI_BG_DEEP, "Give Up",  PopupFailBtnForfeit);

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

            var backdrop = Btn(root, "Backdrop", Vector2.zero, new Vector2(1080, 1920), new Color(0,0,0,0), "");
            Stretch(backdrop);

            // Bottom-sheet: bottom-anchored, 760px tall
            var border = Child(root, "Border");
            BottomStrip(border, 780);
            Img(border, Hex("2B003B"));
            
            var panel = Child(border, "InnerPanel");
            BottomStrip(panel, 760);
            Img(panel, UI_BG_MID);

            var title = RibbonTitle(panel, "TitleText", "Settings", CommonSettings);

            var bgmRow   = ToggleRow(panel, "BGMRow",         new Vector2(0, 200),  "BGM",          PopupSettingsBgm);
            var sfxRow   = ToggleRow(panel, "SFXRow",         new Vector2(0,  110),  "SFX",          PopupSettingsSfx);
            var shakeRow = ToggleRow(panel, "ScreenShakeRow", new Vector2(0,   20), "Screen Shake", PopupSettingsScreenShake);

            var accBtn = Btn(panel, "AccountButton",  new Vector2(0, -110), new Vector2(800, 90), UI_BG_DEEP, "Account ->", PopupSettingsAccount);
            var verTxt = TMP(panel, "VersionText",    Center(0, -230, 600, 50), 16, UI_TEXT, "v1.0.0", null, TextCategory.Normal);

            var so = new SerializedObject(root.GetComponent<SettingsPanelView>());
            so.FindProperty("_bgmToggle").objectReferenceValue         = bgmRow.GetComponentInChildren<Toggle>();
            so.FindProperty("_sfxToggle").objectReferenceValue         = sfxRow.GetComponentInChildren<Toggle>();
            so.FindProperty("_screenShakeToggle").objectReferenceValue = shakeRow.GetComponentInChildren<Toggle>();
            so.FindProperty("_accountButton").objectReferenceValue     = accBtn.GetComponent<Button>();
            so.FindProperty("_backdropButton").objectReferenceValue    = backdrop.GetComponent<Button>();
            so.FindProperty("_versionText").objectReferenceValue       = verTxt;
            so.ApplyModifiedProperties();

            Save(root, "SettingsPanelView");
        }

        static void CreateAccountPopup()
        {
            var root = FullScreen("AccountPopupView");
            Img(root, DIM); Comp<AccountPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel   = Panel(root, "Panel", new Vector2(700, 580), UI_BG_MID);
            var uidTxt = RibbonTitle(panel, "UserIdText", "Guest");
            
            var linkBtn = Btn(panel, "LinkAccountButton",   new Vector2(0,   50), new Vector2(500, 85), UI_PRIMARY, "Link Account",   PopupAccountBtnLink);
            var swBtn   = Btn(panel, "SwitchAccountButton", new Vector2(0,   50), new Vector2(500, 85), UI_PRIMARY, "Switch Account", PopupAccountBtnSwitch);
            var logBtn  = Btn(panel, "LogoutButton",        new Vector2(0,  -60), new Vector2(500, 85), UI_DANGER,  "Logout",         PopupAccountBtnLogout);
            var clsBtn  = Btn(panel, "CloseButton",         new Vector2(0, -170), new Vector2(200, 75), UI_BG_DEEP, "Close",          CommonBtnClose);

            var so = new SerializedObject(root.GetComponent<AccountPopupView>());
            so.FindProperty("_userIdText").objectReferenceValue          = uidTxt;
            so.FindProperty("_linkAccountButton").objectReferenceValue   = linkBtn.GetComponent<Button>();
            so.FindProperty("_switchAccountButton").objectReferenceValue = swBtn.GetComponent<Button>();
            so.FindProperty("_logoutButton").objectReferenceValue        = logBtn.GetComponent<Button>();
            so.FindProperty("_closeButton").objectReferenceValue         = clsBtn.GetComponent<Button>();
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
            
            // Stars container
            var starsRoot = Child(root, "Stars");
            Fixed(starsRoot, new Vector2(0f, -60f), new Vector2(120f, 40f));
            var hlg = starsRoot.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            
            var s0 = StarGO(starsRoot, "Star0"); var s0Fill = Child(s0, "Fill"); Stretch(s0Fill); Img(s0Fill, UI_CTA);
            var s1 = StarGO(starsRoot, "Star1"); var s1Fill = Child(s1, "Fill"); Stretch(s1Fill); Img(s1Fill, UI_CTA);
            var s2 = StarGO(starsRoot, "Star2"); var s2Fill = Child(s2, "Fill"); Stretch(s2Fill); Img(s2Fill, UI_CTA);
            
            RT(s0).sizeDelta = RT(s1).sizeDelta = RT(s2).sizeDelta = new Vector2(30f, 30f);

            // Lock Overlay
            var lockOverlay = Child(root, "LockOverlay");
            Stretch(lockOverlay);
            Img(lockOverlay, new Color(0.1f, 0.1f, 0.15f, 0.6f));
            var lockIcon = Child(lockOverlay, "LockIcon");
            Fixed(lockIcon, Vector2.zero, new Vector2(40f, 40f));
            Img(lockIcon, Color.white);
            lockOverlay.SetActive(false);

            // Pulse Ring
            var pulseRing = Child(root, "PulseRing");
            Fixed(pulseRing, Vector2.zero, new Vector2(150f, 150f));
            Img(pulseRing, UI_CTA);
            Comp<UIScalePulse>(pulseRing);
            pulseRing.SetActive(false);

            // Button
            var btn = Comp<Button>(root);
            btn.targetGraphic = borderImg;

            // Wire StageNodeView properties
            var so = new SerializedObject(snv);
            so.FindProperty("_stageLabel").objectReferenceValue = stageLabel;
            so.FindProperty("_lockOverlay").objectReferenceValue = lockOverlay;
            so.FindProperty("_pulseRing").objectReferenceValue  = pulseRing;
            so.FindProperty("_button").objectReferenceValue     = btn;
            so.FindProperty("_border").objectReferenceValue     = borderImg;
            
            var starFillsArr = so.FindProperty("_starFills");
            starFillsArr.arraySize = 3;
            starFillsArr.GetArrayElementAtIndex(0).objectReferenceValue = s0Fill;
            starFillsArr.GetArrayElementAtIndex(1).objectReferenceValue = s1Fill;
            starFillsArr.GetArrayElementAtIndex(2).objectReferenceValue = s2Fill;
            so.ApplyModifiedProperties();

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
            var timerText = TMP(panel, "TimerText", Center(0, -30, 500, 64), 26, UI_CTA, "MAX", null, TextCategory.Normal);

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
            
            // Bind Serialized Fields on ChapterChestView
            var so = new SerializedObject(chestView);
            so.FindProperty("_chestImage").objectReferenceValue = chestImg;
            so.FindProperty("_statusText").objectReferenceValue = textTmp;
            so.FindProperty("_button").objectReferenceValue = chestBtn;
            so.FindProperty("_glowEffect").objectReferenceValue = glow;
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
        static GameObject Btn(GameObject parent, string name, Vector2 pos, Vector2 size, Color color, string label, string labelStringId = null)
        {
            var go = Child(parent, name); Fixed(go, pos, size);
            
            // Shadow underlay for 3D look
            var shadowGo = Child(go, "Shadow");
            Fixed(shadowGo, new Vector2(0, -8f), size);
            Img(shadowGo, Hex("2B003B"));
            
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
                Fixed(iconGo, Vector2.zero, size * 0.6f);
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

        static GameObject StarGO(GameObject parent, string name)
        {
            var go = Child(parent, name);
            Fixed(go, Vector2.zero, new Vector2(70, 70));
            Img(go, UI_CTA);
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
    }
}
#endif
