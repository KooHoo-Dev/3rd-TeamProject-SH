using UnityEngine;

/// <summary>
/// 임시 카테고리. 나중에 데이터 테이블 쪽에서 정의될 예정
/// </summary>
public enum AreaNum
{
    Area1,
    Area2,
    Area3,
    Area4,
}
public class DisplayStand : MonoBehaviour
{
    public float width;
    public float height;
    public float depth;
    float widthScale = 1f;
    float heightScale = 1f;
    float depthScale = 1f;
    MeshFilter meshFilter;

    GridCell grid;

    public GridCell Grid => grid ??= new GridCell(0, 0, transform.position);


    public ItemDef Item { get; private set; }

    [field: SerializeField] public AreaNum areaNum { get; private set; } = AreaNum.Area1;
    public CategoryType CategoryType { get; set; } = CategoryType.None;
    void Awake()
    {
        InitSize();
    }

    void Start()
    {
        DisplayStandManager.Instance.RegisterDisplayStand(this);
    }
    void OnDisable()
    {
        DisplayStandManager.Instance.UnregisterDisplayStand(this);
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
        grid = new GridCell(width, depth, transform.position);
    }
    public void SetItem(ItemDef item)
    {
        Item = item;
        CategoryType = item.CategoryType;
    }
    public bool IsStay(Vector3 target)
    {
        return grid.IsStay(target);
    }
}
