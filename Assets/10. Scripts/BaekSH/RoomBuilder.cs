using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RoomBuilder : MonoBehaviour
{
    [Header("Room Size")]
    [SerializeField] private int width = 4;
    [SerializeField] private int depth = 4;

    [Header("Prefabs")]
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject wallPrefab;

    [Header("Wall Settings")]
    [SerializeField] private bool createFrontWall = false;

    // 벽의 보이는 면이 반대라면 180으로 변경
    [SerializeField] private float wallRotationOffset = 0f;

    private GameObject roomRoot;

    private const string ROOM_ROOT_NAME = "GeneratedRoom";


    // ==================================================
    // Generate
    // ==================================================

    [ContextMenu("Generate Room")]
    public void GenerateRoom()
    {
        if (floorPrefab == null)
        {
            Debug.LogError(
                "[RoomBuilder] Floor Prefab을 넣어주세요."
            );
            return;
        }

        if (wallPrefab == null)
        {
            Debug.LogError(
                "[RoomBuilder] Wall Prefab을 넣어주세요."
            );
            return;
        }

        if (width <= 0 || depth <= 0)
        {
            Debug.LogError(
                "[RoomBuilder] Width와 Depth는 1 이상이어야 합니다."
            );
            return;
        }

        ClearRoom();


        roomRoot = new GameObject(
            ROOM_ROOT_NAME
        );

        roomRoot.transform.SetParent(
            transform
        );

        roomRoot.transform.localPosition =
            Vector3.zero;

        roomRoot.transform.localRotation =
            Quaternion.identity;

        roomRoot.transform.localScale =
            Vector3.one;


        Vector3 floorSize =
            GetPrefabSize(
                floorPrefab
            );

        float floorWidth =
            floorSize.x;

        float floorDepth =
            floorSize.z;


        CreateFloor(
            floorWidth,
            floorDepth
        );

        CreateWalls(
            floorWidth,
            floorDepth
        );
    }


    // ==================================================
    // Floor
    // ==================================================

    private void CreateFloor(
        float floorWidth,
        float floorDepth)
    {
        GameObject floorRoot =
            new GameObject(
                "Floor"
            );

        floorRoot.transform.SetParent(
            roomRoot.transform
        );

        floorRoot.transform.localPosition =
            Vector3.zero;

        floorRoot.transform.localRotation =
            Quaternion.identity;

        floorRoot.transform.localScale =
            Vector3.one;


        float roomWidth =
            width * floorWidth;

        float roomDepth =
            depth * floorDepth;


        float startX =
            -roomWidth * 0.5f
            + floorWidth * 0.5f;

        float startZ =
            -roomDepth * 0.5f
            + floorDepth * 0.5f;


        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject floor =
                    Instantiate(
                        floorPrefab,
                        floorRoot.transform
                    );


                floor.name =
                    $"Floor_{x}_{z}";


                floor.transform.localPosition =
                    new Vector3(
                        startX
                        + x * floorWidth,

                        0f,

                        startZ
                        + z * floorDepth
                    );


                floor.transform.localRotation =
                    Quaternion.identity;

                floor.transform.localScale =
                    Vector3.one;
            }
        }
    }


    // ==================================================
    // Walls
    // ==================================================

    private void CreateWalls(
        float floorWidth,
        float floorDepth)
    {
        GameObject wallRoot =
            new GameObject(
                "Walls"
            );

        wallRoot.transform.SetParent(
            roomRoot.transform
        );

        wallRoot.transform.localPosition =
            Vector3.zero;

        wallRoot.transform.localRotation =
            Quaternion.identity;

        wallRoot.transform.localScale =
            Vector3.one;


        float roomWidth =
            width * floorWidth;

        float roomDepth =
            depth * floorDepth;


        float halfWidth =
            roomWidth * 0.5f;

        float halfDepth =
            roomDepth * 0.5f;


        // ==================================================
        // Back
        // ==================================================

        float startX =
            -halfWidth
            + floorWidth * 0.5f;


        for (int x = 0; x < width; x++)
        {
            Vector3 position =
                new Vector3(
                    startX
                    + x * floorWidth,

                    0f,

                    halfDepth
                );


            CreateWall(
                wallRoot.transform,
                $"Wall_Back_{x}",
                position,
                180f
            );
        }


        // ==================================================
        // Left / Right
        // ==================================================

        float startZ =
            -halfDepth
            + floorDepth * 0.5f;


        for (int z = 0; z < depth; z++)
        {
            float zPosition =
                startZ
                + z * floorDepth;


            // Left
            CreateWall(
                wallRoot.transform,
                $"Wall_Left_{z}",
                new Vector3(
                    -halfWidth,
                    0f,
                    zPosition
                ),
                90f
            );


            // Right
            CreateWall(
                wallRoot.transform,
                $"Wall_Right_{z}",
                new Vector3(
                    halfWidth,
                    0f,
                    zPosition
                ),
                -90f
            );
        }


        // ==================================================
        // Front
        // ==================================================

        if (createFrontWall)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 position =
                    new Vector3(
                        startX
                        + x * floorWidth,

                        0f,

                        -halfDepth
                    );


                CreateWall(
                    wallRoot.transform,
                    $"Wall_Front_{x}",
                    position,
                    0f
                );
            }
        }
    }


    // ==================================================
    // Create One Wall
    // ==================================================

    private void CreateWall(
        Transform parent,
        string wallName,
        Vector3 targetLocalPosition,
        float rotationY)
    {
        GameObject wall =
            Instantiate(
                wallPrefab,
                parent
            );


        wall.name =
            wallName;


        wall.transform.localScale =
            Vector3.one;


        wall.transform.localRotation =
            Quaternion.Euler(
                0f,
                rotationY
                + wallRotationOffset,
                0f
            );


        // 우선 Pivot을 목표 위치에 둔다.
        wall.transform.localPosition =
            targetLocalPosition;


        // ==================================================
        // Pivot 보정
        //
        // Prefab의 Transform Pivot과
        // 실제 Mesh 중심이 다를 수 있으므로
        // Renderer Bounds 중심을 실제 목표 위치에 맞춘다.
        // ==================================================

        Renderer[] renderers =
            wall.GetComponentsInChildren<Renderer>(
                true
            );


        if (renderers.Length == 0)
        {
            Debug.LogWarning(
                $"[RoomBuilder] {wallName}에 Renderer가 없습니다."
            );
            return;
        }


        Bounds wallBounds =
            renderers[0].bounds;


        for (int i = 1;
             i < renderers.Length;
             i++)
        {
            wallBounds.Encapsulate(
                renderers[i].bounds
            );
        }


        Vector3 targetWorldPosition =
            parent.TransformPoint(
                targetLocalPosition
            );


        Vector3 correction =
            targetWorldPosition
            - wallBounds.center;


        // 벽 높이(Y)는 Prefab 원래 Pivot을 유지
        correction.y = 0f;


        wall.transform.position +=
            correction;
    }


    // ==================================================
    // Get Prefab Size
    // ==================================================

    private Vector3 GetPrefabSize(
        GameObject prefab)
    {
        Renderer[] renderers =
            prefab.GetComponentsInChildren<Renderer>(
                true
            );


        if (renderers.Length == 0)
        {
            Debug.LogError(
                $"[RoomBuilder] {prefab.name}에 Renderer가 없습니다."
            );

            return Vector3.one;
        }


        Bounds bounds =
            renderers[0].bounds;


        for (int i = 1;
             i < renderers.Length;
             i++)
        {
            bounds.Encapsulate(
                renderers[i].bounds
            );
        }


        return bounds.size;
    }


    // ==================================================
    // Clear
    // ==================================================

    [ContextMenu("Clear Room")]
    public void ClearRoom()
    {
        Transform oldRoom =
            transform.Find(
                ROOM_ROOT_NAME
            );


        if (oldRoom == null)
        {
            roomRoot = null;
            return;
        }


#if UNITY_EDITOR

        if (Application.isPlaying == false)
        {
            Undo.DestroyObjectImmediate(
                oldRoom.gameObject
            );
        }
        else
        {
            Destroy(
                oldRoom.gameObject
            );
        }

#else

        Destroy(
            oldRoom.gameObject
        );

#endif


        roomRoot = null;
    }
}