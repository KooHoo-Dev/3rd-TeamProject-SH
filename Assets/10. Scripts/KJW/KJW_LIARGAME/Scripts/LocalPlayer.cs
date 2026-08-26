using TMPro;
using UnityEngine;

namespace KJW_LIARGAME.Scripts
{
    // 내가 조종하는 캐릭터. 씬에 하나 놓고 RoomClient 에 연결한다.
    // 스폰된 자리에 고정되며 움직이지 않는다.
    public class LocalPlayer : MonoBehaviour
    {
        [Header("모양")]
        [SerializeField] private TMP_Text label;

        #region LiarState

        public PlayerState PlayerState =
            new  PlayerState()
                {Id = "ID1", IsLiar = false, Items = new string[3]{"축구공","양고기","연필"}};


        #endregion


        public void Setup(User user)
        {
            PlayerState.Id = user.Id;
            label.text = $"{user.NickName} ({user.Id})";
        }
    }
}