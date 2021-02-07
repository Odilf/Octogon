using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using System.Linq;
using System.Threading.Tasks;

public class ComputerBrain : MonoBehaviour
{
    public class Line
    {
        public List<int> moves = new List<int>();
        public float score = float.MinValue;

        public Line Clone()
        {
            return new Line {moves = new List<int>(moves), score = score };
        }
    }
    public int computerOrder = 1;

    private BoardController realBoard = null;
    private BoardController dummyBoard = null;

    public Line line = new Line();
    private readonly Line emptyLine = new Line { score = 0 };

    private GameManager gameManager = null;
    public int difficulty = 3;

    private Vector2Int boardSize = Vector2Int.zero;

    private System.Random rand = new System.Random();

    public void ComputerStart()
    {
        dummyBoard = gameObject.AddComponent<BoardController>();
        gameManager = GetComponent<GameManager>();
        boardSize = gameManager.boardSize;
        realBoard = gameManager.boardController;
    }

    private void PrintBoard(int[,] board, string mes = "")
    {
        string caca = mes + "\n";
        for (int i = 0; i < boardSize.x; i++)
        {
            for (int j = 0; j < boardSize.y; j++)
            {
                caca += board[i, j];
            }
            caca += "\n";
        }
        print(caca);
    }

    public async void ComputerPlay()
    {
        line = await CreateLine(difficulty, (int[,])realBoard.board.Clone(), emptyLine, 0.1f); //Maybe needs clone()
        print($"placing on {line.moves[0]} with score {line.score}");

        if (line.score == Mathf.NegativeInfinity || line.moves[0] > 69)
            print("I, computer, resign.");
        else
            gameManager.PlaceActualObject(line.moves[0], computerOrder);
    }

    private async Task<Line> CreateLine(int vision, int[,] board, Line entryLine, float agr = 0.5f, int index = 0)
    {
        int turn = (index + computerOrder + 1) % 2 + 1; //When index == 0 => Turn == computerOrder
        float k; //Weighting
        int n; //For inverting negative score comparasion
        Line bestLine = new Line(); //Line to return

        //Weigths
        if (turn == computerOrder)
            n = 1;
        else
            n = -1;

        k = agr + (n - 1) / 2; // k = agr - 1 when n = -1, else  k = agr

        bestLine.score = float.MinValue * n; //Minimize default score to always pass first test

        Line[] lines = new Line[boardSize.y];

        void IgnoreLine(int i) {
            Line currentLine = entryLine.Clone();
            for (int j = 0; j < vision - index + 1; j++)
            {
                currentLine.moves.Add(420 + j);
                currentLine.score = float.MinValue * n;
            }
            lines[i] = currentLine; 
        }
        
        for (int i = 0; i < boardSize.y; i++)
        {
            if (board[3, i] == 0) //If column is not full
            {
                dummyBoard.board = (int[,])board.Clone();

                Line currentLine = entryLine.Clone(); //Line to analyze

                int[] status = dummyBoard.PlaceObject(i, turn).status();

                currentLine.moves.Add(i);
                currentLine.score += k * status[0] * status[0] * (status[0] - 1) * status[1] * (1f / (index + 1) * (index + 1));
                if (status[0] >= 4 && turn != computerOrder) //Don't ever let other player win
                    IgnoreLine(i);

                if (index < vision)
                    currentLine = await CreateLine(vision, (int[,])dummyBoard.board.Clone(), currentLine.Clone(), agr, index + 1);

                lines[i] = currentLine;
            }
            else
            {
                //Ignore full columns, add negative infinity score if its index == 0 in order to never chose that. 
                IgnoreLine(i);
            }
        }

        bestLine = CompareLines(lines, n);

        // string caca1 = "Lines: \n";
        // if (index == 0)
        // {
        //     foreach (Line caca in lines)
        //     {
        //         caca1 += "score = " + caca.score + ", " + "moves = " + caca.moves[0] + ", " + caca.moves[1] + ", " + caca.moves[2] + "\n";
        //     }
        //     caca1 += "BEST LINE. score = " + bestLine.score + ", " + "moves = " + bestLine.moves[0] + ", " + bestLine.moves[1] + ", " + bestLine.moves[2];
        //     print(caca1);
        // }

        return bestLine;
    }

    private Line CompareLines(Line[] lines, int n)
    {
        Line bestLine = new Line() { score = float.MinValue * n };
        bool hasChanged = false;

        foreach (Line line in lines)
        {
            if (line.score * n > bestLine.score * n)
            {
                bestLine = line;
                hasChanged = true;
            } 
            else if (line.score * n == bestLine.score * n && rand.Next(2) == 0) //If have the same score 50/50 chance to change it to new one. Introduces randomness
                bestLine = line;
        }
        if (!hasChanged)
            Debug.LogWarning("Haven't passed any test in line comparason");
        return bestLine;
    }
}