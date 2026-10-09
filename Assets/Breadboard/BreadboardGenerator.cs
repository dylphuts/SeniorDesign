using UnityEngine;

public class BreadboardGenerator : MonoBehaviour
{
    public GameObject holePrefab;

    public int rows = 2;
    public int holesPerRow = 5;

    public float holeSpacing = 0.5f;
    public float rowSpacing = 0.8f;

    private Hole[,] holes;

    void Awake()
    {
        holes = new Hole[rows, holesPerRow];

        Generate();
    }

    void Generate()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < holesPerRow; col++)
            {
                Vector3 position = new Vector3(
                    col * holeSpacing,
                    0,
                    row * rowSpacing
                );

                GameObject newHole = Instantiate(
                    holePrefab,
                    transform
                );

                newHole.transform.localPosition = position;

                Hole hole = newHole.GetComponent<Hole>();

                hole.row = row;

                holes[row, col] = hole;
            }
        }
    }

    public void MakeRowGreen(int row)
    {
        for (int col = 0; col < holesPerRow; col++)
        {
            holes[row, col].SetGreen(true);
        }
    }

    public void ClearRows()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < holesPerRow; col++)
            {
                holes[row, col].SetGreen(false);
            }
        }
    }
}