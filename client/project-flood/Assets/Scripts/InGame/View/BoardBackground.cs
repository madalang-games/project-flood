using System.Collections.Generic;
using UnityEngine;

namespace Game.InGame.View
{
    public class BoardBackground : MonoBehaviour
    {
        [SerializeField] private Color _boardColor = new Color(0.055f, 0.071f, 0.118f);
        [SerializeField] private Color _socketColor = new Color(0.118f, 0.137f, 0.212f);
        [SerializeField] private Color _socketHighlight = new Color(0.306f, 0.925f, 1f);
        [SerializeField] private Color _socketShadow = new Color(0.318f, 0.122f, 0.612f);
        [SerializeField] private Color _neonCyan = new Color(0.15f, 0.95f, 1f);
        [SerializeField] private Color _neonPink = new Color(1f, 0.22f, 0.78f);
        [SerializeField] private bool _animateTexture = true;
        [SerializeField] private float _effectFps = 12f;

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

        public void Build(int width, int height, float cellSize, Vector3[,] cellPositions)
        {
            ClearRuntimeObjects();
            _boardWidth = width;
            _boardHeight = height;
            _holes = new bool[height, width];
            BuildPanel(width, height, cellSize);
            BuildSockets(width, height, cellSize, cellPositions);
            RenderPanelFrame(0);
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

            RenderPanelFrame(_frame);
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
            float panelWidth = width * cellSize + cellSize * 0.28f;
            float panelHeight = height * cellSize + cellSize * 0.28f;

            var go = CreateRuntimeObject("BoardPanel");
            go.transform.localPosition = Vector3.zero;

            _panelTexWidth = Mathf.Clamp(width * 14, 48, 128);
            _panelTexHeight = Mathf.Clamp(height * 14, 48, 128);
            _panelPixels = new Color[_panelTexWidth * _panelTexHeight];
            _panelTexture = new Texture2D(_panelTexWidth, _panelTexHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
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

            _panel = go.AddComponent<SpriteRenderer>();
            _panel.sprite = sprite;
            _panel.drawMode = SpriteDrawMode.Sliced;
            _panel.size = new Vector2(panelWidth, panelHeight);
            _panel.sortingOrder = SortingOrderPanel;
        }

        private void BuildSockets(int width, int height, float cellSize, Vector3[,] cellPositions)
        {
            _sockets = new SpriteRenderer[height, width];
            var socketSprite = CreateSocketSprite(cellSize);

            for (int r = 0; r < height; r++)
            for (int c = 0; c < width; c++)
            {
                var go = CreateRuntimeObject($"Socket_{r}_{c}");
                go.transform.localPosition = cellPositions[r, c];

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = socketSprite;
                sr.sortingOrder = SortingOrderSocket;
                _sockets[r, c] = sr;
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

                bool border = x < 3 || y < 3 || x >= _panelTexWidth - 3 || y >= _panelTexHeight - 3;
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

                _panelPixels[y * _panelTexWidth + x] = color;
            }

            _panelTexture.SetPixels(_panelPixels);
            _panelTexture.Apply(false);
        }

        private bool TryGetHolePixel(int x, int y, out Color color)
        {
            color = Color.clear;
            if (_holes == null || _boardWidth <= 0 || _boardHeight <= 0) return false;

            const float panelPaddingCells = 0.14f;
            float boardX = ((x + 0.5f) / _panelTexWidth) * (_boardWidth + panelPaddingCells * 2f) - panelPaddingCells;
            float boardYFromBottom = ((y + 0.5f) / _panelTexHeight) * (_boardHeight + panelPaddingCells * 2f) - panelPaddingCells;
            float boardY = _boardHeight - boardYFromBottom;

            int col = Mathf.FloorToInt(boardX);
            int row = Mathf.FloorToInt(boardY);
            if (row < 0 || row >= _boardHeight || col < 0 || col >= _boardWidth) return false;
            if (!_holes[row, col]) return false;

            color = Color.clear;
            return true;
        }

        private bool TryGetVoidEdgePixel(int x, int y, out Color color)
        {
            color = Color.clear;
            if (_holes == null || _boardWidth <= 0 || _boardHeight <= 0) return false;

            const float panelPaddingCells = 0.14f;
            float boardX = ((x + 0.5f) / _panelTexWidth) * (_boardWidth + panelPaddingCells * 2f) - panelPaddingCells;
            float boardYFromBottom = ((y + 0.5f) / _panelTexHeight) * (_boardHeight + panelPaddingCells * 2f) - panelPaddingCells;
            float boardY = _boardHeight - boardYFromBottom;

            int col = Mathf.FloorToInt(boardX);
            int row = Mathf.FloorToInt(boardY);
            if (row < 0 || row >= _boardHeight || col < 0 || col >= _boardWidth) return false;
            if (_holes[row, col]) return false;

            float localX = boardX - col;
            float localY = boardY - row;
            bool touchesVoidLeft = col > 0 && _holes[row, col - 1] && localX < 0.08f;
            bool touchesVoidRight = col < _boardWidth - 1 && _holes[row, col + 1] && localX > 0.92f;
            bool touchesVoidTop = row > 0 && _holes[row - 1, col] && localY < 0.08f;
            bool touchesVoidBottom = row < _boardHeight - 1 && _holes[row + 1, col] && localY > 0.92f;

            if (!touchesVoidLeft && !touchesVoidRight && !touchesVoidTop && !touchesVoidBottom)
                return false;

            float pulse = 0.5f + Mathf.Sin((_frame + row * 3 + col * 5) * 0.35f) * 0.5f;
            color = Color.Lerp(_neonCyan, _neonPink, pulse);
            return true;
        }

        private void PulseSockets()
        {
            if (_sockets == null) return;

            float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 4.5f) * 0.5f;
            Color tint = Color.Lerp(Color.white, _neonCyan, 0.08f + pulse * 0.12f);

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
