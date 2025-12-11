using UnityEngine;
using System.Collections;

public class RollDice : MonoBehaviour
{
    private Sprite[] diceSides;
    private SpriteRenderer rend;
    public int diceValue = 1; // Default 1 om errors te voorkomen
    public bool isRolling = false;

    void Start()
    {
        rend = GetComponent<SpriteRenderer>();
        diceSides = Resources.LoadAll<Sprite>("DiceSides");
        
        // Veiligheidscheck
        if(diceSides.Length > 0) rend.sprite = diceSides[5];
    }

    public IEnumerator RollTheDice()
    {
        isRolling = true;
        
        // Rol animatie
        for (int i = 0; i < 20; i++)
        {
            int randomDiceSide = Random.Range(0, 6);
            rend.sprite = diceSides[randomDiceSide];
            yield return new WaitForSeconds(0.05f);
        }

        // Definitieve worp
        int finalSide = Random.Range(0, 6);
        diceValue = finalSide + 1;
        rend.sprite = diceSides[finalSide];

        isRolling = false;
    }
}