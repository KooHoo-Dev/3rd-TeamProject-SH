using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TeamSH
{
    // 내가 조종하는 캐릭터. 씬에 하나 놓고 RoomClient 에 연결한다.
    // 스폰된 자리에 고정되며 움직이지 않는다.
    public class LocalPlayer : MonoBehaviour
    {
        [Header("모양")]
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private TMP_Text label;

        [Header("움직임")]
        [SerializeField] private float speed = 5f;

        public bool CanMove { get; set; } = true;

        public void Setup(User user)
        {
            label.text = $"{user.NickName} ({user.Id})";

            // 씬이 바껴도 삭제 X
            DontDestroyOnLoad(gameObject);
        }

        /*
        private void Update()
        {
            if (CanMove == false) return;

            Keyboard keyboard = Keyboard.current;

            float x = (keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed ? 1 : 0);
            float y = (keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.downArrowKey.isPressed ? 1 : 0);

            transform.Translate(new Vector3(x, y) * (speed * Time.deltaTime));
        }
        */
    }
}
