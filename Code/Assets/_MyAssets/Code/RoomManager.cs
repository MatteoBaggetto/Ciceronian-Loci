using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using System;

public class RoomManager : MonoBehaviour
{

    /// <summary>
    /// Code which stores the current room
    /// </summary>
    private string currentRoomCode = null;

    /// <summary>
    /// The room where the user is in
    /// </summary>
    private MRUKRoom currentRoom = null;

    /// <summary>
    /// The area of the room
    /// </summary>
    private int roomArea = 0;



    /// <summary>
    /// Method for loading the current scene
    /// </summary>
    /// <returns></returns>
    public void LoadCurrentScene()
    {

        if (Debugging.IsOnPC())
        {
            string json = System.IO.File.ReadAllText("Assets/_MyAssets/Saving/roomjson.json");
            MRUK.Instance.LoadSceneFromJsonString(json);
        }
        else
        {
            MRUK.Instance.LoadSceneFromDevice();
        }

    }



    /// <summary>
    /// This method saves the current room
    /// </summary>
    public void SaveCurrentRoom()
    {

        currentRoom = MRUK.Instance.GetCurrentRoom();

        if (currentRoom != null)
        {
            currentRoomCode = currentRoom.ToString();
            SaveArea();
        }

        Debug.Log("mydebug rooms loaded " + MRUK.Instance.Rooms.Count);

        Debug.Log("mydebug scene uuid " + currentRoomCode);

        Debug.Log("mydebug room area " + roomArea);

    }



    private void SaveArea()
    {

        List<Vector3> floorVertices = currentRoom.GetRoomOutline();

        int vertexCount = floorVertices.Count;

        if (vertexCount < 3)
        {
            Debug.LogError("mydebug not valid room");
            return;
        }

        //Shoelace formula

        float sum = 0f;

        for (int i = 0; i < vertexCount; i++)
        {
            Vector3 current = floorVertices[i];
            Vector3 next = floorVertices[(i + 1) % vertexCount];


            sum += (current.x * next.z) - (current.z * next.x);
        }

        Math.Ceiling(sum);


        roomArea = (int)(Mathf.Abs(sum) * 0.5f);

        Debug.Log("mydebug room area " + roomArea);



    }



    /// <summary>
    /// This method returns the area of the room
    /// </summary>
    /// <returns></returns>
    public int GetRoomArea()
    {
        return roomArea;
    }



    /// <summary>
    /// This method returns the current room code
    /// </summary>
    /// <returns></returns>
    public string GetCurrentRoomCode()
    {
        return currentRoomCode;
    }



    /// <summary>
    /// This method returns the current room
    /// </summary>
    /// <returns></returns>
    public MRUKRoom GetCurrentRoom()
    {
        return currentRoom;
    }



    /// <summary>
    /// This method checks if the bounds of an object are entirely in the room
    /// </summary>
    /// <param name="bounds"></param>
    /// <returns></returns>
    public bool IsBoundInRoom(Bounds bounds)
    {

        Vector3[] boundsVertices = {

            new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.max.z)
        };

        foreach (Vector3 vertex in boundsVertices)
        {
            if (currentRoom.IsPositionInRoom(vertex) == false)
            {
                Debug.Log("mydebug object out of room bounds");
                return false;
            }
        }

        Debug.Log("mydebug object in room bounds");
        return true;
    }



}
