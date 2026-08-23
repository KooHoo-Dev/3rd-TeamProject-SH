using UnityEngine;

/// <summary>
/// 임시 카테고리. 나중에 데이터 테이블 쪽에서 정의될 예정
/// </summary>
public enum ItemCategory
{
    Food,
    Drink,
    Snack,
    Household,
    Electronics,
    Clothing,
    Other
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

    Grid grid;

    public Grid Grid => grid ??= new Grid(0, 0, transform.position);

    // 임시로 아이템 타입을 Item으로 지정. 나중에 새 아이템 클래스가 정의되면 해당 클래스로 변경될 예정
    public MartItem Item { get; private set; }

    [field: SerializeField] public ItemCategory category { get; private set; } = ItemCategory.Other;
    void Awake()
    {
        InitSize();
    }
    private void OnEnable()
    {
        if (grid != null)
            DisplayStandManager.Instance.RegisterDisplayStand(this);
    }
    void Start()
    {
        if(grid != null)
        DisplayStandManager.Instance.RegisterDisplayStand(this);
    }
    void OnDestroy()
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
        grid = new Grid(width, depth, transform.position);
    }

    public bool IsStay(Vector3 target)
    {
        return grid.IsStay(target);
    }
}
