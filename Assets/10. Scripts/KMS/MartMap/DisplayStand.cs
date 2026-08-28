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


    public ItemDef Item { get; private set; }

    public MartItem MartItem { get; private set; }

    [field: SerializeField] public AreaNum areaNum { get; private set; } = AreaNum.Area1;
    public CategoryType CategoryType { get; set; } = CategoryType.None;
    void Awake()
    {
        //parent = transform.parent;
        ViewGrid = GetComponent<LineRenderer>();
        InitSize();
    }

    void Start()
    {
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
    public void SetItem(ItemDef item)
    {
        Item = item;
        CategoryType = item.CategoryType;
        // 아이템 프리펩이 로드되지 않았다면 리턴(현재는 프리펩이 없으므로 이 로직을 만듬) 
        if(item.PrefabKey == null) return;

                                                                // 임의로 중심에서 대충 위로 띄운 리스폰 포인트임
        MartItem = Instantiate(item.PrefabKey, transform.position + new Vector3(0, 1.5f, 0), item.PrefabKey.transform.rotation);
        MartItem?.transform.SetParent(transform);

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
