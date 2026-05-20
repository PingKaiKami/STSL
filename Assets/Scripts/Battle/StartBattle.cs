using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;

public class StartBattle : MonoBehaviour
{
    public void StartBattleButton()
    {
        GameState curState = GameManager.Instance.currentState;

        if (curState == GameState.Preparation)
        {
            // 1. 計算場上所有的玩家與敵人
            GameManager.Instance.playerCount = GameObject.FindGameObjectsWithTag("Player").Length;
            GameManager.Instance.enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

            Debug.Log($"戰鬥開始！玩家數量：{GameManager.Instance.playerCount}，敵人數量：{GameManager.Instance.enemyCount}");

            // 如果場上根本沒敵人或沒玩家，直接結束（防呆）
            if (GameManager.Instance.playerCount == 0 || GameManager.Instance.enemyCount == 0)
            {
                GameManager.Instance.EndCombat(true);
                return;
            }

            GameManager.Instance.currentState = GameState.Combat;
        }
    }
}
