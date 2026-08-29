using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 임시 카테고리. 나중에 데이터 테이블 쪽에서 정의될 예정
/// </summary>
public enum AreaNum
{
    Area1,
    Area2,
    Area3,
}
public class DisplayStand : MonoBehaviour
{
    public float width;
    public float height;
    public float depth;
    float widthScale = 1f;
    float heightScale = 1f;
    float depthScale = 1f;
    private LineRenderer ViewGrid;
   [field: SerializeField] public bool ShowGrid { get; set; } = false;
    MeshFilter meshFilter;

    GridCell grid;

    public GridCell Grid => grid ??= new GridCell(0, 0, transform.position);


    // 아래는 아이템 한개
    // public ItemDef Item { get; private set; }
    //
    // public MartItem MartItem { get; private set; }

    // 아이템 여러 개 기억하기 위해 일단 임시 코드
    public List<ItemDef> Items { get; private set; } = new();
    public List<MartItem> MartItems { get; private set; } = new();
    
    [field: SerializeField] public AreaNum areaNum { get; private set; } = AreaNum.Area1;
    public CategoryType CategoryType { get; set; } = CategoryType.None;

    // 아이템 3등분 위치
    private readonly List<Vector3> itemPositions = new();
    private const int ItemMaxCount = 3;
    
    void Awake()
    {
        //parent = transform.parent;
        ViewGrid = GetComponent<LineRenderer>();
        InitSize();
        GetItemPosition();
    }

    void Start()
    {
        Debug.Log($"[DisplayStand] AreaNumber : {areaNum} - {gameObject.name}");
        RegisterAsync().Forget();
    }
    void OnDisable()
    {
        DisplayStandManager.Instance.UnregisterDisplayStand(this);
    }
    private async UniTask RegisterAsync()
    {
        await DisplayStandManager.Instance.RegisterDisplayStand(this);
    }
    void InitSize()
    {
        meshFilter = GetComponent<MeshFilter>();
        width = meshFilter.mesh.bounds.size.x;
        height = meshFilter.mesh.bounds.size.y;
        depth = meshFilter.mesh.bounds.size.z;

        widthScale = transform.localScale.x;
        heightScale = transform.localScale.y;
        depthScale = transform.localScale.z;

        width *= widthScale;
        height *= heightScale;
        depth *= depthScale;

        InitGrid();
    }
    void InitGrid()
    {
        // 피봇을 mesh의 중심으로 옮기는 오프셋 계산 
        Vector3 centerOffset = meshFilter.mesh.bounds.center;
        grid = new GridCell(width, depth, transform.position + centerOffset);
        SetViewGrid();
    }
    # region 추가 로직
    /// <summary>
    /// AreaNum을 CategoryAreaManager에 연결짓는 함수
    /// </summary>
    /// <param name="item"></param>
    public void SetAreaNum(AreaNum areaNum)
    {
        this.areaNum = areaNum;
    }

    /// <summary>
    /// DisplayStand의 width(x)를 n등분하고 아이템을 가져올 위치를 저장한다.
    /// </summary>
    private void GetItemPosition()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        float startPoint = meshFilter.mesh.bounds.min.x * widthScale;
        float dividedLength = width / ItemMaxCount;

        for (int i = 0; i < ItemMaxCount; i++)
        {
            float x = startPoint + dividedLength * i + dividedLength * 0.5f;
            Vector3 position = new Vector3(x, 0f, meshFilter.mesh.bounds.center.z);

            itemPositions.Add(position);
        }
    }
    
    # endregion
    
    
    public void SetItem(ItemDef item)
    {
        Items.Add(item);
        CategoryType = item.CategoryType;
        // 아이템 프리펩이 로드되지 않았다면 리턴(현재는 프리펩이 없으므로 이 로직을 만듬) 
        if(item.PrefabKey == null) return;
        if (MartItems.Count >= itemPositions.Count) return;

        Vector3 itemPosition = itemPositions[MartItems.Count];
        
        MartItem martItem = Instantiate
            (item.PrefabKey, 
                transform.position + new Vector3(itemPosition.x, height, itemPosition.z),
                item.PrefabKey.transform.rotation);
        martItem?.transform.SetParent(transform);
        MartItems.Add(martItem);
    }
    
    public bool IsStay(Vector3 target)
    {
        return grid.IsStay(target);
    }
    public bool RayCheck(Vector3 origin, Vector3 target)
    {
        return grid.RayCheck(origin, target);
    }
    private void SetViewGrid()
    {
        if (ViewGrid == null) return;
        if (ShowGrid == false)
        {
            ViewGrid.enabled = false;
            return;
        }
        else
        {
            ViewGrid.enabled = true;
        }


        ViewGrid.positionCount = 4;
        Vector3[] positions = new Vector3[4];
        for(int i = 0; i < 4; i++)
        {
            positions[i] = grid.GetGridCorner(i);
        }

        ViewGrid.SetPositions(positions);
    }
}
