using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Cheat : MonoBehaviour
{
    private void Awake()
    {
        var btn = transform.GetComponentInChildren<Button>();
        var ps = GameObject.Find("Player").GetComponent<PlayerShooting>();
        btn.onClick.AddListener(() => { ps.WeaponPower++; });
    }
}
