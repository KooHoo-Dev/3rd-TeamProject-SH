using System.Collections.Generic;
using Jay;
using UnityEngine;

namespace TeamSH
{
    public class InteractionBinder
    {
        public void Init()
        {
            // NetworkManager.Instance.InteractionEvent += Interaction_System.Instance.To;
        }
    }
    
    public class Interaction_System : SingletonBase<Interaction_System>
    {
        private List<IInteractionObserver> observerList = new List<IInteractionObserver>();
        private Dictionary<Collider, PushableBox> pushableBoxDic { get; set; } = new();

        protected override void OnInitialize()
        {
            base.OnInitialize();

            // NetworkManager.Instance.PushEvent += (sendUserId, receiveUserId, power, position, direction) =>
            // {
            //     Interaction_Player sender = LiarClient.Instance.GetPlayer(sendUserId);
            //     Interaction_Player receiver = LiarClient.Instance.GetPlayer(receiveUserId);
            //
            //     InteractionEvent @event = new InteractionEvent();
            //     @event.PushPower = power;
            //     @event.Position = position;
            //     @event.HitDirection = direction;
            //
            //     To(sender, receiver, @event);
            // };
            // 
        }

        public void Subscribe(IInteractionObserver observer)
        {
            if (observerList.Contains(observer)) return;
            observerList.Add(observer);
        }

        public void UnSubscribe(IInteractionObserver observer)
        {
            if (observerList.Contains(observer) == false) return;
            observerList.Remove(observer);
        }

        public void To(Interaction_Player sender, Interaction_Player receiver, InteractionEvent @event)
        {
            if (sender == null || receiver == null)
            {
                Debug.LogWarning($"Interaction_System ::: sender : {sender != null}, receiver : {receiver != null}");
                return;
            }

            Debug.Log($"{sender.name}이(가) {receiver.name}에게 밀치기 충격량({@event.PushPower}) 전달!");
            SoundManager.Instance.PlaySfx(SoundManager.GameSfx.PlayerPushed);
            HandlePushEvent(sender, receiver, @event);
        }

        private void HandlePushEvent(Interaction_Player sender, Interaction_Player receiver, InteractionEvent @event)
        {

            if (receiver == LiarClient.Instance.Local)
            {
                receiver.ApplyKnockback(@event.HitDirection, @event.PushPower);
            }

            // 옵저버들에게 알림
            for (int i = 0; i < observerList.Count; ++i)
            {
                observerList[i].PushOther(sender, receiver, @event);
            }
        }

        #region PushableBox Dictionary 제어
        public void AddPushableBox(Collider collider, PushableBox pushableBox)
        {
            pushableBoxDic.TryAdd(collider, pushableBox);
        }

        public void RemovePushableBox(Collider collider)
        {
            if (pushableBoxDic.ContainsKey(collider) == false) return;
            pushableBoxDic.Remove(collider);
        }

        public PushableBox GetPushableBoxOrNull(Collider collider)
        {
            pushableBoxDic.TryGetValue(collider, out PushableBox reval);
            return reval;
        }
        #endregion
    }
}