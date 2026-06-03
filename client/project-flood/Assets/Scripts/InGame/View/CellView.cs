using Game.InGame.Board;
using ProjectFlood.Contracts.GameTypes;
using UnityEngine;

namespace Game.InGame.View
{
    public class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _baseRenderer;
        [SerializeField] private SpriteRenderer _protectorOverlay;
        [SerializeField] private GameObject _coreIndicator;

        [SerializeField] private Sprite _basicSprite;
        [SerializeField] private Sprite _obstacleSprite;
        [SerializeField] private Sprite _protectorSprite1;
        [SerializeField] private Sprite _protectorSprite2;

        public void SetData(CellData? data, Color cellColor)
        {
            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            var cell = data.Value;

            _baseRenderer.sprite = cell.cell_type == CellType.Obstacle ? _obstacleSprite : _basicSprite;
            _baseRenderer.color = cell.cell_type == CellType.Obstacle ? Color.white : cellColor;

            if (_protectorOverlay != null)
            {
                bool hasProtector = cell.protector_strength > 0;
                _protectorOverlay.gameObject.SetActive(hasProtector);
                if (hasProtector)
                    _protectorOverlay.sprite = cell.protector_strength == 2 ? _protectorSprite2 : _protectorSprite1;
            }

            if (_coreIndicator != null)
                _coreIndicator.SetActive(cell.is_core);
        }
    }
}
