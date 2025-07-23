using System;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.Video;

public class ObjectManager : MonoBehaviour
{
    /// <summary>
    /// This dictionary is used to connect each kind of spawnable object set in the inspector to its prefab gameobject
    /// </summary>
    private readonly Dictionary<ObjectType, GameObject> objectPrefabDictionaryInspector =
        new Dictionary<ObjectType, GameObject>();

    /// <summary>
    /// This is the list of objects which can be spawned in the sample scene
    /// </summary>
    private readonly List<ObjectType> sampleSceneObjectsType = new List<ObjectType>
            { ObjectType.LociSphere, ObjectType.LociCube };

    /// <summary>
    /// This is the list of concepts currently rendered in the scene.
    /// </summary>
    private readonly List<GameObject> conceptsInScene = new List<GameObject>();

    /// <summary>
    /// This is the dictionary that connects each concept id to its gameobject that is already in the scene
    /// because they are all downloaded and instantiated at the beginning of the activity, but they are all disabled and
    /// waiting to be enabled and "spanwed" in the right position as they were instantiated in this moment. This
    /// is done for performance reasons.
    /// </summary>
    private Dictionary<string, GameObject> conceptsToMove;

    /// <summary>
    /// This is used for knowing the order of the concepts to spawn in the scene
    /// </summary>
    private List<string> conceptsIdOrder = new List<string>();

    /// <summary>
    /// This is the list of magnets in the scene
    /// </summary>
    private List<GameObject> magnetsInScene = new List<GameObject>();



    /// <summary>
    /// Initializer of ObjectManager
    /// </summary>
    public void Init(Dictionary<string, GameObject> conceptsToMove, List<string> conceptsIdOrder)
    {
        this.conceptsIdOrder = conceptsIdOrder;
        this.conceptsToMove = conceptsToMove;

        if (InspectorManager.instance == null)
            Debug.LogError("mydebug InspectorManager not found");

        else
            Debug.Log("mydebug InspectorManager found");


        foreach (GameObject gameObject in InspectorManager.instance.GetObjectsToSpawnPrefabInspector())
        {
            objectPrefabDictionaryInspector.Add(Enum.Parse<ObjectType>(gameObject.tag), gameObject);
        }

        Debug.Log("mydebug ObjectManager initialized");
    }



    /// <summary>
    /// This method returns the prefab of the object type associated, it works for prefab passed in ispector manager, not for concepts
    /// </summary>
    /// <param name="objectType"></param>
    /// <returns></returns>
    public GameObject GetObjectPrefabFromTypeNoConcept(ObjectType objectType)
    {
        if (objectType != ObjectType.LociConcept)
        {
            return objectPrefabDictionaryInspector[objectType];
        }
        else
        {
            Debug.LogError("mydebug YOU CAN'T GET A CONCEPT PREFAB WITH THIS METHOD");
        }

        Debug.LogError("mydebug GetObjectFromType object not found");
        return null;
    }



    /// <summary>
    /// This method returns the list of gameobjects of the concepts in the scene
    /// </summary>
    /// <returns></returns>
    public List<GameObject> GetConceptsInScene()
    {
        return conceptsInScene;
    }



    /// <summary>
    /// This method returns the list of gameobjects of the magnets in the scene
    /// </summary>
    /// <returns></returns>
    public List<GameObject> GetMagnetsInScene()
    {
        return magnetsInScene;
    }



    /// <summary>
    /// This method returns a random object type from the sample scene
    /// </summary>
    /// <returns></returns>
    public ObjectType GetSampleSceneObjectType()
    {
        return sampleSceneObjectsType[UnityEngine.Random.Range(0, sampleSceneObjectsType.Count)];
    }



    /// <summary>
    /// This method instantiate the next concept to be "spawnded" in the scene.
    /// It is not really instantiated, but it is enabled and moved to the right position because it 
    /// was downloaded and instantiated at the beginning of the activity, but it is disabled and waiting to be enabled.
    /// However, for semplicity, we can say that it is instantiated and the download is hidden in this phase 
    /// </summary>
    /// <param name="position"></param>
    /// <param name="lociManager"></param>
    public GameObject SpawnNextConcept(Vector3 position, LociManager lociManager)
    {

        List<string> conceptsInSceneIds = new List<string>();
        foreach (GameObject concept in conceptsInScene)
        {
            conceptsInSceneIds.Add(concept.GetComponent<ConceptController>().GetId());
        }

        foreach (string conceptId in conceptsIdOrder)
        {
            if (!conceptsInSceneIds.Contains(conceptId))
            {
                GameObject currGameObject = conceptsToMove[conceptId];
                EnableConcept(conceptId, position, Quaternion.LookRotation(InspectorManager.instance.GetCam().transform.forward));
                currGameObject.transform.Translate(new Vector3(0, GetTotalBounds(currGameObject).size.y / 2, 0));
                return currGameObject;
            }
        }

        Debug.LogError("mydebug No concepts available");
        return null;
    }



    /// <summary>
    /// This method returns number of concepts of the application
    /// </summary>
    /// <returns></returns>
    public int GetConceptsCount()
    {
        return conceptsIdOrder.Count;
    }



    /// <summary>
    /// This method returns true if the object type is a concept, false otherwise
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool IsConcept(GameObject obj)
    {
        if (obj.CompareTag(ObjectType.LociConcept.ToString()))
        {
            Debug.Log("mydebug IsConcept: " + ObjectType.LociConcept.ToString());
            return true;

        }


        return false;
    }



    /// <summary>
    /// This method returns true if the object type is a magnet, false otherwise
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool IsMagnet(GameObject obj)
    {

        if (obj.CompareTag(ObjectType.LociMagnet.ToString()))
        {
            Debug.Log("mydebug IsMagnet: " + ObjectType.LociMagnet.ToString());
            return true;

        }


        return false;
    }



    /// <summary>
    /// This method returns true if the object type is a table, false otherwise
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool IsTable(GameObject obj)
    {

        if (obj.CompareTag(ObjectType.LociTable.ToString()))
        {
            Debug.Log("mydebug IsTable: " + ObjectType.LociTable.ToString());
            return true;
        }


        return false;
    }



    /// <summary>
    /// This method adds a concept to the list of concepts enabled in the scene
    /// </summary>
    /// <param name="concept"></param>
    public void AddConceptToScene(GameObject concept)
    {
        conceptsInScene.Add(concept);
    }



    /// <summary>
    /// This method initializes the concepts in the scene
    /// </summary>
    /*public void InitializeConceptsInScene(SpatialAnchorManager spatialAnchorManager)
    {
        foreach (GameObject concept in spatialAnchorManager.GetLociConceptsInScene())
        {
            conceptsInScene.Add(concept);
        }
    }*/



    /// <summary>
    /// This method resets the gameobjects of concepts in scene
    /// </summary>
    public void ResetConceptsInScene()
    {
        conceptsInScene.Clear();
    }





    /// <summary>
    /// This method is used for enabling a concept in the scene, it is used for enabling the concept when it is needed and put it in the right position
    /// </summary>
    /// <param name="id"></param>
    /// <param name="pos"></param>
    /// <param name="rot"></param>
    public GameObject EnableConcept(string id, Vector3 pos, Quaternion rot)
    {
        conceptsToMove[id].SetActive(true);
        conceptsToMove[id].transform.position = pos;
        conceptsToMove[id].transform.rotation = rot;

        return conceptsToMove[id];
    }




    /// <summary>
    /// This method is used for disabling a concept in the scene
    /// </summary>
    /// <param name="id"></param>
    public void DisableConcept(string id)
    {
        conceptsToMove[id].SetActive(false);
    }

    /// <summary>
    /// This method is used for getting the total bounds of a game object
    /// </summary>
    /// <param name="gameObject"></param>
    /// <returns></returns>
    private Bounds GetTotalBounds(GameObject go)
    {

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

        Debug.Log("mydebug number of renderers found " + renderers.Length);

        if (renderers.Length == 0)
        {
            Debug.LogError("mydebug no renderers found");
            return new Bounds(go.transform.position, Vector3.zero);
        }

        Bounds totalBounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            totalBounds.Encapsulate(renderers[i].bounds);
        }

        return totalBounds;
    }



    /// <summary>
    /// This method is used for getting the list of concepts id order
    /// </summary>
    /// <returns></returns>
    public List<string> GetConceptsIdOrder()
    {
        return conceptsIdOrder;
    }



    /// <summary>
    /// This method is used for reordering the magnets in the scene from the first concept keeping the orderer list
    /// </summary>
    /// <param name="sortedMagnetsClockWise"></param>
    /// <param name="magnetsData"></param>
    /// <returns></returns>
    public List<GameObject> ReorderMagnetsFromFirst(List<GameObject> sortedMagnetsClockWise,
        Dictionary<GameObject, MagnetData> magnetsData)
    {

        GameObject firstMagnet = null;
        int targetIndex = -1;

        foreach (GameObject magnet in magnetsData.Keys)
        {
            if (magnetsData[magnet].GetConceptAssociated().GetComponent<ConceptController>().GetId() ==
                conceptsIdOrder[0])
            {
                firstMagnet = magnet;
                break;
            }
        }

        if (firstMagnet == null)
        {
            Debug.LogError("mydebug first magnet not found");
            return null;
        }

        for (int i = 0; i < sortedMagnetsClockWise.Count; i++)
        {
            if (sortedMagnetsClockWise[i] == firstMagnet)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex <= 0)
        {
            return sortedMagnetsClockWise;
        }

        else
        {

            List<GameObject> reorderedMagnets = new List<GameObject>();

            reorderedMagnets.AddRange(sortedMagnetsClockWise.GetRange(targetIndex,
                sortedMagnetsClockWise.Count - targetIndex));
            reorderedMagnets.AddRange(sortedMagnetsClockWise.GetRange(0, targetIndex));

            return reorderedMagnets;

        }

    }

    /// <summary>
    /// This method instantiates a 3D concept in the scene, keeping it disabled.
    /// MISSING FUNCTIONALITY: it shouldn't receive a prefab, but downloaded resources
    /// </summary>
    /// <param name="id"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="objectPrefab"></param>
    /// <returns></returns>
    public GameObject Instantiate3DObject(string id, Vector3 position, Quaternion rotation, GameObject objectPrefab, LociManager lociManager)
    {
        Debug.Log("mydebug InstantiateConcept, object: " + id);

        GameObject obj = Instantiate(objectPrefab, Vector3.zero, Quaternion.identity);

        GameObject fatherObj = new GameObject("3d concept");

        if (obj.GetComponent<Collider>() != null)
            Destroy(obj.GetComponent<Collider>());

        obj.transform.SetParent(fatherObj.transform);

        BoxCollider collider = fatherObj.AddComponent<BoxCollider>();

        Bounds totalBounds = GetTotalBounds(fatherObj);

        collider.center = fatherObj.transform.InverseTransformPoint(totalBounds.center);

        Vector3 scale = fatherObj.transform.lossyScale;
        collider.size = new Vector3(
            totalBounds.size.x / Mathf.Abs(scale.x),
            totalBounds.size.y / Mathf.Abs(scale.y),
            totalBounds.size.z / Mathf.Abs(scale.z)
        );

        Debug.Log("mydebug InstantiateConcept, object dimensions: " + totalBounds.size);

        Vector3 initialScale = fatherObj.transform.localScale;
        float maxDimension = Mathf.Max(totalBounds.size.x, totalBounds.size.y, totalBounds.size.z);
        Debug.Log("mydebug maxDimension: " + maxDimension);
        float scaleFactor = 0.4f / maxDimension;
        fatherObj.transform.localScale = new Vector3(scaleFactor * initialScale.x, scaleFactor * initialScale.y, scaleFactor * initialScale.z);

        ObjectManipulator objectManipulator = fatherObj.AddComponent<ObjectManipulator>();
        fatherObj.AddComponent<NearInteractionGrabbable>();
        SpatialAnchorManipulation spatialAnchorManipulation = fatherObj.AddComponent<SpatialAnchorManipulation>();
        ConceptController conceptController = fatherObj.AddComponent<ConceptController>();
        fatherObj.tag = "LociConcept";
        conceptController.PrepareConcept(id, ConceptType.OBJECT3D);
        objectManipulator.OnManipulationStarted.AddListener(spatialAnchorManipulation.ManipulationStarted);
        objectManipulator.OnManipulationEnded.AddListener(spatialAnchorManipulation.ManipulationEnded);

        Debug.Log("mydebug InstantiateConcept, object dimensions: " + totalBounds.size);

        fatherObj.transform.rotation = rotation;
        fatherObj.transform.position = position;

        fatherObj.transform.parent = lociManager.gameObject.transform;

        fatherObj.SetActive(false);
        return fatherObj;
    }


    /// <summary>
    /// This method instantiates a audio object in the scene, keeping it disabled.
    /// MISSING FUNCTIONALITY: it shouldn't receive an audio, but downloaded resources
    /// </summary>
    /// <param name="id"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="clip"></param>
    /// <returns></returns>
    public GameObject InstantiateAudioObject(string id, Vector3 position, Quaternion rotation, AudioClip clip, LociManager lociManager)
    {
        Debug.Log("mydebug InstantiateConcept, audio: " + id);

        GameObject obj = Instantiate(InspectorManager.instance.GetSoundIndicatorPrefab(), position, rotation);

        AudioSource audioSource = obj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f;
        audioSource.volume = 1.0f;
        audioSource.loop = true;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = 3.0f;
        audioSource.minDistance = 0.1f;
        audioSource.clip = clip;

        obj.AddComponent<BoxCollider>();

        ObjectManipulator objectManipulator = obj.AddComponent<ObjectManipulator>();
        obj.AddComponent<NearInteractionGrabbable>();
        SpatialAnchorManipulation spatialAnchorManipulation = obj.AddComponent<SpatialAnchorManipulation>();
        ConceptController conceptController = obj.AddComponent<ConceptController>();
        obj.tag = "LociConcept";
        conceptController.PrepareConcept(id, ConceptType.AUDIO);
        objectManipulator.OnManipulationStarted.AddListener(spatialAnchorManipulation.ManipulationStarted);
        objectManipulator.OnManipulationEnded.AddListener(spatialAnchorManipulation.ManipulationEnded);

        obj.transform.parent = lociManager.gameObject.transform;
        obj.SetActive(false);
        return obj;
    }


    /// <summary>
    /// This method instantiates a video object in the scene, keeping it disabled.
    /// MISSING FUNCTIONALITY: it shouldn't receive a video, but downloaded resources
    /// </summary>
    /// <param name="id"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="clip"></param>
    /// <returns></returns>
    public GameObject InstantiateVideoObject(string id, Vector3 position, Quaternion rotation, VideoClip clip, LociManager lociManager)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        Debug.Log("mydebug InstantiateConcept, video: " + id);

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);

        AudioSource audioSource = quad.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f;
        audioSource.volume = 1.0f;
        audioSource.loop = false;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.maxDistance = 3.0f;
        audioSource.minDistance = 0.1f;

        VideoPlayer videoPlayer = quad.AddComponent<VideoPlayer>();
        VideoClip video = clip;
        videoPlayer.clip = video;
        videoPlayer.isLooping = true;
        videoPlayer.playOnAwake = false;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);


        GameObject backQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backQuad.transform.SetParent(quad.transform);
        backQuad.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 180, 0));
        backQuad.transform.localScale = Vector3.one;
        Destroy(backQuad.GetComponent<MeshCollider>());

        VideoPlayer videoPlayerBack = backQuad.AddComponent<VideoPlayer>();
        videoPlayerBack.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayerBack.clip = video;
        videoPlayerBack.isLooping = true;
        videoPlayerBack.playOnAwake = false;


        float aspectRatio = (float)video.width / video.height;
        Vector3 newScale = Vector3.one;


        if (aspectRatio >= 1f)
        {
            newScale.x = 0.4f;
            newScale.y = 0.4f / aspectRatio;
        }

        else
        {
            newScale.x = 0.4f * aspectRatio;
            newScale.y = 0.4f;
        }

        quad.transform.localScale = newScale;

        Destroy(quad.GetComponent<MeshCollider>());
        quad.AddComponent<BoxCollider>();

        ObjectManipulator objectManipulator = quad.AddComponent<ObjectManipulator>();
        quad.AddComponent<NearInteractionGrabbable>();
        SpatialAnchorManipulation spatialAnchorManipulation = quad.AddComponent<SpatialAnchorManipulation>();
        ConceptController conceptController = quad.AddComponent<ConceptController>();
        quad.tag = "LociConcept";
        conceptController.PrepareConcept(id, ConceptType.VIDEO);
        objectManipulator.OnManipulationStarted.AddListener(spatialAnchorManipulation.ManipulationStarted);
        objectManipulator.OnManipulationEnded.AddListener(spatialAnchorManipulation.ManipulationEnded);

        quad.transform.position = position;
        quad.transform.rotation = rotation;

        quad.transform.parent = lociManager.gameObject.transform;

        quad.GetComponent<Renderer>().material = material;
        backQuad.GetComponent<Renderer>().material = material;
        quad.SetActive(false);
        return quad;
    }


    /// <summary>
    /// This method instantiates an image object in the scene, keeping it disabled.
    /// MISSING FUNCTIONALITY: it shouldn't receive a texture, but downloaded resources
    /// </summary>
    /// <param name="id"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="textureInput"></param>
    /// <returns></returns>
    public GameObject InstantiateImageObject(string id, Vector3 position, Quaternion rotation, Texture2D textureInput, LociManager lociManager)
    {
        Debug.Log("mydebug InstantiateConcept, image: " + id);

        Texture2D texture = textureInput;

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Renderer quadRenderer = quad.GetComponent<Renderer>();

        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0f);
        material.SetFloat("_Surface", 0);

        quadRenderer.material = material;

        GameObject backQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Renderer backQuadRenderer = backQuad.GetComponent<Renderer>();
        backQuadRenderer.material = material;
        backQuad.transform.SetParent(quad.transform);
        backQuad.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 180, 0));
        backQuad.transform.localScale = Vector3.one;
        Destroy(backQuad.GetComponent<MeshCollider>());

        float aspectRatio = (float)texture.width / texture.height;
        Vector3 newScale = Vector3.one;


        if (aspectRatio >= 1f)
        {
            newScale.x = 0.4f;
            newScale.y = 0.4f / aspectRatio;
        }

        else
        {
            newScale.x = 0.4f * aspectRatio;
            newScale.y = 0.4f;
        }

        quad.transform.localScale = newScale;

        Destroy(quad.GetComponent<MeshCollider>());
        quad.AddComponent<BoxCollider>();

        ObjectManipulator objectManipulator = quad.AddComponent<ObjectManipulator>();
        quad.AddComponent<NearInteractionGrabbable>();
        SpatialAnchorManipulation spatialAnchorManipulation = quad.AddComponent<SpatialAnchorManipulation>();
        ConceptController conceptController = quad.AddComponent<ConceptController>();
        quad.tag = "LociConcept";
        conceptController.PrepareConcept(id, ConceptType.IMAGE);
        objectManipulator.OnManipulationStarted.AddListener(spatialAnchorManipulation.ManipulationStarted);
        objectManipulator.OnManipulationEnded.AddListener(spatialAnchorManipulation.ManipulationEnded);

        quad.transform.position = position;
        quad.transform.rotation = rotation;

        quad.transform.parent = lociManager.gameObject.transform;
        quad.SetActive(false);
        return quad;
    }



}
