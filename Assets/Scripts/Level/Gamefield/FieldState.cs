﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

#endregion

[Serializable]
public class FieldState : GamefieldState
{
    public float TimeFromTip = 0;
    public GameObject DownArrow;

    #region Direction enum

    public enum Direction
    {
        Right,
        Left,
        Top,
        Bottom
    };

    #endregion

    private Vector3 _delta;
    private Vector3 _deltaTouch;
    private bool _axisChozen;
    private Vector3 _dragOrigin;
    private Chuzzle _draggable;
    private bool _isVerticalDrag;
    public List<Chuzzle> SelectedChuzzles = new List<Chuzzle>();
    public Chuzzle CurrentChuzzle;
    public Vector3 CurrentChuzzlePrevPosition;
    public Direction CurrentDirection;

    private float _minX;
    private float _minY;
    private float _maxY;
    private float _maxX;

    public List<Chuzzle> AnimatedChuzzles = new List<Chuzzle>();

    public void UpdateState(IEnumerable<Chuzzle> draggableChuzzles)
    {
        if (isReturning)
        {
            return;
        }
        

        TimeFromTip += Time.deltaTime;
        if (TimeFromTip > 1 && !Gamefield.Level.ActiveChuzzles.Any(x => x.Shine))
        {
            IntVector2 targetPosition = null;
            Chuzzle arrowChuzzle = null;
            var possibleCombination = GamefieldUtility.Tip(Gamefield.Level.ActiveChuzzles, out targetPosition,
                out arrowChuzzle);
            if (possibleCombination.Any())
            {
                foreach (var chuzzle in possibleCombination)
                {
                    chuzzle.Shine = true;
                }
                GamefieldUtility.ShowArrow(arrowChuzzle, targetPosition, DownArrow);
            }
            else
            {
                RepaintRandom();
                return;
            }


            TimeFromTip = 0;
        }

        #region Drag

        if (CurrentChuzzle == null &&
            (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)))
        {
            _dragOrigin = Input.mousePosition;
            Debug.Log("Position: " + _dragOrigin);

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                //   Debug.Log("is touch drag started");
                _dragOrigin = new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y);
            }

            var ray = Camera.main.ScreenPointToRay(_dragOrigin);

            //Debug.Log("Ray: " + ray);
            var hit = Physics2D.Raycast(ray.origin, ray.direction, Single.MaxValue, Gamefield.ChuzzleMask);
            if (hit.transform != null)
            {
                //Debug.Log("hit: " + hit.transform.gameObject);
                var wasNull = CurrentChuzzle == null;
                CurrentChuzzle = hit.transform.gameObject.transform.parent.GetComponent<Chuzzle>();
                if (wasNull)
                {
                    _minY = GamefieldUtility.CellPositionInWorldCoordinate(
                        GamefieldUtility.MinColumnAvailiablePosition(CurrentChuzzle.Current.x,
                            Gamefield.Level.ActiveCells),
                        CurrentChuzzle.Scale).y;

                    _maxY =
                        GamefieldUtility.CellPositionInWorldCoordinate(
                            GamefieldUtility.MaxColumnAvailiablePosition(CurrentChuzzle.Current.x,
                                Gamefield.Level.ActiveCells), CurrentChuzzle.Scale).y;

                    _minX =
                        GamefieldUtility.CellPositionInWorldCoordinate(
                            GamefieldUtility.MinRowAvailiablePosition(CurrentChuzzle.Current.y,
                                Gamefield.Level.ActiveCells),
                            CurrentChuzzle.Scale).x;

                    _maxX =
                        GamefieldUtility.CellPositionInWorldCoordinate(
                            GamefieldUtility.MaxRowAvailiablePosition(CurrentChuzzle.Current.y,
                                Gamefield.Level.ActiveCells),
                            CurrentChuzzle.Scale).x;
                }
            }

            return;
        }

        // CHECK DRAG STATE (Mouse or Touch)
        if ((!Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)) && 0 == Input.touchCount)
        {
            DropDrag();
            return;
        }

        if (CurrentChuzzle == null)
        {
            return;
        }


        if (Input.GetMouseButton(0)) // Get Position Difference between Last and Current Touches
        {
            // MOUSE
            _delta = Camera.main.ScreenToWorldPoint(Input.mousePosition) - Camera.main.ScreenToWorldPoint(_dragOrigin);
        }
        else
        {
            if (Input.touchCount > 0)
            {
                // TOUCH
                _deltaTouch =
                    Camera.main.ScreenToWorldPoint(new Vector3(Input.GetTouch(0).position.x,
                        Input.GetTouch(0).position.y, 0));
                _delta = _deltaTouch - Camera.main.ScreenToWorldPoint(_dragOrigin);
            }
        }

        //Debug.Log("Delta: " + _delta);
        _delta = Vector3.ClampMagnitude(_delta, 0.45f*CurrentChuzzle.Scale.x);

        if (!_axisChozen)
        {
            //chooze drag direction
            if (Mathf.Abs(_delta.x) < 1.5*Mathf.Abs(_delta.y) || Mathf.Abs(_delta.x) > 1.5*Mathf.Abs(_delta.y))
            {
                if (Mathf.Abs(_delta.x) < Mathf.Abs(_delta.y))
                {
                    SelectedChuzzles = draggableChuzzles.Where(x => x.Current.x == CurrentChuzzle.Current.x).ToList();
                    _isVerticalDrag = true;
                }
                else
                {
                    SelectedChuzzles = draggableChuzzles.Where(x => x.Current.y == CurrentChuzzle.Current.y).ToList();
                    _isVerticalDrag = false;
                }

                _axisChozen = true;
                //Debug.Log("Direction chozen. Vertical: " + _isVerticalDrag);
            }
        }

        if (_axisChozen)
        {
            if (_isVerticalDrag)
            {
                CurrentDirection = _delta.y > 0 ? Direction.Top : Direction.Bottom;
                _delta.z = _delta.x = 0;
            }
            else
            {
                CurrentDirection = _delta.x > 0 ? Direction.Right : Direction.Left;
                _delta.y = _delta.z = 0;
            }
        }

        // RESET START POINT
        _dragOrigin = Input.mousePosition;

        #endregion
    }

    private void RepaintRandom()
    {
        Debug.Log("Random repaint");
        var randomChuzzle = Gamefield.Level.Chuzzles[Random.Range(0, Gamefield.Level.Chuzzles.Count)];

        Gamefield.Level.CreateRandomChuzzle(randomChuzzle.Current.x, randomChuzzle.Current.y, true);

        Object.Destroy(randomChuzzle.gameObject);
        Gamefield.Level.ActiveChuzzles.Remove(randomChuzzle);
        Gamefield.Level.Chuzzles.Remove(randomChuzzle);
    }

    public void LateUpdateState(List<Cell> activeCells)
    {
        if (isReturning)
        {
            foreach (var selectedChuzzle in SelectedChuzzles)
            {
                selectedChuzzle.transform.position += selectedChuzzle.Velocity*Time.deltaTime;
            }

            if (_isVerticalDrag)
            {
                CurrentDirection = CurrentChuzzle.Velocity.y > 0 ? Direction.Top : Direction.Bottom;
            }
            else
            {
                CurrentDirection = CurrentChuzzle.Velocity.x > 0 ? Direction.Right : Direction.Left;  
            }

            MoveChuzzles(activeCells);

            bool isAllOnPosition = true;
            foreach (var selectedChuzzle in SelectedChuzzles)
            {
                if (Vector3.Distance(
                    selectedChuzzle.transform.position,
                    GamefieldUtility.CellPositionInWorldCoordinate(selectedChuzzle.MoveTo, selectedChuzzle.Scale)
                    ) < 0.05f)
                {
                    selectedChuzzle.Velocity = Vector3.zero;
                    selectedChuzzle.transform.position = GamefieldUtility.CellPositionInWorldCoordinate(selectedChuzzle.MoveTo, selectedChuzzle.Scale);
                }
                else
                {
                    isAllOnPosition = false;
                }
            }
            if (isAllOnPosition)
            {
                Reset();
            }
            return;
        }

        if (SelectedChuzzles.Any() && _axisChozen)
        {
            var pos = CurrentChuzzle.transform.position;

            //clamp drag
            if (_isVerticalDrag)
            {
                if (CurrentDirection == Direction.Top && Math.Abs(pos.y - _maxY) < 0.01f)
                {
                    return;
                }

                if (CurrentDirection == Direction.Bottom && Math.Abs(pos.y - _minY) < 0.01f)
                {
                    return;
                }


                var maybePosition = CurrentChuzzle.transform.position.y + _delta.y;
                var clampPosition = Mathf.Clamp(maybePosition, _minY, _maxY);

                if (Math.Abs(maybePosition - clampPosition) > 0.001f)
                {
                    _delta.y = clampPosition - maybePosition;
                }
            }
            else
            {
                if (CurrentDirection == Direction.Right && Math.Abs(pos.x - _maxX) < 0.01f)
                {
                    return;
                }

                if (CurrentDirection == Direction.Left && Math.Abs(pos.x - _minX) < 0.01f)
                {
                    return;
                }

                var maybePosition = CurrentChuzzle.transform.position.x + _delta.x;
                var clampPosition = Mathf.Clamp(maybePosition, _minX, _maxX);

                if (Math.Abs(maybePosition - clampPosition) > 0.001f)
                {
                    _delta.x = clampPosition - maybePosition;
                }
            }

            foreach (var c in SelectedChuzzles)
            {
                c.transform.position += _delta;
            }

            MoveChuzzles(activeCells);
        }
    }

    private void MoveChuzzles(List<Cell> activeCells)
    {
        foreach (var c in SelectedChuzzles)
        {   
            var copyPosition = c.transform.position;

            var real = GamefieldUtility.ToRealCoordinates(c);
            var targetCell = GamefieldUtility.CellAt(activeCells, real.x, real.y);

            var difference = c.transform.position - GamefieldUtility.ConvertXYToPosition(real.x, real.y, c.Scale);

            var isNeedCopy = false;

                if (targetCell != null && !targetCell.IsTemporary)
                {
                    if (!_isVerticalDrag)
                    {
                        if (difference.x > 0)
                        {
                            isNeedCopy = targetCell.Right == null ||
                                         (targetCell.Right != null && targetCell.Right.Type != CellTypes.Usual);
                            if (isNeedCopy)
                            {
                                var rightCell = GetRightCell(activeCells, targetCell.Right, c);
                                copyPosition = GamefieldUtility.CellPositionInWorldCoordinate(rightCell, c.Scale) +
                                               difference - new Vector3(c.Scale.x, 0, 0);
                            }
                        }
                        else
                        {
                            isNeedCopy = targetCell.Left == null ||
                                         (targetCell.Left != null && targetCell.Left.Type != CellTypes.Usual);
                            if (isNeedCopy)
                            {
                                var leftCell = GetLeftCell(activeCells, targetCell.Left, c);
                                copyPosition =
                                    GamefieldUtility.ConvertXYToPosition(leftCell.x, leftCell.y, c.Scale) +
                                    difference + new Vector3(c.Scale.x, 0, 0);
                            }
                        }
                    }
                    else
                    {
                        if (difference.y > 0)
                        {
                            isNeedCopy = targetCell.Top == null ||
                                         (targetCell.Top != null &&
                                          (targetCell.Top.Type == CellTypes.Block || targetCell.Top.IsTemporary));
                            if (isNeedCopy)
                            {
                                var topCell = GetTopCell(activeCells, targetCell.Top, c);
                                copyPosition = GamefieldUtility.ConvertXYToPosition(topCell.x, topCell.y, c.Scale) +
                                               difference - new Vector3(0, c.Scale.y, 0);
                            }
                        }
                        else
                        {
                            isNeedCopy = targetCell.Bottom == null ||
                                         (targetCell.Bottom != null && targetCell.Bottom.Type == CellTypes.Block);
                            if (isNeedCopy)
                            {
                                var bottomCell = GetBottomCell(activeCells, targetCell.Bottom, c);
                                copyPosition =
                                    GamefieldUtility.ConvertXYToPosition(bottomCell.x, bottomCell.y, c.Scale) +
                                    difference + new Vector3(0, c.Scale.y, 0);
                            }
                        }
                    }
                }
                else
                {
                    isNeedCopy = true;
                }

                if (targetCell == null || targetCell.Type == CellTypes.Block || targetCell.IsTemporary)
                {
                    switch (CurrentDirection)
                    {
                        case Direction.Left:
                            //if border
                            targetCell = GetLeftCell(activeCells, targetCell, c);
                            break;
                        case Direction.Right:
                            targetCell = GetRightCell(activeCells, targetCell, c);
                            break;
                        case Direction.Top:
                            //if border
                            targetCell = GetTopCell(activeCells, targetCell, c);
                            break;
                        case Direction.Bottom:
                            targetCell = GetBottomCell(activeCells, targetCell, c);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("Current direction can not be shit");
                    }

                c.transform.position = GamefieldUtility.CellPositionInWorldCoordinate(targetCell, c.Scale) +
                                       difference;

                // Debug.Log("New coord: "+GamefieldUtility.ToRealCoordinates(c)+" for "+c.gameObject.name + " pos: "+c.transform.position);
            }

            if (difference.magnitude < (CurrentChuzzle.Scale.x/25))
            {
                isNeedCopy = false;
            }

                if (isNeedCopy)
                {
                    var teleportable = c.GetComponent<TeleportableEntity>();
                    if (teleportable != null)
                    {
                        if (!teleportable.HasCopy)
                        {
                            teleportable.CreateCopy();
                        }
                        //Debug.Log("Copyied: " + teleportable);
                        teleportable.Copy.transform.position = copyPosition;
                    }
                }
                else
                {
                    var teleportable = c.GetComponent<TeleportableEntity>();
                    if (teleportable != null)
                    {
                        if (teleportable.HasCopy)
                        {
                            teleportable.DestroyCopy();
                    }
                }
            }
        }
    }

    private static Cell GetBottomCell(List<Cell> activeCells, Cell targetCell, Chuzzle c)
    {
//if border
        if (targetCell == null)
        {
            targetCell = GamefieldUtility.CellAt(activeCells, c.Current.x,
                activeCells.Where(x => !x.IsTemporary).Max(x => x.y));
            if (targetCell.Type == CellTypes.Block)
            {
                targetCell = targetCell.GetBottomWithType();
            }
        }
        else
        {
            targetCell = targetCell.GetBottomWithType();

            if (targetCell == null)
            {
                targetCell = GamefieldUtility.CellAt(activeCells, c.Current.x,
                    activeCells.Where(x => !x.IsTemporary).Max(x => x.y));
                if (targetCell.Type == CellTypes.Block)
                {
                    targetCell = targetCell.GetBottomWithType();
                }
            }
        }
        return targetCell;
    }

    private static Cell GetTopCell(List<Cell> activeCells, Cell targetCell, Chuzzle c)
    {
        if (targetCell == null || targetCell.IsTemporary)
        {
            targetCell = GamefieldUtility.CellAt(activeCells, c.Current.x,
                activeCells.Where(x => !x.IsTemporary).Min(x => x.y));
            if (targetCell.Type == CellTypes.Block)
            {
                targetCell = targetCell.GetTopWithType();
            }
        }
        else
        {
            targetCell = targetCell.GetTopWithType();

            if (targetCell == null)
            {
                targetCell = GamefieldUtility.CellAt(activeCells, c.Current.x,
                    activeCells.Where(x => !x.IsTemporary).Min(x => x.y));
                if (targetCell.Type == CellTypes.Block)
                {
                    targetCell = targetCell.GetTopWithType();
                }
            }
        }
        return targetCell;
    }

    private static Cell GetRightCell(List<Cell> activeCells, Cell targetCell, Chuzzle c)
    {
//if border
        if (targetCell == null)
        {
            targetCell = GamefieldUtility.CellAt(activeCells, activeCells.Min(x => x.x), c.Current.y);
            if (targetCell.Type == CellTypes.Block)
            {
                targetCell = targetCell.GetRightWithType();
            }
        }
        else
        {
            targetCell = targetCell.GetRightWithType();
            if (targetCell == null)
            {
                targetCell = GamefieldUtility.CellAt(activeCells, activeCells.Min(x => x.x),
                    c.Current.y);
                if (targetCell.Type == CellTypes.Block)
                {
                    targetCell = targetCell.GetRightWithType();
                }
            }
        }
        return targetCell;
    }

    private static Cell GetLeftCell(List<Cell> activeCells, Cell targetCell, Chuzzle c)
    {
        if (targetCell == null)
        {
            targetCell = GamefieldUtility.CellAt(activeCells, activeCells.Max(x => x.x), c.Current.y);
            if (targetCell.Type == CellTypes.Block)
            {
                targetCell = targetCell.GetLeftWithType();
            }
        }
        else
        {
            //if block
            targetCell = targetCell.GetLeftWithType();

            if (targetCell == null)
            {
                targetCell = GamefieldUtility.CellAt(activeCells, activeCells.Max(x => x.x),
                    c.Current.y);
                if (targetCell.Type == CellTypes.Block)
                {
                    targetCell = targetCell.GetLeftWithType();
                }
            }
        }
        return targetCell;
    }

    private void DropDrag()
    {
        if (SelectedChuzzles.Any())
        {
            //drop shining
            foreach (var chuzzle in Gamefield.Level.ActiveChuzzles)
            {
                chuzzle.Shine = false;
            }

            //move all tiles to new real coordinates
            foreach (var chuzzle in SelectedChuzzles)
            {
                chuzzle.Real = Gamefield.Level.GetCellAt(
                    Mathf.RoundToInt(chuzzle.transform.position.x/chuzzle.Scale.x),
                    Mathf.RoundToInt(chuzzle.transform.position.y/chuzzle.Scale.y),
                    false);
                chuzzle.GetComponent<TeleportableEntity>().DestroyCopy();
            }

            foreach (var c in Gamefield.Level.Chuzzles)
            {
                c.MoveTo = c.Real;
            }

            var anyMove = MoveAllChuzzlesToMoveToPosition(Gamefield.Level.Chuzzles);
            if (!anyMove)
            {
                OnChuzzleCompletedTweens();
            }
        }
    }

    public void Reset()
    {
        SelectedChuzzles.Clear();
        CurrentChuzzle = null;
        _axisChozen = false;
        _isVerticalDrag = false;
        isReturning = false;
    }

    public override void OnEnter()
    {
    }

    public override void OnExit()
    {
    }

    public override void UpdateState()
    {
        if (!AnimatedChuzzles.Any())
        {
            UpdateState(Gamefield.Level.ActiveChuzzles);
        }
    }

    public override void LateUpdateState()
    {
        if (!AnimatedChuzzles.Any())
        {
            LateUpdateState(Gamefield.Level.ActiveCells);
        }
    }

    private bool isReturning;

    public FieldState(bool isReturning)
    {
        this.isReturning = isReturning;
    }

    public void OnChuzzleCompletedTweens()
    {
        var combinations = GamefieldUtility.FindCombinations(Gamefield.Level.ActiveChuzzles);
        if (combinations.Any())
        {
            foreach (var c in Gamefield.Level.Chuzzles)
            {               
                c.MoveTo = c.Current = c.Real;
            }
            Gamefield.SwitchStateTo(Gamefield.CheckSpecialState);

            Gamefield.GameMode.HumanTurn();

            Reset();
        }
        else
        {
            //Debug.Log("Current chuzzle:"+CurrentChuzzle);
            var velocity = -3f* (
                GamefieldUtility.CellPositionInWorldCoordinate(CurrentChuzzle.Real, CurrentChuzzle.Scale) - 
                GamefieldUtility.CellPositionInWorldCoordinate(CurrentChuzzle.Current, CurrentChuzzle.Scale));
            //Debug.Log("V:"+velocity);
            foreach (var c in SelectedChuzzles)
            {
                c.MoveTo = c.Real = c.Current;
                c.Velocity = velocity;      
            }
            
            isReturning = true;
        }     
    }     
    private bool Move(Chuzzle c, Action<object> callback)
    {   
        var isMove = false;
        var cell = c.MoveTo;
        var targetPosition = GamefieldUtility.CellPositionInWorldCoordinate(cell, c.Scale);
        if (Vector3.Distance(c.transform.position, targetPosition) > 0.1f)
        {
            isMove = true;
            AnimatedChuzzles.Add(c);
            iTween.MoveTo(c.gameObject,
                iTween.Hash("x", targetPosition.x, "y", targetPosition.y, "z", targetPosition.z, "time", 0.3f,
                    "oncomplete", callback, "oncompletetarget", Gamefield.gameObject,
                    "oncompleteparams", c));
        }
        else
        {
            c.transform.position = targetPosition;
        }
        return isMove;
    }

    public bool MoveAllChuzzlesToMoveToPosition(List<Chuzzle> targetChuzzles)
    {
        bool anyMove = false;
        foreach (Chuzzle chuzzle in targetChuzzles)
        {
            var isMove = Move(chuzzle, OnTweenMoveAfterDrag);
            if (isMove)
            {
                anyMove = true;
            }
        }
        return anyMove;
    }

    private void OnTweenMoveAfterDrag(object chuzzleObject)
    {
        var chuzzle = chuzzleObject as Chuzzle;

        if (AnimatedChuzzles.Contains(chuzzle))
        {
            AnimatedChuzzles.Remove(chuzzle);
        }

        if (!AnimatedChuzzles.Any())
        {
            OnChuzzleCompletedTweens();
        }
    }
}