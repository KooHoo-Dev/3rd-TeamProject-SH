using Jay;
using System.Collections.Generic;
using UnityEngine;

public class DisplayStandManager : SingletonBase<DisplayStandManager>
{
    List<DisplayStand> AllDisplayStand = new List<DisplayStand>();
    protected override void OnInitialize()
    {
        base.OnInitialize();


    }
    public void RegisterDisplayStand(DisplayStand displayStand)
    {
        if (!AllDisplayStand.Contains(displayStand))
        {
            AllDisplayStand.Add(displayStand);
        }
    }
    public void UnregisterDisplayStand(DisplayStand displayStand)
    {
        if (AllDisplayStand.Contains(displayStand))
        {
            AllDisplayStand.Remove(displayStand);
        }
    }
    /// <summary>
    /// 포지션이 어떤 스탠드 안에 있는지 모든 스탠드를 검사하고, 있다면 해당 스탠드를 반환합니다. 없다면 null을 반환합니다.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public DisplayStand CheckWhenStend(Vector3 target)
    {
        for(int i = 0; i < AllDisplayStand.Count; i++)
        {
            
            if (AllDisplayStand[i].IsStay(target))
            {
                return AllDisplayStand[i];
            }
        }
        return null;
    }

}
