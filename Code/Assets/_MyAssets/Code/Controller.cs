using System.Collections;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using Microsoft.MixedReality.Toolkit.UI;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Video;


public class Controller : MonoBehaviour
{

    /// <summary>
    /// The dictionary containing the experiences data of a given user in a given room, these two
    /// information are the keys of the dictionary.
    /// </summary>
    private static Dictionary<string, ExperienceData> experiences = new Dictionary<string, ExperienceData>();

    /// <summary>
    /// This is the current experience key, it's made by user code + room code
    /// </summary>
    private string currentKeyExperience;

    /// <summary>
    /// This is the ID of the current user. It should be taken from the server
    /// MISSING FUNCTIONALITY
    /// </summary>
    private string userId = "UserID";

    /// <summary>
    /// This is the ID of current experience. It should be taken from the server
    /// MISSING FUNCTIONALITY
    /// </summary>
    private string experienceId = "ExperienceID";

    /// <summary>
    /// This is the name of the current user. It should be taken from the server
    /// MISSING FUNCTIONALITY
    /// </summary>
    private string userName = "Matteo";

    //Here starts references to the other scripts used for organizing the code
    private ObjectManager objectManager = new ObjectManager();
    private RoomManager roomManager = new RoomManager();

    private SpatialAnchorManager spatialAnchorManager = new SpatialAnchorManager();
    private LociManager lociManager;


    void Start()
    {
        LoadSceneEvent();
    }

    /// <summary>
    /// This method is called after the scene is loaded
    /// </summary>
    public void StartAfterSceneLoaded()
    {

        Debug.Log("mydebug scene loaded");

        StartCoroutine(StartAfterFewTimeAfterSceneLoaded());
    }



    /// <summary>
    /// This method is created for initializing the ObjectManager with hard-coded content. They should be passed by the server
    /// MISSING FUNCTIONALITY
    /// </summary>
    private void SampleObjectManager()
    {
        objectManager = gameObject.AddComponent<ObjectManager>();

        List<string> conceptsIDorder = new List<string>
        {
            "1",
            "2",
            "3",
            "4",
            "5",
        };


        Dictionary<string, Texture2D> conceptsImage = new Dictionary<string, Texture2D>();
        conceptsImage.Add("2", InspectorManager.instance.GetConceptsImages()[0]);
        conceptsImage.Add("3", InspectorManager.instance.GetConceptsImages()[1]);


        Dictionary<string, GameObject> conceptsObject = new Dictionary<string, GameObject>();
        conceptsObject.Add("1", InspectorManager.instance.GetConceptsObjects()[0]);
        conceptsObject.Add("5", InspectorManager.instance.GetConceptsObjects()[1]);


        Dictionary<string, VideoClip> conceptsVideo = new Dictionary<string, VideoClip>();
        conceptsVideo.Add("4", InspectorManager.instance.GetVideoClips()[0]);


        Dictionary<string, AudioClip> conceptsAudio = new Dictionary<string, AudioClip>();



        Dictionary<string, GameObject> concepts = new Dictionary<string, GameObject>();
        foreach (string conceptID in conceptsImage.Keys)
        {
            GameObject obj = objectManager.InstantiateImageObject(conceptID, Vector3.zero, Quaternion.identity, conceptsImage[conceptID], lociManager);
            concepts.Add(conceptID, obj);
        }
        foreach (string conceptID in conceptsObject.Keys)
        {
            GameObject obj = objectManager.Instantiate3DObject(conceptID, Vector3.zero, Quaternion.identity, conceptsObject[conceptID], lociManager);
            concepts.Add(conceptID, obj);
        }
        foreach (string conceptID in conceptsVideo.Keys)
        {
            GameObject obj = objectManager.InstantiateVideoObject(conceptID, Vector3.zero, Quaternion.identity, conceptsVideo[conceptID], lociManager);
            concepts.Add(conceptID, obj);
        }
        foreach (string conceptID in conceptsAudio.Keys)
        {
            GameObject obj = objectManager.InstantiateAudioObject(conceptID, Vector3.zero, Quaternion.identity, conceptsAudio[conceptID], lociManager);
            concepts.Add(conceptID, obj);
        }

        objectManager.Init(concepts, conceptsIDorder);
    }



    /// <summary>
    /// This is called after a few time after the scene is loaded, 
    /// THIS IS NECESSARY because scene loaded event could be called before instantiating other things!
    /// </summary>
    /// <returns></returns>
    private IEnumerator StartAfterFewTimeAfterSceneLoaded()
    {
        yield return new WaitForSeconds(2);

        lociManager = gameObject.AddComponent<LociManager>();

        Dictionary<string, GameObject> conceptsObject = new Dictionary<string, GameObject>();

        roomManager.SaveCurrentRoom();

        SampleObjectManager();

        bool inScannedRoom = CheckifUserIsInScannedRoomAtStart();

        if (inScannedRoom == true)
        {
            LoadExperiencesIfAny();

            currentKeyExperience = roomManager.GetCurrentRoomCode() + userId + experienceId;
            Debug.Log("mydebug currentKeyExperience: " + currentKeyExperience);

            if (!experiences.ContainsKey(currentKeyExperience))
            {
                experiences.Add(currentKeyExperience,
                    new ExperienceData(new Dictionary<System.Guid, AnchorType>(), new Dictionary<string, List<float>>()));
                Debug.Log("mydebug new experience added");
            }

            Dictionary<string, Dictionary<System.Guid, AnchorType>> anchorDictionary =
                new Dictionary<string, Dictionary<System.Guid, AnchorType>>();

            foreach (KeyValuePair<string, ExperienceData> experience in experiences)
            {
                anchorDictionary.Add(experience.Key, experience.Value.GetAnchorData());
            }


            Dictionary<string, List<float>> concepts3DRotation = experiences[currentKeyExperience].GetRotationData();
            Debug.Log("mydebug concepts3DRotation loaded rotation with size: " +
                      concepts3DRotation.Count);



            foreach (KeyValuePair<string, List<float>> concept in concepts3DRotation)
            {

                Quaternion rotation = new Quaternion(concept.Value[0], concept.Value[1], concept.Value[2], concept.Value[3]);


                Vector3 eulerAngles = rotation.eulerAngles;

                Debug.Log("mydebug concept: " + concept.Key +
                          " | quaternion: (" + concept.Value[0] + ", " + concept.Value[1] + ", " +
                          concept.Value[2] + ", " + concept.Value[3] + ")" +
                          " | euler angles: (" + eulerAngles.x + ", " + eulerAngles.y + ", " + eulerAngles.z + ")");
            }




            spatialAnchorManager.Init(anchorDictionary, currentKeyExperience, objectManager, this, lociManager, concepts3DRotation);

            StartCoroutine(StartAfterAnchorsReady());
        }
    }



    /// <summary>
    /// This method is used wait until anchors are loaded
    /// </summary>
    /// <returns></returns>
    private IEnumerator StartAfterAnchorsReady()
    {
        while (!spatialAnchorManager.AreAnchorsReady())
        {
            yield return null;
        }


        Debug.Log("mydebug anchors ready");

        Dictionary<string, int> standings = LoadStandings();

        lociManager.Init(spatialAnchorManager, objectManager, this, roomManager, standings);

        if (Debugging.IsMainScene() == false)
            StartCoroutine(SampleSceneUpdate());
        else if (Debugging.AllowDeleteMemoryLoci())
            StartCoroutine(SceneUpdate());
    }



    /// <summary>
    /// This method loads the scene and then checks if the scene is loaded and calls StartAfterSceneLoaded
    /// </summary>
    private void LoadSceneEvent()
    {

        Debug.Log("mydebug LOAD SCENE EVENT LAUNCHED");

        //roomManager.LoadCurrentScene();

        MRUK.Instance.RegisterSceneLoadedCallback(StartAfterSceneLoaded);
    }



    /// <summary>
    /// This method is used for loading saved experiences if any
    /// </summary>
    private void LoadExperiencesIfAny()
    {

        string jsonExperiences;
        string filePath;
        string jsonName;

        Debug.Log("mydebug starting experiences loading");

        if (Debugging.IsMainScene() == false)
            jsonName = "experiencesSample.json";

        else
            jsonName = "LociExperiences.json";

        if (Debugging.IsOnPC())
        {
            filePath = "Assets/_MyAssets/Saving";
        }

        else
        {
            filePath = Application.persistentDataPath;

            Debug.Log("mydebug AppBaseDirectory: " + filePath);
        }

        filePath = filePath + "/" + jsonName;

        Debug.Log("mydebug file loading, path and name:" + filePath);

        if (System.IO.File.Exists(filePath))
        {
            jsonExperiences = System.IO.File.ReadAllText(filePath);
            experiences = JsonConvert.DeserializeObject<Dictionary<string, ExperienceData>>(jsonExperiences);


            Debug.Log("mydebug experiences loaded");
            Debug.Log("mydebug experience file:" + jsonExperiences);
        }
        else
        {
            Debug.Log("mydebug no experiences found");
        }
    }



    /// <summary>
    /// This method is used for saving the anchor dictionary
    /// </summary>
    /// <param name="anchorDictionary"></param>
    public void SaveAnchorDictionary(Dictionary<string, Dictionary<System.Guid, AnchorType>> anchorDictionary)
    {

        string jsonName;
        string filePath;

        Debug.Log("mydebug saving anchor dictionary");

        /*foreach (string experienceCode in experiences.Keys.ToList())
        {
            if (!anchorDictionary.ContainsKey(experienceCode))
                experiences.Remove(experienceCode);
        }*/



        foreach (KeyValuePair<string, Dictionary<System.Guid, AnchorType>> anchorData in anchorDictionary)
        {
            experiences[anchorData.Key].SetAnchorData(anchorData.Value);
        }

        string json = JsonConvert.SerializeObject(experiences);

        if (Debugging.IsMainScene() == false)
            jsonName = "experiencesSample.json";

        else
            jsonName = "LociExperiences.json";

        if (Debugging.IsOnPC())
        {
            filePath = "Assets/_MyAssets/Saving";
        }

        else
        {
            filePath = Application.persistentDataPath;

            Debug.Log("mydebug AppBaseDirectory: " + filePath);
        }

        filePath = filePath + "/" + jsonName;

        Debug.Log("mydebug file saving, path and name:" + filePath);

        System.IO.File.WriteAllText(filePath, json);


        Debug.Log("mydebug anchorDictionary correctly saved");

        Debug.Log("Current experience json: " + json);

    }


    /// <summary>
    /// This method is used for saving the rotation data to memory
    /// </summary>
    /// <param name="rotationData"></param>
    public void SaveRotationData(string id, List<float> rotationData)
    {
        string jsonName;
        string filePath;

        experiences[currentKeyExperience].SetRotationData(id, rotationData);

        string json = JsonConvert.SerializeObject(experiences);

        if (Debugging.IsMainScene() == false)
            jsonName = "experiencesSample.json";

        else
            jsonName = "LociExperiences.json";

        if (Debugging.IsOnPC())
        {
            filePath = "Assets/_MyAssets/Saving";
        }

        else
        {
            filePath = Application.persistentDataPath;

            Debug.Log("mydebug AppBaseDirectory: " + filePath);
        }

        filePath = filePath + "/" + jsonName;

        Debug.Log("mydebug file saving, path and name:" + filePath);

        System.IO.File.WriteAllText(filePath, json);


        Debug.Log("mydebug rotation data correctly saved");

        Debug.Log("Current experience json: " + json);
    }



    /// <summary>
    /// This method is used for deleting the savings
    /// </summary>
    private void DeleteSavings()
    {
        string jsonName;
        string filePath;

        if (Debugging.IsMainScene() == false)
            jsonName = "experiencesSample.json";

        else
            jsonName = "LociExperiences.json";


        if (Debugging.IsOnPC() == false)
            filePath = Application.persistentDataPath;
        else
            filePath = "Assets/_MyAssets/Saving";

        filePath = filePath + "/" + jsonName;

        Debug.Log("mydebug file deleting, path and name:" + filePath);

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            Debug.Log("mydebug file deleted");
        }

        else
            Debug.Log("mydebug file not exists");
    }



    /// <summary>
    /// This method is used for instantiating a dialog box
    /// </summary>
    /// <param name="title"></param>
    /// <param name="message"></param>
    /// <param name="dialogType"></param>
    private void InstantiateDialogBox(string title, string message, DialogType dialogType = DialogType.Generic)
    {

        Dialog dialog = Dialog.Open(InspectorManager.instance.GetDialogPrefab(), DialogButtonType.OK, title, message, true);
        dialog.transform.parent = lociManager.gameObject.transform;
    }



    /// <summary>
    /// This method is used for checking if the user is in a scanned room at the start of the app
    /// </summary>
    /// <returns></returns>
    private bool CheckifUserIsInScannedRoomAtStart()
    {
        if (roomManager.GetCurrentRoom() == null || !roomManager.GetCurrentRoom()
                .IsPositionInRoom(InspectorManager.instance.GetCam().transform.position))
        {
            InstantiateDialogBox("SPACE SETUP ERROR",
                "You are not a scanned room, please close the app and scan the room in which you want to use the app, if you haven't done it yet. You can do it going to settings -> Environment setup -> Space setup ");
            return false;
        }
        else if (roomManager.GetRoomArea() == 0)
        {
            InstantiateDialogBox("SPACE SETUP ERROR",
                "You are not in a valid room, please do the space setup again");
            return false;
        }
        else
        {
            return true;
        }
    }



    /// <summary>
    /// Used in main scene for debugging purposes
    /// </summary>
    /// <returns></returns>
    private IEnumerator SceneUpdate()
    {

        while (true)
        {

            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch) ||
                Input.GetKeyDown(KeyCode.C))
            {

                DeleteSavings();
            }

            yield return null;
        }
    }



    /// <summary>
    /// This is used in sample scene for debugging purposes
    /// </summary>
    private IEnumerator SampleSceneUpdate()
    {
        Debug.Log("mydebug sample scene update started");

        while (true)
        {

            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch) ||
                Input.GetKeyDown(KeyCode.C))
            {

                DeleteSavings();
            }


            if (roomManager.GetCurrentRoom()
                .IsPositionInRoom(InspectorManager.instance.GetCam().transform.position))
            {

                if (Debugging.IsOnPC())
                {

                    if (Input.GetKeyDown(KeyCode.Alpha1))
                    {
                        spatialAnchorManager.CreateObjectWithoutAnchorPCSampleScene(InspectorManager.instance
                            .GetCam().transform);
                    }
                }

                else
                {

                    if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
                    {
                        spatialAnchorManager.CreateObjectWithAnchorSampleScene();
                    }

                    else if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
                    {
                        spatialAnchorManager.LoadAllAnchorsFromCurrentExperience();
                    }

                    else if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
                    {
                        spatialAnchorManager.DestroyAllAnchors();
                    }

                    else if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
                    {
                        spatialAnchorManager.EraseAllAnchorsFromSpecificExperience(currentKeyExperience);
                    }

                    else if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
                    {

                        spatialAnchorManager.EraseAllAnchorsFromAllExperiences();
                    }
                }
            }

            yield return null;
        }
    }



    /// <summary>
    /// This methods load the standings of the users in the current experience from the server
    /// </summary>
    /// <returns></returns>
    private Dictionary<string, int> LoadStandings()
    {

        Dictionary<string, int> standings = new Dictionary<string, int>();

        //This should be taken from the server.
        // MISSING FUNCTIONALITY

        standings.Add("giorgio", 1);
        standings.Add("matteo", 2);
        standings.Add("martina", 3);
        standings.Add("paolo", 4);
        standings.Add("samanta", 2);
        standings.Add("francesca", 4);
        standings.Add("pietro", 7);
        standings.Add("elisa", 5);
        standings.Add("maria", 10);
        standings.Add("andrea", 20);
        standings.Add("gianfranco", 1);
        standings.Add("ernesto", 7);


        return standings;
    }



    /// <summary>
    /// This method saves the standings of the users in the current experience to the server
    /// </summary>
    /// <param name="standings"></param>
    public void SaveStandings(Dictionary<string, int> standings)
    {
        //This method should save the standings to the server.
        // MISSING FUNCTIONALITY
    }



    /// <summary>
    /// This method is used for getting the current user ID
    /// </summary>
    /// <returns></returns>
    public string GetUserID()
    {
        return userId;
    }


    public string GetUserName()
    {
        return userName;
    }


}



