using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
	//Basic game stuff
	readonly Dictionary<int, string> objDict = new Dictionary<int, string>() {
		{0, "Empty"},
		{1, "Circles"},
		{2, "Squares"},
		{3, "big peepe"}

	};
	
	[Header("Update Board")]
	[SerializeField] private new Camera camera; //For selecting
	[SerializeField] private Sprite[] objects = new Sprite[3]; //Crosses and circles sprites
	[SerializeField] private GameObject placeholderPrefab = null;
	private SpriteRenderer placeholder = null;
	[SerializeField] private float[] vertSpacing = new float[] { 0.3f, 0.8f, 1.2f, 1.6f }; //Vertical spacings
	[SerializeField] private float rotOffset = Mathf.PI * 0.16f; //Rotation offset
	private Quaternion[,] bRot = new Quaternion[4, 8]; //Coordinates in editor
	private Vector3[,] bPos = new Vector3[4, 8]; //Coordinates in editor
	private SpriteRenderer[,] bSprites = new SpriteRenderer[4, 8]; //Sprite of every object
	[SerializeField] private GameObject boardPrefab = null; //board prefab
	private GameObject sheetObject = null; //Board object

	[Header("Computer")]
	private ComputerBrain computer = null; //Yep
	[SerializeField] private int difficulty;

	[Header("Board Logic")]
	public int numPlayers = 2;
	[HideInInspector] public BoardController boardController = null; //board controller
	public Vector2Int boardSize = new Vector2Int(4, 8); //BoardSize
	private bool canPlace = false; //Removes placeholder when can't place
	public bool isMultiplayer = true; //For multiplayer. CAREFUL, has to match default toggle state. actually i dont think so
	private bool hasWon = false; //For going back and fowards
	private int turn = 0;
	private int numTurns = 0;
	private List<int> history = new List<int>();
	[SerializeField] private GameObject afterGameUI = null;
	private float[] championScores = new float[2];

	[Header("Timer")]
	[SerializeField] private TextMeshProUGUI timerText = null;
	[SerializeField] private float timerTotalSeconds = 300;
	private float[] timerValue = new float[2] {0, 0}; //Change for more players
	[Header("Other?")]
	[SerializeField] private bool tetrisMode = false;

	[Header("Menu bs")]
	[SerializeField] private Toggle multiplayerToggle;
	[SerializeField] private Toggle tetrisToggle;
	[SerializeField] private Animator cameraAnim;
	[SerializeField] private TMP_InputField difficultyInput;

	private void Start()
	{
		if (numPlayers > objects.Length - 1)
			Debug.LogError("More players than player sprites in objects[] array");

		SetMultiplayer();
		SetTetris();
	}

	public void SetMultiplayer() {
		isMultiplayer = multiplayerToggle.isOn;
	}
	public void SetTetris() {
		tetrisMode = tetrisToggle.isOn;
	}

	public void SetDifficulty(){
		if (isMultiplayer)
			numPlayers = int.Parse(difficultyInput.text);
		else
			difficulty = int.Parse(difficultyInput.text);
	}

	private void SetBoardSize() {
		
	}

	public void StartGame()
	{
		if (turn == 0) {
			bPos = new Vector3[boardSize.x, boardSize.y];
			bRot = new Quaternion[boardSize.x, boardSize.y];
			bSprites = new SpriteRenderer[boardSize.x, boardSize.y];

			//Instantiate placeholders & get coordinates
			sheetObject = Instantiate(new GameObject(), transform);
			GameObject boardObj = Instantiate(boardPrefab, sheetObject.transform);

			for (int i = 0; i < boardSize.x; i++) {
				for (int j = 0; j < boardSize.y; j++) {
					float angle = 2 * j * Mathf.PI * 0.125f + rotOffset;
					bPos[i, j] = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * vertSpacing[i];
					bRot[i, j] = Quaternion.Euler(new Vector3(90, - Mathf.Rad2Deg * angle + 90, 0));
					GameObject tempObj = Instantiate(placeholderPrefab, bPos[i, j], bRot[i, j], sheetObject.transform);

					bSprites[i,j] = tempObj.GetComponent<SpriteRenderer>();
					//bSprites[i,j].sprite = objects[3]; //Peenify
				}
			}

			placeholder = Instantiate(placeholderPrefab).GetComponent<SpriteRenderer>();

			boardController = boardObj.GetComponent<BoardController>();
			boardController.boardSize = boardSize;
			boardController.board = new int[boardSize.x, boardSize.y];
			boardController.tetrisMode = tetrisMode;

			championScores = new float[numPlayers + 1];
			timerValue = new float[numPlayers];

			if (!isMultiplayer)
			{
				computer = GetComponent<ComputerBrain>();
				computer.ComputerStart();
				computer.difficulty = 2*difficulty + 1;
				numPlayers = 2;
			}

			cameraAnim.SetBool("gameStarted", true);

			ChangeTurn();
			ResetTimer();
		} else {
			print("fast as heck boi");
		}
	}
	private void Update()
	{
		if (turn != 0) //If game started
		{
			//Find closest point
			int closestCol = 0;

			Ray ray = camera.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform.CompareTag("Board"))
			{
				float minDistance = float.MaxValue;

				for (int i = 0; i < boardSize.y; i++)
				{
					Debug.DrawLine(hit.point, bPos[0, i]);
					if (Vector3.Distance(hit.point, bPos[0, i]) < minDistance)
					{
						minDistance = Vector3.Distance(hit.point, bPos[0, i]);
						closestCol = i;
					}
				}

				if (boardController.board[boardSize.x - 1, closestCol] != 0) // Check if col is full
					canPlace = false;
				else if (history.Count == numTurns - 1 && !hasWon) //Cant place if rewinding or has won
						canPlace = true;

			} else
				canPlace = false; //If mouse is not on board, can't place

			//Until here we have the closest col to the mouse and a bool canPlace. 
			if (canPlace)
			{
				Vector2Int placePos = new Vector2Int(boardController.ApplyGravity(closestCol).x, closestCol);
				placeholder.transform.position = bPos[placePos.x, placePos.y]; //Put placeholder in position
				placeholder.transform.rotation = bRot[placePos.x, placePos.y];

				if (Input.GetMouseButtonDown(0) && boardController.board[3, closestCol] == 0) //When clicked and column is not full
					PlaceActualObject(closestCol, turn);

			}
		
			if (Input.GetKeyDown("left") && numTurns > 1) { //Go back
				boardController.RemoveObject(history[numTurns - 2]);

				numTurns--;
				canPlace = false;
				UpdateBoard();
			}

			if (Input.GetKeyDown("right") && numTurns < history.Count + 1) { //Go fowards
				boardController.PlaceObject(history[numTurns - 1], GetTurn(turn));
				numTurns++;
				if (history.Count == numTurns - 1) 
					if (!hasWon) 
						turn = GetTurn(numTurns);
						
				UpdateBoard();
			}

			placeholder.gameObject.SetActive(canPlace); //Set preview according to if mouse is on board

			if (!hasWon){
				timerValue[turn - 1] -= Time.deltaTime;

				if (timerValue[turn - 1] < 0)
					WinGame(GetTurn(numTurns++), new BoardController.WinObj());
				else
					timerText.text = FormatText(timerValue);
			}
		}
		afterGameUI.SetActive(hasWon);
	}
	
	public void PlaceActualObject(int col, int turn)
	{
		int row = boardController.ApplyGravity(col).x;

		BoardController.WinObj winObj = boardController.PlaceObject(col, turn);
		int[] status = winObj.status();

		UpdateBoard();

		history.Add(col);

		if (status[0] >= 4)
			WinGame(turn, winObj);
		else { //Check if whole board is full, then if truthy win with player = 0 to indicate draw
			int caca = 1;
			for (int i = 0; i < boardSize.y; i++)
				caca *= boardController.board[3, i];
			
			if (caca != 0)
				WinGame(0, winObj);
			else
				ChangeTurn();
		}
	}
	public void UpdateBoard() {
		for (int i = 0; i < boardSize.x; i++)
		{
			for (int j = 0; j < boardSize.y; j++)
			{
				if (bSprites[i,j].sprite != objects[boardController.board[i,j]])
					bSprites[i,j].sprite = objects[boardController.board[i,j]];
			}
		}
	}
	private void ChangeTurn()
	{
		numTurns += 1;
		turn = GetTurn(turn);

		if (!isMultiplayer && turn == computer.computerOrder)
			computer.ComputerPlay(); 

		placeholder.sprite = objects[turn]; 
	}
	private void WinGame(int player, BoardController.WinObj winObj)
	{
		int length = winObj.chainlenght;
		int repetitions = winObj.mclRepeticions;
		Vector2Int dir = winObj.dir;

		numTurns++; //Add turn for cheess like back and fowards, since `PlaceActualObject()` didn't get to change turn
		hasWon = true;
		if (player == 0) {
			for (int i = 0; i < numPlayers; i++) { //Check if draw
				championScores[i] += 0.5f;
			}
			print("Draw. boooo");
		} else { //Win actual game
			championScores[player] += 1;
			print($"{objDict[player]} wins with {repetitions} chains of {length} \nHe has {championScores[player]} points");
			canPlace = false;
			turn = 0;

			//Display line
		}
	}
	public void Restart() {

		EndGame();

		//Reset board
		for (int i = 0; i < boardSize.x; i++) {
			for (int j = 0; j < boardSize.y; j++) {
				boardController.board[i,j] = 0;
			}
		}
		UpdateBoard();
		ChangeTurn();
	}

	private void EndGame(){
		hasWon = false;
		canPlace = true;
		numTurns = 0;
		history = new List<int>();
		ResetTimer();
	}

	public void ReturnToMenu(){
		EndGame();
		Destroy(sheetObject, 2); //Check timing. 
		cameraAnim.SetBool("gameStarted", false);
	}

	#region Utility functions
	private string FormatText(float[] time) {
		string output = "";
		foreach (float timeToDisplay in time)
		{
			if (timeToDisplay == 69420)
				continue;
			int min = Mathf.FloorToInt(timeToDisplay / 60);
			float sec = Mathf.FloorToInt(timeToDisplay % 60);
			output += string.Format("{0:00}:{1:00}", min, sec);
			output += "\n";
		}
		return output;
	}
	private int GetTurn(int totalTurns){
		return (numTurns - 1) % numPlayers + 1; //Change here for more players
	}
		private void ResetTimer() {
		for (int i = 0; i < timerValue.Length; i++)
			timerValue[i] = timerTotalSeconds;
	}
	#endregion
}