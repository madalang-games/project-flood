using Game.Services;
using Game.Utils;
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
        [SerializeField] private VirtualizedScrollRect _virtualizedScrollRect;

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
                _titleText.text = _rankingType == "stars" ? LocalizationService.Instance.Get("lobby.ranking.stars_title") : LocalizationService.Instance.Get("lobby.ranking.stage_title");
            if (_myRankText != null)
                _myRankText.text = LocalizationService.Instance.Get("lobby.ranking.my_rank_empty");
            if (_entriesText != null)
                _entriesText.text = LocalizationService.Instance.Get("lobby.ranking.loading");

            api.FetchGlobalPage(_rankingType, 0, PageLimit, page =>
            {
                if (page.Entries.Count == 0)
                {
                    if (_entriesText != null) _entriesText.text = LocalizationService.Instance.Get("lobby.ranking.no_data");
                    if (_virtualizedScrollRect != null) _virtualizedScrollRect.gameObject.SetActive(false);
                    return;
                }

                if (_virtualizedScrollRect != null)
                {
                    if (_entriesText != null) _entriesText.gameObject.SetActive(false);
                    _virtualizedScrollRect.gameObject.SetActive(true);

                    var entryList = page.Entries;
                    _virtualizedScrollRect.Init(entryList.Count, (idx, go) =>
                    {
                        if (idx < 0 || idx >= entryList.Count) return;
                        var entry = entryList[idx];

                        var rankText = go.transform.Find("RankText")?.GetComponent<TMP_Text>();
                        var nameText = go.transform.Find("NameText")?.GetComponent<TMP_Text>();
                        var scoreText = go.transform.Find("ScoreText")?.GetComponent<TMP_Text>();

                        if (rankText != null) rankText.text = $"#{entry.Rank}";
                        if (nameText != null) nameText.text = entry.DisplayName;
                        if (scoreText != null) scoreText.text = entry.Score.ToString();
                    });
                }
                else
                {
                    if (_entriesText != null)
                    {
                        _entriesText.gameObject.SetActive(true);
                        var lines = new System.Text.StringBuilder();
                        foreach (var entry in page.Entries)
                            lines.Append('#').Append(entry.Rank).Append("  ")
                                 .Append(entry.DisplayName).Append("  ")
                                 .Append(entry.Score).AppendLine();
                        _entriesText.text = lines.ToString();
                    }
                }
            }, _ => SetUnavailable());

            api.FetchMyGlobalRank(_rankingType, mine =>
            {
                if (_myRankText == null) return;
                _myRankText.text = mine.Entry == null
                    ? LocalizationService.Instance.Get("lobby.ranking.my_rank_empty")
                    : string.Format(LocalizationService.Instance.Get("lobby.ranking.my_rank_format"), mine.Entry.Rank, mine.Entry.Score);
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
            if (_titleText != null) _titleText.text = LocalizationService.Instance.Get("lobby.ranking.default_title");
            if (_myRankText != null) _myRankText.text = LocalizationService.Instance.Get("lobby.ranking.my_rank_empty");
            if (_entriesText != null) _entriesText.text = LocalizationService.Instance.Get("lobby.ranking.unavailable");
            if (_virtualizedScrollRect != null) _virtualizedScrollRect.gameObject.SetActive(false);
        }
    }
}
