using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAPManager : MonoBehaviour
{
    public int currentAP;
    public int maxAP = 3;
    public void ResetAP()
    {
        currentAP = maxAP;
    }
    public bool TrySpendAP(int cost)
    {
        if (currentAP >= cost)
        {
            currentAP -= cost;
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
}
