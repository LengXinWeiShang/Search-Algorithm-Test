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

// A*�����㷨����ֵ
public class AScore : IComparable<AScore>
{
    // ���ߵĲ���
    public int G;

    // ���յ�������پ���
    public int H;

    // ����ֵ
    public float F
    {
        get { return G + H; }
    }

    // ���ڵ㣨�ߵ��õ����һ���ڵ㣩
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
    private int H = 20;           // ��ͼ�߶�
    private int W = 30;           // ��ͼ���
    private int pathDefaultValue = int.MaxValue; // ·����Ĭ��ֵ

    private GameObject[,] pathBlock;           // ·������
    private GameObject[,] mapBlock;            // ��ͼ����

    public GameObject prefabBlock;
    public GameObject prefabPath;

    public enum SearchMethod
    {
        BFS,
        DFS,
        AStar,
    }

    public SearchMethod searchMethod = SearchMethod.BFS;

    private int[,] map;                 // ���ݲ���ĵ�ͼ��0������ǽ��1������ǽ
    private int[,] pathMap;             // ·�����飬���ִ����ߵ���Ӧ����Ҫ�Ĳ���
    private AScore[,] score_map;        // ����ֵ���飬A*����������������

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

    private Pos start;                  // ���
    private Pos target;                 // �յ�

    private const int START = 8;
    private const int END = 9;
    private const int WALL = 1;

    private void Start()
    {
        map = new int[H, W];
        pathMap = new int[H, W];
        mapBlock = new GameObject[H, W];
        pathBlock = new GameObject[H, W];

        // ��ȡ��ͼ�ļ�
        ReadMapFile();

        // �������з���
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

    // ��ȡ��ͼ�ı��ļ�
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
            // �м���
            int y = 0;
            // ������һ�У��߽磩
            read.ReadLine();
            strLine = read.ReadLine();

            while (strLine != null && y < H)
            {
                for (int x = 0; x < W && x < strLine.Length; ++x)
                {
                    // ��������
                    map[y, x] = strLine[x] == '1' ? 1 : 0;
                }
                y++;
                strLine = read.ReadLine();
            }
            // �ͷ��ļ���
            read.Dispose();
        }
    }

    // �������е�ͼ����
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

    // ˢ�µ�ͼ
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

    // ˢ��·��
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

    // ���������յ�
    private bool SetPoint(int n)
    {
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // ��ȡ���е�Ŀ�귽��
            RaycastHit hit = new RaycastHit();
            Physics.Raycast(ray, out hit, 100);
            Debug.Log(hit.point);
            if (hit.transform != null && hit.transform.name.Equals("Ground"))
            {
                // λ��ȡ����
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

    // ����ί�У�BFS��DFSͨ��ʵ�������ί����ɶ���һ����������ж�
    private delegate bool Func(int x, int y, int curStep);

    // BFS�����㷨
    private IEnumerator BFS()
    {
        int curDepth = 0;
        // ��ʼ����������
        Queue<Pos> searchPos = new Queue<Pos>();
        // ��ʼ��·������
        InitPathMap();
        // ����ǰ�����Ϊ��һ��������
        searchPos.Enqueue(start);
        pathMap[start.y, start.x] = 0;
        // ��lambda���ʽ��������ί��
        Func func = (int x, int y, int curStep) =>
        {
            if (map[y, x] == END)
            {
                Debug.Log("�ҵ��յ㣡");
                pathMap[y, x] = curStep + 1;
                gameState = GameState.ShowPath;
                return true;
            }
            if (map[y, x] == 0 && pathMap[y, x] > curStep + 1)
            {
                // �µĵ����������������������
                searchPos.Enqueue(new Pos(x, y));
                pathMap[y, x] = curStep + 1;
            }
            return false;
        };

        // �������в�Ϊ��ʱ��������
        while (searchPos.Count > 0)
        {
            // ȡ����ǰ�Ĵ�������
            Pos cur = searchPos.Dequeue();
            int curStep = pathMap[cur.y, cur.x];
            // ���ĸ���������
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
            Debug.Log($"���г��ȣ�{searchPos.Count}");
            // ÿ0.1��ˢ��һ��
            if (pathMap[cur.y, cur.x] > curDepth)
            {
                curDepth = pathMap[cur.y, cur.x];
                RefreshPath();
                yield return new WaitForSeconds(0.1f);
            }
        }
        Debug.Log("�޷������յ㣡");
    }

    // DFS�����㷨
    private IEnumerator DFS()
    {
        // ��������
        List<Pos> searchPos = new List<Pos>();
        // ��ʼ������·������
        InitPathMap();
        // ���������Ϊ��һ��������
        searchPos.Add(start);
        pathMap[start.y, start.x] = 0;
        // ��������ί��
        Func func = (int x, int y, int curStep) =>
        {
            if (map[y, x] == END)
            {
                Debug.Log("�ҵ��յ㣡");
                pathMap[y, x] = curStep + 1;
                gameState = GameState.ShowPath;
                return true;
            }
            if (map[y, x] == 0 && pathMap[y, x] > curStep + 1)
            {
                // �µĵ����������������������
                searchPos.Add(new Pos(x, y));
                pathMap[y, x] = curStep + 1;
            }
            return false;
        };
        // �������в�Ϊ��ʱ��������
        while (searchPos.Count > 0)
        {
            // ģ�����ȶ��У����յ��յ�ľ��������������
            searchPos.Sort((Pos a, Pos b) =>
            {
                // �����پ���
                float da = Pos.ManhattanDistance(target, a);
                float db = Pos.ManhattanDistance(target, b);
                return da.CompareTo(db);
            });
            // ȡ����ǰ�Ĵ�������
            Pos cur = searchPos[0];
            searchPos.RemoveAt(0);
            int curStep = pathMap[cur.y, cur.x];
            // ���ĸ���������
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
            Debug.Log($"���г��ȣ�{searchPos.Count}");
            RefreshPath();
            yield return new WaitForSeconds(0.01f);
        }
        Debug.Log("�޷������յ㣡");
    }

    // A*�����㷨
    private IEnumerator AStar()
    {
        // ��ʼ������·������
        InitPathMap();
        // ��������
        List<Pos> openList = new List<Pos>();
        // ����ֵ����
        score_map = new AScore[H, W];
        // ����������������
        openList.Add(start);
        // ��������ֵ��Ϊ0
        score_map[start.y, start.x] = new AScore(0, 0);
        // ���в�Ϊ��ʱ��������
        while (openList.Count > 0)
        {
            var test = score_map[4, 4];
            if (test == null)
            {
            }
            // ģ��С���ѣ����������н��а�����ֵ������
            openList.Sort((Pos p1, Pos p2) =>
            {
                AScore a1 = score_map[p1.y, p1.x];
                AScore a2 = score_map[p2.y, p2.x];
                return a1.CompareTo(a2);
            });
            // ȡ������ֵ��С��������
            Pos cur = openList[0];
            openList.RemoveAt(0);
            // ���ĸ�����Ѱ�ҿ�������
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
            Debug.Log($"���г��ȣ�{openList.Count}");
            // ˢ��·��
            RefreshPath();
            yield return new WaitForSeconds(0.01f);
        }
        Debug.Log("�޷������յ㣡");
    }

    // A*�����㷨�ĺ����жϺ���
    private bool AStarHelper(Pos cur, int ox, int oy, List<Pos> openList)
    {
        // �������������ֵ
        AScore oScore = score_map[cur.y + oy, cur.x + ox];
        // ��ǰ�����������ֵ
        AScore curScore = score_map[cur.y, cur.x];
        // ��������
        Pos oP = new Pos(cur.x + ox, cur.y + oy);

        if (map[cur.y + oy, cur.x + ox] == END)
        {
            // �ҵ��յ�
            Debug.Log("�ҵ��յ㣡");
            oScore = new AScore(curScore.G + 1, Pos.ManhattanDistance(oP, target));
            oScore.father = cur;
            score_map[cur.y + oy, cur.x + ox] = oScore;
            // ����·������
            pathMap[cur.y + oy, cur.x + ox] = (int)oScore.F;
            gameState = GameState.ShowPath;
            return true;
        }
        if (map[cur.y + oy, cur.x + ox] == 0)
        {
            // �����ϰ�����һ���ж�
            if (oScore == null)
            {
                // �õ�û�б���������������������У��������ĵ�һ��������ֵ��
                oScore = new AScore(curScore.G + 1, Pos.ManhattanDistance(oP, target));
                oScore.father = cur;
                score_map[cur.y + oy, cur.x + ox] = oScore;
                openList.Add(oP);
            }
            else if (oScore.G > curScore.G + 1)
            {
                // �õ��ѱ����������������ߵ�·��֮ǰ�ߵ�������·���̣�����
                oScore.G = curScore.G + 1;
                oScore.father = cur;
                // �жϸõ��Ƿ������������У����������¼���
                if (!openList.Contains(oP))
                {
                    openList.Add(oP);
                }
            }
            // ����·������
            pathMap[cur.y + oy, cur.x + ox] = (int)oScore.F;
        }
        return false;
    }

    private void ShowBFSOrDFSPath()
    {
        // ���յ㿪ʼ����
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