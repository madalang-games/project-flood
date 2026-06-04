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

        private void Awake()
        {
            _navBar.OnTabChanged += ShowTab;
        }

        private void Start()
        {
            RefreshGold();
            ShowTab(LobbyTab.Home);
        }

        private void RefreshGold()
        {
            int gold = PlayerProgressService.Instance?.Gold ?? 0;
            _header?.SetGold(gold);
        }

        private void ShowTab(LobbyTab tab)
        {
            _homeTabRoot?.SetActive(tab == LobbyTab.Home);
            _shopTabRoot?.SetActive(tab == LobbyTab.Shop);
        }
    }
}
