﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

#endregion

[Serializable]
public class CheckSpecialState : GamefieldState
{
    public bool IsNeedToChangeState;

    #region Event Handlers

    public override void OnEnter()
    {
        AnimatedChuzzles.Clear();
        Chuzzle.AnimationStarted += OnAnimationStarted;

        var combinations = GamefieldUtility.FindCombinations(Gamefield.Level.ActiveChuzzles);
        if (!CheckForSpecial(combinations))
        {
            Gamefield.SwitchStateTo(Gamefield.RemoveState);
        }
    }

    public override void OnExit()
    {
        if (AnimatedChuzzles.Any())
        {
            Debug.LogError("FUCK YOU: " + AnimatedChuzzles.Count);
        }
        Chuzzle.AnimationStarted -= OnAnimationStarted;
    }

    private void OnAnimationStarted(Chuzzle chuzzle)
    {
        if (!AnimatedChuzzles.Contains(chuzzle))
        {
            AnimatedChuzzles.Add(chuzzle);
            chuzzle.AnimationFinished += OnAnimationFinished;
        }
    }

    private void OnAnimationFinished(Chuzzle chuzzle)
    {
        chuzzle.AnimationFinished -= OnAnimationFinished;
        AnimatedChuzzles.Remove(chuzzle);

        if (chuzzle.IsDiying == false)
        {
            chuzzle.Destroy(true, false);
        }

        if (!AnimatedChuzzles.Any())
        {
            Gamefield.SwitchStateTo(Gamefield.CreateNewChuzzlesState);
        }
    }

    #endregion

    public override void UpdateState()
    {
    }

    public override void LateUpdateState()
    {
    }

    public bool CheckForSpecial(List<List<Chuzzle>> combinations)
    {
        var isNewSpecial = false;

        foreach (var comb in combinations)
        {
            //if any tile is powerup - then don't check for new bonuses
            //or any tile has counter
            if (comb.Any(x => !(GamefieldUtility.IsUsual(x))))
            {
                continue;
            }

            if (comb.Count == 4)
            {
                isNewSpecial = CreateLine(comb);
            }
            else
            {
                if (comb.Count >= 5)
                {
                    isNewSpecial = CreateBomb(comb);
                }
            }
        }

        return isNewSpecial;
    }

    private bool CreateBomb(List<Chuzzle> comb)
    {
        return CreateSpecialWithType(comb, TilesFactory.Instance.BombChuzzlePrefabs);
    }

    public bool CreateSpecialWithType(List<Chuzzle> ordered, GameObject[] prefabs)
    {

        var targetTile = ordered[Random.Range(0, ordered.Count)];
        foreach (var chuzzle in ordered)
        {
            if (chuzzle != targetTile)
            {
                chuzzle.AnimateMoveTo(targetTile.Current.Position); 
            }
        }                               

        var powerUp = prefabs.First(x => x.GetComponent<Chuzzle>().Color == targetTile.Color);
        TilesFactory.Instance.CreateChuzzle(targetTile.Current, powerUp);
        targetTile.Destroy(false, false);
        ordered.Remove(targetTile);

        IsNeedToChangeState = true;

        return true;
    }

    private bool CreateLine(List<Chuzzle> comb)
    {
        return CreateSpecialWithType(comb,
            Random.Range(0, 100) > 50 ? TilesFactory.Instance.HorizontalLineChuzzlePrefabs : TilesFactory.Instance.VerticalLineChuzzlePrefabs);
    }
}