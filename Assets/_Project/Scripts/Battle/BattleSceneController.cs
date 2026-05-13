using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mathcalibur.Battle
{
    public class BattleSceneController : MonoBehaviour
    {
        [SerializeField] private BattleConfig config;

        private BattleTileView[,] _grid;
        private RectTransform _boardRoot;
        private BattleHudView _hud;
        private Camera _uiCamera;
        private readonly List<BattleTileView> _selection = new();
        private bool _dragging;
        private int _playerHp;
        private int _enemyHp;
        private int _validTurnCount;

        private void Start()
        {
            if (config == null)
            {
                Debug.LogError("BattleConfig is missing.");
                return;
            }
            EnsureUiExists();
            BuildBoard();
            InitBattle();
        }

        private void Update()
        {
            if (_playerHp <= 0 || _enemyHp <= 0) return;
            HandleInput();
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0)) { _dragging = true; ClearSelectionVisual(); TryAddTileAtScreen(Input.mousePosition); }
            if (_dragging && Input.GetMouseButton(0)) TryAddTileAtScreen(Input.mousePosition);
            if (_dragging && Input.GetMouseButtonUp(0)) { _dragging = false; ConfirmSelection(); }
        }

        private void EnsureUiExists()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("BattleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0.5f;
            }
            if (FindAnyObjectByType<EventSystem>() == null) _ = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            _uiCamera = Camera.main;
            BuildHudAndBoardRoots(canvas.transform as RectTransform);
        }

        private void BuildHudAndBoardRoots(RectTransform canvasRoot)
        {
            var hudRoot = CreateUiPanel("CombatArea", canvasRoot, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -0.3f), new Vector2(0, 0));
            _boardRoot = CreateUiPanel("BoardArea", canvasRoot, new Vector2(0, 0), new Vector2(1, 0.7f), new Vector2(0, 0), new Vector2(0, 0));

            _hud = hudRoot.gameObject.AddComponent<BattleHudView>();
            var fields = new[] { "Player HP", "Enemy HP", "Enemy Attack In", "Expression", "Result", "Message" };
            var textComponents = new List<TMP_Text>();
            for (int i = 0; i < fields.Length; i++)
            {
                var t = CreateText(fields[i], hudRoot, new Vector2(0.02f, 0.95f - i * 0.16f), 38);
                textComponents.Add(t);
            }
            typeof(BattleHudView).GetField("playerHpText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_hud, textComponents[0]);
            typeof(BattleHudView).GetField("enemyHpText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_hud, textComponents[1]);
            typeof(BattleHudView).GetField("countdownText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_hud, textComponents[2]);
            typeof(BattleHudView).GetField("expressionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_hud, textComponents[3]);
            typeof(BattleHudView).GetField("resultText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_hud, textComponents[4]);
            typeof(BattleHudView).GetField("messageText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_hud, textComponents[5]);
        }

        private void BuildBoard()
        {
            _grid = new BattleTileView[config.Columns, config.Rows];
            var cellW = config.BoardWidth / config.Columns;
            var cellH = config.BoardHeight / config.Rows;
            for (var y = 0; y < config.Rows; y++)
            for (var x = 0; x < config.Columns; x++)
            {
                var tile = CreateTile(x, y, cellW, cellH);
                SpawnTileValue(tile, x, y);
                _grid[x, y] = tile;
            }
        }

        private void InitBattle()
        {
            _playerHp = config.PlayerMaxHp;
            _enemyHp = config.EnemyMaxHp;
            _validTurnCount = 0;
            RefreshHud("", "-");
        }

        private BattleTileView CreateTile(int x, int y, float cellW, float cellH)
        {
            var go = new GameObject($"Tile_{x}_{y}", typeof(Image), typeof(BattleTileView));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(_boardRoot, false);
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(cellW - 8f, cellH - 8f);
            rt.anchoredPosition = new Vector2(x * cellW + 4f, -y * cellH - 4f);

            var text = new GameObject("Label", typeof(TMP_Text)).GetComponent<TMP_Text>();
            text.transform.SetParent(rt, false);
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 64;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            var tile = go.GetComponent<BattleTileView>();
            typeof(BattleTileView).GetField("background", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(tile, go.GetComponent<Image>());
            typeof(BattleTileView).GetField("label", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(tile, text);
            tile.SetGridPos(x, y);
            return tile;
        }

        private void SpawnTileValue(BattleTileView tile, int x, int y)
        {
            var numberChance = ((x + y) % 2 == 0) ? 0.6f : 0.4f;
            if (UnityEngine.Random.value < numberChance) tile.SetNumber(PickNumber());
            else tile.SetOperator((OperatorType)UnityEngine.Random.Range(0, 3));
        }

        private int PickNumber()
        {
            var total = config.NumberWeights.Sum(w => w.Weight);
            var roll = UnityEngine.Random.Range(1, total + 1);
            var sum = 0;
            foreach (var item in config.NumberWeights) { sum += item.Weight; if (roll <= sum) return item.Value; }
            return 1;
        }

        private void TryAddTileAtScreen(Vector2 pos)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_boardRoot, pos, _uiCamera, out var lp)) return;
            foreach (var tile in _grid)
            {
                var rt = tile.GetComponent<RectTransform>();
                if (!RectTransformUtility.RectangleContainsScreenPoint(rt, pos, _uiCamera)) continue;
                TryAppendTile(tile);
                return;
            }
        }

        private void TryAppendTile(BattleTileView tile)
        {
            if (_selection.Count > 0)
            {
                var last = _selection[^1];
                if (_selection.Count >= 2 && tile == _selection[^2]) { _selection[^1].SetSelected(false); _selection.RemoveAt(_selection.Count - 1); RefreshHud(GetExpressionString(), "-"); return; }
                if (_selection.Contains(tile)) return;
                if (Mathf.Abs(last.X - tile.X) + Mathf.Abs(last.Y - tile.Y) != 1) return;
            }
            if (_selection.Count >= config.MaxExpressionLength) return;
            _selection.Add(tile);
            tile.SetSelected(true);
            RefreshHud(GetExpressionString(), "-");
        }

        private void ConfirmSelection()
        {
            if (!TryEvaluateSelection(out var result, out var error))
            {
                _hud.SetMessage($"Invalid: {error}");
                ClearSelectionVisual();
                RefreshHud("", "-");
                return;
            }

            _validTurnCount++;
            if (result >= 0) _enemyHp = Mathf.Max(0, _enemyHp - result);
            else _enemyHp = Mathf.Min(config.EnemyMaxHp, _enemyHp + Math.Abs(result));
            ResolveBoard();
            var countdown = config.EnemyAttackEveryValidTurns - (_validTurnCount % config.EnemyAttackEveryValidTurns);
            if (_validTurnCount % config.EnemyAttackEveryValidTurns == 0 && _enemyHp > 0)
            {
                _playerHp = Mathf.Max(0, _playerHp - config.EnemyAttackDamage);
                _hud.SetMessage($"Enemy attacked for {config.EnemyAttackDamage}!");
            }
            else _hud.SetMessage("Valid expression!");
            RefreshHud("", result.ToString());
            if (_enemyHp <= 0) _hud.SetMessage("Victory!");
            if (_playerHp <= 0) _hud.SetMessage("Defeat!");
        }

        private bool TryEvaluateSelection(out int result, out string error)
        {
            result = 0; error = string.Empty;
            if (_selection.Count < config.MinExpressionLength) { error = "Too short"; return false; }
            if (_selection.Count > config.MaxExpressionLength) { error = "Too long"; return false; }
            if (_selection[0].Kind != TileKind.Number || _selection[^1].Kind != TileKind.Number) { error = "Must start/end with number"; return false; }
            for (var i = 0; i < _selection.Count; i++)
            {
                var expected = i % 2 == 0 ? TileKind.Number : TileKind.Operator;
                if (_selection[i].Kind != expected) { error = "Must alternate number/operator"; return false; }
            }
            var values = new List<int> { _selection[0].NumberValue };
            var ops = new List<OperatorType>();
            for (var i = 1; i < _selection.Count; i += 2)
            {
                ops.Add(_selection[i].Operator);
                values.Add(_selection[i + 1].NumberValue);
            }
            for (var i = 0; i < ops.Count;)
            {
                if (ops[i] is OperatorType.Multiply or OperatorType.Divide)
                {
                    if (ops[i] == OperatorType.Divide && values[i + 1] == 0) { error = "Divide by zero"; return false; }
                    values[i] = ops[i] == OperatorType.Multiply ? values[i] * values[i + 1] : values[i] / values[i + 1];
                    values.RemoveAt(i + 1); ops.RemoveAt(i);
                }
                else i++;
            }
            result = values[0];
            for (var i = 0; i < ops.Count; i++) result = ops[i] == OperatorType.Add ? result + values[i + 1] : result - values[i + 1];
            return true;
        }

        private void ResolveBoard()
        {
            foreach (var tile in _selection)
            {
                _grid[tile.X, tile.Y] = null;
                Destroy(tile.gameObject);
            }
            for (var x = 0; x < config.Columns; x++)
            {
                var writeY = config.Rows - 1;
                for (var y = config.Rows - 1; y >= 0; y--)
                {
                    var tile = _grid[x, y];
                    if (tile == null) continue;
                    _grid[x, y] = null;
                    _grid[x, writeY] = tile;
                    writeY--;
                }
                var cellW = config.BoardWidth / config.Columns;
                var cellH = config.BoardHeight / config.Rows;
                for (var y = writeY; y >= 0; y--)
                {
                    var tile = CreateTile(x, y, cellW, cellH);
                    SpawnTileValue(tile, x, y);
                    _grid[x, y] = tile;
                }
                for (var y = 0; y < config.Rows; y++)
                {
                    _grid[x, y].SetGridPos(x, y);
                    _grid[x, y].GetComponent<RectTransform>().anchoredPosition = new Vector2(x * cellW + 4f, -y * cellH - 4f);
                }
            }
            ClearSelectionVisual();
        }

        private string GetExpressionString()
        {
            return string.Join(" ", _selection.Select(t => t.Kind == TileKind.Number ? t.NumberValue.ToString() : (t.Operator == OperatorType.Add ? "+" : t.Operator == OperatorType.Subtract ? "-" : t.Operator == OperatorType.Multiply ? "*" : "/")));
        }

        private void ClearSelectionVisual()
        {
            foreach (var tile in _selection) if (tile != null) tile.SetSelected(false);
            _selection.Clear();
        }

        private void RefreshHud(string expression, string result)
        {
            _hud.SetHp(_playerHp, _enemyHp);
            var left = config.EnemyAttackEveryValidTurns - (_validTurnCount % config.EnemyAttackEveryValidTurns);
            _hud.SetCountdown(left);
            _hud.SetExpression(expression);
            _hud.SetResult(result);
        }

        private static RectTransform CreateUiPanel(string name, RectTransform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return rt;
        }

        private static TMP_Text CreateText(string name, RectTransform parent, Vector2 anchorPos, float fontSize)
        {
            var go = new GameObject(name, typeof(TMP_Text));
            var t = go.GetComponent<TMP_Text>();
            var rt = t.rectTransform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(anchorPos.x, anchorPos.y);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(900, 120);
            t.fontSize = fontSize;
            t.alignment = TextAlignmentOptions.TopLeft;
            t.text = name;
            t.color = Color.white;
            return t;
        }
    }
}
