using System;
using System.Collections.Generic;
using UnityEngine;
#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif

namespace Game.Services
{
    public class AdMobService : MonoBehaviour, IAdService
    {
#if GOOGLE_MOBILE_ADS
#if UNITY_EDITOR || !UNITY_ANDROID
        static readonly Dictionary<string, string> RewardedAdUnitIds = new Dictionary<string, string>
        {
            ["STAMINA_LIFE"]              = "ca-app-pub-3940256099942544/5224354917",
            ["STAGE_REVIVE"]              = "ca-app-pub-3940256099942544/5224354917",
            ["DOUBLE_REWARD_STAGE_CLEAR"] = "ca-app-pub-3940256099942544/5224354917",
        };
        const string InterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
#else
        // TODO: Replace test IDs with production AdMob ad unit IDs from console before release
        static readonly Dictionary<string, string> RewardedAdUnitIds = new Dictionary<string, string>
        {
            ["STAMINA_LIFE"]              = "ca-app-pub-3940256099942544/5224354917",
            ["STAGE_REVIVE"]              = "ca-app-pub-3940256099942544/5224354917",
            ["DOUBLE_REWARD_STAGE_CLEAR"] = "ca-app-pub-3940256099942544/5224354917",
        };
        const string InterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
#endif

        private readonly Dictionary<string, RewardedAd> _rewardedAds = new Dictionary<string, RewardedAd>();
        private InterstitialAd _interstitialAd;
        private Action _pendingAction;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            MobileAds.Initialize(_ =>
            {
                foreach (var placementId in RewardedAdUnitIds.Keys)
                    LoadRewarded(placementId);
                LoadInterstitial();
            });
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var ad in _rewardedAds.Values)
                ad?.Destroy();
            _interstitialAd?.Destroy();
        }

        private void Update()
        {
            var action = _pendingAction;
            if (action == null) return;
            _pendingAction = null;
            action();
        }

        public void WatchRewardedAd(string placementId, Action<AdWatchResult?> onComplete)
        {
            if (!_rewardedAds.TryGetValue(placementId, out var ad) || ad == null || !ad.CanShowAd())
            {
                LoadRewarded(placementId);
                onComplete?.Invoke(null);
                return;
            }

            // SSV: nonce must be set before Show() so AdMob includes it in the server-side callback.
            var nonce = Guid.NewGuid().ToString("N");
            ad.SetServerSideVerificationOptions(
                new ServerSideVerificationOptions.Builder().SetCustomData(nonce).Build());

            bool earned    = false;
            bool completed = false;
            void Complete(AdWatchResult? result)
            {
                if (completed) return;
                completed = true;
                LoadRewarded(placementId);
                onComplete?.Invoke(result);
            }

            ad.OnAdFullScreenContentClosed += () =>
            {
                AdWatchResult? result = earned
                    ? new AdWatchResult { Earned = true, AdToken = nonce }
                    : (AdWatchResult?)null;
                _pendingAction = () => Complete(result);
            };
            ad.OnAdFullScreenContentFailed += _ =>
                _pendingAction = () => Complete(null);

            ad.Show(_ => earned = true);
        }

        public void ShowInterstitialIfEligible(int stageId, bool suppressByDoubleReward, Action<bool> onComplete)
        {
            if (suppressByDoubleReward || !IsInterstitialEligible())
            {
                onComplete?.Invoke(false);
                return;
            }
            ShowInterstitial(onComplete);
        }

        private bool IsInterstitialEligible()
        {
            var cache = AdEligibilityCache.Instance;
            return cache != null && cache.IsEligible("INTERSTITIAL_POST_STAGE");
        }

        private void ShowInterstitial(Action<bool> onComplete)
        {
            if (_interstitialAd == null || !_interstitialAd.CanShowAd())
            {
                LoadInterstitial();
                onComplete?.Invoke(false);
                return;
            }

            bool completed = false;
            void Complete(bool wasShown)
            {
                if (completed) return;
                completed = true;
                if (wasShown)
                    AdEligibilityCache.Instance?.OnInterstitialShown();
                LoadInterstitial();
                onComplete?.Invoke(wasShown);
            }

            _interstitialAd.OnAdFullScreenContentClosed += () => _pendingAction = () => Complete(true);
            _interstitialAd.OnAdFullScreenContentFailed += _ => _pendingAction = () => Complete(false);
            _interstitialAd.Show();
        }

        private void LoadRewarded(string placementId)
        {
            if (_rewardedAds.TryGetValue(placementId, out var existing))
                existing?.Destroy();
            _rewardedAds[placementId] = null;

            if (!RewardedAdUnitIds.TryGetValue(placementId, out var adUnitId)) return;

            RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null) return;
                _rewardedAds[placementId] = ad;
            });
        }

        private void LoadInterstitial()
        {
            _interstitialAd?.Destroy();
            _interstitialAd = null;
            InterstitialAd.Load(InterstitialAdUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null) return;
                _interstitialAd = ad;
            });
        }

#else
        // Dev stub — used when Google Mobile Ads SDK is not installed.
        // Reward success must not be mocked on the client.
        // ShowInterstitialIfEligible skips the ad and calls onComplete(false).

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void WatchRewardedAd(string placementId, Action<AdWatchResult?> onComplete)
        {
            onComplete?.Invoke(null);
        }

        public void ShowInterstitialIfEligible(int stageId, bool suppressByDoubleReward, Action<bool> onComplete)
        {
            onComplete?.Invoke(false);
        }
#endif

        public static AdMobService Instance { get; private set; }
    }
}
