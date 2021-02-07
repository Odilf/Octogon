using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour
{
	public bool tetrisMode = false;

	public readonly Vector2Int[] checkDirections = { Vector2Int.right, Vector2Int.up, Vector2Int.one, Vector2Int.right + Vector2Int.down }; //For WinCheck
	public Vector2Int boardSize = new Vector2Int(4, 8); //BoardSize
	public int[,] board = new int[0, 0]; //Actual board
	public struct Caca {
		public int chainlenght;
		public Vector2Int edgePos;
	}

	public struct WinObj {
		public int chainlenght;
		public int mclRepeticions;
		public Vector2Int dir;
		public Vector2Int edgePos;
		public int[] status() { //ugly but i refuse to fix it
			return new int[2] {chainlenght, mclRepeticions};
		}
	}

	int Mod(int x, int m) //Mod que funciona con negativos
	{
		return (x % m + m) % m; //Change here for regular connect 4
	}
	
	public WinObj PlaceObject(int col, int player) //PlaceObject returns maxChainLength and mclRepetitions in an int[2]
	{
		if (col > boardSize.y)
			Debug.LogError("Column " + col + " is outside board");

		int row = ApplyGravity(col).x;
		board[row, col] = player; //Place the object

		if (tetrisMode) 
			if (Tetrify())
				return WinCheck(row - 1, col, player);

		return WinCheck(row, col, player);

	}

	private bool Tetrify() {
		int tetror = 1;
		for (int i = 0; i < boardSize.y; i++)
			tetror *= board[0, i];

		if (tetror != 0) { 
			for (int j = 0; j < boardSize.y; j++)
			{
				for (int i = 0; i < boardSize.x - 1; i++)
				{
					board[i, j] = board[i + 1, j];
				}
				board[boardSize.x - 1, j] = 0;

			}

			return true;
		} else
			return false;	
	}

	public WinObj WinCheck(int row, int col, int player){

		WinObj winObj = new WinObj();

		foreach (Vector2Int dir in checkDirections) //Right, up, up-right, down-right
		{
			//Calculate chainlength
			int tempCL = 0;
			for (int j = 1; j >= -1; j -= 2)
			{
				int i = 1;
				for (bool k = false; k == false && tempCL < boardSize.y - 1; i++)
				{
					Vector2Int checkPos = new Vector2Int(row, col) + i * j * dir;
					checkPos.y = Mod(checkPos.y, boardSize.y);

					if (checkPos.x < 0 || checkPos.x > boardSize.x - 1 || board[checkPos.x, Mod(checkPos.y, boardSize.y)] != player) {
						k = true;
						winObj.edgePos = checkPos;
					}
					else
						tempCL += 1;
				}
			}

			//Compare chainlenght
			if (tempCL > winObj.chainlenght) {
				winObj.chainlenght = tempCL;
				winObj.mclRepeticions = 1;
				winObj.dir = dir;
			} else if (tempCL != 0 && tempCL == winObj.chainlenght)
				winObj.mclRepeticions += 1;
		}
		winObj.chainlenght += 1;
		return winObj;
	}
	
	public void RemoveObject(int col){
		for (int i = boardSize.x - 1; i >= 0; i--) {
			if (board[i, col] != 0) {
				board[i, col] = 0;
				break;
			}
		}
	}

	public Vector2Int ApplyGravity(int col)
	{
		int row = 0;
		for (int i = 0; i < boardSize.x; i++) //Gravity
		{
			if (board[i, col] == 0)
			{
				row = i;
				break;
			}
			if (i == boardSize.x - 1)
			{
				Debug.LogWarning("Column " + col + " is full on object " + name);
			}
		}
		return new Vector2Int(row, col);
	}
}