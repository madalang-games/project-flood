using Game.Services;
using UnityEngine;

namespace Game.OutGame.Lobby
{
    public class LobbyView : MonoBehaviour
    {
        [SerializeField] private HeaderView      _header;
        [SerializeField] private BottomNavBarView _navBar;
        [SerializeField] private GameObject      _homeTabRoot;
        [SerializeField] private GameObject      _shopTabRoot;
        [SerializeField] private GameObject      _rankingTabRoot;
        [SerializeField] private RankingTabView  _rankingTabView;

        private void Awake()
        {
            _navBar.OnTabChanged += ShowTab;
        }

        private void Start()
        {
            RefreshGold();
            ShowTab(LobbyTab.Home);
            FetchServerState();
        }

        private void RefreshGold()
        {
            int gold = PlayerProgressService.Instance?.Gold ?? 0;
            _header?.SetGold(gold);
        }

        private void FetchServerState()
        {
            StaminaApiService.Instance?.FetchStamina();
            CurrencyApiService.Instance?.FetchGold(snap => _header?.SetGold((int)snap.SoftAmount));
            InventoryApiService.Instance?.FetchInventory();
        }

        private void ShowTab(LobbyTab tab)
        {
            _homeTabRoot?.SetActive(tab == LobbyTab.Home);
            _shopTabRoot?.SetActive(tab == LobbyTab.Shop);
            _rankingTabRoot?.SetActive(tab == LobbyTab.Ranking);
            if (tab == LobbyTab.Ranking)
                _rankingTabView?.Refresh();
        }
    }
}
