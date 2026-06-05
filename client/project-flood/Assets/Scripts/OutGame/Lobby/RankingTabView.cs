using Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    public class RankingTabView : MonoBehaviour
    {
        [SerializeField] private Button _starsTabButton;
        [SerializeField] private Button _maxStageTabButton;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _myRankText;
        [SerializeField] private TMP_Text _entriesText;

        private const int PageLimit = 50;
        private string _rankingType = "stars";

        private void Awake()
        {
            if (_starsTabButton != null)
                _starsTabButton.onClick.AddListener(() => Select("stars"));
            if (_maxStageTabButton != null)
                _maxStageTabButton.onClick.AddListener(() => Select("max-stage"));
        }

        private void OnEnable() => Refresh();

        public void Refresh()
        {
            var api = RankingApiService.Instance;
            if (api == null)
            {
                SetUnavailable();
                return;
            }

            if (_titleText != null)
                _titleText.text = _rankingType == "stars" ? "Star Ranking" : "Max Stage Ranking";
            if (_myRankText != null)
                _myRankText.text = "My Rank: -";
            if (_entriesText != null)
                _entriesText.text = "Loading...";

            api.FetchGlobalPage(_rankingType, 0, PageLimit, page =>
            {
                if (_entriesText == null) return;
                if (page.Entries.Count == 0)
                {
                    _entriesText.text = "No ranking data";
                    return;
                }

                var lines = new System.Text.StringBuilder();
                foreach (var entry in page.Entries)
                    lines.Append('#').Append(entry.Rank).Append("  ")
                         .Append(entry.DisplayName).Append("  ")
                         .Append(entry.Score).AppendLine();
                _entriesText.text = lines.ToString();
            }, _ => SetUnavailable());

            api.FetchMyGlobalRank(_rankingType, mine =>
            {
                if (_myRankText == null) return;
                _myRankText.text = mine.Entry == null
                    ? "My Rank: -"
                    : $"My Rank: #{mine.Entry.Rank}  {mine.Entry.Score}";
            });
        }

        private void Select(string rankingType)
        {
            if (_rankingType == rankingType)
                return;

            _rankingType = rankingType;
            Refresh();
        }

        private void SetUnavailable()
        {
            if (_titleText != null) _titleText.text = "Ranking";
            if (_myRankText != null) _myRankText.text = "My Rank: -";
            if (_entriesText != null) _entriesText.text = "Ranking unavailable";
        }
    }
}
