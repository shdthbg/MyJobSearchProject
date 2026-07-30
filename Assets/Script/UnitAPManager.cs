using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAPManager : MonoBehaviour
{
    public int currentAP;
    public int maxAP = 3;
    private UnitIdentity identity;
    void Awake()
    {
        identity = GetComponent<UnitIdentity>();
    }
    public void ResetAP()
    {
        currentAP = maxAP;
        NotifyAPChanged();
    }
    public bool TrySpendAP(int cost)
    {
        if (currentAP >= cost)
        {
            currentAP -= cost;
            NotifyAPChanged();
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool IsAPExhausted()
    {
        if(currentAP == 0) return true;
        else return false;
    }
    private void NotifyAPChanged()
    {
        if(identity != null)
        {
            EventBus.EventTrigger(E_EventType.APChanged,new UnitAPData{
                unitID = identity.unitID,
                currentAP = currentAP,
                maxAP = maxAP
            });
        }
    }
}
