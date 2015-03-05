using UnityEngine;
using System.Collections;

public struct boardPosition
  {
    public int x;
    public int y;
    public override string ToString()
    {
      return "(" + x + "," + y + ")";
    }
  }

public enum diceState {ACTIVE = 0, WAITING, APPEARING, DYING};
public enum diceMoves {NONE = 0, UP = 1, RIGHT = 2, DOWN = 3, LEFT = 4};
public class DiceController : MonoBehaviour {

  //the 3d model displayed
  GameObject diceModel;

  public int startValue = -1;
  //face currently showing
  public int Up;

  //destroy object at the right moment
  public bool kill = false;

  //main controller, needed for board information
  public GameController game;

  //initial click position, used to detect dragging
  Vector3 clickPosition;
  bool clicked = false;

  //Minimum length of a drag to be noticed
  const int minDrag = 70;

  //How off the drag line can be
  const int maxDragWarp = 60;

  //currentstate
  diceState state;

  //current board coordinates
  public boardPosition position;
  public boardPosition lastPosition;
  public boardPosition nextPosition;

Vector3 []faceRotation = new Vector3[6] {new Vector3(270,0,0),
                                         new Vector3(0,0,0),
                                         new Vector3(0,0,90),
                                         new Vector3(0,0,270),
                                         new Vector3(0,0,180),
                                         new Vector3(90,0,0)};

	void Start () {
    diceModel = transform.GetChild(0).gameObject;
    if (startValue >= 0)
    {
      setValue(startValue);
    }
    updateFace();
    state = diceState.APPEARING;
    diceModel.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
	}
  void printSides()
  {
    Debug.Log(diceModel.transform.up + "\t" +
      diceModel.transform.right + "\t" +
      diceModel.transform.forward + "\t" +
      -diceModel.transform.forward + "\t" +
      -diceModel.transform.right + "\t" +
      -diceModel.transform.up + "\n"+
      diceModel.transform.rotation
      );

  }
  //set starting value
  //can't call setValue directly before start() because model is not initialized
  public void setInitialValue(int value)
  {
    startValue = value;

  }
  //set which side of the dice is facing upwards
  public void setValue(int value)
  {
    //rotate to set the face shown
    diceModel.transform.Rotate(faceRotation[value]);
    diceModel.transform.Rotate(new Vector3(0, (int)(Random.Range(0, 3)) * 90, 0),Space.World);
  }
  //Get which face is pointing upwards (i.e. facing the player)
  int getFaceUp()
  {
    Vector3 [] sides = new Vector3[6] {diceModel.transform.forward,
                                       diceModel.transform.up,
                                       diceModel.transform.right,
                                       -diceModel.transform.right,
                                       -diceModel.transform.up,
                                       -diceModel.transform.forward};
    int i = 0;
    bool found = false;
    while (i < 6 && !found)
    {
      found = Mathf.Abs(1 - Vector3.Dot(Vector3.up, sides[i])) < 0.01f;
      i++;
    }
    if (found)
    {
      return i;
    } else { 
      return 0;
    }
  }
	void Update () {
    if (state == diceState.APPEARING)
    {
      state = diceState.WAITING;
      iTween.ScaleTo(diceModel, iTween.Hash("scale", Vector3.one, "time", 0.5f, "easetype", iTween.EaseType.easeInOutElastic, "oncomplete", "finishAppearing", "oncompletetarget",gameObject));
    }
    if (kill && state == diceState.ACTIVE)
    {
      iTween.FadeTo(gameObject,iTween.Hash("alpha", 0, "time", 0.5f, "easetype", iTween.EaseType.linear));
      iTween.ShakePosition(gameObject, iTween.Hash("amount", new Vector3(0.1f, 0.1f, 0.1f), "time", 0.8f, "oncomplete", "finishDisappearing", "oncompleteTarget", gameObject));
      state = diceState.WAITING;
    }
	}
  void finishDisappearing()
  {
      Destroy(gameObject);
  }
  void finishAppearing()
  {
    state = diceState.ACTIVE;
  }
  void OnMouseDown()
  {
    if (state == diceState.ACTIVE)
    {
      //store position
      clickPosition = Input.mousePosition;
      clicked = true;
    }
  }

  bool startMove(diceMoves direction)
  {
    int[,] moveDirections = new int[4, 2] { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };
    Vector3[] rotateDirections = new Vector3[4]{new Vector3(0.25f, 0, 0),
                                                     new Vector3(0, 0, -0.25f),
                                                     new Vector3(-0.25f, 0, 0),
                                                     new Vector3(0,0 , 0.25f)};

    bool canMove = true;
    if (direction == diceMoves.NONE){
      return false;
    }
    int dir = (int)direction - 1;

    nextPosition.x = position.x + moveDirections[dir, 0];
    nextPosition.y = position.y + moveDirections[dir, 1];
    canMove = game.isAvailable(nextPosition);
    if (canMove)
    {
      game.blockPosition(nextPosition);
      iTween.MoveBy(gameObject,
                    new Vector3(moveDirections[dir, 0], 0, moveDirections[dir, 1]),
                    0.5f);
      iTween.RotateBy(diceModel, iTween.Hash("amount", rotateDirections[dir], "space", "world", "time", 0.5f, "easetype", iTween.EaseType.linear, "oncomplete", "finishMove", "oncompletetarget", gameObject));

    }
    return canMove;
  }
  void OnMouseUp()
  {
    if (state == diceState.ACTIVE && clicked)
    {
      Vector3 drag = clickPosition - Input.mousePosition;
      diceMoves dir = diceMoves.NONE;
      // 1 up, 2 right, 3 down, 4 left
      if (Mathf.Abs(drag.x) > minDrag && Mathf.Abs(drag.y) < maxDragWarp)
      {
        if (drag.x > 0)
        {
          //drag left
          dir = diceMoves.LEFT;
        }
        else
        {
          //drag right
          dir = diceMoves.RIGHT;
        }
      }
      else if (Mathf.Abs(drag.y) > minDrag && Mathf.Abs(drag.x) < maxDragWarp)
      {
        if (drag.y > 0)
        {
          //drag down
          dir = diceMoves.DOWN;
        }
        else
        {
          //drag up
          dir = diceMoves.UP;
        }
      }
      
      if (startMove(dir))
      {
        state = diceState.WAITING;
        clicked = false;
      }
    }
  }

  void finishMove()
  {
    updateFace();
    lastPosition = position;
    position = nextPosition;
    game.freePosition(lastPosition);
    game.changePosition(this);
    state = diceState.ACTIVE;
  }
  //Update the value of the facing point upwards
  void updateFace()
  {
    Up = getFaceUp();
  }
}
