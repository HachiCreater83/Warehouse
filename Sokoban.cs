using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Sokoban : MonoBehaviour
{
    // タイルの種類,番号ごとに状態を表す変数
    private enum TileType
    {
        /*タイル番号の振り分け,マップ上に表示する際に用いる
         * NONE=0,何も無い空間
         * GROUND=1,地面
         * TARGET=2,目的地
         * PLAYER=3,プレイヤー
         * BLOCK=4ブロック
         * PLAYER_ON_TARGET=5,プレイヤー（目的地の上にいる場合）
         * BLOCK_ON_TARGET=6 ,ブロック（目的地の上にある場合）
         */

        NONE,
        GROUND,
        TARGET,
        PLAYER,
        BLOCK,
        PLAYER_ON_TARGET,
        BLOCK_ON_TARGET,
    }

    // 移動方向の種類
    private enum DirectionType
    {
        //上下左右の移動処理時に用いる判定
        UP,
        RIGHT,
        DOWN,
        LEFT,
    }

    #region//タイル情報
    /*
     * ステージ構造が記述されたテキストファイルを読み込む
     * 行数の設定用の変数
     * 列数の設定用の変数
     * タイル情報を管理する二次元配列
     * タイルのサイズ,アセットの表示サイズに合わせる
     */
    public TextAsset _stageFile = default;
    private int _rows = default;
    private int _columns = default;
    private TileType[,] _tileList = default;
    public float _tileSize = default;
    #endregion

    #region //スプライトの設定,マップに表示する際に用いる設定
    [SerializeField, Header("地面のスプライト")] private Sprite _groundSprite = default;
    [SerializeField, Header("目的地のスプライト")] private Sprite _targetSprite = default;
    [SerializeField, Header("ブロックのスプライト")] private Sprite _blockSprite = default;
    [SerializeField, Header("プレイヤーのスプライト")] private Sprite _playerSprite = default;
    [SerializeField, Header("プレイヤーの上向きスプライト")] private Sprite _player_upSprite = default;
    [SerializeField, Header("プレイヤーの左向きスプライト")] private Sprite _player_leftSprite = default;
    [SerializeField, Header("プレイヤーの下向きスプライト")] private Sprite _player_downSprite = default;
    [SerializeField, Header("プレイヤーの右向きスプライト")] private Sprite _player_rightSprite = default;
    #endregion

    #region //クリア,ミス判定,表示するスプライト,行動回数などに用いる変数
    /*
     * プレイヤーのゲームオブジェクト
     * 中心位置の設定
     * ブロックの数情報
     * ゲームのクリア判定 
     * ゲームの終了判定 
     * プレイヤーの方向に向く変数
     * 行動回数を表すキャンバス
     * プレイヤーの行動回数を表示するテキスト
     * プレイヤーの行動回数
     */
    private GameObject _player = default;
    private Vector2 _middleOffset = default;
    private int _blockCount = default;
    private bool _isClear = false;
    private bool _isMiss = false;
    private SpriteRenderer _playersprite = default;
    [SerializeField] private Canvas _countCanvas = default;
    [SerializeField] private Text _actionCountText = default;
    [SerializeField] private int _numberActions = default;
    [SerializeField, Header("Scene切り替え時に表示されるCanvas")] private GameObject _cutInCanvas = default;
    [SerializeField, Header("このScene後に飛ばすScene名")] private string _nextSceneName = default;
    #endregion

    // 各位置に存在するゲームオブジェクトを管理するための連想配列
    private Dictionary<GameObject, Vector2Int> _gameObjectPosTable = new Dictionary<GameObject, Vector2Int>();

    private void Start()
    {
        /*
         * タイルの情報を読み込む処理
         * ステージを作成する処理
         * 行動回数の表示,回数はNumberActiinsから呼ぶ
         */
        LoadTileData();
        CreateStage();
        _actionCountText.text = _numberActions.ToString();
    }

    // タイルの情報を読み込む処理
    private void LoadTileData()
    {
        // タイルの情報を一行ごとに分割する
        var lines = _stageFile.text.Split
        (
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries
        );

        // タイルの列数を計算
        var nums = lines[0].Split(new[] { ',' });

        /*  タイルの列数と行数を保持する処理
         *  行数の設定
         *  列数の設定
         */
        _rows = lines.Length;
        _columns = nums.Length;

        // タイル情報を int 型の２次元配列で保持
        _tileList = new TileType[_columns, _rows];
        for (int y = 0; y < _rows; y++)
        {
            // 一文字ずつ取得する
            var st = lines[y];
            nums = st.Split(new[] { ',' });
            for (int x = 0; x < _columns; x++)
            {
                // 読み込んだ文字を数値に変換して保持
                _tileList[x, y] = (TileType)int.Parse(nums[x]);
            }
        }
    }

    // ステージを作成する処理
    private void CreateStage()
    {
        // ステージの中心位置を計算
        _middleOffset.x = _columns * _tileSize * 0.5f - _tileSize * 0.5f;
        _middleOffset.y = _rows * _tileSize * 0.5f - _tileSize * 0.5f; ;

        for (int y = 0; y < _rows; y++)
        {
            for (int x = 0; x < _columns; x++)
            {
                var val = _tileList[x, y];

                // 何も無い場所は無視
                if (val == TileType.NONE) continue;

                // タイルの名前に行番号と列番号を付与
                var name = "tile" + y + "_" + x;

                // タイルのゲームオブジェクトを作成
                var tile = new GameObject(name);

                // タイルにスプライトを描画する機能を追加
                var sr = tile.AddComponent<SpriteRenderer>();

                // タイルのスプライトを設定
                sr.sprite = _groundSprite;

                // タイルの位置を設定
                tile.transform.position = GetDisplayPosition(x, y);

                // 目的地を生成する場合
                if (val == TileType.TARGET)
                {
                    /*
                     * 目的地のゲームオブジェクトを作成
                     * 目的地にスプライトを描画する機能を追加
                     * 目的地のスプライトを設定
                     * 目的地の描画順を手前にする
                     * 目的地の位置を設定
                     */
                    var destination = new GameObject("destination");
                    sr = destination.AddComponent<SpriteRenderer>();
                    sr.sprite = _targetSprite;
                    sr.sortingOrder = 1;
                    destination.transform.position = GetDisplayPosition(x, y);
                }
                // プレイヤーを生成する場合
                else if (val == TileType.PLAYER)
                {
                    /*
                     * プレイヤーのゲームオブジェクトを作成
                     * プレイヤーにスプライトを描画する機能を追加
                     * プレイヤーのスプライトを設定
                     * プレイヤーの描画順を手前にする
                     * プレイヤーの位置を設定
                     * プレイヤーを連想配列に追加
                     */
                    _player = new GameObject("player");
                    sr = _player.AddComponent<SpriteRenderer>();
                    sr.sprite = _playerSprite;
                    sr.sortingOrder = 2;
                    _player.transform.position = GetDisplayPosition(x, y);
                    _gameObjectPosTable.Add(_player, new Vector2Int(x, y));
                }
                // ブロックを生成する場合
                else if (val == TileType.BLOCK)
                {
                    /*
                     * ブロックの数を増やす
                     * ブロックのゲームオブジェクトを作成
                     * ブロックにスプライトを描画する機能を追加
                     * ブロックのスプライトを設定
                     * ブロックの描画順を手前にする
                     * ブロックの位置を設定
                     * ブロックを連想配列に追加
                     * 
                     */
                    _blockCount++;
                    var block = new GameObject("block" + _blockCount);
                    sr = block.AddComponent<SpriteRenderer>();
                    sr.sprite = _blockSprite;
                    sr.sortingOrder = 2;
                    block.transform.position = GetDisplayPosition(x, y);
                    _gameObjectPosTable.Add(block, new Vector2Int(x, y));
                }
            }
        }
    }

    // 指定された行番号と列番号からスプライトの表示位置を計算して返す
    private Vector2 GetDisplayPosition(int x, int y)
    {
        return new Vector2
        (
            x * _tileSize - _middleOffset.x,
            y * -_tileSize + _middleOffset.y
        );
    }

    // 指定された位置に存在するゲームオブジェクトを返します
    private GameObject GetGameObjectAtPosition(Vector2Int pos)
    {
        foreach (var pair in _gameObjectPosTable)
        {
            // 指定された位置が見つかった場合
            if (pair.Value == pos)
            {
                // その位置に存在するゲームオブジェクトを返す
                return pair.Key;
            }
        }
        return null;
    }

    // 指定された位置のタイルがブロックなら true を返す
    private bool IsBlock(Vector2Int pos)
    {
        var cell = _tileList[pos.x, pos.y];
        return cell == TileType.BLOCK || cell == TileType.BLOCK_ON_TARGET;
    }

    // 指定された位置がステージ内なら true を返す
    private bool IsValidPosition(Vector2Int pos)
    {
        if (0 <= pos.x && pos.x < _columns && 0 <= pos.y && pos.y < _rows)
        {
            return _tileList[pos.x, pos.y] != TileType.NONE;
        }
        return false;
    }

    private void Update()
    {
        // ゲームの終了判定中は操作できないようにする
        if (_isClear || _isMiss) return;

        #region //移動設定
        // 上方向の移動処理が発生した場合
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            // プレイヤーが上に移動できるか検証
            TryMovePlayer(DirectionType.UP);
        }
        // 右方向の移動処理が発生した場合
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            // プレイヤーが右に移動できるか検証
            TryMovePlayer(DirectionType.RIGHT);
        }
        // 下方向の移動処理が発生した場合
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            // プレイヤーが下に移動できるか検証
            TryMovePlayer(DirectionType.DOWN);
        }
        // 左方向の移動処理が発生した場合
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            // プレイヤーが左に移動できるか検証
            TryMovePlayer(DirectionType.LEFT);
        }
        #endregion
    }

    // 指定された方向にプレイヤーが移動できるか検証
    // 移動できる場合は指定の方向に移動する
    private void TryMovePlayer(DirectionType direction)
    {
        /*
         * プレイヤーの現在地を取得
         * プレイヤーの移動先の位置を計算
         * プレイヤーの移動先がステージ内ではない場合は無視
         */
        var currentPlayerPos = _gameObjectPosTable[_player];
        var nextPlayerPos = GetNextPositionAlong(currentPlayerPos, direction);
        if (!IsValidPosition(nextPlayerPos)) return;

        // プレイヤーの移動先にブロックが存在する場合
        if (IsBlock(nextPlayerPos))
        {
            // ブロックの移動先の位置を計算
            var nextBlockPos = GetNextPositionAlong(nextPlayerPos, direction);

            // ブロックの移動先がステージ内の場合かつ,ブロックの移動先にブロックが存在しない場合
            if (IsValidPosition(nextBlockPos) && !IsBlock(nextBlockPos))
            {
                //プレイヤーの行動回数を減らす
                _numberActions--;
                _actionCountText.text = _numberActions.ToString();

                /*
                 * 移動するブロックを取得
                 * プレイヤーの移動先のタイルの情報を更新
                 * ブロックを移動
                 * ブロックの位置を更新
                 */
                var block = GetGameObjectAtPosition(nextPlayerPos);
                UpdateGameObjectPosition(nextPlayerPos);
                block.transform.position = GetDisplayPosition(nextBlockPos.x, nextBlockPos.y);
                _gameObjectPosTable[block] = nextBlockPos;

                // ブロックの移動先の番号を更新
                if (_tileList[nextBlockPos.x, nextBlockPos.y] == TileType.GROUND)
                {
                    // 移動先が地面ならブロックの番号に更新
                    _tileList[nextBlockPos.x, nextBlockPos.y] = TileType.BLOCK;
                }
                else if (_tileList[nextBlockPos.x, nextBlockPos.y] == TileType.TARGET)
                {
                    // 移動先が目的地ならブロック（目的地の上）の番号に更新
                    _tileList[nextBlockPos.x, nextBlockPos.y] = TileType.BLOCK_ON_TARGET;
                }

                /*
                 * プレイヤーの現在地のタイルの情報を更新
                 * プレイヤーを移動
                 * プレイヤーの位置を更新
                 */
                UpdateGameObjectPosition(currentPlayerPos);
                _player.transform.position = GetDisplayPosition(nextPlayerPos.x, nextPlayerPos.y);
                _gameObjectPosTable[_player] = nextPlayerPos;

                // プレイヤーの移動先の番号を更新
                if (_tileList[nextPlayerPos.x, nextPlayerPos.y] == TileType.GROUND)
                {
                    // 移動先が地面ならプレイヤーの番号に更新
                    _tileList[nextPlayerPos.x, nextPlayerPos.y] = TileType.PLAYER;
                }
                else if (_tileList[nextPlayerPos.x, nextPlayerPos.y] == TileType.TARGET)
                {
                    // 移動先が目的地ならプレイヤー（目的地の上）の番号に更新
                    _tileList[nextPlayerPos.x, nextPlayerPos.y] = TileType.PLAYER_ON_TARGET;
                }
            }
        }
        // プレイヤーの移動先にブロックが存在しない場合
        else
        {
            /*
             * プレイヤーの現在地のタイルの情報を更新
             * プレイヤーを移動
             * プレイヤーの位置を更新
             * プレイヤーの行動回数を減らす
             * 設定した音楽を鳴らす
             */
            UpdateGameObjectPosition(currentPlayerPos);
            _player.transform.position = GetDisplayPosition(nextPlayerPos.x, nextPlayerPos.y);
            _gameObjectPosTable[_player] = nextPlayerPos;
            _numberActions--;
            _actionCountText.text = _numberActions.ToString();
            GetComponent<AudioSource>().Play();

            // プレイヤーの移動先の番号を更新,移動先が地面ならプレイヤーの番号に更新
            if (_tileList[nextPlayerPos.x, nextPlayerPos.y] == TileType.GROUND)
            {
                _tileList[nextPlayerPos.x, nextPlayerPos.y] = TileType.PLAYER;
            }

            // 移動先が目的地ならプレイヤー（目的地の上）の番号に更新
            else if (_tileList[nextPlayerPos.x, nextPlayerPos.y] == TileType.TARGET)
            {
                _tileList[nextPlayerPos.x, nextPlayerPos.y] = TileType.PLAYER_ON_TARGET;
            }
        }
        // ゲームをクリアしたかどうか確認する
        CheckCompletion();
    }

    // 指定された方向の位置を返す移動後の処理
    private Vector2Int GetNextPositionAlong(Vector2Int pos, DirectionType direction)
    {
        /*
         * プレイヤーオブジェクトを探し
         * スプライト情報を取得
         */
        _player = GameObject.Find("player");
        _playersprite = _player.GetComponent<SpriteRenderer>();

        //まだ行動できるかどうかの確認
        if (_numberActions <= 0)
        {
            _isMiss = true;
            //シーン切り替え用のコルーチン
            StartCoroutine(DelayCoroutine());
        }
        //移動後の処理
        switch (direction)
        {
            // 上方向の移動後処理
            case DirectionType.UP:
                pos.y -= 1;
                _playersprite.sprite = _player_upSprite;
                break;

            // 右方向の移動後処理
            case DirectionType.RIGHT:
                pos.x += 1;
                _playersprite.sprite = _player_rightSprite;
                break;

            // 下方向の移動後処理
            case DirectionType.DOWN:
                pos.y += 1;
                _playersprite.sprite = _player_downSprite;
                break;

            // 左方向の移動後処理
            case DirectionType.LEFT:
                pos.x -= 1;
                _playersprite.sprite = _player_leftSprite;
                break;
        }
        return pos;
    }

    // 指定された位置のタイルを更新する処理
    private void UpdateGameObjectPosition(Vector2Int pos)
    {
        // 指定された位置のタイルの番号を取得
        var cell = _tileList[pos.x, pos.y];

        // プレイヤーもしくはブロックの場合
        if (cell == TileType.PLAYER || cell == TileType.BLOCK)
        {
            // 地面に変更
            _tileList[pos.x, pos.y] = TileType.GROUND;
        }
        // 目的地に乗っているプレイヤーもしくはブロックの場合
        else if (cell == TileType.PLAYER_ON_TARGET || cell == TileType.BLOCK_ON_TARGET)
        {
            // 目的地に変更
            _tileList[pos.x, pos.y] = TileType.TARGET;
        }
    }

    // ゲームをクリアしたかどうか確認する処理
    private void CheckCompletion()
    {
        // 目的地に乗っているブロックの数を計算
        int blockOnTargetCount = 0;

        for (int y = 0; y < _rows; y++)
        {
            for (int x = 0; x < _columns; x++)
            {
                if (_tileList[x, y] == TileType.BLOCK_ON_TARGET)
                {
                    //目的地に乗っている場合加算する
                    blockOnTargetCount++;
                }
            }
        }

        // すべてのブロックが目的地の上に乗っている場合
        if (blockOnTargetCount == _blockCount)
        {
            //シーン切り替え用のコルーチン
            StartCoroutine(DelayCoroutine());
            // ゲームクリア
            _isClear = true;
            return;
        }
    }

    //コルーチン処理。一定時間後に次の処理をする
    private IEnumerator DelayCoroutine()
    {
        //設定したCanvasの表示
        _cutInCanvas.SetActive(true);

        // Time.timeScale の影響を受けずに実時間で2秒待つ
        yield return new WaitForSecondsRealtime(2);

        //ゲーム終了時にクリアしてるかどうかの確認
        //クリアしてない場合
        if (_isClear == false)
        {
            //リトライするために現在のシーンを再読み込みする
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        //クリアしている場合
        else
        {
            //指定した次のシーンを読み込む
            SceneManager.LoadScene(_nextSceneName, LoadSceneMode.Single);
        }
    }

}
