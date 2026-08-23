using UnityEngine;
/// <summary>
/// 자신의 2차원 범위 안에 들어와 있는지 확인하는 클래스 
/// </summary>
public class GridCell
{
    private float halfWidth;
    private float halfHeight;
    private Vector3 centerPosition;
    private float boundaryMinX;
    private float boundaryMinZ;
    private float boundaryMaxX;
    private float boundaryMaxZ;

    public GridCell(float width, float height, Vector3 centerPosition)
    {
        this.halfWidth = width / 2f;
        this.halfHeight = height / 2f;
        this.centerPosition = centerPosition;
        this.boundaryMinX = centerPosition.x - halfWidth;
        this.boundaryMaxX = centerPosition.x + halfWidth;
        this.boundaryMinZ = centerPosition.z - halfHeight;
        this.boundaryMaxZ = centerPosition.z + halfHeight;
    }
    /// <summary>
    /// 2차원 범위 안에 들어와 있는지 확인하는 메서드
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool IsStay(Vector3 target)
    {
        if(halfHeight == 0 && halfWidth == 0)
        {
            Debug.LogWarning("그리드의 크기가 0입니다. 그리드 위치: " + centerPosition);
            return false;
        }
        if (target.x >= boundaryMinX && target.x <= boundaryMaxX &&
           target.z >= boundaryMinZ && target.z <= boundaryMaxZ)
        {
            return true;
        }
        else
        {
            return false;
        }
    }




}
