using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class StartBattle : MonoBehaviour
{
    public void StartBattleButton()
    {
        GameState curState = GameManager.Instance.currentState;

        if (curState == GameState.Preparation)
        {
            // 1. 計算場上所有的玩家與敵人
            int playerCount = GameObject.FindGameObjectsWithTag("Player").Length;
            int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

            Debug.Log($"戰鬥開始！玩家數量：{playerCount}，敵人數量：{enemyCount}");

            // 如果場上根本沒敵人或沒玩家，直接結束（防呆）
            if (playerCount == 0 || enemyCount == 0)
            {
                GameManager.Instance.EndCombat(true);
                return;
            }

            GameManager.Instance.currentState = GameState.Combat;
        }
    }
}
