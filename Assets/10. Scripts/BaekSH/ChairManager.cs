using TeamSH;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BaekSH
{
    // 회의실 캐릭터 배치 코드
    public class ChairManager : MonoBehaviour
    {
        [Header("SittingPoints")] 
        [SerializeField] private Transform[] sittingPoints;
        
        # region 캐릭터 배치 임시 테스트 코드
        
        [SerializeField] private GameObject character;
        private void Update()
        {
            if (SimpleInput.GetKeyDown(Key.Digit1))
            {
                Sit(character, 0);
            }
            if (SimpleInput.GetKeyDown(Key.Digit2))
            {
                Sit(character, 1);
            }
            if (SimpleInput.GetKeyDown(Key.Digit3))
            {
                Sit(character, 2);
            }
            if (SimpleInput.GetKeyDown(Key.Digit4))
            {
                Sit(character, 3);
            }
        }
        # endregion
        
        // Sitting Point의 좌표를 가져온다.
        public Transform GetSittingPoint(int index)
        {
            if (index < 0 || index >= sittingPoints.Length)
            {
                Debug.LogError("index를 벗어난 좌석입니다.");
                return null;
            }

            return sittingPoints[index];
        }

        // 캐릭터를 앉힌다.
        public void Sit(GameObject character, int seatIndex)
        {
            if (character == null)
            {
                Debug.LogError("character가 존재하지 않습니다.");
                return;
            }

            Transform sittingPoint = GetSittingPoint(seatIndex);

            if (sittingPoint == null) return;

            character.transform.SetPositionAndRotation(sittingPoint.position, sittingPoint.rotation);

            // character에 달린 애니메이터를 가져온다 -> sit 연결 부탁드립니다.
            PlayerController animator = character.GetComponent<PlayerController>();
            animator.ApplyMeetingPlayer();
        }

    }
}