using System.Collections.Generic;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;

namespace TeamSH
{
    // CategoreAreaManager
    // : 게임 시작할 때 서버에게 전달 받은 카테고리들을 바탕으로
    // CategoryArea(총 3개)
    public class CategoryAreaManager : MonoBehaviour
    {
        [Header("DisplayStand Prefab")] [SerializeField]
        private GameObject displayStandPrefab;

        private const int DisplayStandMaxCount = 3;
        private int displayStandCount;
        private float areaLength;
        private List<Vector3> displayStandPositions = new();

        # region 매대 호출 테스트

        private void Update()
        {
            if (SimpleInput.GetKeyDown(Key.Q))
            {
                SpawnDisplayStand();
            }
        }

        # endregion

        # region 매대 배치하기

        /// <summary>
        /// 매대를 여러 개 불러온다.
        /// </summary>
        private void SpawnDisplayStand()
        {
            GetDisplayStandLength();
            for (int i = 0; i < displayStandPositions.Count; i++)
            {
                if (displayStandCount >= DisplayStandMaxCount)
                {
                    Debug.LogWarning("이미 매대가 3개 생성되어 있습니다.");
                    return;
                }

                GameObject stand =
                    Instantiate
                    (displayStandPrefab,
                        displayStandPositions[i],
                        displayStandPrefab.transform.rotation);
                stand.transform.SetParent(transform);
                displayStandCount++;
            }
        }

        /// <summary>
        ///  n등분 배치를 하기 위해 매대의 위치를 가져온다.
        /// </summary>
        private void GetDisplayStandLength()
        {
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            areaLength = boxCollider.bounds.size.z;
            float startPoint = boxCollider.bounds.min.z;
            float dividedLength = areaLength / DisplayStandMaxCount;

            for (int i = 0; i < DisplayStandMaxCount; i++)
            {
                float z = startPoint + dividedLength * i + dividedLength * 0.5f;
                Vector3 position = new Vector3(boxCollider.bounds.center.x, 0f, z);

                displayStandPositions.Add(position);
            }
        }


        # endregion

    }
}
