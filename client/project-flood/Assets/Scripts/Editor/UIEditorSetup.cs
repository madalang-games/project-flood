#if UNITY_EDITOR
using System.IO;
using Game.Core;
using Game.Core.UI;
using Game.InGame.View;
using Game.OutGame.Boot;
using Game.OutGame.Lobby;
using Game.OutGame.Settings;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// Tools/UI Setup — one-shot editor scripts.
    /// Run menu items once per project (safe to re-run; overwrites).
    /// </summary>
    public static class UIEditorSetup
    {
        // Prefab output paths
        private const string PrefabRoot   = "Assets/Resources/Prefabs/UI";
        private const string PrefabCommon = "Assets/UI/Prefabs/Common";
        private const string PrefabBase   = "Assets/UI/Prefabs/Base";
        private const string PrefabFinal  = "Assets/UI/Prefabs/Final";

        // Color tokens from ui-ux-config.md
        static Color UI_BG_DEEP  => Hex("0D1B2A");
        static Color UI_BG_MID   => Hex("1A2F45");
        static Color UI_PRIMARY  => Hex("4A90D9");
        static Color UI_CTA      => Hex("E8A020");
        static Color UI_SUCCESS  => Hex("3DBE6E");
        static Color UI_DANGER   => Hex("E84040");
        static Color UI_TEXT     => Hex("F0EAD6");
        static Color DIM         => new Color(0.05f, 0.1f, 0.16f, 0.8f);

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
            AssetDatabase.Refresh();
            Debug.Log("[UIEditorSetup] All prefabs created → " + PrefabRoot);
        }

        [MenuItem("Tools/UI Setup/2 - Setup Boot Canvas Scene", false, 200)]
        static void SetupBoot()
        {
            var canvas = FindOrCreateSceneCanvas();
            var content = Child(canvas, "Content");
            Stretch(content);
            TMP(content, "LogoText",    Center(0, 200, 600, 120), 48, UI_TEXT, "PROJECT FLOOD");
            TMP(content, "SpinnerText", Center(0, -200, 600, 80),                24, UI_TEXT, "Loading...");
            Debug.Log("[UIEditorSetup] Boot Canvas_Scene ready.");
        }

        [MenuItem("Tools/UI Setup/3 - Setup Lobby Canvas Scene", false, 201)]
        static void SetupLobby()
        {
            var canvas = FindOrCreateSceneCanvas();
            canvas.AddComponent<LobbyView>();

            // SafeAreaRoot — fills Screen.safeArea
            var safeRoot = Child(canvas, "SafeAreaRoot");
            Stretch(safeRoot);
            Comp<SafeAreaHandler>(safeRoot);

            // Header — top 120px
            var header = Child(safeRoot, "Header");
            TopStrip(header, 120);
            Img(header, UI_BG_DEEP);
            var hv = Comp<HeaderView>(header);
            var avatarBtn = Btn(header, "AvatarButton", new Vector2(-460, 0), new Vector2(80, 80), UI_BG_MID, "👤");
            TMP(header, "GoldText", Center(380, 0, 240, 60), 22, UI_CTA, "🪙 0");
            // GoldText: content-driven width (coin amount length varies)
            var goldCsf = Comp<ContentSizeFitter>(Child(header, "GoldText"));
            goldCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // BottomNavBar — bottom 120px, HorizontalLayoutGroup for even tab distribution
            var navBar = Child(safeRoot, "BottomNavBar");
            BottomStrip(navBar, 120);
            Img(navBar, UI_BG_DEEP);
            var bnv = Comp<BottomNavBarView>(navBar);
            var navHlg = Comp<HorizontalLayoutGroup>(navBar);
            navHlg.childAlignment      = TextAnchor.MiddleCenter;
            navHlg.childForceExpandWidth  = true;
            navHlg.childForceExpandHeight = true;
            navHlg.padding = new RectOffset(0, 0, 0, 0);
            navHlg.spacing = 0;
            var homeBtn    = BtnHlg(navBar, "HomeButton",    UI_BG_MID, "🏠\nHome");
            var shopBtn    = BtnHlg(navBar, "ShopButton",    UI_BG_MID, "🛒\nShop");
            var rankBtn    = BtnHlg(navBar, "RankingButton", UI_BG_MID, "🏆\nRank");

            // Tab content area — fills between header and nav
            var tabContent = Child(safeRoot, "TabContent");
            PaddedStretch(tabContent, 120, 120);

            // HomeTab — ScrollRect with content transform
            var homeTab = Child(tabContent, "HomeTab");  Stretch(homeTab);
            var htv = Comp<HomeTabView>(homeTab);
            if (!homeTab.TryGetComponent<ScrollRect>(out var scrollRect))
                scrollRect = homeTab.AddComponent<ScrollRect>();
            scrollRect.horizontal = false; scrollRect.vertical = true;
            var viewportGo = Child(homeTab, "Viewport"); Stretch(viewportGo);
            var viewportMask = Comp<RectMask2D>(viewportGo);
            var contentGo = Child(viewportGo, "Content");
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1); contentRt.sizeDelta = new Vector2(0, 3000);
            scrollRect.viewport = viewportGo.GetComponent<RectTransform>();
            scrollRect.content  = contentRt;

            // Load StageNodeView prefab if it exists
            var nodeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/StageNodeView.prefab");

            var soHtv = new SerializedObject(htv);
            soHtv.FindProperty("_scrollRect").objectReferenceValue   = scrollRect;
            soHtv.FindProperty("_contentRoot").objectReferenceValue  = contentRt;
            if (nodeAsset != null)
                soHtv.FindProperty("_stageNodePrefab").objectReferenceValue = nodeAsset;
            soHtv.ApplyModifiedProperties();

            var shopTab = Child(tabContent, "ShopTab");  Stretch(shopTab); shopTab.SetActive(false);

            // Wire LobbyView refs
            var soLobby = new SerializedObject(canvas.GetComponent<LobbyView>());
            soLobby.FindProperty("_header").objectReferenceValue      = hv;
            soLobby.FindProperty("_navBar").objectReferenceValue      = bnv;
            soLobby.FindProperty("_homeTabRoot").objectReferenceValue = homeTab;
            soLobby.FindProperty("_shopTabRoot").objectReferenceValue = shopTab;
            soLobby.ApplyModifiedProperties();

            // Wire BottomNavBarView
            var soNav = new SerializedObject(bnv);
            soNav.FindProperty("_homeButton").objectReferenceValue    = homeBtn.GetComponent<Button>();
            soNav.FindProperty("_shopButton").objectReferenceValue    = shopBtn.GetComponent<Button>();
            soNav.FindProperty("_rankingButton").objectReferenceValue = rankBtn.GetComponent<Button>();
            soNav.ApplyModifiedProperties();

            // Wire HeaderView
            var soHeader = new SerializedObject(hv);
            soHeader.FindProperty("_avatarButton").objectReferenceValue = avatarBtn.GetComponent<Button>();
            soHeader.FindProperty("_goldText").objectReferenceValue     = header.transform.Find("GoldText")?.GetComponent<TMP_Text>();
            soHeader.ApplyModifiedProperties();

            Debug.Log("[UIEditorSetup] Lobby Canvas_Scene ready.");
        }

        [MenuItem("Tools/UI Setup/4 - Setup InGame Canvas Scene", false, 202)]
        static void SetupInGame()
        {
            var canvas = FindOrCreateSceneCanvas();
            Comp<UIScreenShake>(canvas);

            // HUD — top 160px fixed strip (no SafeAreaHandler: would overwrite TopStrip anchor to Stretch)
            var hud = Child(canvas, "HUD");
            TopStrip(hud, 160);
            var hudView = Comp<HUDView>(hud);

            var pauseBtn = Btn(hud, "PauseButton",  new Vector2(-480, -60), new Vector2(80, 80), UI_BG_MID, "⏸");
            var turnsTxt = TMP(hud, "TurnsText",    Center(0, -60, 200, 80), 36, UI_TEXT, "20");

            var ratioBar = Child(hud, "RatioBar");
            PaddedStretch(ratioBar, 0, 0);
            // right half of HUD, top area
            var ratioRt = ratioBar.GetComponent<RectTransform>();
            ratioRt.anchorMin = new Vector2(0.5f, 0.5f); ratioRt.anchorMax = new Vector2(1, 1);
            ratioRt.offsetMin = new Vector2(20, 30); ratioRt.offsetMax = new Vector2(-30, -30);
            var fillImg = Img(ratioBar, UI_SUCCESS);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;

            // Wire HUDView
            var soHud = new SerializedObject(hudView);
            soHud.FindProperty("_pauseButton").objectReferenceValue = pauseBtn.GetComponent<Button>();
            soHud.FindProperty("_turnsText").objectReferenceValue   = turnsTxt;
            soHud.FindProperty("_ratioFill").objectReferenceValue   = fillImg;
            soHud.ApplyModifiedProperties();

            // BoardContainer (anchor for world-space board)
            var board = Child(canvas, "BoardContainer"); Stretch(board);

            Debug.Log("[UIEditorSetup] InGame Canvas_Scene ready.");
        }

        // ════════════════════════════════════════════════════════════════
        //  PREFAB BUILDERS — all saved to Resources/Prefabs/UI/
        // ════════════════════════════════════════════════════════════════

        static void CreateConfirmDialog()
        {
            var root = FullScreen("ConfirmDialogView");
            Comp<ConfirmDialogView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var backdrop = Btn(root, "Backdrop", Vector2.zero, new Vector2(1080, 1920), DIM, "");
            Stretch(backdrop);

            var panel = Panel(root, "Panel", new Vector2(900, 500), UI_BG_MID);
            var title  = TMP(panel, "TitleText", Center(0, 160, 800, 80), 24, UI_TEXT, "Title");
            var body   = TMP(panel, "BodyText",  Center(0,  50, 800,120), 18, UI_TEXT, "Body");
            // BodyText: auto-height for variable message length
            body.enableWordWrapping = true;
            var bodyCsf = Comp<ContentSizeFitter>(Child(panel, "BodyText"));
            bodyCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var cancel = Btn(panel, "CancelButton",  new Vector2(-200, -160), new Vector2(360, 80), UI_BG_DEEP, "Cancel");
            var confirm= Btn(panel, "ConfirmButton", new Vector2( 200, -160), new Vector2(360, 80), UI_PRIMARY,  "OK");

            var so = new SerializedObject(root.GetComponent<ConfirmDialogView>());
            so.FindProperty("_titleText").objectReferenceValue         = title;
            so.FindProperty("_bodyText").objectReferenceValue          = body;
            so.FindProperty("_cancelLabel").objectReferenceValue       = cancel.transform.Find("Label").GetComponent<TMP_Text>();
            so.FindProperty("_confirmLabel").objectReferenceValue      = confirm.transform.Find("Label").GetComponent<TMP_Text>();
            so.FindProperty("_cancelButton").objectReferenceValue      = cancel.GetComponent<Button>();
            so.FindProperty("_confirmButton").objectReferenceValue     = confirm.GetComponent<Button>();
            so.FindProperty("_backdropButton").objectReferenceValue    = backdrop.GetComponent<Button>();
            so.FindProperty("_confirmButtonImage").objectReferenceValue = confirm.GetComponent<Image>();
            so.ApplyModifiedProperties();

            Save(root, PrefabRoot + "/ConfirmDialogView.prefab");
        }

        static void CreateToast()
        {
            var root = new GameObject("ToastView");
            root.AddComponent<RectTransform>();
            // bottom strip, 100px tall, above nav bar
            BottomStrip(root, 100);
            var rt = root.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 140); // sit above nav bar
            Img(root, UI_BG_MID); Comp<ToastView>(root); Comp<CanvasGroup>(root);
            // Toast panel is fixed width (full width) — TMP overflows via truncation, not CSF
            var msgTxt = TMP(root, "MessageText", Center(80, 0, 900, 80), 18, UI_TEXT, "Message");
            msgTxt.overflowMode = TextOverflowModes.Ellipsis;
            msgTxt.enableWordWrapping = false;

            Save(root, PrefabRoot + "/ToastView.prefab");
        }

        static void CreateLoadingOverlay()
        {
            var root = FullScreen("LoadingOverlayView");
            Img(root, new Color(0.05f, 0.1f, 0.16f, 0.6f));
            Comp<LoadingOverlayView>(root);
            var spinner = Child(root, "Spinner"); Fixed(spinner, Vector2.zero, new Vector2(96, 96));
            Img(spinner, Color.white).preserveAspect = true;
            var msgTxt = TMP(root, "MessageText", Center(0, -120, 600, 60), 20, UI_TEXT, "");

            var so = new SerializedObject(root.GetComponent<LoadingOverlayView>());
            so.FindProperty("_messageText").objectReferenceValue = msgTxt;
            so.ApplyModifiedProperties();

            Save(root, PrefabRoot + "/LoadingOverlayView.prefab");
        }

        static void CreateNetworkError()
        {
            var root = FullScreen("NetworkErrorView");
            Img(root, DIM); Comp<NetworkErrorView>(root);

            var panel = Panel(root, "Panel", new Vector2(800, 400), UI_BG_MID);
            var msg   = TMP(panel, "MessageText", Center(0, 60, 680, 180), 20, UI_TEXT, "Check your network connection.");
            var retry = Btn(panel, "RetryButton", new Vector2(0, -130), new Vector2(300, 80), UI_PRIMARY, "Retry");

            var so = new SerializedObject(root.GetComponent<NetworkErrorView>());
            so.FindProperty("_messageText").objectReferenceValue = msg;
            so.FindProperty("_retryButton").objectReferenceValue = retry.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, PrefabRoot + "/NetworkErrorView.prefab");
        }

        static void CreateRewardPopup()
        {
            var root = FullScreen("RewardPopupView");
            Img(root, DIM); Comp<RewardPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel = Panel(root, "Panel", new Vector2(700, 600), UI_BG_MID);
            TMP(panel, "TitleText", Center(0, 230, 600, 70), 28, UI_CTA, "Reward!");

            var items = Child(panel, "ItemContainer"); Fixed(items, new Vector2(0, 10), new Vector2(600, 280));
            var vlg = items.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12; vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false; vlg.childControlWidth = false;

            var ok = Btn(panel, "OkButton", new Vector2(0, -220), new Vector2(300, 80), UI_PRIMARY, "OK");

            var so = new SerializedObject(root.GetComponent<RewardPopupView>());
            so.FindProperty("_itemContainer").objectReferenceValue = items.transform;
            so.FindProperty("_okButton").objectReferenceValue      = ok.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, PrefabRoot + "/RewardPopupView.prefab");
        }

        static void CreateReLoginView()
        {
            var root = FullScreen("ReLoginView");
            Img(root, UI_BG_DEEP); Comp<ReLoginView>(root);

            var panel   = Panel(root, "Panel", new Vector2(900, 500), UI_BG_MID);
            TMP(panel, "TitleText",  Center(0, 160, 800, 70), 28, UI_TEXT, "Session expired");
            var relogin = Btn(panel, "ReLoginButton",         new Vector2(0,  30), new Vector2(500, 90), UI_PRIMARY, "Re-login");
            var guest   = Btn(panel, "ContinueAsGuestButton", new Vector2(0, -80), new Vector2(500, 80), UI_BG_DEEP, "Continue as Guest");

            var so = new SerializedObject(root.GetComponent<ReLoginView>());
            so.FindProperty("_reLoginButton").objectReferenceValue         = relogin.GetComponent<Button>();
            so.FindProperty("_continueAsGuestButton").objectReferenceValue = guest.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, PrefabRoot + "/ReLoginView.prefab");
        }

        static void CreateStageInfoPopup()
        {
            var root = FullScreen("StageInfoPopupView");
            Img(root, DIM); Comp<StageInfoPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var backdrop = Btn(root, "Backdrop", Vector2.zero, new Vector2(1080, 1920), new Color(0, 0, 0, 0), "");
            Stretch(backdrop);

            var panel = Panel(root, "Panel", new Vector2(700, 380), UI_BG_MID);
            var title  = TMP(panel, "StageTitleText", Center(0, 120, 600, 70), 28, UI_TEXT, "Stage 1");
            var best   = TMP(panel, "BestRecordText", Center(0,  40, 600, 55), 20, UI_TEXT, "Best: ★★☆");
            // BestRecord: content-driven width (move count varies)
            var bestCsf = Comp<ContentSizeFitter>(Child(panel, "BestRecordText"));
            bestCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 3 star placeholders
            var starsRoot = Child(panel, "Stars"); Fixed(starsRoot, new Vector2(0, -30), new Vector2(300, 60));
            var hlg = starsRoot.AddComponent<HorizontalLayoutGroup>(); hlg.spacing = 10; hlg.childAlignment = TextAnchor.MiddleCenter;
            var s0 = StarGO(starsRoot, "Star0"); var s1 = StarGO(starsRoot, "Star1"); var s2 = StarGO(starsRoot, "Star2");

            var play = Btn(panel, "PlayButton", new Vector2(0, -140), new Vector2(400, 90), UI_CTA, "PLAY");

            var so = new SerializedObject(root.GetComponent<StageInfoPopupView>());
            so.FindProperty("_stageTitle").objectReferenceValue     = title;
            so.FindProperty("_bestRecord").objectReferenceValue     = best;
            so.FindProperty("_playButton").objectReferenceValue     = play.GetComponent<Button>();
            so.FindProperty("_backdropButton").objectReferenceValue = backdrop.GetComponent<Button>();
            var starsArr = so.FindProperty("_bestStarFills");
            starsArr.arraySize = 3;
            starsArr.GetArrayElementAtIndex(0).objectReferenceValue = s0;
            starsArr.GetArrayElementAtIndex(1).objectReferenceValue = s1;
            starsArr.GetArrayElementAtIndex(2).objectReferenceValue = s2;
            so.ApplyModifiedProperties();

            Save(root, PrefabRoot + "/StageInfoPopupView.prefab");
        }

        static void CreateResultOverlay()
        {
            var root = FullScreen("ResultOverlayView");
            Img(root, DIM); Comp<ResultOverlayView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel = Panel(root, "Panel", new Vector2(900, 800), UI_BG_MID);
            var title  = TMP(panel, "TitleText", Center(0,  320, 800, 80), 32, UI_CTA,  "Stage Clear!");
            var ratio  = TMP(panel, "RatioText", Center(0,   80, 700, 60), 22, UI_TEXT, "Cleared: 90%");
            var turns  = TMP(panel, "TurnsText", Center(0,   10, 700, 60), 22, UI_TEXT, "Turns: 12/20");

            var goldRow = Child(panel, "GoldRow"); Fixed(goldRow, new Vector2(0, -60), new Vector2(700, 60));
            var goldTxt = TMP(goldRow, "GoldText", Center(0, 0, 700, 60), 24, UI_CTA, "+120 🪙");

            // Stars
            var starsRoot = Child(panel, "Stars"); Fixed(starsRoot, new Vector2(0, 200), new Vector2(400, 80));
            var hlg = starsRoot.AddComponent<HorizontalLayoutGroup>(); hlg.spacing = 20; hlg.childAlignment = TextAnchor.MiddleCenter;
            var s0 = StarGO(starsRoot, "Star0"); var s1 = StarGO(starsRoot, "Star1"); var s2 = StarGO(starsRoot, "Star2");

            var retry = Btn(panel, "RetryButton", new Vector2(-270, -320), new Vector2(220, 80), UI_BG_DEEP, "Retry");
            var next  = Btn(panel, "NextButton",  new Vector2(   0, -320), new Vector2(220, 80), UI_PRIMARY,  "Next");
            var map   = Btn(panel, "MapButton",   new Vector2( 270, -320), new Vector2(220, 80), UI_BG_DEEP,  "Map");

            var so = new SerializedObject(root.GetComponent<ResultOverlayView>());
            so.FindProperty("_titleText").objectReferenceValue  = title;
            so.FindProperty("_ratioText").objectReferenceValue  = ratio;
            so.FindProperty("_turnsText").objectReferenceValue  = turns;
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

            Save(root, PrefabRoot + "/ResultOverlayView.prefab");
        }

        static void CreateFailOverlay()
        {
            var root = FullScreen("FailOverlayView");
            Img(root, DIM); Comp<FailOverlayView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel = Panel(root, "Panel", new Vector2(700, 500), UI_BG_MID);
            TMP(panel, "TitleText", Center(0, 180, 600, 80), 32, UI_CTA, "조금만 더!");
            var contLabel = TMP(panel, "ContinueLabel", Center(0,  80, 600, 60), 24, UI_TEXT, "+3 Turns");
            var costTxt   = TMP(panel, "CostText",       Center(0,  10, 600, 55), 22, UI_CTA,  "🪙 150");
            var ownedTxt  = TMP(panel, "OwnedGoldText",  Center(0, -50, 600, 55), 20, UI_TEXT, "🪙 320");

            var contBtn = Btn(panel, "ContinueButton", new Vector2(-150, -160), new Vector2(280, 90), UI_CTA,    "계속하기");
            var forfBtn = Btn(panel, "ForfeitButton",  new Vector2( 150, -160), new Vector2(280, 90), UI_BG_DEEP, "포기");

            var so = new SerializedObject(root.GetComponent<FailOverlayView>());
            so.FindProperty("_continueLabel").objectReferenceValue  = contLabel;
            so.FindProperty("_costText").objectReferenceValue       = costTxt;
            so.FindProperty("_ownedGoldText").objectReferenceValue  = ownedTxt;
            so.FindProperty("_continueButton").objectReferenceValue = contBtn.GetComponent<Button>();
            so.FindProperty("_forfeitButton").objectReferenceValue  = forfBtn.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, PrefabRoot + "/FailOverlayView.prefab");
        }

        static void CreatePausePopup()
        {
            var root = FullScreen("PausePopupView");
            Img(root, DIM); Comp<PausePopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel = Panel(root, "Panel", new Vector2(600, 560), UI_BG_MID);
            TMP(panel, "TitleText", Center(0, 220, 500, 70), 30, UI_TEXT, "일시정지");

            var resume  = Btn(panel, "ResumeButton",      new Vector2(0,  100), new Vector2(480, 90), UI_PRIMARY, "재개");
            var restart = Btn(panel, "RestartButton",     new Vector2(0,  -10), new Vector2(480, 90), UI_BG_DEEP, "처음부터");
            var settings= Btn(panel, "SettingsButton",    new Vector2(0, -120), new Vector2(480, 90), UI_BG_DEEP, "Settings");
            var select  = Btn(panel, "StageSelectButton", new Vector2(0, -230), new Vector2(480, 90), UI_BG_DEEP, "스테이지 선택");

            var so = new SerializedObject(root.GetComponent<PausePopupView>());
            so.FindProperty("_resumeButton").objectReferenceValue      = resume.GetComponent<Button>();
            so.FindProperty("_restartButton").objectReferenceValue     = restart.GetComponent<Button>();
            so.FindProperty("_settingsButton").objectReferenceValue    = settings.GetComponent<Button>();
            so.FindProperty("_stageSelectButton").objectReferenceValue = select.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, PrefabRoot + "/PausePopupView.prefab");
        }

        static void CreateSettingsPanel()
        {
            var root = FullScreen("SettingsPanelView");
            Img(root, DIM); Comp<SettingsPanelView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var backdrop = Btn(root, "Backdrop", Vector2.zero, new Vector2(1080, 1920), new Color(0,0,0,0), "");
            Stretch(backdrop);

            // Bottom-sheet: bottom-anchored, 700px tall
            var panel = Child(root, "Panel");
            BottomStrip(panel, 700);
            Img(panel, UI_BG_MID);

            TMP(panel, "TitleText", Center(0, 300, 600, 70), 28, UI_TEXT, "Settings");

            var bgmRow   = ToggleRow(panel, "BGMRow",         new Vector2(0, 170),  "BGM");
            var sfxRow   = ToggleRow(panel, "SFXRow",         new Vector2(0,  80),  "SFX");
            var shakeRow = ToggleRow(panel, "ScreenShakeRow", new Vector2(0,  -10), "Screen Shake");

            var accBtn = Btn(panel, "AccountButton",  new Vector2(0, -120), new Vector2(800, 80), UI_BG_DEEP, "Account  →");
            var verTxt = TMP(panel, "VersionText",    Center(0, -230, 600, 50), 16, UI_TEXT, "v1.0.0");

            var so = new SerializedObject(root.GetComponent<SettingsPanelView>());
            so.FindProperty("_bgmToggle").objectReferenceValue         = bgmRow.GetComponentInChildren<Toggle>();
            so.FindProperty("_sfxToggle").objectReferenceValue         = sfxRow.GetComponentInChildren<Toggle>();
            so.FindProperty("_screenShakeToggle").objectReferenceValue = shakeRow.GetComponentInChildren<Toggle>();
            so.FindProperty("_accountButton").objectReferenceValue     = accBtn.GetComponent<Button>();
            so.FindProperty("_backdropButton").objectReferenceValue    = backdrop.GetComponent<Button>();
            so.FindProperty("_versionText").objectReferenceValue       = verTxt;
            so.ApplyModifiedProperties();

            Save(root, PrefabRoot + "/SettingsPanelView.prefab");
        }

        static void CreateAccountPopup()
        {
            var root = FullScreen("AccountPopupView");
            Img(root, DIM); Comp<AccountPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel   = Panel(root, "Panel", new Vector2(700, 550), UI_BG_MID);
            var uidTxt  = TMP(panel, "UserIdText",    Center(0, 170, 600, 70), 24, UI_TEXT, "Guest");
            var linkBtn = Btn(panel, "LinkAccountButton",   new Vector2(0,  60), new Vector2(500, 80), UI_PRIMARY, "Link Account");
            var swBtn   = Btn(panel, "SwitchAccountButton", new Vector2(0,  60), new Vector2(500, 80), UI_PRIMARY, "Switch Account");
            var logBtn  = Btn(panel, "LogoutButton",        new Vector2(0, -50), new Vector2(500, 80), UI_DANGER,  "Logout");
            var clsBtn  = Btn(panel, "CloseButton",         new Vector2(0,-160), new Vector2(200, 70), UI_BG_DEEP, "Close");

            var so = new SerializedObject(root.GetComponent<AccountPopupView>());
            so.FindProperty("_userIdText").objectReferenceValue          = uidTxt;
            so.FindProperty("_linkAccountButton").objectReferenceValue   = linkBtn.GetComponent<Button>();
            so.FindProperty("_switchAccountButton").objectReferenceValue = swBtn.GetComponent<Button>();
            so.FindProperty("_logoutButton").objectReferenceValue        = logBtn.GetComponent<Button>();
            so.FindProperty("_closeButton").objectReferenceValue         = clsBtn.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, PrefabRoot + "/AccountPopupView.prefab");
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
            var go = Child(parent, name); Fixed(go, Vector2.zero, size); Img(go, color); return go;
        }

        // Button with label child
        static GameObject Btn(GameObject parent, string name, Vector2 pos, Vector2 size, Color color, string label)
        {
            var go = Child(parent, name); Fixed(go, pos, size);
            var img = Img(go, color);
            if (!go.TryGetComponent<Button>(out var btn)) btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            Comp<UIButtonAnimator>(go);
            if (!string.IsNullOrEmpty(label))
                TMP(go, "Label", Center(0, 0, size.x, size.y), 20, UI_TEXT, label);
            return go;
        }

        // Button for use inside LayoutGroup — no Fixed() anchor, uses LayoutElement for sizing
        static GameObject BtnHlg(GameObject parent, string name, Color color, string label)
        {
            var go = Child(parent, name);
            // Reset RT to default stretch so HLG can drive it
            var rt = RT(go);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var le = Comp<LayoutElement>(go);
            le.flexibleWidth  = 1;
            le.preferredHeight = 100;
            var img = Img(go, color);
            if (!go.TryGetComponent<Button>(out var btn)) btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            Comp<UIButtonAnimator>(go);
            TMP(go, "Label", Center(0, 0, 180, 80), 18, UI_TEXT, label);
            return go;
        }

        static TMP_Text TMP(GameObject parent, string name, Rect rect, int size, Color color, string text)
        {
            var go = Child(parent, name);
            Fixed(go, new Vector2(rect.x, rect.y), new Vector2(rect.width, rect.height));
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize  = size;
            tmp.color     = color;
            tmp.text      = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
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
            Fixed(go, Vector2.zero, new Vector2(60, 60));
            Img(go, UI_CTA);
            return go;
        }

        static GameObject ToggleRow(GameObject parent, string rowName, Vector2 pos, string label)
        {
            var row = Child(parent, rowName); Fixed(row, pos, new Vector2(800, 70));
            TMP(row, "Label",  Center(-250, 0, 400, 60), 22, UI_TEXT, label);

            var tgo = Child(row, "Toggle"); Fixed(tgo, new Vector2(280, 0), new Vector2(80, 50));
            if (!tgo.TryGetComponent<Toggle>(out var toggle)) toggle = tgo.AddComponent<Toggle>();
            var bg  = Child(tgo, "Background"); Fixed(bg,   Vector2.zero, new Vector2(80, 50)); Img(bg, UI_BG_DEEP);
            var chk = Child(tgo, "Checkmark");  Fixed(chk,  Vector2.zero, new Vector2(40, 40)); Img(chk, UI_CTA);
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic       = chk.GetComponent<Image>();
            toggle.isOn          = true;
            return row;
        }

        static void Save(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static GameObject FindOrCreateSceneCanvas()
        {
            var existing = GameObject.Find("Canvas_Scene");
            if (existing != null) return existing;
            var go = new GameObject("Canvas_Scene");
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
            foreach (var path in new[] { PrefabRoot, PrefabCommon, PrefabBase, PrefabFinal })
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
