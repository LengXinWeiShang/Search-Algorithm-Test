using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public struct Pos
{
    public int x;
    public int y;

    public Pos(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static int ManhattanDistance(Pos a, Pos b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    public bool Equals(Pos p)
    {
        return p.x == x && p.y == y;
    }
}

// A*搜索算法评估值
public class AScore : IComparable<AScore>
{
    // 已走的步数
    public int G;

    // 到终点的曼哈顿距离
    public int H;

    // 评估值
    public float F
    {
        get { return G + H; }
    }

    // 父节点（走到该点的上一个节点）
    public Pos father;

    public AScore(int g, int h)
    {
        G = g;
        H = h;
    }

    public int CompareTo(AScore a2)
    {
        return F.CompareTo(a2.F);
    }
}

public class Search_Test : MonoBehaviour
{
    private int H = 20;           // 地图高度
    private int W = 30;           // 地图宽度
    private int pathDefaultValue = int.MaxValue; // 路径的默认值

    private GameObject[,] pathBlock;           // 路径方块
    private GameObject[,] mapBlock;            // 地图方块

    public GameObject prefabBlock;
    public GameObject prefabPath;

    public enum SearchMethod
    {
        BFS,
        DFS,
        AStar,
    }

    public SearchMethod searchMethod = SearchMethod.BFS;

    private int[,] map;                 // 数据层面的地图，0代表无墙，1代表有墙
    private int[,] pathMap;             // 路径数组，数字代表走到对应点需要的步数
    private AScore[,] score_map;        // 评估值数组，A*中用于评估搜索点

    private enum GameState
    {
        SetStartPoint,
        SetTargetPoint,
        StartSearching,
        Searching,
        ShowPath,
        Finish,
    }

    private GameState gameState = GameState.SetStartPoint;

    private Pos start;                  // 起点
    private Pos target;                 // 终点

    private const int START = 8;
    private const int END = 9;
    private const int WALL = 1;

    private void Start()
    {
        map = new int[H, W];
        pathMap = new int[H, W];
        mapBlock = new GameObject[H, W];
        pathBlock = new GameObject[H, W];

        // 读取地图文件
        ReadMapFile();

        // 创建所有方块
        CreateBlocks();

        RefreshMap();
    }

    private void Update()
    {
        switch (gameState)
        {
            case GameState.SetStartPoint:
                if (SetPoint(START))
                {
                    RefreshMap();
                    gameState = GameState.SetTargetPoint;
                }
                break;

            case GameState.SetTargetPoint:
                if (SetPoint(END))
                {
                    RefreshMap();
                    gameState = GameState.StartSearching;
                }
                break;

            case GameState.StartSearching:
                if (searchMethod == SearchMethod.BFS)
                {
                    StartCoroutine(BFS());
                }
                else if (searchMethod == SearchMethod.DFS)
                {
                    StartCoroutine(DFS());
                }
                else if (searchMethod == SearchMethod.AStar)
                {
                    StartCoroutine(AStar());
                }
                gameState = GameState.Searching;
                break;

            case GameState.Searching:
                break;

            case GameState.ShowPath:
                if (searchMethod == SearchMethod.BFS || searchMethod == SearchMethod.DFS)
                {
                    ShowBFSOrDFSPath();
                }
                else if (searchMethod == SearchMethod.AStar)
                {
                    ShowAStarPath();
                }
                gameState = GameState.Finish;
                break;

            case GameState.Finish:
                break;

            default:
                break;
        }
    }

    // 读取地图文本文件
    private void ReadMapFile()
    {
        string path = Application.dataPath + "//Resources//map.txt";
        if (!File.Exists(path))
        {
            return;
        }

        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            StreamReader read = new StreamReader(fs, Encoding.UTF8);
            string strLine = "";
            // 行计数
            int y = 0;
            // 跳过第一行（边界）
            read.ReadLine();
            strLine = read.ReadLine();

            while (strLine != null && y < H)
            {
                for (int x = 0; x < W && x < strLine.Length; ++x)
                {
                    // 方块类型
                    map[y, x] = strLine[x] == '1' ? 1 : 0;
                }
                y++;
                strLine = read.ReadLine();
            }
            // 释放文件流
            read.Dispose();
        }
    }

    // 创建所有地图方块
    private void CreateBlocks()
    {
        for (int i = 0; i < H; ++i)
        {
            for (int j = 0; j < W; ++j)
            {
                pathBlock[i, j] = Instantiate(prefabPath, new Vector3(j, -0.35f, i), Quaternion.identity);
                pathBlock[i, j].SetActive(false);
                mapBlock[i, j] = Instantiate(prefabBlock, new Vector3(j, 0, i), Quaternion.identity);
                mapBlock[i, j].SetActive(false);
            }
        }
    }

    // 刷新地图
    private void RefreshMap()
    {
        for (int i = 0; i < H; ++i)
        {
            for (int j = 0; j < W; ++j)
            {
                GameObject obj = mapBlock[i, j].gameObject;
                switch (map[i, j])
                {
                    case 0:
                        obj.SetActive(false);
                        break;

                    case WALL:
                        obj.SetActive(true);
                        obj.GetComponent<MeshRenderer>().material.color = Color.white;
                        break;

                    case START:
                    case END:
                        obj.SetActive(true);
                        obj.GetComponent<MeshRenderer>().material.color = Color.red;
                        break;
                }
            }
        }
    }

    // 刷新路径
    private void RefreshPath()
    {
        for (int i = 0; i < H; ++i)
        {
            for (int j = 0; j < W; ++j)
            {
                if (pathMap[i, j] == pathDefaultValue)
                {
                    pathBlock[i, j].SetActive(false);
                }
                else
                {
                    TextMesh[] texts = pathBlock[i, j].GetComponentsInChildren<TextMesh>();
                    pathBlock[i, j].SetActive(true);
                    if (searchMethod == SearchMethod.BFS || searchMethod == SearchMethod.DFS)
                    {
                        texts[0].text = pathMap[i, j].ToString();
                        texts[1].text = "";
                    }
                    else if (searchMethod == SearchMethod.AStar)
                    {
                        texts[0].text = score_map[i, j].G.ToString();
                        texts[1].text = score_map[i, j].F.ToString();
                    }
                }
            }
        }
    }

    // 设置起点和终点
    private bool SetPoint(int n)
    {
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // 获取击中的目标方块
            RaycastHit hit = new RaycastHit();
            Physics.Raycast(ray, out hit, 100);
            Debug.Log(hit.point);
            if (hit.transform != null && hit.transform.name.Equals("Ground"))
            {
                // 位置取整数
                int x = (int)hit.point.x;
                int y = (int)hit.point.z;

                map[y, x] = n;
                if (n == START)
                {
                    start = new Pos(x, y);
                }
                else if (n == END)
                {
                    target = new Pos(x, y);
                }
                return true;
            }
        }
        return false;
    }

    // 定义委托，BFS和DFS通过实例化这个委托完成对下一个搜索点的判断
    private delegate bool Func(int x, int y, int curStep);

    // BFS搜索算法
    private IEnumerator BFS()
    {
        int curDepth = 0;
        // 初始化搜索队列
        Queue<Pos> searchPos = new Queue<Pos>();
        // 初始化路径数组
        InitPathMap();
        // 将当前的起点为第一个搜索点
        searchPos.Enqueue(start);
        pathMap[start.y, start.x] = 0;
        // 用lambda表达式定义匿名委托
        Func func = (int x, int y, int curStep) =>
        {
            if (map[y, x] == END)
            {
                Debug.Log("找到终点！");
                pathMap[y, x] = curStep + 1;
                gameState = GameState.ShowPath;
                return true;
            }
            if (map[y, x] == 0 && pathMap[y, x] > curStep + 1)
            {
                // 新的点符合条件，加入搜索队列
                searchPos.Enqueue(new Pos(x, y));
                pathMap[y, x] = curStep + 1;
            }
            return false;
        };

        // 搜索队列不为空时持续搜索
        while (searchPos.Count > 0)
        {
            // 取出当前的待搜索点
            Pos cur = searchPos.Dequeue();
            int curStep = pathMap[cur.y, cur.x];
            // 向四个方向搜索
            if (cur.y > 0)
            {
                if (func(cur.x, cur.y - 1, curStep)) { yield break; }
            }
            if (cur.y < H - 1)
            {
                if (func(cur.x, cur.y + 1, curStep)) { yield break; }
            }
            if (cur.x > 0)
            {
                if (func(cur.x - 1, cur.y, curStep)) { yield break; }
            }
            if (cur.x < W - 1)
            {
                if (func(cur.x + 1, cur.y, curStep)) { yield break; }
            }
            Debug.Log($"队列长度：{searchPos.Count}");
            // 每0.1秒刷新一层
            if (pathMap[cur.y, cur.x] > curDepth)
            {
                curDepth = pathMap[cur.y, cur.x];
                RefreshPath();
                yield return new WaitForSeconds(0.1f);
            }
        }
        Debug.Log("无法到达终点！");
    }

    // DFS搜索算法
    private IEnumerator DFS()
    {
        // 搜索队列
        List<Pos> searchPos = new List<Pos>();
        // 初始化搜索路径数组
        InitPathMap();
        // 将起点设置为第一个搜索点
        searchPos.Add(start);
        pathMap[start.y, start.x] = 0;
        // 定义匿名委托
        Func func = (int x, int y, int curStep) =>
        {
            if (map[y, x] == END)
            {
                Debug.Log("找到终点！");
                pathMap[y, x] = curStep + 1;
                gameState = GameState.ShowPath;
                return true;
            }
            if (map[y, x] == 0 && pathMap[y, x] > curStep + 1)
            {
                // 新的点符合条件，加入搜索队列
                searchPos.Add(new Pos(x, y));
                pathMap[y, x] = curStep + 1;
            }
            return false;
        };
        // 搜索队列不为空时持续搜索
        while (searchPos.Count > 0)
        {
            // 模拟优先队列，按照到终点的距离对搜索点排序
            searchPos.Sort((Pos a, Pos b) =>
            {
                // 曼哈顿距离
                float da = Pos.ManhattanDistance(target, a);
                float db = Pos.ManhattanDistance(target, b);
                return da.CompareTo(db);
            });
            // 取出当前的待搜索点
            Pos cur = searchPos[0];
            searchPos.RemoveAt(0);
            int curStep = pathMap[cur.y, cur.x];
            // 向四个方向搜索
            if (cur.y > 0)
            {
                if (func(cur.x, cur.y - 1, curStep)) { yield break; }
            }
            if (cur.y < H - 1)
            {
                if (func(cur.x, cur.y + 1, curStep)) { yield break; }
            }
            if (cur.x > 0)
            {
                if (func(cur.x - 1, cur.y, curStep)) { yield break; }
            }
            if (cur.x < W - 1)
            {
                if (func(cur.x + 1, cur.y, curStep)) { yield break; }
            }
            Debug.Log($"队列长度：{searchPos.Count}");
            RefreshPath();
            yield return new WaitForSeconds(0.01f);
        }
        Debug.Log("无法到达终点！");
    }

    // A*搜索算法
    private IEnumerator AStar()
    {
        // 初始化搜索路径数组
        InitPathMap();
        // 搜索队列
        List<Pos> openList = new List<Pos>();
        // 评估值数组
        score_map = new AScore[H, W];
        // 将起点加入搜索队列
        openList.Add(start);
        // 起点的评估值设为0
        score_map[start.y, start.x] = new AScore(0, 0);
        // 队列不为空时持续搜索
        while (openList.Count > 0)
        {
            var test = score_map[4, 4];
            if (test == null)
            {
            }
            // 模拟小根堆，对搜索队列进行按评估值的排序
            openList.Sort((Pos p1, Pos p2) =>
            {
                AScore a1 = score_map[p1.y, p1.x];
                AScore a2 = score_map[p2.y, p2.x];
                return a1.CompareTo(a2);
            });
            // 取出评估值最小的搜索点
            Pos cur = openList[0];
            openList.RemoveAt(0);
            // 向四个方向寻找可搜索点
            if (cur.y > 0)
            {
                if (AStarHelper(cur, 0, -1, openList)) { yield break; }
            }
            if (cur.y < H - 1)
            {
                if (AStarHelper(cur, 0, 1, openList)) { yield break; }
            }
            if (cur.x > 0)
            {
                if (AStarHelper(cur, -1, 0, openList)) { yield break; }
            }
            if (cur.x < W - 1)
            {
                if (AStarHelper(cur, 1, 0, openList)) { yield break; }
            }
            Debug.Log($"队列长度：{openList.Count}");
            // 刷新路径
            RefreshPath();
            yield return new WaitForSeconds(0.01f);
        }
        Debug.Log("无法到达终点！");
    }

    // A*搜索算法的核心判断函数
    private bool AStarHelper(Pos cur, int ox, int oy, List<Pos> openList)
    {
        // 新搜索点的评估值
        AScore oScore = score_map[cur.y + oy, cur.x + ox];
        // 当前搜索点的评估值
        AScore curScore = score_map[cur.y, cur.x];
        // 新搜索点
        Pos oP = new Pos(cur.x + ox, cur.y + oy);

        if (map[cur.y + oy, cur.x + ox] == END)
        {
            // 找到终点
            Debug.Log("找到终点！");
            oScore = new AScore(curScore.G + 1, Pos.ManhattanDistance(oP, target));
            oScore.father = cur;
            score_map[cur.y + oy, cur.x + ox] = oScore;
            // 更新路径数组
            pathMap[cur.y + oy, cur.x + ox] = (int)oScore.F;
            gameState = GameState.ShowPath;
            return true;
        }
        if (map[cur.y + oy, cur.x + ox] == 0)
        {
            // 不是障碍，进一步判断
            if (oScore == null)
            {
                // 该点没有被搜索过，加入待搜索队列（搜索过的点一定有评估值）
                oScore = new AScore(curScore.G + 1, Pos.ManhattanDistance(oP, target));
                oScore.father = cur;
                score_map[cur.y + oy, cur.x + ox] = oScore;
                openList.Add(oP);
            }
            else if (oScore.G > curScore.G + 1)
            {
                // 该点已被搜索过，且现在走的路比之前走到这个点的路更短，更新
                oScore.G = curScore.G + 1;
                oScore.father = cur;
                // 判断该点是否还在搜索队列中，不在则重新加入
                if (!openList.Contains(oP))
                {
                    openList.Add(oP);
                }
            }
            // 更新路径数组
            pathMap[cur.y + oy, cur.x + ox] = (int)oScore.F;
        }
        return false;
    }

    private void ShowBFSOrDFSPath()
    {
        // 从终点开始反推
        Pos p = target;
        int step = pathMap[p.y, p.x];
        while (!p.Equals(start))
        {
            if (p.y < H - 1 && pathMap[p.y + 1, p.x] == step - 1)
            {
                p = new Pos(p.x, p.y + 1);
                step--;
            }
            else if (p.y > 0 && pathMap[p.y - 1, p.x] == step - 1)
            {
                p = new Pos(p.x, p.y - 1);
                step--;
            }
            else if (p.x > 0 && pathMap[p.y, p.x - 1] == step - 1)
            {
                p = new Pos(p.x - 1, p.y);
                step--;
            }
            else if (p.x < W - 1 && pathMap[p.y, p.x + 1] == step - 1)
            {
                p = new Pos(p.x + 1, p.y);
                step--;
            }
            else
            {
                return;
            }
            var go = pathBlock[p.y, p.x];
            var render = go.GetComponent<MeshRenderer>();
            render.material.color = Color.blue;
        }
    }

    private void ShowAStarPath()
    {
        Pos p = target;
        while (!p.Equals(start))
        {
            var go = pathBlock[p.y, p.x];
            var render = go.GetComponent<MeshRenderer>();
            render.material.color = Color.blue;
            p = score_map[p.y, p.x].father;
        }
    }

    private void InitPathMap()
    {
        for (int i = 0; i < H; ++i)
        {
            for (int j = 0; j < W; ++j)
            {
                pathMap[i, j] = pathDefaultValue;
            }
        }
    }
}