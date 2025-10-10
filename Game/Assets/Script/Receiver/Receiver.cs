using UnityEngine;

public class Receiver : MonoBehaviour
{
    public DoorController[] doors;

    public void Activate()
    {
        foreach(DoorController door in doors)
            {
            door.OpenDoor();
        }
    }
}
