using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class InspectorManager : MonoBehaviour
{

    /// <summary>
    /// This is the list of prefabs which can be instantiated and they are set in the inspector
    /// </summary>
    [SerializeField]
    private List<GameObject> objectsToSpawnPrefabInspector;

    /// <summary>
    /// This is the list of concepts available for the application
    /// </summary>
    [SerializeField] private List<GameObject> conceptsObjects;

    /// <summary>
    /// This is the list of images for the concepts available for the application
    /// </summary>
    [SerializeField] private List<Texture2D> conceptsImages;

    /// <summary>
    /// This is the list of video clips for the concepts available for the application
    /// </summary>
    [SerializeField] private List<VideoClip> videoClips;

    /// <summary>
    /// This is the list of audio clips for the concepts available for the application
    /// </summary>
    [SerializeField] private List<AudioClip> audioClips;

    /// <summary>
    /// Right hand/controller position
    /// </summary>
    [SerializeField]
    private Transform rightTransform;

    /// <summary>
    /// Left hand/controller position
    /// </summary>
    [SerializeField]
    private Transform leftTransform;

    /// <summary>
    /// Camera used for getting the position of the user
    /// </summary>
    [SerializeField] private Camera cam;

    /// <summary>
    /// Instance for the singleton
    /// </summary>
    public static InspectorManager instance;

    /// <summary>
    /// Sound played when the magnet is attached to a concept
    /// </summary>
    [SerializeField] private AudioClip magnetAttachSound;

    /// <summary>
    /// Sound played when the magnet is detached from a concept
    /// </summary>
    [SerializeField] private AudioClip magnetDetachSound;

    /// <summary>
    /// Sound played when a concept or magnet is spawned
    /// </summary>
    [SerializeField] private AudioClip spawnSound;

    /// <summary>
    /// Background music for the application
    /// </summary>
    [SerializeField] private AudioClip backgroundMusic;

    /// <summary>
    /// This is the prefab of dialog box connected to MRTK 2
    /// </summary>
    [SerializeField] private GameObject dialogPrefab;

    /// <summary>
    /// Material for the particle system
    /// </summary>
    [SerializeField] private Material particleMaterial;

    /// <summary>
    /// This is the main menu
    /// </summary>
    [SerializeField] private GameObject buttonBar;



    /// <summary>
    /// This is the prefab for the room, used on pc
    /// </summary>
    [SerializeField] private GameObject RoomPrefab;

    /// <summary>
    /// This is the visualizer for the room prefab
    /// </summary>
    [SerializeField] private Material roomVisualizer;



    /// <summary>
    /// This is the prefab for the sound indicator, used for audio concepts
    /// </summary>
    [SerializeField] private GameObject SoundIndicatorPrefab;

    /// <summary>
    /// This is the prefab with buttons for rotating the concepts when spawned
    /// </summary>
    [SerializeField] private GameObject conceptRotatorPrefab;






    /// <summary>
    /// Executed before Start
    /// </summary>
    void Awake()
    {

        if (instance == null)
        {
            instance = this;
            Debug.Log("mydebug InspectorManager created");
        }
        else if (instance != this)
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);

        Debug.Log("mydebug InspectorManager finished");
    }



    /// <summary>
    /// Getter for RoomPrefab
    /// </summary>
    /// <returns></returns>
    public GameObject GetRoomPrefab()
    {
        return RoomPrefab;
    }



    /// <summary>
    /// Getter for roomVisualizer
    /// </summary>
    /// <returns></returns>
    public Material GetRoomVisualizer()
    {
        return roomVisualizer;
    }



    /// <summary>
    /// Getter for buttonBar
    /// </summary>
    /// <returns></returns>
    public GameObject GetButtonBar()
    {
        return buttonBar;
    }



    /// <summary>
    /// Getter for objectsToSpawnPrefabInspector
    /// </summary>
    /// <returns></returns>
    public List<GameObject> GetObjectsToSpawnPrefabInspector()
    {
        return objectsToSpawnPrefabInspector;
    }

    /// <summary>
    /// Getter for concepts objects
    /// </summary>
    /// <returns></returns>
    public List<GameObject> GetConceptsObjects()
    {
        return conceptsObjects;
    }

    /// <summary>
    /// Getter for rightTransform
    /// </summary>
    /// <returns></returns>
    public Transform GetRightTransform()
    {
        return rightTransform;
    }

    /// <summary>
    /// Getter for leftTransform
    /// </summary>
    /// <returns></returns>
    public Transform GetLeftTransform()
    {
        return leftTransform;
    }

    /// <summary>
    /// Getter for cam
    /// </summary>
    /// <returns></returns>
    public Camera GetCam()
    {
        return cam;
    }

    /// <summary>
    /// Getter for magnetAttachSound
    /// </summary>
    /// <returns></returns>
    public AudioClip GetMagnetAttachSound()
    {
        return magnetAttachSound;
    }

    /// <summary>
    /// Getter for magnetDetachSound
    /// </summary>
    /// <returns></returns>
    public AudioClip GetMagnetDetachSound()
    {
        return magnetDetachSound;
    }



    /// <summary>
    /// Getter for spawnSound
    /// </summary>
    /// <returns></returns>
    public AudioClip GetSpawnSound()
    {
        return spawnSound;
    }



    /// <summary>
    /// Getter for dialogPrefab
    /// </summary>
    /// <returns></returns>
    public GameObject GetDialogPrefab()
    {
        return dialogPrefab;
    }



    /// <summary>
    /// Getter for backgroundMusic
    /// </summary>
    /// <returns></returns>
    public AudioClip GetBackgroundMusic()
    {
        return backgroundMusic;
    }



    /// <summary>
    /// Getter for particleMaterial
    /// </summary>
    /// <returns></returns>
    public Material GetParticleMaterial()
    {
        return particleMaterial;
    }



    /// <summary>
    /// Getter for conceptsImages
    /// </summary>
    /// <returns></returns>
    public List<Texture2D> GetConceptsImages()
    {
        return conceptsImages;
    }



    /// <summary>
    /// Getter for videoClips
    /// </summary>
    /// <returns></returns>
    public List<VideoClip> GetVideoClips()
    {
        return videoClips;
    }



    /// <summary>
    /// Getter for audioClips
    /// </summary>
    /// <returns></returns>
    public List<AudioClip> GetAudioClips()
    {
        return audioClips;
    }



    /// <summary>
    /// Getter for SoundIndicatorPrefab
    /// </summary>
    /// <returns></returns>
    public GameObject GetSoundIndicatorPrefab()
    {
        return SoundIndicatorPrefab;
    }

    public GameObject GetConceptRotatorPrefab()
    {
        return conceptRotatorPrefab;
    }
}
