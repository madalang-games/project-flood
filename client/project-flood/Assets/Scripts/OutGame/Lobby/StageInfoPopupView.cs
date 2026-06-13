using System;
using Game.Core;
using Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    public class StageInfoPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text   _stageTitle;
        [SerializeField] private TMP_Text   _bestRecord;
        [SerializeField] private Button     _playButton;
        [SerializeField] private Button     _backdropButton;
        [SerializeField] private GameObject[] _bestStarFills;
        [SerializeField] private Toggle     _extraTurnsToggle;
        [SerializeField] private Image      _extraTurnsStateIcon;
        [SerializeField] private Sprite     _toggleOnSprite;
        [SerializeField] private Sprite     _toggleOffSprite;
        [SerializeField] private Image      _ribbonImage;
        [SerializeField] private TMP_Text    _itemCountText;
        [SerializeField] private CanvasGroup _itemContainerGroup;

        private int    _stageId;
        private Action _onPlay;
        private Color  _defaultRibbonColor;

        private void Awake()
        {
            _playButton.onClick.AddListener(OnPlay);
            if (_backdropButton != null) _backdropButton.onClick.AddListener(OnClose);
            if (_ribbonImage != null) _defaultRibbonColor = _ribbonImage.color;
        }

        public void Init(int stageId, int bestStars, int bestMoves, Action onPlay, int difficulty = 0, bool isLocked = false)
        {
            _stageId   = stageId;
            _onPlay    = onPlay;
            _playButton.interactable = !isLocked;

            if (_ribbonImage != null)
                _ribbonImage.color = DifficultyStyle.Get(difficulty, _defaultRibbonColor);

            if (_stageTitle != null)
                _stageTitle.text = string.Format(Game.Services.LocalizationService.Instance.Get("popup.stage_info.title"), stageId);
            if (_bestRecord != null)
            {
                if (bestStars > 0)
                    _bestRecord.text = string.Format(Game.Services.LocalizationService.Instance.Get("popup.stage_info.best_stars"), bestStars);
                else
                    _bestRecord.text = Game.Services.LocalizationService.Instance.Get("popup.stage_info.no_record");
            }

            for (int i = 0; i < _bestStarFills.Length; i++)
                if (_bestStarFills[i] != null) _bestStarFills[i].SetActive(i < bestStars);

            int addTurnsCount = Game.Services.PlayerProgressService.Instance != null ? Game.Services.PlayerProgressService.Instance.GetItemCount(1) : 0;

            if (_itemCountText != null)
                _itemCountText.text = string.Format(Game.Services.LocalizationService.Instance.Get("popup.stage_info.item_count_fmt"), addTurnsCount);
            if (_itemContainerGroup != null)
                _itemContainerGroup.alpha = addTurnsCount > 0 ? 1f : 0.4f;

            if (_extraTurnsToggle != null)
            {
                _extraTurnsToggle.interactable = addTurnsCount > 0;
                _extraTurnsToggle.isOn = false;
                _extraTurnsToggle.onValueChanged.RemoveAllListeners();
                _extraTurnsToggle.onValueChanged.AddListener(OnExtraTurnsToggled);
                OnExtraTurnsToggled(false);
            }
        }

        private void OnPlay()
        {
            bool useBooster = _extraTurnsToggle != null && _extraTurnsToggle.isOn;

            if (useBooster)
            {
                int current = Game.Services.PlayerProgressService.Instance?.GetItemCount(1) ?? 0;
                if (current <= 0)
                {
                    if (_extraTurnsToggle != null) _extraTurnsToggle.isOn = false;
                    useBooster = false;
                }
                else
                {
                    var inventoryApi = Game.Services.InventoryApiService.Instance;
                    if (inventoryApi == null)
                    {
                        Game.Core.UIManager.Instance?.ShowToast(
                            LocalizationService.Instance.Get("toast.item_spend_failed"),
                            Game.Core.UI.ToastType.Warning);
                        return;
                    }

                    Game.Core.UIManager.Instance?.ShowLoading();
                    inventoryApi.SpendItem(1, 1, "use_pre_game",
                        onSuccess: _ =>
                        {
                            Game.Core.UIManager.Instance?.HideLoading();
                            ScrollStateCache.UseExtraTurnsItem  = true;
                            ScrollStateCache.UseStartingBomb    = false;
                            ScrollStateCache.UseStartingHRocket = false;
                            ScrollStateCache.LastPlayedStageId  = _stageId;
                            ProceedToPlay();
                        },
                        onError: err =>
                        {
                            Game.Core.UIManager.Instance?.HideLoading();
                            Game.Core.UIManager.Instance?.ShowToast(
                                LocalizationService.Instance.Get("toast.item_spend_failed"),
                                Game.Core.UI.ToastType.Warning);
                        });
                    return;
                }
            }

            ScrollStateCache.UseExtraTurnsItem  = false;
            ScrollStateCache.UseStartingBomb    = false;
            ScrollStateCache.UseStartingHRocket = false;
            ScrollStateCache.LastPlayedStageId  = _stageId;
            ProceedToPlay();
        }

        private void OnExtraTurnsToggled(bool isOn)
        {
            if (_extraTurnsStateIcon == null) return;
            var spr = isOn ? _toggleOnSprite : _toggleOffSprite;
            _extraTurnsStateIcon.sprite  = spr;
            _extraTurnsStateIcon.enabled = spr != null;
        }

        private void ProceedToPlay()
        {
            var staminaApi = Game.Services.StaminaApiService.Instance;
            bool canPlay = false;
            if (staminaApi != null)
            {
                if (staminaApi.LatestStamina != null && staminaApi.LatestStamina.IsUnlimited && staminaApi.GetSecondsOfUnlimitedRemaining() > 0)
                    canPlay = true;
                else if (staminaApi.GetEstimatedLife() > 0)
                    canPlay = true;
            }
            else
            {
                canPlay = true;
            }

            if (!canPlay)
            {
                ShowStaminaAdPopup();
                return;
            }

            _onPlay?.Invoke();
            Close();
        }

        private void ShowStaminaAdPopup()
        {
            Game.Core.UIManager.Instance?.ShowPopup<Game.Core.UI.ConfirmDialogView>(v => v.Init(
                title: LocalizationService.Instance.Get("popup.stamina.out_of_lives"),
                body: LocalizationService.Instance.Get("popup.stamina.watch_ad_body"),
                confirmLabel: LocalizationService.Instance.Get("popup.fail.watch_ad"),
                onConfirm: () =>
                {
                    var staminaApi = Game.Services.StaminaApiService.Instance;
                    var adMob = Game.Services.AdMobService.Instance;
                    if (staminaApi == null || adMob == null)
                    {
                        Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("error.ad_failed"), Game.Core.UI.ToastType.Warning);
                        return;
                    }

                    Game.Core.UIManager.Instance?.ShowLoading();
                    adMob.WatchRewardedAd("STAMINA_LIFE", result =>
                    {
                        if (!result.HasValue || !result.Value.Earned)
                        {
                            Game.Core.UIManager.Instance?.HideLoading();
                            Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("error.ad_failed"), Game.Core.UI.ToastType.Warning);
                            return;
                        }

                        staminaApi.ClaimAdLife("admob", result.Value.AdToken,
                            onSuccess: resp =>
                            {
                                Game.Core.UIManager.Instance?.HideLoading();
                                Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.life_gained"), Game.Core.UI.ToastType.Success);
                            },
                            onError: err =>
                            {
                                Game.Core.UIManager.Instance?.HideLoading();
                                Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("error.ad_failed"), Game.Core.UI.ToastType.Warning);
                            }
                        );
                    });
                },
                onCancel: null,
                cancelLabel: LocalizationService.Instance.Get("common.btn_cancel")
            ));
        }

        private void OnClose() => Close();

        private void Close()
        {
            var appear = GetComponent<Core.UI.UIPanelAppear>();
            if (appear != null)
                appear.Disappear(() => Game.Core.UIManager.Instance?.CloseTopPopup());
            else
                Game.Core.UIManager.Instance?.CloseTopPopup();
        }
    }
}
