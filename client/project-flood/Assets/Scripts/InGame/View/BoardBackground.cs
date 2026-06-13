using System.Collections.Generic;
using UnityEngine;

namespace Game.InGame.View
{
    public class BoardBackground : MonoBehaviour
    {
        [System.Serializable]
        public struct ThemeVisualConfig
        {
            public int themeId;
            public string themeName;
            public Color boardColor;
            public Color socketColor;
            public Color borderHighlightColor;
            public Color borderShadowColor;
            public Color neonCyan;
            public Color neonPink;
            public Sprite customBorderSprite;
            public Sprite customSocketSprite;
            public bool animateTexture;
            public float borderPaddingFactor;
        }

        [SerializeField] private List<ThemeVisualConfig> _themes = new List<ThemeVisualConfig>();
        private ThemeVisualConfig _activeTheme;
        private float _currentPaddingFactor = 0.28f;

        [SerializeField] private Color _boardColor = new Color(0.055f, 0.071f, 0.118f);
        [SerializeField] private Color _socketColor = new Color(0.118f, 0.137f, 0.212f);
        [SerializeField] private Color _socketHighlight = new Color(0.306f, 0.925f, 1f);
        [SerializeField] private Color _socketShadow = new Color(0.318f, 0.122f, 0.612f);
        [SerializeField] private Color _neonCyan = new Color(0.15f, 0.95f, 1f);
        [SerializeField] private Color _neonPink = new Color(1f, 0.22f, 0.78f);
        [SerializeField] private bool _animateTexture = true;
        [SerializeField] private float _effectFps = 12f;
        [SerializeField] private Sprite[] _socketSprites;
        [SerializeField] private Sprite _defaultSocketSprite;

        private const int SocketTexSize = 16;
        private const int SortingOrderPanel = -10;
        private const int SortingOrderSocket = -5;

        private readonly List<GameObject> _runtimeObjects = new();

        private SpriteRenderer _panel;
        private SpriteRenderer[,] _sockets;
        private Texture2D _panelTexture;
        private Color[] _panelPixels;
        private bool[,] _holes;
        private int _boardWidth;
        private int _boardHeight;
        private int _panelTexWidth;
        private int _panelTexHeight;
        private int _frame;
        private float _nextFrameTime;

        private void InitializeDefaultThemes()
        {
            if (_themes != null && _themes.Count > 0) return;

            _themes = new List<ThemeVisualConfig>
            {
                new ThemeVisualConfig
                {
                    themeId = 1,
                    themeName = "Classic",
                    boardColor = new Color(0.055f, 0.071f, 0.118f),
                    socketColor = new Color(0.118f, 0.137f, 0.212f),
                    borderHighlightColor = new Color(0.15f, 0.95f, 1f),
                    borderShadowColor = new Color(0.318f, 0.122f, 0.612f),
                    neonCyan = new Color(0.15f, 0.95f, 1f),
                    neonPink = new Color(1f, 0.22f, 0.78f),
                    animateTexture = false,
                    borderPaddingFactor = 0.70f
                },
                new ThemeVisualConfig
                {
                    themeId = 2,
                    themeName = "Neon",
                    boardColor = new Color(0.055f, 0.071f, 0.118f),
                    socketColor = new Color(0.118f, 0.137f, 0.212f),
                    borderHighlightColor = new Color(0.15f, 0.95f, 1f),
                    borderShadowColor = new Color(0.318f, 0.122f, 0.612f),
                    neonCyan = new Color(0.15f, 0.95f, 1f),
                    neonPink = new Color(1f, 0.22f, 0.78f),
                    animateTexture = true,
                    borderPaddingFactor = 0.65f
                },
                new ThemeVisualConfig
                {
                    themeId = 3,
                    themeName = "Wood",
                    boardColor = new Color(0.25f, 0.15f, 0.08f),
                    socketColor = new Color(0.35f, 0.22f, 0.12f),
                    borderHighlightColor = new Color(0.5f, 0.35f, 0.2f),
                    borderShadowColor = new Color(0.15f, 0.08f, 0.04f),
                    neonCyan = new Color(0.5f, 0.35f, 0.2f),
                    neonPink = new Color(0.5f, 0.35f, 0.2f),
                    animateTexture = false,
                    borderPaddingFactor = 0.75f
                },
                new ThemeVisualConfig
                {
                    themeId = 4,
                    themeName = "Cyberpunk",
                    boardColor = new Color(0.03f, 0.03f, 0.05f),
                    socketColor = new Color(0.08f, 0.08f, 0.15f),
                    borderHighlightColor = new Color(1f, 0.78f, 0f),
                    borderShadowColor = new Color(1f, 0f, 0.3f),
                    neonCyan = new Color(1f, 0.78f, 0f),
                    neonPink = new Color(1f, 0f, 0.3f),
                    animateTexture = true,
                    borderPaddingFactor = 0.70f
                }
            };
        }

        public float PaddingFactor => _currentPaddingFactor;

        public void SetTheme(int themeId)
        {
            InitializeDefaultThemes();
            _activeTheme = _themes[0];
            foreach (var theme in _themes)
            {
                if (theme.themeId == themeId)
                {
                    _activeTheme = theme;
                    break;
                }
            }

            _boardColor = _activeTheme.boardColor;
            _socketColor = _activeTheme.socketColor;
            _socketHighlight = _activeTheme.borderHighlightColor;
            _socketShadow = _activeTheme.borderShadowColor;
            _neonCyan = _activeTheme.neonCyan;
            _neonPink = _activeTheme.neonPink;
            _animateTexture = _activeTheme.animateTexture;
            _currentPaddingFactor = _activeTheme.themeId > 0 ? _activeTheme.borderPaddingFactor : 0.65f;

            if (_activeTheme.customSocketSprite != null)
            {
                _defaultSocketSprite = _activeTheme.customSocketSprite;
            }
        }

        public void Build(int width, int height, float cellSize, Vector3[,] cellPositions, int[,] initialColorIds)
        {
            if (_activeTheme.themeId == 0)
            {
                SetTheme(1);
            }
            ClearRuntimeObjects();
            _boardWidth = width;
            _boardHeight = height;
            _holes = new bool[height, width];
            BuildPanel(width, height, cellSize);
            BuildSockets(width, height, cellSize, cellPositions, initialColorIds);
            if (_panelTexture != null)
            {
                RenderPanelFrame(0);
            }
        }

        public void Refresh(int width, int height, bool[,] showSocket, bool[,] showHole)
        {
            if (_sockets == null) return;

            for (int r = 0; r < height; r++)
            for (int c = 0; c < width; c++)
            {
                _sockets[r, c].enabled = showSocket[r, c] && !showHole[r, c];
                _holes[r, c] = showHole[r, c];
            }

            if (_panelTexture != null)
            {
                RenderPanelFrame(_frame);
            }
        }

        private void Update()
        {
            if (!_animateTexture || _panelTexture == null) return;
            if (Time.unscaledTime < _nextFrameTime) return;

            _nextFrameTime = Time.unscaledTime + 1f / Mathf.Max(1f, _effectFps);
            RenderPanelFrame(++_frame);
            PulseSockets();
        }

        private void BuildPanel(int width, int height, float cellSize)
        {
            _currentPaddingFactor = _activeTheme.themeId > 0 ? _activeTheme.borderPaddingFactor : 0.65f;
            float panelWidth = width * cellSize + cellSize * _currentPaddingFactor;
            float panelHeight = height * cellSize + cellSize * _currentPaddingFactor;

            var go = CreateRuntimeObject("BoardPanel");
            go.transform.localPosition = Vector3.zero;

            _panel = go.AddComponent<SpriteRenderer>();
            _panel.sortingOrder = SortingOrderPanel;

            if (_activeTheme.customBorderSprite != null)
            {
                _panel.sprite = _activeTheme.customBorderSprite;
                _panel.drawMode = SpriteDrawMode.Sliced;
                _panel.size = new Vector2(panelWidth, panelHeight);
                _panel.color = Color.white;
                _panelTexture = null;
                _panelPixels = null;
            }
            else
            {
                _panelTexWidth = width * 32;
                _panelTexHeight = height * 32;
                _panelPixels = new Color[_panelTexWidth * _panelTexHeight];
                _panelTexture = new Texture2D(_panelTexWidth, _panelTexHeight, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };

                var border = new Vector4(4f, 4f, 4f, 4f);
                var sprite = Sprite.Create(
                    _panelTexture,
                    new Rect(0, 0, _panelTexWidth, _panelTexHeight),
                    new Vector2(0.5f, 0.5f),
                    16f,
                    0,
                    SpriteMeshType.FullRect,
                    border);

                _panel.sprite = sprite;
                _panel.drawMode = SpriteDrawMode.Sliced;
                _panel.size = new Vector2(panelWidth, panelHeight);
            }
        }

        private void BuildSockets(int width, int height, float cellSize, Vector3[,] cellPositions, int[,] initialColorIds)
        {
            _sockets = new SpriteRenderer[height, width];
            var socketSprite = _defaultSocketSprite != null ? _defaultSocketSprite : CreateSocketSprite(cellSize);

            for (int r = 0; r < height; r++)
            for (int c = 0; c < width; c++)
            {
                var go = CreateRuntimeObject($"Socket_{r}_{c}");
                go.transform.localPosition = cellPositions[r, c];

                var sr = go.AddComponent<SpriteRenderer>();
                sr.color = new Color(1f, 1f, 1f, 0.35f); // Subtle alpha to keep sockets background-like and less intrusive
                int colorId = (initialColorIds != null && r >= 0 && r < initialColorIds.GetLength(0) && c >= 0 && c < initialColorIds.GetLength(1)) ? initialColorIds[r, c] : -1;
                Sprite targetSprite = null;
                if (_socketSprites != null && colorId >= 0 && colorId < _socketSprites.Length && _socketSprites[colorId] != null)
                {
                    targetSprite = _socketSprites[colorId];
                }
                else
                {
                    targetSprite = socketSprite;
                }
                
                sr.sprite = targetSprite;
                sr.sortingOrder = SortingOrderSocket;
                _sockets[r, c] = sr;

                if (targetSprite != null)
                {
                    Vector2 spriteSize = targetSprite.bounds.size;
                    float targetSize = (targetSprite == socketSprite) ? cellSize * 0.88f : cellSize;
                    float sx = spriteSize.x > 0f ? targetSize / spriteSize.x : targetSize;
                    float sy = spriteSize.y > 0f ? targetSize / spriteSize.y : targetSize;
                    go.transform.localScale = new Vector3(sx, sy, 1f);
                }
                else
                {
                    go.transform.localScale = Vector3.one * cellSize;
                }
            }
        }

        private Sprite CreateSocketSprite(float cellSize)
        {
            int s = SocketTexSize;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color[s * s];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                if (x == 0 || x == s - 1 || y == 0 || y == s - 1) continue;
                if ((x == 1 && y == 1) || (x == 1 && y == s - 2) ||
                    (x == s - 2 && y == 1) || (x == s - 2 && y == s - 2)) continue;

                Color color = _socketColor;
                if (y == s - 2 || x == 1)
                    color = Color.Lerp(_socketColor, _socketHighlight, 0.5f);
                else if (y == 1 || x == s - 2)
                    color = Color.Lerp(_socketColor, _socketShadow, 0.55f);

                if ((x == 2 && y == s - 3) || (x == s - 3 && y == 2))
                    color = _socketHighlight;

                pixels[y * s + x] = color;
            }

            tex.SetPixels(pixels);
            tex.Apply();

            float ppu = s / (cellSize * 0.88f);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), ppu);
        }

        private void RenderPanelFrame(int frame)
        {
            if (_panelTexture == null || _panelPixels == null) return;
            float pulse = 0.5f + Mathf.Sin(frame * 0.42f) * 0.5f;
            Color borderColor = Color.Lerp(_neonCyan, _neonPink, pulse);
            Color gridColor = Color.Lerp(_boardColor, _neonCyan, 0.18f);
            Color scanColor = Color.Lerp(_boardColor, Color.black, 0.18f);

            for (int y = 0; y < _panelTexHeight; y++)
            for (int x = 0; x < _panelTexWidth; x++)
            {
                if (TryGetHolePixel(x, y, out var holeColor))
                {
                    _panelPixels[y * _panelTexWidth + x] = holeColor;
                    continue;
                }

                Color color = _boardColor;
                bool voidEdge = TryGetVoidEdgePixel(x, y, out var voidEdgeColor);

                bool border = x < 2 || y < 2 || x >= _panelTexWidth - 2 || y >= _panelTexHeight - 2;
                bool innerGrid = x % 12 == 0 || y % 12 == 0;
                bool scan = (y + frame) % 9 == 0;
                bool trace = (x + y + frame * 2) % 37 == 0;
                bool sparkle = Hash(x, y, frame / 2) % 251 == 0;

                if (scan)
                    color = scanColor;
                if (innerGrid)
                    color = Color.Lerp(color, gridColor, 0.35f);
                if (trace)
                    color = Color.Lerp(color, _neonPink, 0.38f);
                if (sparkle)
                    color = Color.Lerp(color, Color.white, 0.85f);
                if (border)
                    color = Color.Lerp(borderColor, Color.white, x == 0 || y == 0 ? 0.18f : 0f);
                if (voidEdge)
                    color = voidEdgeColor;

                if (TryGetInnerBorderColor(x, y, borderColor, out var innerBorderColor))
                {
                    color = innerBorderColor;
                }

                _panelPixels[y * _panelTexWidth + x] = color;
            }

            _panelTexture.SetPixels(_panelPixels);
            _panelTexture.Apply(false);
        }

        private bool TryGetInnerBorderColor(int x, int y, Color baseBorderColor, out Color color)
        {
            color = Color.clear;
            if (_holes == null || _boardWidth <= 0 || _boardHeight <= 0) return false;

            float panelPaddingCells = _currentPaddingFactor / 2f;
            float boardX = ((x + 0.5f) / _panelTexWidth) * (_boardWidth + panelPaddingCells * 2f) - panelPaddingCells;
            float boardYFromBottom = ((y + 0.5f) / _panelTexHeight) * (_boardHeight + panelPaddingCells * 2f) - panelPaddingCells;
            float boardY = _boardHeight - boardYFromBottom;

            int col = Mathf.FloorToInt(boardX);
            int row = Mathf.FloorToInt(boardY);
            if (row < 0 || row >= _boardHeight || col < 0 || col >= _boardWidth) return false;

            // Inner border outline is only drawn inside void cells
            if (!_holes[row, col]) return false;

            float margin = 0.15f;
            float localX = boardX - col;
            float localY = boardY - row;

            float pixelX = 1f / _panelTexWidth * (_boardWidth + panelPaddingCells * 2f);
            float pixelY = 1f / _panelTexHeight * (_boardHeight + panelPaddingCells * 2f);
            float borderWidth = 2f;

            bool hasSolidTop = row == 0 || !_holes[row - 1, col];
            bool hasSolidBottom = row == _boardHeight - 1 || !_holes[row + 1, col];
            bool hasSolidLeft = col == 0 || !_holes[row, col - 1];
            bool hasSolidRight = col == _boardWidth - 1 || !_holes[row, col + 1];

            bool isBorder = false;

            if (hasSolidTop && localY >= margin - borderWidth * pixelY && localY <= margin)
                isBorder = true;
            else if (hasSolidBottom && localY >= 1 - margin && localY <= 1 - margin + borderWidth * pixelY)
                isBorder = true;
            else if (hasSolidLeft && localX >= margin - borderWidth * pixelX && localX <= margin)
                isBorder = true;
            else if (hasSolidRight && localX >= 1 - margin && localX <= 1 - margin + borderWidth * pixelX)
                isBorder = true;

            if (isBorder)
            {
                color = baseBorderColor;
                return true;
            }

            return false;
        }

        private bool TryGetHolePixel(int x, int y, out Color color)
        {
            color = Color.clear;
            if (_holes == null || _boardWidth <= 0 || _boardHeight <= 0) return false;

            float panelPaddingCells = _currentPaddingFactor / 2f;
            float boardX = ((x + 0.5f) / _panelTexWidth) * (_boardWidth + panelPaddingCells * 2f) - panelPaddingCells;
            float boardYFromBottom = ((y + 0.5f) / _panelTexHeight) * (_boardHeight + panelPaddingCells * 2f) - panelPaddingCells;
            float boardY = _boardHeight - boardYFromBottom;

            int col = Mathf.FloorToInt(boardX);
            int row = Mathf.FloorToInt(boardY);
            if (row < 0 || row >= _boardHeight || col < 0 || col >= _boardWidth) return false;
            if (!_holes[row, col]) return false;

            // Spacing margin inside the void cell adjacent to solid cells
            float margin = 0.15f;
            float localX = boardX - col;
            float localY = boardY - row;

            bool hasSolidTop = row == 0 || !_holes[row - 1, col];
            bool hasSolidBottom = row == _boardHeight - 1 || !_holes[row + 1, col];
            bool hasSolidLeft = col == 0 || !_holes[row, col - 1];
            bool hasSolidRight = col == _boardWidth - 1 || !_holes[row, col + 1];

            bool inSolidMargin = (hasSolidTop && localY < margin) ||
                                 (hasSolidBottom && localY > 1 - margin) ||
                                 (hasSolidLeft && localX < margin) ||
                                 (hasSolidRight && localX > 1 - margin);

            if (inSolidMargin)
            {
                return false; // Render as solid background margin
            }

            color = Color.clear;
            return true;
        }

        private bool TryGetVoidEdgePixel(int x, int y, out Color color)
        {
            color = Color.clear;
            return false;
        }

        private void PulseSockets()
        {
            if (_sockets == null) return;

            float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 4.5f) * 0.5f;
            Color tint = Color.Lerp(Color.white, _neonCyan, 0.08f + pulse * 0.12f);
            tint.a = 0.35f; // Set lower alpha to make sockets more subtle

            foreach (var socket in _sockets)
            {
                if (socket != null)
                    socket.color = tint;
            }
        }

        private GameObject CreateRuntimeObject(string objectName)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            _runtimeObjects.Add(go);
            return go;
        }

        private void ClearRuntimeObjects()
        {
            foreach (var go in _runtimeObjects)
            {
                if (go == null) continue;
                if (Application.isPlaying)
                    Destroy(go);
                else
                    DestroyImmediate(go);
            }

            _runtimeObjects.Clear();
            _panel = null;
            _sockets = null;
            _panelTexture = null;
            _panelPixels = null;
            _holes = null;
        }

        private static int Hash(int x, int y, int frame)
        {
            unchecked
            {
                int h = x * 73856093 ^ y * 19349663 ^ frame * 83492791;
                return h & 0x7fffffff;
            }
        }
    }
}
