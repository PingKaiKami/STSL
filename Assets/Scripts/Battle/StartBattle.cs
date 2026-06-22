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
            GameManager.Instance.RegisterInitialBattleUnits();

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
