using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


/*
ATTENTION

1) In meta quest 3 "scene" comprehends all rooms saved in the device, but at the same time
in this code is used to indicate the context of the application. It is easy to understand the difference
while reading! 

2) MRUKRoom.ToString() appears to give a cutstomized code which identyifies the room saved in meta quest 3, even if there are not overrides of the method.
While using this method with pre-made prefabs of MRUK only return the name of the prefab. 

*/




public class SpatialAnchorManager : MonoBehaviour
{

    /// <summary>
    /// This is the list of all anchors currently rendered in the scene with their object, NOT the list of all saved anchors in the memory
    /// </summary>
    private readonly List<OVRSpatialAnchor> allAnchorsEnabled = new List<OVRSpatialAnchor>();

    /// <summary>
    /// This dictionary is used to access all anchors connected to a certain experience
    /// </summary>
    private Dictionary<string, Dictionary<System.Guid, AnchorType>> anchorDictionary =
        new Dictionary<string, Dictionary<System.Guid, AnchorType>>();

    /// <summary>
    /// Used for loading and instantiating saved anchors from memory to the current scene with their associated object
    /// </summary>
    private Action<bool, OVRSpatialAnchor.UnboundAnchor> onLocalized;

    /// <summary>
    /// There is an hard limit of how many anchors can be deleted in one call. For my choice, here number is 30 instead of 32 (which is the real limit)
    /// </summary>
    private const int maxAnchorsForErasing = 30;

    /// <summary>
    /// There is an hard limit of how many anchors can be loaded in one call. For my choice, here number is 45 instead of 50 (which is the real limit)
    /// </summary>
    private const int maxAnchorsForLoading = 45;

    /// <summary>
    /// This is the key of the current experience
    /// </summary>
    private string currentKeyExperience;

    /// <summary>
    /// This is the reference to the object manager
    /// </summary>
    private ObjectManager objectManager;

    /// <summary>
    /// This is the reference to the controller
    /// </summary>
    private Controller controller;

    /// <summary>
    /// This is the reference to the loci manager
    /// </summary>
    private LociManager lociManager;

    /// <summary>
    /// Dictionary used to restore the rotation of the 3D concepts
    /// </summary>
    private Dictionary<string, List<float>> concepts3dRotation;



    /// <summary>
    /// This method is used for initializing the SpatialAnchorManager
    /// </summary>
    /// <param name="anchorDictionary"></param>
    /// <param name="currentKeyExperience"></param>
    /// <param name="objectManager"></param>
    /// <param name="controller"></param>
    public void Init(Dictionary<string, Dictionary<System.Guid, AnchorType>> anchorDictionary,
        string currentKeyExperience, ObjectManager objectManager, Controller controller,
        LociManager lociManager, Dictionary<string, List<float>> concepts3dRotation)
    {
        this.anchorDictionary = anchorDictionary;
        this.currentKeyExperience = currentKeyExperience;
        this.objectManager = objectManager;
        this.controller = controller;
        this.lociManager = lociManager;
        this.concepts3dRotation = concepts3dRotation;

        onLocalized = OnLocalized;

        if (Debugging.IsOnPC() == false)
        {
            LoadAllAnchorsFromCurrentExperience();

            Debug.Log("mydebug first anchor load OK");
        }

        Debug.Log("mydebug SpatialAnchorManager initialized");

    }



    /// <summary>
    /// This method returns if anchors are ready at the start of the application
    /// </summary>
    /// <returns></returns>
    public bool AreAnchorsReady()
    {

        Debug.Log("mydebug anchors ready?, size: anchors in memory " +
                  anchorDictionary[currentKeyExperience].Keys.Count + "size anchors in scene: " +
                  allAnchorsEnabled.Count);

        if (anchorDictionary[currentKeyExperience].Keys.Count == allAnchorsEnabled.Count)
        {
            Debug.Log("mydebug anchors ready");
            return true;
        }
        else
        {
            Debug.Log("mydebug anchors not ready");
            return false;
        }
    }



    /// <summary>
    /// Method used to load all anchors previously created in current experience from memory
    /// </summary>
    /// <param name="onlyMagnetsAndConceptsAndTable"></param>
    public async void LoadAllAnchorsFromCurrentExperience(bool onlyMagnetsAndConceptsAndTable = false)
    {

        if (anchorDictionary[currentKeyExperience].Keys.Count == 0)
        {
            Debug.Log("mydebug no anchors to load");
        }

        if (anchorDictionary == null)
        {
            Debug.LogError("mydebug anchorDictionary is null");
        }

        if (anchorDictionary[currentKeyExperience] == null)
        {
            Debug.LogError("mydebug anchorDictionary[currentKeyExperience] is null");
        }

        foreach (var guid in anchorDictionary[currentKeyExperience].Keys)
        {
            Debug.Log("mydebug current dictionary element during load: " +
                      anchorDictionary[currentKeyExperience][guid].GetObjectType());
        }

        if (anchorDictionary[currentKeyExperience].Keys.Count > maxAnchorsForLoading)
        {

            Debug.Log("mydebug load anchors many anchors");

            List<OVRSpatialAnchor.UnboundAnchor> allUnbounds = new List<OVRSpatialAnchor.UnboundAnchor>();

            int count = 0;
            List<List<System.Guid>> guids = new List<List<System.Guid>> { new List<System.Guid>() };


            foreach (System.Guid guid in anchorDictionary[currentKeyExperience].Keys)
            {

                if (guids[count].Count == maxAnchorsForLoading)
                {
                    count++;
                    guids.Add(new List<System.Guid>());
                }

                guids[count].Add(guid);

            }

            for (int k = 0; k <= count; k++)
            {

                // Load and localize
                var unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
                var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(guids[k], unboundAnchors);

                Debug.Log("mydebug unbound anchors found");

                if (result.Success)
                {
                    foreach (var anchor in unboundAnchors)
                    {
                        allUnbounds.Add(anchor);
                    }

                    Debug.Log("mydebug LoadAllAnchors OK");
                }
                else
                {
                    Debug.LogError($"mydebug LoadAllAnchors failed with {result.Status}.");
                }

            }

            foreach (var anchor in allUnbounds)
            {

                if (onlyMagnetsAndConceptsAndTable == true)
                {
                    if (anchorDictionary[currentKeyExperience][anchor.Uuid].GetObjectType() ==
                        ObjectType.LociConcept ||
                        anchorDictionary[currentKeyExperience][anchor.Uuid].GetObjectType() ==
                        ObjectType.LociMagnet ||
                        anchorDictionary[currentKeyExperience][anchor.Uuid].GetObjectType() == ObjectType.LociTable)
                    {
                        Debug.Log("mydebug anchor to localize of object: " +
                                  anchorDictionary[currentKeyExperience][anchor.Uuid].GetObjectType().ToString());
                        anchor.LocalizeAsync().ContinueWith(onLocalized, anchor);

                    }

                }

                else
                    anchor.LocalizeAsync().ContinueWith(onLocalized, anchor);

            }

        }
        else
        {

            Debug.Log("mydebug load anchors few anchors");

            // Load and localize
            var unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
            var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(anchorDictionary[currentKeyExperience].Keys,
                unboundAnchors);

            Debug.Log("mydebug unbound anchors found");

            if (result.Success)
            {
                foreach (var anchor in unboundAnchors)
                {
                    if (onlyMagnetsAndConceptsAndTable == true)
                    {
                        if (anchorDictionary[currentKeyExperience][anchor.Uuid].GetObjectType() ==
                            ObjectType.LociConcept ||
                            anchorDictionary[currentKeyExperience][anchor.Uuid].GetObjectType() ==
                            ObjectType.LociMagnet ||
                            anchorDictionary[currentKeyExperience][anchor.Uuid].GetObjectType() ==
                            ObjectType.LociTable)
                        {
                            Debug.Log("mydebug anchor to localize of object: " +
                                      anchorDictionary[currentKeyExperience][anchor.Uuid].GetObjectType()
                                          .ToString());
                            anchor.LocalizeAsync().ContinueWith(onLocalized, anchor);
                        }
                    }

                    else
                        anchor.LocalizeAsync().ContinueWith(onLocalized, anchor);
                }

                Debug.Log("mydebug LoadAllAnchors OK");
            }
            else
            {
                Debug.LogError($"mydebug LoadAllAnchors failed with {result.Status}.");
            }
        }

        Debug.Log("mydebug LoadAllAnchors, size anchors displayed: " + allAnchorsEnabled.Count);

        Debug.Log(
            "mydebug LoadAllAnchors, size anchors saved: " + anchorDictionary[currentKeyExperience].Keys.Count);
    }



    /// <summary>
    /// Method used for istantiating a specific anchor loaded from memory to the scene with its gameObject
    /// </summary>
    /// <param name="success"></param>
    /// <param name="unboundAnchor"></param>
    private void OnLocalized(bool success, OVRSpatialAnchor.UnboundAnchor unboundAnchor)
    {
        GameObject spawnedObject;
        if (unboundAnchor.TryGetPose(out var pose))
        {

            if (anchorDictionary[currentKeyExperience][unboundAnchor.Uuid].GetObjectType() ==
                ObjectType.LociConcept)
            {

                spawnedObject = objectManager.EnableConcept(
                    anchorDictionary[currentKeyExperience][unboundAnchor.Uuid].GetId(), pose.position,
                    pose.rotation);


                string conceptID = anchorDictionary[currentKeyExperience][unboundAnchor.Uuid].GetId();

                if (concepts3dRotation.ContainsKey(conceptID) == true)
                {
                    Quaternion rotation = new Quaternion(
                    concepts3dRotation[conceptID][0],
                    concepts3dRotation[conceptID][1],
                    concepts3dRotation[conceptID][2],
                    concepts3dRotation[conceptID][3]);
                    spawnedObject.transform.GetChild(0).transform.localRotation = rotation;

                    Debug.Log("mydebug concept rotation restored");
                }


                Debug.Log("mydebug concept instantiated");

                objectManager.GetConceptsInScene().Add(spawnedObject);

                Debug.Log("mydebug concepts in scene: " + objectManager.GetConceptsInScene().Count);
                foreach (var concept in objectManager.GetConceptsInScene())
                {
                    Debug.Log("mydebug concept in scene: " + concept.name);
                }
            }
            else
            {
                spawnedObject =
                    Instantiate(
                        objectManager.GetObjectPrefabFromTypeNoConcept(
                            anchorDictionary[currentKeyExperience][unboundAnchor.Uuid].GetObjectType()),
                        pose.position, pose.rotation);
                spawnedObject.transform.parent = lociManager.gameObject.transform;
                Debug.Log("mydebug non concept instantiated");


                if (spawnedObject.CompareTag(ObjectType.LociMagnet.ToString()))
                {
                    objectManager.GetMagnetsInScene().Add(spawnedObject);
                    Debug.Log("mydebug magnets in scene: " + objectManager.GetMagnetsInScene().Count);
                }
                else if (spawnedObject.CompareTag(ObjectType.LociTable.ToString()))
                {
                    lociManager.SetTable(spawnedObject);
                    Debug.Log("mydebug table instantiated and saved reference in lociManager");
                }
            }

            if (Debugging.IsMainScene() == false)
            {
                spawnedObject.GetComponent<SpatialAnchorManipulation>().Init(objectManager, lociManager, this);
                Debug.Log("mydebug SpatialAnchorManipulation initialized in not main scene");
            }


            /*if(spawnedObject.CompareTag(ObjectType.LociConcept.ToString()) || spawnedObject.CompareTag(ObjectType.LociMagnet.ToString())){
                spawnedObject.SetActive(false);
                Debug.Log("mydebug object disabled for concept or magnet");
            }*/

            var anchor = spawnedObject.AddComponent<OVRSpatialAnchor>();

            unboundAnchor.BindTo(anchor);

            // Add the anchor to the running total
            allAnchorsEnabled.Add(anchor);

            Debug.Log("mydebug OnLocalized OK");
        }
        else
        {
            Debug.LogError("mydebug OnLocalized failed.");
        }
    }



    /// <summary>
    /// Method used for destroying all anchors and their objects from the scene but not from the memory
    /// </summary>
    public void DestroyAllAnchors()
    {

        foreach (var anchor in allAnchorsEnabled)
        {
            Destroy(anchor.gameObject);
        }

        allAnchorsEnabled.Clear();


    }



    /// <summary>
    /// Method used for erasing all anchors from memory from a certain scene
    /// </summary>
    public async void EraseAllAnchorsFromSpecificExperience(string experienceCode)
    {

        List<System.Guid> keysToErase = new List<System.Guid>();

        foreach (System.Guid guid in anchorDictionary[experienceCode].Keys)
        {
            keysToErase.Add(guid);
        }

        if (keysToErase.Count > maxAnchorsForErasing)
        {


            int count = 0;
            List<List<System.Guid>> guids = new List<List<System.Guid>> { new List<System.Guid>() };

            foreach (System.Guid guid in keysToErase)
            {

                if (guids[count].Count == maxAnchorsForErasing)
                {
                    count++;
                    guids.Add(new List<System.Guid>());
                }

                guids[count].Add(guid);

            }

            for (int k = 0; k <= count; k++)
            {

                var result = await OVRSpatialAnchor.EraseAnchorsAsync(anchors: null, uuids: guids[k]);
                if (result.Success)
                {
                    // Erase our reference lists

                    foreach (System.Guid guid in guids[k])
                    {
                        anchorDictionary[experienceCode].Remove(guid);
                    }

                    SaveAnchorDictionary();

                    Debug.Log("mydebug EraseAllAnchors OK");
                }
                else
                {
                    Debug.LogError($"mydebug EraseAllAnchors failed with {result.Status}");
                }

            }
        }

        else
        {

            var result = await OVRSpatialAnchor.EraseAnchorsAsync(anchors: null, uuids: keysToErase);
            if (result.Success)
            {
                // Erase our reference lists

                foreach (System.Guid guid in keysToErase)
                {
                    anchorDictionary[experienceCode].Remove(guid);
                }

                SaveAnchorDictionary();

                Debug.Log("mydebug EraseAllAnchors OK");
            }
            else
            {
                Debug.LogError($"mydebug EraseAllAnchors failed with {result.Status}");
            }

        }


    }



    /// <summary>
    /// Method used for erasing all anchors from memory from all scenes
    /// </summary>
    public void EraseAllAnchorsFromAllExperiences()
    {
        foreach (string experienceCode in anchorDictionary.Keys)
        {
            EraseAllAnchorsFromSpecificExperience(experienceCode);
        }
    }



    /// <summary>
    /// Method used to erasing from memory, but not from scene, a specific anchor
    /// </summary>
    /// <param name="anchor"></param>
    private async void EraseAnchorFromCurrentExperience(OVRSpatialAnchor anchor)
    {

        var result = await anchor.EraseAnchorAsync();
        if (result.Success)
        {
            ObjectType type = anchorDictionary[currentKeyExperience][anchor.Uuid].GetObjectType();

            anchorDictionary[currentKeyExperience].Remove(anchor.Uuid);

            SaveAnchorDictionary();

            Debug.Log($"mydebug Successfully erased anchor." + type.ToString());
        }
        else
        {
            Debug.LogError($"mydebug Failed to erase anchor {anchor.Uuid} with result {result.Status}");
        }


    }



    /// <summary>
    /// Method used to erasing from memory and from scene a specific anchor
    /// </summary>
    /// <param name="anchor"></param>
    /// <param name="obj"></param>
    private async void EraseAnchorObjectFromCurrentExperience(OVRSpatialAnchor anchor, GameObject obj)
    {

        var result = await anchor.EraseAnchorAsync();
        if (result.Success)
        {
            ObjectType type = anchorDictionary[currentKeyExperience][anchor.Uuid].GetObjectType();

            anchorDictionary[currentKeyExperience].Remove(anchor.Uuid);

            SaveAnchorDictionary();

            Debug.Log($"mydebug Successfully erased anchor." + type.ToString());

            Destroy(obj);
            Debug.Log("mydebug object destroyed");
        }
        else
        {
            Debug.LogError($"mydebug Failed to erase anchor {anchor.Uuid} with result {result.Status}");
        }


    }



    /// <summary>
    /// Method used to instantiate an object from the list of sampleSceneObjectType and attaching an anchor to it
    /// </summary>
    public void CreateObjectWithAnchorSampleScene()
    {

        ObjectType objectType = objectManager.GetSampleSceneObjectType();
        GameObject objectToSpawnPrefab = objectManager.GetObjectPrefabFromTypeNoConcept(objectType);

        GameObject spawnedObject = Instantiate(objectToSpawnPrefab, InspectorManager.instance.GetRightTransform().position, InspectorManager.instance.GetRightTransform().rotation);


        spawnedObject.transform.parent = lociManager.gameObject.transform;
        spawnedObject.GetComponent<SpatialAnchorManipulation>().Init(objectManager, lociManager, this);

        if (spawnedObject.CompareTag(ObjectType.LociConcept.ToString()))
        {
            SetupAnchorAsync(spawnedObject.AddComponent<OVRSpatialAnchor>(), spawnedObject.tag,
                spawnedObject.GetComponent<ConceptController>().GetId());
        }
        else
        {
            SetupAnchorAsync(spawnedObject.AddComponent<OVRSpatialAnchor>(), spawnedObject.tag);
        }

    }



    /// <summary>
    /// Method used to save a specific anchor in memory with its information
    /// </summary>
    /// <param name="anchor"></param>
    /// <param name="objectType"></param>
    /// <param name="id"></param>
    private async void SetupAnchorAsync(OVRSpatialAnchor anchor, string objectType, string id = null)
    {
        if (anchor == null)
        {
            Debug.LogError("mydebug NULL ANCHOR");
            return;
        }

        // Keep checking for a valid and localized anchor state
        if (!await anchor.WhenLocalizedAsync())
        {
            Debug.LogError($"mydebug SetupAnchorAsync failed.");
            Destroy(anchor.gameObject);
            return;
        }

        // Add the anchor to the list of all anchors active in the scene
        allAnchorsEnabled.Add(anchor);

        // save anchors
        if ((await anchor.SaveAnchorAsync()).Success)
        {
            // Remember UUID so you can load the anchor later, with associated information
            anchorDictionary[currentKeyExperience]
                .Add(anchor.Uuid, new AnchorType(Enum.Parse<ObjectType>(objectType), id));

            SaveAnchorDictionary();

            Debug.Log("mydebug SetupAnchorAsync OK " + objectType);

        }
    }



    /// <summary>
    /// Save all information from anchorDictionary to PlayerPrefs
    /// </summary>
    private void SaveAnchorDictionary()
    {

        controller.SaveAnchorDictionary(anchorDictionary);

    }



    /// <summary>
    /// Method used to erase from memory a specific anchor and destroy anchor component without touching its object
    /// </summary>
    /// <param name="anchor"></param>
    private void EraseAndDestroyAnchorFromCurrentExperience(OVRSpatialAnchor anchor)
    {


        anchor.enabled = false;

        allAnchorsEnabled.Remove(anchor);

        EraseAnchorFromCurrentExperience(anchor);

        Destroy(anchor);

    }



    /// <summary>
    /// Method used for eliminating an anchor and its object from the scene and from memory
    /// </summary>
    /// <param name="anchor"></param>
    /// <param name="gameObject"></param>
    public void EliminateAnchorAndObject(OVRSpatialAnchor anchor, GameObject gameObject)
    {

        anchor.enabled = false;

        gameObject.SetActive(false);

        allAnchorsEnabled.Remove(anchor);

        EraseAnchorObjectFromCurrentExperience(anchor, gameObject);


    }




    /// <summary>
    /// Method used to destroy a specific anchor without deleting it from memory and without touching its object
    /// </summary>
    /// <param name="anchor"></param>
    public void DestroyAnchor(OVRSpatialAnchor anchor)
    {

        allAnchorsEnabled.Remove(anchor);

        Destroy(anchor);

    }



    /// <summary>
    /// Method used in ObjectManipulation
    /// </summary>
    /// <param name="anchor"></param>
    public void MovementStarted(OVRSpatialAnchor anchor)
    {
        EraseAndDestroyAnchorFromCurrentExperience(anchor);
    }



    /// <summary>
    /// This method is used for eliminating an anchor from an object and from memory, and then it disables the object
    /// </summary>
    /// <param name="anchor"></param>
    public void DisableObjectEraseAndDestroyAnchor(OVRSpatialAnchor anchor)
    {
        GameObject obj = anchor.gameObject;
        EraseAndDestroyAnchorFromCurrentExperience(anchor);
        obj.SetActive(false);
    }



    /// <summary>
    /// Erase all anchors from memory which are concepts, do not use if these anchors are in the scene!
    /// </summary>
    public async Task EraseConceptsAnchorFromMemoryCurrentExperience()
    {

        List<System.Guid> keysToErase = new List<System.Guid>();


        foreach (System.Guid guid in anchorDictionary[currentKeyExperience].Keys)
        {
            if (anchorDictionary[currentKeyExperience][guid].GetObjectType() == ObjectType.LociConcept)
            {
                keysToErase.Add(guid);
            }
        }


        if (keysToErase.Count > maxAnchorsForErasing)
        {


            int count = 0;
            List<List<System.Guid>> guids = new List<List<System.Guid>> { new List<System.Guid>() };

            foreach (System.Guid guid in keysToErase)
            {

                if (guids[count].Count == maxAnchorsForErasing)
                {
                    count++;
                    guids.Add(new List<System.Guid>());
                }

                guids[count].Add(guid);

            }

            for (int k = 0; k <= count; k++)
            {

                var result = await OVRSpatialAnchor.EraseAnchorsAsync(anchors: null, uuids: guids[k]);
                if (result.Success)
                {
                    // Erase our reference lists

                    foreach (System.Guid guid in guids[k])
                    {
                        anchorDictionary[currentKeyExperience].Remove(guid);
                    }

                    SaveAnchorDictionary();

                    Debug.Log("mydebug EraseAllAnchors OK");
                }
                else
                {
                    Debug.LogError($"mydebug EraseAllAnchors failed with {result.Status}");
                }

            }
        }

        else
        {

            var result = await OVRSpatialAnchor.EraseAnchorsAsync(anchors: null, uuids: keysToErase);
            if (result.Success)
            {
                // Erase our reference lists

                foreach (System.Guid guid in keysToErase)
                {
                    anchorDictionary[currentKeyExperience].Remove(guid);
                }

                SaveAnchorDictionary();

                Debug.Log("mydebug EraseAllAnchors OK");

                Debug.Log("mydebug current dictionary size: " +
                          anchorDictionary[currentKeyExperience].Keys.Count);

                foreach (var guid in anchorDictionary[currentKeyExperience].Keys)
                {
                    Debug.Log("mydebug current dictionary element: " +
                              anchorDictionary[currentKeyExperience][guid].GetObjectType());
                }
            }
            else
            {
                Debug.LogError($"mydebug EraseAllAnchors failed with {result.Status}");
            }

        }

    }




    /// <summary>
    /// Erase all anchors from memory which are magnets, do not use if these anchors are in the scene!
    /// </summary>
    public async void EraseMagnetsAnchorFromMemoryCurrentExperience()
    {

        List<System.Guid> keysToErase = new List<System.Guid>();


        foreach (System.Guid guid in anchorDictionary[currentKeyExperience].Keys)
        {
            if (anchorDictionary[currentKeyExperience][guid].GetObjectType() == ObjectType.LociMagnet)
            {
                keysToErase.Add(guid);
            }
        }


        if (keysToErase.Count > maxAnchorsForErasing)
        {


            int count = 0;
            List<List<System.Guid>> guids = new List<List<System.Guid>> { new List<System.Guid>() };

            foreach (System.Guid guid in keysToErase)
            {

                if (guids[count].Count == maxAnchorsForErasing)
                {
                    count++;
                    guids.Add(new List<System.Guid>());
                }

                guids[count].Add(guid);

            }

            for (int k = 0; k <= count; k++)
            {

                var result = await OVRSpatialAnchor.EraseAnchorsAsync(anchors: null, uuids: guids[k]);
                if (result.Success)
                {
                    // Erase our reference lists

                    foreach (System.Guid guid in guids[k])
                    {
                        anchorDictionary[currentKeyExperience].Remove(guid);
                    }

                    SaveAnchorDictionary();

                    Debug.Log("mydebug EraseAllAnchors OK");
                }
                else
                {
                    Debug.LogError($"mydebug EraseAllAnchors failed with {result.Status}");
                }

            }
        }

        else
        {

            var result = await OVRSpatialAnchor.EraseAnchorsAsync(anchors: null, uuids: keysToErase);
            if (result.Success)
            {
                // Erase our reference lists

                foreach (System.Guid guid in keysToErase)
                {
                    anchorDictionary[currentKeyExperience].Remove(guid);
                }

                SaveAnchorDictionary();

                Debug.Log("mydebug EraseAllAnchors OK");
            }
            else
            {
                Debug.LogError($"mydebug EraseAllAnchors failed with {result.Status}");
            }

        }

    }



    /// <summary>
    /// Method used to erase from memory a list of anchors and destroy the component from the object, deleting or disabling also the object
    /// </summary>
    /// <param name="objs"></param>
    public async void EraseAndDestroyAnchorObjectListCurrentExperience(List<GameObject> objs)

    {
        List<System.Guid> keysToErase = new List<System.Guid>();

        foreach (GameObject obj in objs)
        {
            if (obj.TryGetComponent<OVRSpatialAnchor>(out OVRSpatialAnchor anchor))
            {
                anchor.enabled = false;
                allAnchorsEnabled.Remove(anchor);
                keysToErase.Add(anchor.Uuid);
                if (anchorDictionary[currentKeyExperience].ContainsKey(anchor.Uuid) == false)
                {
                    Debug.LogError("mydebug EraseAndDestroyListOfAnchorsCurrentExperience, anchor not found in dictionary, probably not current experience");
                }
            }
            else
            {
                Debug.Log($"mydebug GameObject {obj.name} doesn't have an OVRSpatialAnchor component");
            }
        }

        foreach (GameObject obj in objs)
        {
            if (obj.TryGetComponent<OVRSpatialAnchor>(out OVRSpatialAnchor anchor))
            {

                if (obj.tag == ObjectType.LociConcept.ToString())
                {
                    obj.SetActive(false);
                    Destroy(anchor);
                }
                else
                {
                    Destroy(obj);
                }

            }
            else
            {
                Debug.Log($"mydebug GameObject {obj.name} doesn't have an OVRSpatialAnchor component");
            }
        }


        if (keysToErase.Count > maxAnchorsForErasing)
        {


            int count = 0;
            List<List<System.Guid>> guids = new List<List<System.Guid>> { new List<System.Guid>() };

            foreach (System.Guid guid in keysToErase)
            {

                if (guids[count].Count == maxAnchorsForErasing)
                {
                    count++;
                    guids.Add(new List<System.Guid>());
                }

                guids[count].Add(guid);

            }

            for (int k = 0; k <= count; k++)
            {

                var result = await OVRSpatialAnchor.EraseAnchorsAsync(anchors: null, uuids: guids[k]);
                if (result.Success)
                {
                    // Erase our reference lists

                    foreach (System.Guid guid in guids[k])
                    {
                        anchorDictionary[currentKeyExperience].Remove(guid);
                    }

                    SaveAnchorDictionary();

                    Debug.Log("mydebug EraseAllAnchors OK");
                }
                else
                {
                    Debug.LogError($"mydebug EraseAllAnchors failed with {result.Status}");
                }

            }
        }

        else
        {

            var result = await OVRSpatialAnchor.EraseAnchorsAsync(anchors: null, uuids: keysToErase);
            if (result.Success)
            {
                // Erase our reference lists

                foreach (System.Guid guid in keysToErase)
                {
                    anchorDictionary[currentKeyExperience].Remove(guid);
                }

                SaveAnchorDictionary();

                Debug.Log("mydebug EraseAllAnchors OK");
            }
            else
            {
                Debug.LogError($"mydebug EraseAllAnchors failed with {result.Status}");
            }

        }



        Debug.Log("mydebug current dictionary size: " +
                          anchorDictionary[currentKeyExperience].Keys.Count);

        foreach (var guid in anchorDictionary[currentKeyExperience].Keys)
        {
            Debug.Log("mydebug current dictionary element: " +
                      anchorDictionary[currentKeyExperience][guid].GetObjectType());
        }
    }



    /// <summary>
    /// Method used in ObjectManipulation
    /// </summary>
    /// <param name="anchor"></param>
    /// <param name="objectType"></param>
    public void MovementEnded(OVRSpatialAnchor anchor, string objectType)
    {
        SetupAnchorAsync(anchor, objectType);
    }




    /// <summary>
    /// Method used to add an anchor to a specific object
    /// </summary>
    /// <param name="obj"></param>
    public void AddAnchorToObject(GameObject obj)
    {
        if (obj.GetComponent<OVRSpatialAnchor>() != null)
        {
            Debug.Log("mydebug OBJ HAS ALREADY AN ANCHOR" + obj.tag);
            return;
        }

        Debug.Log("mydebug start to add anchor to object: " + obj.tag);

        if (obj.CompareTag(ObjectType.LociConcept.ToString()))
        {
            SetupAnchorAsync(obj.AddComponent<OVRSpatialAnchor>(), obj.tag,
                obj.GetComponent<ConceptController>().GetId());
        }
        else
        {
            SetupAnchorAsync(obj.AddComponent<OVRSpatialAnchor>(), obj.tag);
        }
    }



    /// <summary>
    /// Method used for retriving all anchors in the scene which are concepts
    /// </summary>
    /// <returns></returns>
    /*public List<GameObject> GetLociConceptsInScene()
    {

        List<GameObject> concepts = new List<GameObject>();

        foreach (var anchor in allAnchorsEnabled)
        {
            if (objectManager.IsConcept(anchor.gameObject))
            {
                concepts.Add(anchor.gameObject);
            }
        }

        Debug.Log("mydebug concepts found: " + concepts.Count);
        return concepts;

    }*/



    /// <summary>
    /// Method used for retriving all anchors in the scene which are magnets
    /// </summary>
    /// <returns></returns>
    /*public List<GameObject> GetLociMagnetsInScene()
    {

        List<GameObject> magnets = new List<GameObject>();

        foreach (var anchor in allAnchorsEnabled)
        {
            if (objectManager.IsMagnet(anchor.gameObject))
            {
                magnets.Add(anchor.gameObject);
            }
        }

        Debug.Log("mydebug magnets found: " + magnets.Count);
        return magnets;

    }*/



    /// <summary>
    /// Method used for retriving the table anchor in the scene
    /// </summary>
    /// <returns></returns>
    /*public GameObject GetLociTableInScene()
    {

        Debug.Log("mydebug how many anchors: " + allAnchorsEnabled.Count);
        foreach (var anchor in allAnchorsEnabled)
        {
            Debug.Log("mydebug anchor found: " + anchor.gameObject.tag);
        }

        foreach (var anchor in allAnchorsEnabled)
        {
            if (objectManager.IsTable(anchor.gameObject))
            {
                Debug.Log("mydebug table found");
                return anchor.gameObject;
            }
        }

        Debug.LogError("mydebug table not found");
        return null;
    }*/



    /// <summary>
    /// Method used only on pc for debugging
    /// </summary>
    /// <param name="spawn"></param>
    public void CreateObjectWithoutAnchorPCSampleScene(Transform spawn)
    {

        ObjectType objectType = objectManager.GetSampleSceneObjectType();
        GameObject objectToSpawnPrefab = objectManager.GetObjectPrefabFromTypeNoConcept(objectType);

        GameObject spawnedObject = Instantiate(objectToSpawnPrefab,
            spawn.transform.position + spawn.transform.forward * 0.4f, objectToSpawnPrefab.transform.rotation);
        spawnedObject.transform.parent = lociManager.gameObject.transform;
    }



}