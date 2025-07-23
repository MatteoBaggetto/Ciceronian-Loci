using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class ConceptController : MonoBehaviour
{
    /// <summary>
    /// This is the id of the concept, it univocally identifies the concept
    /// </summary>
    [SerializeField] private string id;

    /// <summary>
    /// This is the type of the concept
    /// </summary>
    private ConceptType conceptType;

    /// <summary>
    /// This is the list of video players, if the concept is a video
    /// </summary>
    private List<VideoPlayer> videoPlayers = new List<VideoPlayer>();

    /// <summary>
    /// This is the audiosource, if the concept is an audio
    /// </summary>
    private AudioSource audioSource;

    /// <summary>
    /// This is the coroutine of the concept, if the concept has any
    /// </summary>
    private Coroutine conceptCoroutine = null;



    /// <summary>
    /// This method is called when the concept is moved for the first time, it saves its rotation
    /// </summary>
    /// <param name="controller"></param>
    /// <param name="RotatorConcepts"></param>
    public void ConceptMovedFirstTime(Controller controller)
    {

        if (conceptType == ConceptType.OBJECT3D)
        {
            Debug.Log("mydebug: Concept moved first time, calling concept moved");
            Quaternion rotation = gameObject.transform.GetChild(0).transform.localRotation;
            List<float> rotationList = new List<float>();
            rotationList.Add(rotation.x);
            rotationList.Add(rotation.y);
            rotationList.Add(rotation.z);
            rotationList.Add(rotation.w);
            controller.SaveRotationData(id, rotationList);

        }

    }


    /// <summary>
    /// Setter for the id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="conceptType"></param>
    public void PrepareConcept(string id, ConceptType conceptType)
    {
        this.id = id;
        this.conceptType = conceptType;

        if (conceptType == ConceptType.VIDEO)
        {
            foreach (VideoPlayer videoPlayer in GetComponentsInChildren<VideoPlayer>())
            {
                videoPlayers.Add(videoPlayer);
            }

            conceptCoroutine = StartCoroutine(VideoConceptUpdate());

        }
        else if (conceptType == ConceptType.AUDIO)
        {
            audioSource = GetComponentInChildren<AudioSource>();

            conceptCoroutine = StartCoroutine(AudioConceptUpdate());
        }

    }



    /// <summary>
    /// Getter for the id
    /// </summary>
    /// <returns></returns>
    public string GetId()
    {
        return id;
    }



    /// <summary>
    /// Getter for the concept type
    /// </summary>
    /// <returns></returns>
    public ConceptType GetConceptType()
    {
        return conceptType;
    }



    /// <summary>
    /// This method plays or stops the video
    /// </summary>
    /// <param name="play"></param>
    private void PlayVideo(bool play)
    {
        if (conceptType != ConceptType.VIDEO)
        {
            Debug.LogError("This concept is not a video, you can't play it");
            return;
        }

        if (play)
        {
            foreach (VideoPlayer videoPlayer in videoPlayers)
            {
                if (videoPlayer.isPlaying == false)
                    videoPlayer.Play();
            }

        }
        else
        {
            foreach (VideoPlayer videoPlayer in videoPlayers)
            {
                videoPlayer.Pause();
            }
        }

    }



    /// <summary>
    /// Used as update method for video concepts
    /// </summary>
    /// <returns></returns>
    private IEnumerator VideoConceptUpdate()
    {
        Debug.Log("mydebug: VideoConceptUpdate started because the concept is a video" + id);

        while (true)
        {


            Vector3 directionToTarget =
                (gameObject.transform.position - InspectorManager.instance.GetCam().transform.position).normalized;
            float dotProduct = Vector3.Dot(InspectorManager.instance.GetCam().transform.forward, directionToTarget);
            float threshold = Mathf.Cos(20 * Mathf.Deg2Rad);
            if (dotProduct > threshold && Vector3.Distance(gameObject.transform.position,
                    InspectorManager.instance.GetCam().transform.position) < 0.8f)
                PlayVideo(true);
            else
                PlayVideo(false);

            yield return null;
        }


    }



    /// <summary>
    /// Used as update method for audio concepts
    /// </summary>
    /// <returns></returns>
    private IEnumerator AudioConceptUpdate()
    {
        Debug.Log("mydebug: AudioConceptUpdate started because the concept is a audio" + id);

        bool audioPlaying = false;

        Material firstMaterial = gameObject.GetComponent<Renderer>().material;
        Material secondMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        secondMaterial.SetColor("_BaseColor", Color.red);
        secondMaterial.SetFloat("_Metallic", 0.45f);
        secondMaterial.SetFloat("_Smoothness", 0.88f);

        while (true)
        {


            Vector3 directionToTarget =
                (gameObject.transform.position - InspectorManager.instance.GetCam().transform.position).normalized;
            float dotProduct = Vector3.Dot(InspectorManager.instance.GetCam().transform.forward, directionToTarget);
            float threshold = Mathf.Cos(20 * Mathf.Deg2Rad);
            if (dotProduct > threshold && Vector3.Distance(gameObject.transform.position,
                    InspectorManager.instance.GetCam().transform.position) < 0.8f)
            {
                if (audioPlaying == false)
                {
                    gameObject.GetComponent<Renderer>().material = secondMaterial;
                    audioSource.Play();
                    Debug.Log("mydebug: AudioConceptUpdate started playing audio");
                    audioPlaying = true;
                }
            }
            else
            {
                if (audioPlaying)
                {
                    gameObject.GetComponent<Renderer>().material = firstMaterial;
                    audioSource.Pause();
                    audioPlaying = false;
                }
            }

            yield return null;
        }


    }



    /// <summary>
    /// This method is called when the concept is enabled
    /// </summary>
    void OnEnable()
    {
        if (conceptType == ConceptType.VIDEO)
        {
            if (conceptCoroutine != null)
            {
                StopCoroutine(conceptCoroutine);
                Debug.Log("mydebug: VideoConceptUpdate stopped for safety");
                conceptCoroutine = StartCoroutine(VideoConceptUpdate());
            }
            else
            {
                Debug.Log("mydebug: VideoConceptUpdate coroutine is null");
            }
        }

        else if (conceptType == ConceptType.AUDIO)
        {
            if (conceptCoroutine != null)
            {
                StopCoroutine(conceptCoroutine);
                Debug.Log("mydebug: AudioConceptUpdate stopped for safety");
                conceptCoroutine = StartCoroutine(AudioConceptUpdate());
            }
            else
            {
                Debug.Log("mydebug: AudioConceptUpdate coroutine is null");
            }
        }

    }



}
