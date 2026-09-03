using UnityEngine;
using System.Collections.Generic;

// AI관련 닉네임 색상 변경 스크립트

namespace TeamSH
{
    [System.Serializable]
    public class UserInfo
    {
        public string nickname;
        public Color color;

        public UserInfo(string nickname)
        {
            this.nickname = nickname;
        }
    }

    public class UserColorAssigner : MonoBehaviour
    {
        public List<UserInfo> users = new List<UserInfo>();

        void Start()
        {
            // 예시 유저 배열 (닉네임만 있는 상태)
            string[] nicknames = { "철수", "영희", "민수", "지영" };

            // 유저 정보 생성
            foreach (string name in nicknames)
            {
                users.Add(new UserInfo(name));
            }

            // 유저 수만큼 랜덤 색상 뽑아서 저장
            AssignRandomColors();

            // 각 유저 닉네임에 색상 정보 반영해서 출력
            ApplyColorToNickname();
        }

        void AssignRandomColors()
        {
            foreach (UserInfo user in users)
            {
                Color randomColor = new Color(
                    Random.value, // R
                    Random.value, // G
                    Random.value // B
                );

                user.color = randomColor;
            }
        }

        void ApplyColorToNickname()
        {
            foreach (UserInfo user in users)
            {
                // 색상을 16진수 코드로 변환 (예: #A1B2C3)
                string hexColor = ColorUtility.ToHtmlStringRGB(user.color);

                // 닉네임 앞에 색상 태그 붙이기 (TextMeshPro/UGUI Rich Text용)
                string coloredNickname = $"<color=#{hexColor}>{user.nickname}</color>";

                Debug.Log($"원본 닉네임: {user.nickname} / 색상 적용: {coloredNickname}");
            }
        }
    }
}