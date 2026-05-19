using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitIdentity : MonoBehaviour
{
    
    public int unitID;
    public float speed;
    public bool isPlayer;
    public static List<GameObject> playerUnits = new List<GameObject>();

    void OnEnable()
    {
        if (isPlayer && !playerUnits.Contains(gameObject))
            playerUnits.Add(gameObject);
    }

    void OnDisable()
    {
        if (isPlayer)
            playerUnits.Remove(gameObject);
    }
}
