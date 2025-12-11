using UnityEngine;
using System.Collections;

public class PlayerState : MonoBehaviour
{
    public Transform[] waypoints;
    public int currentWaypoint = 0;
    public int numberOfRoundsToWait = 0;
    
    [SerializeField] private float speed = 5f; // Iets sneller zetten

    void Start()
    {
        // Zet speler op startpositie
        if (waypoints.Length > 0)
        {
            transform.position = waypoints[0].transform.position;
        }
    }

    // Deze Coroutine wordt aangeroepen door GameController
    public IEnumerator MoveToWaypoint(int targetIndex)
    {
        // Zorg dat we niet buiten de array gaan (veiligheid)
        if (targetIndex >= waypoints.Length) targetIndex = waypoints.Length - 1;

        // Zolang we nog niet op het doel zijn
        while (currentWaypoint != targetIndex)
        {
            // Bepaal richting: vooruit of achteruit?
            if (currentWaypoint < targetIndex)
            {
                currentWaypoint++;
            }
            else if (currentWaypoint > targetIndex) 
            {
                // Dit is voor situaties zoals "Terug naar 37" of stuiteren bij 63
                currentWaypoint--;
            }

            Vector3 nextPos = waypoints[currentWaypoint].position;

            // Loop animatie naar het volgende vakje
            while (Vector3.Distance(transform.position, nextPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, 
                    nextPos, 
                    speed * Time.deltaTime
                );
                yield return null; // Wacht 1 frame
            }
            
            // Kleine pauze op elk vakje voor het "bordspel gevoel"
            yield return new WaitForSeconds(0.1f);
        }
    }
}