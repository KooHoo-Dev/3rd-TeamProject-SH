using UnityEngine;
using UnityEngine.Serialization;

namespace TeamSH
{
    /// <summary>
    /// 임시로 지정해 놓은 아이템 클래스. 나중에 아이템의 속성이나 기능이 추가될 수 있음.
    /// </summary>
    public class MartItem : MonoBehaviour
    {
        private DisplayStand displayStand; // 집에서 ID기반으로 변경해서 DisplayStandManager를 통하여 RequestActiveItem이 일어나게 하도록 수정할 것
        private int itemIndex = -1; //매대 아이템 리스트 인덱스
        public ItemDef itemDef;

        public void SetItemInfo(DisplayStand displayStand, ItemDef itemDef, int index)
        {
            this.displayStand = displayStand;
            this.itemDef = itemDef;
            this.itemIndex = index;
        }

        public void RequestActiveItem()
        {
            displayStand.RequestActiveItem(itemIndex);
        }
    }
}