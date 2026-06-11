using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    public class StageNodeView : MonoBehaviour
    {
        [SerializeField] private TMP_Text       _stageLabel;
        [SerializeField] private GameObject[]   _starFills;    // 3 star fill images
        [SerializeField] private GameObject     _lockOverlay;
        [SerializeField] private GameObject     _pulseRing;    // UIScalePulse on current stage
        [SerializeField] private Button         _button;
        [SerializeField] private Image          _border;

        private static readonly Dictionary<int, Sprite> _chapterSpriteCache = new();

        public event Action<int> OnTapped;

        private int _stageId;

        private void Awake()
        {
            _button.onClick.AddListener(() => OnTapped?.Invoke(_stageId));
        }

        public void Bind(int stageId, int stars, bool unlocked, bool isCurrent, int chapterId = 0)
        {
            _stageId = stageId;
            if (_stageLabel != null) _stageLabel.text = stageId.ToString();

            bool locked = !unlocked;
            if (_lockOverlay != null) _lockOverlay.SetActive(locked);
            _button.interactable = !locked;

            for (int i = 0; i < _starFills.Length; i++)
                if (_starFills[i] != null) _starFills[i].SetActive(i < stars);

            if (_pulseRing != null) _pulseRing.SetActive(isCurrent && !locked);

            if (_border != null && chapterId > 0)
            {
                if (!_chapterSpriteCache.TryGetValue(chapterId, out var spr))
                {
                    spr = Resources.Load<Sprite>($"Sprites/StageNodes/stage_node_ch_{chapterId}");
                    _chapterSpriteCache[chapterId] = spr;
                }
                if (spr != null) _border.sprite = spr;
            }
        }
    }
}
