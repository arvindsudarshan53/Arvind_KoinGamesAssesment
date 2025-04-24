using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace ConnectFour
{
    public class GameManager : MonoBehaviour
    {
        private enum CellState { Empty = 0, Yellow = 1, Red = 2 }

        // Grid dimensions and win condition
        int rows = 6;
        int columns = 7;
        int connectToWin = 4;
        public float pieceDropSpeed = 4f;

        // Prefabs and UI elements
        public GameObject redPiece;
        public GameObject yellowPiece;
        public GameObject boardCellPrefab;
        public TextMeshProUGUI victoryTextObject;
        public GameObject replayButton;

        // Win/Loss/Draw messages
        public string winText = "You Won!";
        public string loseText = "You Lost!";
        public string tieText = "It's a Draw!";

        // Internal variables
        private GameObject boardParent;
        private GameObject ghostPiece; // piece that hovers above board before dropped

        private int[,] boardGrid; // Grid to track cell state
        private bool isPlayerTurn;
        private bool gameFinished;
        private bool droppingPiece;
        private bool verifyingWin;
        private bool loadingBoard;

        void Start()
        {
            SetupGame(); // Initialize the board and game state
        }

        // Initializes the game grid and UI
        void SetupGame()
        {
            boardGrid = new int[columns, rows];
            victoryTextObject.gameObject.SetActive(false);
            replayButton.SetActive(false);
            gameFinished = false;
            loadingBoard = true;

            // Destroy any old board
            if (boardParent != null)
                DestroyImmediate(boardParent);

            boardParent = new GameObject("Board");

            // Create the board grid
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    boardGrid[x, y] = (int)CellState.Empty;
                    var cell = Instantiate(boardCellPrefab, new Vector3(x, -y, -1), Quaternion.identity);
                    cell.transform.parent = boardParent.transform;
                }
            }

            isPlayerTurn = Random.Range(0, 2) == 0; // Randomly choose who starts
            loadingBoard = false;
        }

        void Update()
        {
            if (loadingBoard || verifyingWin)
                return;

            if (gameFinished)
            {
                victoryTextObject.gameObject.SetActive(true);
                replayButton.SetActive(true);
                return;
            }

            if (isPlayerTurn)
                HandlePlayerInput(); // process user input
            else
                HandleAIMove();      // AI selects and drops a piece
        }

        // Handles user input and ghost piece
        void HandlePlayerInput()
        {
            if (ghostPiece == null)
                ghostPiece = CreateGhostPiece();

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            ghostPiece.transform.position = new Vector3(Mathf.Clamp(mouseWorld.x, 0, columns - 1), boardParent.transform.position.y + 1, 0);

            if (Input.GetMouseButtonDown(0) && !droppingPiece)
                StartCoroutine(DropPiece(ghostPiece));
        }

        // Handles random AI move
        void HandleAIMove()
        {
            if (ghostPiece == null)
                ghostPiece = CreateGhostPiece();

            if (!droppingPiece)
                StartCoroutine(DropPiece(ghostPiece));
        }

        // Spawns a temporary ghost piece (player or AI)
        GameObject CreateGhostPiece()
        {
            Vector3 spawnPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (!isPlayerTurn)
            {
                var validColumns = GetValidMoves();
                int col = validColumns[Random.Range(0, validColumns.Count)];
                spawnPosition = new Vector3(col, 0, 0);
            }

            var prefab = isPlayerTurn ? yellowPiece : redPiece;
            return Instantiate(prefab, new Vector3(Mathf.Clamp(spawnPosition.x, 0, columns - 1), boardParent.transform.position.y + 1, 0), Quaternion.identity);
        }

        // Drop the piece with animation
        IEnumerator DropPiece(GameObject piece)
        {
            droppingPiece = true;

            Vector3 start = piece.transform.position;
            int column = Mathf.RoundToInt(start.x);
            Vector3 end = start;
            bool foundSpot = false;

            // Find the lowest empty row in the selected column
            for (int y = rows - 1; y >= 0; y--)
            {
                if (boardGrid[column, y] == (int)CellState.Empty)
                {
                    boardGrid[column, y] = isPlayerTurn ? (int)CellState.Yellow : (int)CellState.Red;
                    end = new Vector3(column, -y, start.z);
                    foundSpot = true;
                    break;
                }
            }

            if (foundSpot)
            {
                GameObject actualPiece = Instantiate(piece);
                float journey = 0;
                float distance = Vector3.Distance(start, end);

                // Animate falling
                while (journey < 1)
                {
                    journey += Time.deltaTime * pieceDropSpeed * ((rows - distance) + 1);
                    actualPiece.transform.position = Vector3.Lerp(start, end, journey);
                    yield return null;
                }

                actualPiece.transform.parent = boardParent.transform;
                DestroyImmediate(ghostPiece);
                StartCoroutine(CheckWinCondition());

                while (verifyingWin)
                    yield return null;

                isPlayerTurn = !isPlayerTurn;
            }

            droppingPiece = false;
        }

        // Check win/draw condition
        IEnumerator CheckWinCondition()
        {
            verifyingWin = true;

            int layer = isPlayerTurn ? 1 << 8 : 1 << 9;

            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (boardGrid[x, y] != (isPlayerTurn ? (int)CellState.Yellow : (int)CellState.Red))
                        continue;

                    Vector3 origin = new Vector3(x, -y, 0);

                    // Check all directions for winning condition
                    if (CountHits(origin, Vector3.right, layer) >= connectToWin - 1 ||
                        CountHits(origin, Vector3.up, layer) >= connectToWin - 1 ||
                        CountHits(origin, new Vector3(1, 1, 0), layer) >= connectToWin - 1 ||
                        CountHits(origin, new Vector3(-1, 1, 0), layer) >= connectToWin - 1)
                    {
                        gameFinished = true;
                        victoryTextObject.text = isPlayerTurn ? winText : loseText;
                        verifyingWin = false;
                        yield break;
                    }

                    yield return null;
                }
            }

            if (!BoardHasEmptyCell())
            {
                gameFinished = true;
                victoryTextObject.text = tieText;
            }

            verifyingWin = false;
        }

        // Count hits in a given direction using raycasting
        int CountHits(Vector3 origin, Vector3 direction, int layer)
        {
            var hits = Physics.RaycastAll(origin, direction.normalized, connectToWin - 1, layer);
            return hits.Length;
        }

        // Get all columns with at least one empty spot
        List<int> GetValidMoves()
        {
            List<int> valid = new List<int>();
            for (int x = 0; x < columns; x++)
                for (int y = rows - 1; y >= 0; y--)
                    if (boardGrid[x, y] == (int)CellState.Empty)
                    {
                        valid.Add(x);
                        break;
                    }

            return valid;
        }

        // Check if the board is full
        bool BoardHasEmptyCell()
        {
            foreach (int cell in boardGrid)
                if (cell == (int)CellState.Empty)
                    return true;
            return false;
        }

        public void RestartLevel()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0); // Restart scene
        }
    }
}
