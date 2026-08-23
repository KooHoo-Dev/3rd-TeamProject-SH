using System.Collections.Generic;
using Jay;
using UnityEngine;

namespace JunWon.Scripts
{
    public class Interaction_System : SingletonBase<Interaction_System>
    {
        private List<Interaction_Common.IInteractionObserver> observerList = new List<Interaction_Common.IInteractionObserver>();

        protected override void OnInitialize()
        {
            base.OnInitialize();
        }
        
        public void Subscribe(Interaction_Common.IInteractionObserver observer)
        {
            // 두번 등록하는거 방지하기 위한 예외처리(두번 등록 실수 많이함)
            if (observerList.Contains(observer)) return;
            observerList.Add(observer);
        }
        
        public void UnSubscribe(Interaction_Common.IInteractionObserver observer)
        {
            if (observerList.Contains(observer) == false) return;
            observerList.Remove(observer);
        }
        
        // @ : event라고하는 키워드 회피용 접두사 입니다. 추천받아서 내가쓰는거임.
        public void To(Interaction_Player sender, Interaction_Player receiver, Interaction_Common.InteractionEvent @event)
        {
            if(sender == null || receiver == null)
            {
                Debug.LogWarning($"CombatSystem ::: " +
                                 $"sender : {sender != null}, receiver : {receiver != null}");
                return;
            }

            Debug.Log($"{sender.name}이 {receiver.name}에게" +
                      $", {@event.Amount}을 전달!");
        }
        
        private void HandlePushEvent(Interaction_Player sender, Interaction_Player receiver, Interaction_Common.InteractionEvent @event)
        {
            //receiver.PushOther(@event.Amount);

            //for (int i = 0; i < observerList.Count; ++i) observerList[i].OnDamageTaken(sender, receiver, @event);
        }

        // Dictionary<Collider2D, CombatEntity> 사용하셔도 무방합니다.
        // : 저는 주로 Collider를 Key값으로 사용하는데 HurtBox때문에 변경한겁니다.
        private Dictionary<Collider2D, PushableBox> HurtBoxDic { get; set; } = new();

        public void AddHurtBox(Collider2D collder, PushableBox hurtBox)
        {
            HurtBoxDic.TryAdd(collder, hurtBox);
        }

        public void RemoveHurtBox(Collider2D collder)
        {
            if (HurtBoxDic.ContainsKey(collder) == false) return;
            HurtBoxDic.Remove(collder);
        }

        public PushableBox GetHurtBoxOrNull(Collider2D collder)
        {
            HurtBoxDic.TryGetValue(collder, out PushableBox reval);
            return reval;
        }
    }
}