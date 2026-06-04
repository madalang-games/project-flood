using System;
using System.Collections.Generic;
using Game.InGame.Board;
using ProjectFlood.Data.Generated;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.InGame.Controller
{
    public class InGameSceneEntry : MonoBehaviour
    {
        [SerializeField] private InGameController _controller;
        [SerializeField] private int _debugStageId = 1;

#if UNITY_EDITOR
        private static int? _overrideStageId;
        private static bool _reloadQueued;
        private static bool _isFirstLoad = true;

        [UnityEditor.InitializeOnLoadMethod]
        static void Init()
        {
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingEditMode)
                    _isFirstLoad = true;
            };
        }

        private void OnValidate()
        {
            if (!Application.isPlaying || _reloadQueued || _isFirstLoad) return;
            _reloadQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                _reloadQueued = false;
                if (!Application.isPlaying) return;
                _isFirstLoad = true;
                _overrideStageId = _debugStageId;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            };
        }
#endif

        private void Start()
        {
#if UNITY_EDITOR
            _isFirstLoad = false;
            var stageId = _overrideStageId ?? _debugStageId;
            _overrideStageId = null;
#else
            var stageId = _debugStageId;
#endif
            var stage = LoadStage(stageId);
            if (stage == null)
            {
                Debug.LogError($"[InGameSceneEntry] stage_id={stageId} not found");
                return;
            }

            _controller.OnStageEnd += (result, turns) =>
                Debug.Log($"[InGame] End: {result}  turns_left={turns}");
            _controller.OnTurnConsumed += turns =>
                Debug.Log($"[InGame] Turns left: {turns}");

            _controller.Init(stage);
        }

        private static Stage LoadStage(int stageId)
        {
            var asset = Resources.Load<TextAsset>(Stage.ResourcePath);
            if (asset == null)
            {
                Debug.LogError($"[InGameSceneEntry] CSV not found: {Stage.ResourcePath}");
                return null;
            }

            var lines = asset.text.Split('\n');
            if (lines.Length < 2) return null;

            var headers = SplitCsvLine(lines[0]);

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                var cols = SplitCsvLine(line);
                if (cols.Length == 0) continue;
                if (!int.TryParse(cols[0], out int id) || id != stageId) continue;

                return ParseRow(headers, cols);
            }
            return null;
        }

        private static Stage ParseRow(string[] headers, string[] cols)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < headers.Length && i < cols.Length; i++)
                map[headers[i]] = cols[i];

            return new Stage
            {
                stage_id          = int.Parse(map["stage_id"]),
                board_width       = sbyte.Parse(map["board_width"]),
                board_height      = sbyte.Parse(map["board_height"]),
                turn_limit        = sbyte.Parse(map["turn_limit"]),
                color_ids         = map["color_ids"],
                star1_ratio       = float.Parse(map["star1_ratio"], System.Globalization.CultureInfo.InvariantCulture),
                star2_ratio       = float.Parse(map["star2_ratio"], System.Globalization.CultureInfo.InvariantCulture),
                cells             = map["cells"],
                ruleset_version   = sbyte.Parse(map["ruleset_version"]),
            };
        }

        private static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            int i = 0;
            while (i < line.Length)
            {
                if (line[i] == '"')
                {
                    i++;
                    int start = i;
                    while (i < line.Length && line[i] != '"') i++;
                    result.Add(line.Substring(start, i - start));
                    if (i < line.Length) i++; // closing "
                    if (i < line.Length && line[i] == ',') i++;
                }
                else
                {
                    int start = i;
                    while (i < line.Length && line[i] != ',') i++;
                    result.Add(line.Substring(start, i - start));
                    if (i < line.Length) i++; // comma
                }
            }
            return result.ToArray();
        }
    }
}
