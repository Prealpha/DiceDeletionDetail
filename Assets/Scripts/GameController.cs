using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public enum GameState { STARTING, PLAYING, PAUSED, WAITING}
public class GameController : MonoBehaviour {

  public GameObject resetPanel;
  public float debugSpeed = 1;
  DiceController[,] board;
  float elapsedTime = 0;
  int difficulty = 0;
  //time between dice spawn for each difficulty
  float[] spawnTimes = new float[6] { 1, 0.8f, 0.6f, 0.4f, 0.4f, 0.2f };

  //higher dice to spawn
  int[] maxValue = new int[6] { 3, 4, 4, 5, 5, 6 };
  int[] scoreLevels = new int[6] { 24, 60, 100, 160, 220, 286 };

  public int boardWidth;
  public int boardHeight;
  int boardSize;

  ArrayList freePositions;

  //prefab of Die to Instantiate
  public Transform die;

  int score;
  int maxLevel = 5;

  GameState state;

  Vector2 zero;

	void Start () {
    state = GameState.STARTING;
    board = new DiceController[boardWidth, boardHeight];
    boardSize = boardWidth * boardHeight;
    zero = new Vector2( 0 - 3.5f, 0 - 3.5f);
    freePositions = new ArrayList();
    for (int i = 0; i < boardWidth; i++)
    {
      for (int j = 0; j < boardHeight; j++)
      {
        boardPosition free;
        free.x = i;
        free.y = j;
        freePositions.Add(free);
      }
    }
    //Add the first bunch of dice
    for (int i = 0; i < 8; i++)
    {
      spawnDie();
    }
    state = GameState.PLAYING;
    
	}

  public void restart()
  {
    difficulty = 0;
    score = 0;
    elapsedTime = 0;
    for (int i = 0; i < boardWidth; i++)
    {
      for (int j = 0; j < boardHeight; j++)
      {
        boardPosition free;
        free.x = i;
        free.y = j;
        freePositions.Add(free);
        board[i, j].kill = true;
        board[i, j] = null;
      }
    }
    //Add the first bunch of dice
    for (int i = 0; i < 8; i++)
    {
      spawnDie();
    }

    resetPanel.SetActive(false);
    state = GameState.PLAYING;

  }
  //Add a new die to the board at a random position
  void spawnDie()
  {
    //TODO: change fixed values for const values asigned at a higher level
      int randomIndex = (int)Random.Range(0,freePositions.Count);
      boardPosition freePosition = (boardPosition)(freePositions[randomIndex]);
      float sizeXdice = 0.96f;
      float sizeXtile = 1f;
      float startX = zero.x + sizeXtile * freePosition.x + sizeXtile / 2;
      float startY = zero.y + sizeXtile * freePosition.y + sizeXtile / 2;
      Vector3 startPosition = new Vector3(startX, sizeXdice/2, startY);
      Transform o = (Transform)Instantiate(die, startPosition, Quaternion.identity);
      DiceController d = o.gameObject.GetComponent<DiceController>();
      d.position.x = freePosition.x;
      d.position.y = freePosition.y;
      freePositions.RemoveAt(randomIndex);
      d.game = this;
      d.setInitialValue((int)Random.Range(1, maxValue[difficulty]));
      board[freePosition.x, freePosition.y] = d;
     
  }
	
	void Update () {
    if (state == GameState.PLAYING)
    {
      elapsedTime += Time.deltaTime * debugSpeed;
      if (elapsedTime >= spawnTimes[difficulty]*boardSize/(freePositions.Count+1))
      {
        if (freePositions.Count > 0)
        {
          spawnDie();
          elapsedTime = 0;
        }
        else
        {
          //Game Over
          state = GameState.WAITING;
          resetPanel.SetActive(true);
        }
      }
      bool[,] tilesChecked = new bool[boardWidth, boardHeight];
      int tilex = 0;
      int tiley = 0;

      int[,] around = new int[4, 2] { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };
      //check for chains
      while (tiley < boardHeight)
      {
        if (board[tilex, tiley] != null &&
            !tilesChecked[tilex, tiley])
        {
          DiceController currentDie = board[tilex, tiley];
          int value = currentDie.Up;
          if (value > 1) //1 acts as joker
          {
            Queue toCheck = new Queue();
            Queue chain = new Queue();
            toCheck.Enqueue(currentDie);
            int chainLength = 0;
            while (toCheck.Count > 0)
            {
              currentDie = (DiceController)toCheck.Dequeue();
              if (currentDie.Up == value || currentDie.Up == 1)
              {
                chainLength++;
                tilesChecked[currentDie.position.x, currentDie.position.y] = true;
                for (int i = 0; i < 4; i++)
                {
                  boardPosition nextToCheck;
                  nextToCheck.x = currentDie.position.x + around[i, 0];
                  nextToCheck.y = currentDie.position.y + around[i, 1];
                  if (isInsideBounds(nextToCheck)
                    && !tilesChecked[nextToCheck.x, nextToCheck.y]
                    && board[nextToCheck.x, nextToCheck.y] != null)
                  {
                    toCheck.Enqueue(board[nextToCheck.x, nextToCheck.y]);
                  }
                }
                chain.Enqueue(currentDie);
              }
            }
            if (chainLength >= value)
            {
              foreach (DiceController die in chain)
              {
                board[die.position.x, die.position.y] = null;
                freePositions.Add(die.position);
                die.kill = true;
              }
              score += chainLength * value;
              if (difficulty < maxLevel && score > scoreLevels[difficulty])
              {
                difficulty++;
              }
              Text scoreUI = (Text)GameObject.Find("score_ui").GetComponent<Text>();
              // pad score with left 0s
              // TODO: look up format strings for C#
              int scoreLength = 0;
              string scoreString = "";
              int scoreMagnitude = score;
              while (scoreMagnitude > 1)
              {
                scoreMagnitude /= 10;
                scoreLength++;
              }
              scoreString = (difficulty + 1) + " / ";
              for (int i = scoreLength; i < 7; i++)
              {
                scoreString += "0";
              }
              scoreString += score;
                scoreUI.text = scoreString;
            }
          }
        }
        tilex++;
        if (tilex >= boardWidth)
        {
          tilex = 0;
          tiley++;
        }
      }
    }
	}

  bool isInsideBounds(boardPosition position)
  {
    bool inside = true;
    if(position.x >= boardWidth){
      inside = false;
    }else if(position.x < 0){
      inside = false;
    }else if(position.y >= boardHeight){
      inside = false;
    }
    else if (position.y < 0)
    {
      inside = false;
    }
    return inside;
  }

  public bool isAvailable(boardPosition position)
  {
    bool available = true;
    if(position.x >= boardWidth){
      available = false;
    }else if(position.x < 0){
      available = false;
    }else if(position.y >= boardHeight){
      available = false;
    }
    else if (position.y < 0)
    {
      available = false;
    }
    else if (!freePositions.Contains(position))
    {
      available = false;
    }
    return available;
  }

  public void changePosition(DiceController die)
  {
    board[die.lastPosition.x,die.lastPosition.y] = null;
    board[die.position.x,die.position.y] = die;

  }
  public void freePosition(boardPosition position)
  {
    if (!freePositions.Contains(position))
    {
      freePositions.Add(position);
    }
  }
  public void blockPosition(boardPosition position)
  {
    if (freePositions.Contains(position))
    {
      freePositions.Remove(position);
    }
  }
}
