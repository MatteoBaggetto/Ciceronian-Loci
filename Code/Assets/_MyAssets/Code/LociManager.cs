using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Meta.XR.MRUtilityKit;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;


public class LociManager : MonoBehaviour
{

    /// <summary>
    /// Number of magnets left to be spawned, this is equal to number of concepts which will be applied to them
    /// </summary>
    private int leftMagnetToSpawn;

    /// <summary>
    /// Magnets in the scene associated to their data
    /// </summary>
    private readonly Dictionary<GameObject, MagnetData> magnetsData = new Dictionary<GameObject, MagnetData>();

    /// <summary>
    /// Table in the scene over which the magnets and concepts will be spawned
    /// </summary>
    private GameObject table = null;

    /// <summary>
    /// This attribute represents the current phase of the application
    /// </summary>
    private Phase phase;

    /// <summary>
    /// Number of concepts left to be spawned in the scene
    /// </summary>
    private int leftConceptToSpawn;

    /// <summary>
    /// This is the list of magnets in the scene sorted in counterclockwise order considering the center of their positions
    /// </summary>
    List<GameObject> sortedMagnets = new List<GameObject>();

    /// <summary>
    /// This is the streak of correct positioning of the concepts to their magnets during the playing main phase and determines the grade of 
    /// difficulty of the game
    /// </summary>
    private int correctStreak;

    /// <summary>
    /// This is the index of the magnet to be freed during the playing main phase, it's used for the case when the magnets are freed in a counterclockwise order
    /// </summary>
    private int indexToFree;

    /// <summary>
    /// This list contains the magnets to be freed during the playing main phase, it's used for the case when the magnets are freed in a random order. This should change during the playing phase
    /// </summary>
    private List<GameObject> magnetsToFreePlaying = new List<GameObject>();

    /// <summary>
    /// This is the reference to coroutines during playing phase
    /// </summary>
    private readonly List<Coroutine> playingCoroutine = new List<Coroutine>();

    /// <summary>
    /// This is the reference to the spatial anchor manager
    /// </summary>
    private SpatialAnchorManager spatialAnchorManager;

    /// <summary>
    /// This is the reference to the object manager
    /// </summary>
    private ObjectManager objectManager;

    /// <summary>
    /// This is the reference to the controller
    /// </summary>
    private Controller controller;

    /// <summary>
    /// This is the reference to the room manager
    /// </summary>
    private RoomManager roomManager;

    /// <summary>
    /// Used when a concept is attached to its magnet
    /// </summary>
    private ParticleEffect rightParticleEffect;

    /// <summary>
    /// Used when a concept is not attached to a wrong magnet
    /// </summary>
    private ParticleEffect wrongParticleEffect;

    /// <summary>
    /// Points obtained by the user during the game
    /// </summary>
    private int score = 0;

    /// <summary>
    /// Standings of the experience
    /// </summary>
    private Dictionary<string, int> standings = new Dictionary<string, int>();

    /// <summary>
    /// This dictionary is used for tracking track of the buttons on the menu, it's used for performance reasons
    /// </summary>
    private Dictionary<ButtonType, GameObject> buttonsOnMenu = new Dictionary<ButtonType, GameObject>();

    /// <summary>
    /// This is the gameobject of the buttons menu, the parent one containing all the buttons
    /// </summary>
    private GameObject buttonsMenu;


    /// <summary>
    /// This is the list of available phases in the application given the current configuration of the game
    /// </summary>
    private List<Phase> availablePhase = new List<Phase>();



    /// <summary>
    /// This is the time available for playing main phase. It's calculated considering the room area and the number of magnets in the scene
    /// Time for other factors are also calculated from this one
    /// </summary>
    private float gameTime = 0.0f;

    /// <summary>
    /// This boolean is used to interrupt standings message when outside the room
    /// </summary>
    private bool interruptStandings = false;

    /// <summary>
    /// This is the old dialog. If a new dialog is instantiated, the old one is destroyed
    /// </summary>
    private Dialog oldDialog = null;

    /// <summary>
    /// This is the prefab with buttons used for rotating the concepts when spawned
    /// </summary>
    private GameObject conceptRotator;


    /// <summary>
    /// This metod initializes the LociManager
    /// </summary>
    public void Init(SpatialAnchorManager spatialAnchorManager, ObjectManager objectManager, Controller controller,
        RoomManager roomManager, Dictionary<string, int> standings)
    {

        this.spatialAnchorManager = spatialAnchorManager;
        this.objectManager = objectManager;
        this.controller = controller;
        this.roomManager = roomManager;
        this.standings = standings;

        Debug.Log("mydebug locimanager bugan");

        //objectManager.InitializeConceptsInScene(spatialAnchorManager);

        if (Debugging.IsMainScene() == false)
            return;

        PrepareAndPlayBackgroundMusic();

        InitializeParticleSystems();

        PreparePhase();


        StartCoroutine(LociUpdate());

        InitializeButtonsMenu();

        if (!AreAllMagnetsOutsideTableSpace())
        {
            phase = Phase.MagnetDistribution;
            

            if (availablePhase.Contains(Phase.ConceptDistribution))
            {
                availablePhase.Remove(Phase.ConceptDistribution);
            }

        }

        if (phase == Phase.ConceptDistribution)
        {
            SpawnConceptIfPossible();
            foreach(GameObject magnet in objectManager.GetMagnetsInScene())
            {
                magnet.GetComponent<ObjectManipulator>().enabled = false;
            }
        }

        Debug.Log("mydebug table position: " + table.transform.position.ToString());


        foreach (MRUKAnchor anchor in roomManager.GetCurrentRoom().Anchors)
            if (anchor.VolumeBounds.HasValue)
                Debug.Log("Anchor: " + anchor.Label + ", anchor center: " +
                          anchor.transform.InverseTransformPoint(anchor.VolumeBounds.Value.center) + "bounds: " +
                          anchor.VolumeBounds.Value.ToString());


        Debug.Log("mydebug loci manager initialized");
    }



    /// <summary>
    /// This method is used for placing the menu for rotating conceps in the correct position
    /// </summary>
    private void PlaceConceptRotator()
    {
        Vector3 cameraToTable = table.transform.position - InspectorManager.instance.GetCam().transform.position;
        cameraToTable.y = 0; // Project onto horizontal plane
        cameraToTable.Normalize();

        Vector3 userRight = Vector3.Cross(Vector3.up, cameraToTable);

        Vector3 rotatorPosition = table.transform.position + userRight * (GetTotalBounds(table).size.x / 2 + 0.2f);
        rotatorPosition.y = table.transform.position.y + GetTotalBounds(table).size.y + 0.2f;

        bool isInRoom = roomManager.GetCurrentRoom().IsPositionInRoom(rotatorPosition);

        foreach (MRUKAnchor anchor in roomManager.GetCurrentRoom().Anchors)
        {
            if (anchor.VolumeBounds.HasValue == true && anchor.VolumeBounds.Value.Contains(rotatorPosition))
            {
                isInRoom = false;
                break;
            }
        }

        if (!isInRoom)
        {
            rotatorPosition = table.transform.position - userRight * (GetTotalBounds(table).size.x / 2 + 0.2f);
            rotatorPosition.y = table.transform.position.y + GetTotalBounds(table).size.y;
            isInRoom = roomManager.GetCurrentRoom().IsPositionInRoom(rotatorPosition);
        }

        if (!isInRoom)
        {
            rotatorPosition = InspectorManager.instance.GetCam().transform.position +
                            InspectorManager.instance.GetCam().transform.forward * 0.5f;
            rotatorPosition.y = table.transform.position.y + GetTotalBounds(table).size.y + 0.2f;
        }

        conceptRotator.transform.position = rotatorPosition;
    }


    /// <summary>
    /// This method prepares the menu of buttons spawning on table
    /// </summary>
    private void InitializeButtonsMenu()
    {
        GameObject temp2 = InspectorManager.instance.GetConceptRotatorPrefab();
        conceptRotator = Instantiate(temp2, Vector3.zero, temp2.transform.rotation);
        conceptRotator.transform.parent = this.gameObject.transform;
        conceptRotator.SetActive(false);
        PlaceConceptRotator();

        Debug.Log("mydebug concept rotator initialized");



        GameObject temp = InspectorManager.instance.GetButtonBar();
        buttonsMenu = Instantiate(temp, Vector3.zero, temp.transform.rotation);
        buttonsMenu.transform.parent = this.gameObject.transform;
        buttonsMenu.SetActive(false);

        buttonsOnMenu.Add(ButtonType.STANDINGS, FindInChildren(buttonsMenu.transform, "Standings"));
        buttonsOnMenu.Add(ButtonType.MAGNETDISTRIBUTION, FindInChildren(buttonsMenu.transform, "Magnet"));
        buttonsOnMenu.Add(ButtonType.CONCEPTDISTRIBUTION, FindInChildren(buttonsMenu.transform, "Concept"));
        buttonsOnMenu.Add(ButtonType.PLAYING, FindInChildren(buttonsMenu.transform, "Play"));
        buttonsOnMenu.Add(ButtonType.MEMORIZECONCEPTS, FindInChildren(buttonsMenu.transform, "Memorize"));
        buttonsOnMenu.Add(ButtonType.RIGHT, FindInChildren(conceptRotator.transform, "ButtonRIGHT"));
        buttonsOnMenu.Add(ButtonType.LEFT, FindInChildren(conceptRotator.transform, "ButtonLEFT"));
        buttonsOnMenu.Add(ButtonType.UP, FindInChildren(conceptRotator.transform, "ButtonUP"));
        buttonsOnMenu.Add(ButtonType.DOWN, FindInChildren(conceptRotator.transform, "ButtonDOWN"));



        foreach (GameObject button in buttonsOnMenu.Values)
            Debug.Log("mydebug buttons on menu initialized, name: " + button.name);

        foreach (ButtonType buttonType in buttonsOnMenu.Keys)
            ButtonMenuClicked(buttonType);



        Debug.Log("mydebug SIZE INITIALIZED BUTTONS: " + buttonsOnMenu.Count);
    }



    /// <summary>
    /// This is a recursive method used for searching a game object in the children (direct or indirect) of a parent
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    GameObject FindInChildren(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child.gameObject;
            GameObject found = FindInChildren(child, name);
            if (found != null) return found;
        }

        return null;
    }



    /// <summary>
    /// This method is used for restoring some information of magnets or concepts
    /// </summary>
    /// <param name="numConceptsInScene"></param>
    /// <param name="numMagnetsInScene"></param>
    private void LoadPreviousData(int numConceptsInScene, int numMagnetsInScene)
    {

        table.GetComponentInChildren<TableButton>().Init(this);
        PrepareAudio(table);

        Debug.Log("mydebug begin load previous data");

        if (numConceptsInScene == 0)
        {

            Debug.Log("mydebug restoring magnets only");

            List<GameObject> magnets = objectManager.GetMagnetsInScene();

            foreach (GameObject magnet in magnets)
            {
                magnet.GetComponent<SpatialAnchorManipulation>().Init(objectManager, this, spatialAnchorManager);
                PrepareAudio(magnet);
            }

        }

        else
        {

            Debug.Log("mydebug restoring magnets and concepts");

            List<GameObject> magnets = objectManager.GetMagnetsInScene();
            List<GameObject> concepts = objectManager.GetConceptsInScene();

            foreach (GameObject concept in concepts)
            {
                concept.GetComponent<SpatialAnchorManipulation>().Init(objectManager, this, spatialAnchorManager);
            }

            foreach (GameObject magnet in magnets)
            {
                magnet.GetComponent<SpatialAnchorManipulation>().Init(objectManager, this, spatialAnchorManager);
                PrepareAudio(magnet);
            }

        }

        RestoreMagnetData();

        Debug.Log("mydebug load previous data done");

    }



    /// <summary>
    /// This method is used for restoring the data of the magnets in the scene
    /// </summary>
    private void RestoreMagnetData()
    {

        Debug.Log("mydebug restore magnet data");

        List<GameObject> magnets = new List<GameObject>();
        List<GameObject> conceptsCanBeDeleted = new List<GameObject>();


        magnets = objectManager.GetMagnetsInScene();

        Debug.Log("mydebug magnets size: " + magnets.Count);
        Debug.Log("mydebug conceptsCanBeDeleted size: " + conceptsCanBeDeleted.Count);



        foreach (GameObject concept in objectManager.GetConceptsInScene())
        {
            conceptsCanBeDeleted.Add(concept);
        }

        leftMagnetToSpawn = objectManager.GetConceptsCount() - magnets.Count;
        leftConceptToSpawn = objectManager.GetConceptsCount() - conceptsCanBeDeleted.Count;

        int numConceptsInScene = conceptsCanBeDeleted.Count;
        int numMagnetsInScene = magnets.Count;

        magnetsData.Clear();

        Debug.Log("mydebug restore magnet data");

        if (numConceptsInScene == 0 && numMagnetsInScene != 0)
        {

            Debug.Log("mydebug restoring magnet data without concepts");

            foreach (GameObject magnet in magnets)
            {
                magnetsData.Add(magnet, new MagnetData());
            }

            Debug.Log("RestoreMagnetDataDuringGame magnetsdata size: " + magnetsData.Count);

            foreach (GameObject magnet in magnetsData.Keys)
            {
                if (ArePositionsFar(magnet.transform.position,
                        table.transform.position + new Vector3(0.0f, GetTotalBounds(table).size.y, 0.0f)) == true)
                {
                    magnetsData[magnet].SetIsOutSideTableSpace(true);
                    Debug.Log("mydebug restoring magnet: " + magnet.transform.position.ToString() +
                              " outside table space");
                }

                else
                {
                    magnetsData[magnet].SetIsOutSideTableSpace(false);
                    Debug.Log("mydebug restoring magnet: " + magnet.transform.position.ToString() +
                              " inside table space");
                }
            }

        }

        else if (numMagnetsInScene != 0 && numConceptsInScene != 0)
        {

            Debug.Log("mydebug restoring magnet data with concepts");

            List<GameObject> concepts = new List<GameObject>();

            foreach (GameObject concept in conceptsCanBeDeleted)
            {
                concepts.Add(concept);
            }

            foreach (GameObject magnet in magnets)
            {
                magnetsData.Add(magnet, new MagnetData());
            }

            foreach (GameObject magnet in magnetsData.Keys)
            {
                magnetsData[magnet].SetIsOutSideTableSpace(true);
            }

            Debug.Log("RestoreMagnetDataDuringGame magnetsdata size: " + magnetsData.Count);

            ReassociateConceptsToMagnets(conceptsCanBeDeleted);

            Debug.Log("mydebug concepts size: " + concepts.Count);
            Debug.Log("mydebug conceptsCanBeDeleted size: " + conceptsCanBeDeleted.Count);


        }
    }



    /// <summary>
    /// This method reassociates the concepts to the magnets in the scene when the application is started again in a old game
    /// </summary>
    /// <param name="concepts"></param>
    private void ReassociateConceptsToMagnets(List<GameObject> concepts)
    {

        foreach (GameObject magnet in magnetsData.Keys)
        {

            GameObject associatedConcept = null;
            float minDistance = float.MaxValue;

            foreach (GameObject concept in concepts)
            {
                float tempDistance = Vector3.Distance(magnet.transform.position, concept.transform.position);
                if (ArePositionsFar(magnet.transform.position, concept.transform.position) == false &&
                    minDistance > tempDistance)
                {
                    associatedConcept = concept;
                    minDistance = tempDistance;
                }
            }

            if (associatedConcept != null)
            {
                magnetsData[magnet].SetConceptAssociated(associatedConcept);
                concepts.Remove(associatedConcept);
                Debug.Log("mydebug concept reassociated!: " + associatedConcept.tag);
            }

        }

    }



    /// <summary>
    /// This method prepares the phase of the application
    /// </summary>
    private void PreparePhase()
    {

        availablePhase.Add(Phase.MagnetDistribution);

        int numMagnetsInScene = objectManager.GetMagnetsInScene().Count;
        int numConceptsInScene = objectManager.GetConceptsInScene().Count;

        Debug.Log("mydebug num magnets in scene: " + numMagnetsInScene);
        Debug.Log("mydebug num concepts in scene: " + numConceptsInScene);

        leftMagnetToSpawn = objectManager.GetConceptsCount() - numMagnetsInScene;

        Debug.Log("mydebug left magnet to spawn LOADED: " + leftMagnetToSpawn);


        leftConceptToSpawn = objectManager.GetConceptsCount() - numConceptsInScene;

        Debug.Log("mydebug left concept to spawn LOADED: " + leftConceptToSpawn);

        if (numConceptsInScene == objectManager.GetConceptsCount() &&
            numMagnetsInScene == objectManager.GetConceptsCount())
        {
            availablePhase.Add(Phase.ConceptDistribution);
            availablePhase.Add(Phase.PlayingMain);
            phase = Phase.ConceptDistribution;
            Debug.Log("mydebug available phase given by all concepts and magnets placed");
        }
        else if (numMagnetsInScene == objectManager.GetConceptsCount() &&
                 numConceptsInScene < objectManager.GetConceptsCount())
        {
            availablePhase.Add(Phase.ConceptDistribution);
            phase = Phase.ConceptDistribution;
            Debug.Log("mydebug available phase given by all magnets");
        }
        else if (numMagnetsInScene < objectManager.GetConceptsCount())
        {
            phase = Phase.MagnetDistribution;
            Debug.Log("mydebug available phase given by not all magnets placed");
        }


        if (numMagnetsInScene != 0 || numConceptsInScene != 0)
        {
            LoadPreviousData(numConceptsInScene, numMagnetsInScene);
        }
        else
        {
            Debug.Log("mydebug new game started");

            if (table != null)
                spatialAnchorManager.EliminateAnchorAndObject(table.GetComponent<OVRSpatialAnchor>(), table);

            table = objectManager.GetObjectPrefabFromTypeNoConcept(ObjectType.LociTable);

            InstantiateTable();

            if (Debugging.IsOnPC() == false)
                spatialAnchorManager.AddAnchorToObject(table);

            Debug.Log("mydebug new configuration created");

        }
    }



    /// <summary>
    /// Method that spawns a magnet in the scene if possible and necessary
    /// </summary>
    private void SpawnMagnetIfPossible()
    {

        Debug.Log("mydebug check if magnet can be spawned");

        Debug.Log("left magnet to spawn: " + leftMagnetToSpawn);
        Debug.Log("Are all magnets outside table space: " + AreAllMagnetsOutsideTableSpace().ToString());

        if (leftMagnetToSpawn > 0 && AreAllMagnetsOutsideTableSpace())
        {
            GameObject magnetPrefab = objectManager.GetObjectPrefabFromTypeNoConcept(ObjectType.LociMagnet);
            GameObject magnet = Instantiate(magnetPrefab,
                table.transform.position + new Vector3(0.0f, 0.3f, 0.0f) +
                new Vector3(0.0f, GetTotalBounds(table).size.y, 0.0f), magnetPrefab.transform.rotation);
            magnet.GetComponent<SpatialAnchorManipulation>().Init(objectManager, this, spatialAnchorManager);

            magnet.transform.parent = this.gameObject.transform;
            PrepareAudio(magnet);
            PlayTableAudio();

            if (Debugging.IsOnPC() == false)
            {
                spatialAnchorManager.AddAnchorToObject(magnet);
            }

            magnetsData.Add(magnet, new MagnetData());

            leftMagnetToSpawn--;


            Debug.Log("mydebug magnet position: " + magnet.transform.position.ToString());
            Debug.Log("mydebug magnet spawned, left magnets: " + leftMagnetToSpawn);

            magnet.GetComponent<ObjectManipulator>().enabled = true;

            objectManager.GetMagnetsInScene().Add(magnet);

        }
        else
            Debug.Log("mydebug magnet can NOT be spawned");
    }



    /// <summary>
    /// Method that checks if all magnets are outside the table space
    /// </summary>
    /// <returns></returns>
    private bool AreAllMagnetsOutsideTableSpace()
    {

        foreach (MagnetData magnetData in magnetsData.Values)
        {
            if (!magnetData.GetIsOutSideTableSpace())
                return false;
            Debug.Log("mydebug not all magnets outside table space");
        }

        Debug.Log("mydebug all magnets outside table space");
        return true;

    }



    /// <summary>
    /// Method that says if two positions are far
    /// </summary>
    /// <param name="pos1"></param>
    /// <param name="pos2"></param>
    /// <returns></returns>
    private bool ArePositionsFar(Vector3 pos1, Vector3 pos2)
    {
        return Vector3.Distance(pos1, pos2) >= 0.5f;
    }



    /// <summary>
    /// This method checks if a given magnet is outside the table space, updates its data, and calls SpanMagnetIfPossible() if necessary
    /// </summary>
    /// <param name="magnet"></param>
    /// <returns></returns>
    public void MagnetMovedMagnetDistribution(GameObject magnet)
    {

        if (ArePositionsFar(magnet.transform.position,
                table.transform.position + new Vector3(0.0f, GetTotalBounds(table).size.y, 0.0f)) == true)
        {
            magnetsData[magnet].SetIsOutSideTableSpace(true);
            SpawnMagnetIfPossible();
            Debug.Log("mydebug magnet moved outside table space");
        }

        else
        {
            magnetsData[magnet].SetIsOutSideTableSpace(false);
            Debug.Log("mydebug magnet moved inside table space");
        }

        if (AreAllMagnetsOutsideTableSpace() && leftMagnetToSpawn == 0)
        {
            availablePhase.Add(Phase.ConceptDistribution);
            Dialog dialog = InstantiateDialogBox("Concept Distribution Available", "You can now go to the table and select Concept Distribution phase.");
            StartCoroutine(DismissDialog(dialog));
        }
        else
        {
            if (availablePhase.Contains(Phase.ConceptDistribution))
                availablePhase.Remove(Phase.ConceptDistribution);
        }

        foreach (Phase phase in availablePhase)
            Debug.Log("mydebug available phase: " + phase.ToString());

    }



    /// <summary>
    /// This methods wait for the concepts and magnets to be loaded and then continue the change phase process
    /// </summary>
    /// <returns></returns>
    private IEnumerator FromPlayingToMagnetDistribution()
    {


        Task task = spatialAnchorManager.EraseConceptsAnchorFromMemoryCurrentExperience();

        while (!task.IsCompleted)
            yield return null;

        objectManager.GetConceptsInScene().Clear();
        objectManager.GetMagnetsInScene().Clear();
        spatialAnchorManager.LoadAllAnchorsFromCurrentExperience(true);

        Debug.Log("mydebug size magnets in scene: " +
                  objectManager.GetMagnetsInScene().Count);
        Debug.Log("mydebug size concepts in scene: " +
                  objectManager.GetConceptsInScene().Count);

        Debug.Log("mydebug from playing to magnet distribution begin");

        while (spatialAnchorManager.AreAnchorsReady() == false)
            yield return null;

        Debug.Log("mydebug from playing to magnet distribution begin after waiting");

        if (Debugging.IsOnPC() == false)
        {
            table.GetComponentInChildren<TableButton>().Init(this);
            PrepareAudio(table);

            foreach (GameObject magnet in objectManager.GetMagnetsInScene())
            {
                magnet.GetComponent<SpatialAnchorManipulation>().Init(objectManager, this, spatialAnchorManager);
                PrepareAudio(magnet);
            }
        }
        else
        {
            objectManager.GetConceptsInScene().ForEach(concept => concept.SetActive(false));

        }

        Debug.Log("mydebug from playing to concept distribution, magnets size: " +
                  objectManager.GetMagnetsInScene().Count);

        objectManager.ResetConceptsInScene();

        phase = Phase.MagnetDistribution;
        RestoreMagnetData();

        availablePhase.Clear();
        availablePhase.Add(Phase.MagnetDistribution);
        if (AreAllMagnetsOutsideTableSpace() && leftMagnetToSpawn == 0)
            availablePhase.Add(Phase.ConceptDistribution);

        foreach (GameObject magnet in objectManager.GetMagnetsInScene())
        {
            Debug.Log("mydebug magnet in scene to enable object manipulator: " +
                      magnet.transform.position.ToString());
            magnet.SetActive(true);
            magnet.GetComponent<ObjectManipulator>().enabled = true;
        }


    }



    /// <summary>
    /// This methods wait for the concepts and magnets to be loaded and then continue the change phase process
    /// </summary>
    /// <returns></returns>
    private IEnumerator FromPlayingToConceptDistribution()
    {

        while (spatialAnchorManager.AreAnchorsReady() == false)
            yield return null;

        Debug.Log("mydebug from playing to concept distribution begin after waiting");

        Debug.Log("mydebug from playing to concept distribution, magnets size: " +
                  objectManager.GetMagnetsInScene().Count);
        Debug.Log("mydebug from playing to concept distribution, concepts size: " +
                  objectManager.GetConceptsInScene().Count);

        if (Debugging.IsOnPC() == false)
        {
            table.GetComponentInChildren<TableButton>().Init(this);
            PrepareAudio(table);

            foreach (GameObject magnet in objectManager.GetMagnetsInScene())
            {
                magnet.GetComponent<SpatialAnchorManipulation>().Init(objectManager, this, spatialAnchorManager);
                PrepareAudio(magnet);
            }

            objectManager.GetConceptsInScene().ForEach(concept =>
                concept.GetComponent<SpatialAnchorManipulation>().Init(objectManager, this, spatialAnchorManager));

        }

        phase = Phase.ConceptDistribution;
        RestoreMagnetData();

        availablePhase.Clear();
        availablePhase.Add(Phase.MagnetDistribution);
        availablePhase.Add(Phase.ConceptDistribution);
        if (AreAllConceptsAssociated() && leftConceptToSpawn == 0)
            if (availablePhase.Contains(Phase.PlayingMain) == false)
                availablePhase.Add(Phase.PlayingMain);


        foreach (GameObject magnet in objectManager.GetMagnetsInScene())
        {
            Debug.Log("mydebug magnet in scene to disable object manipulator: " +
                      magnet.transform.position.ToString());
            magnet.SetActive(true);
            magnet.GetComponent<ObjectManipulator>().enabled = false;
        }

        foreach (GameObject concept in objectManager.GetConceptsInScene())
        {
            Debug.Log("mydebug concept in scene to enable object manipulator: " +
                      concept.transform.position.ToString());
            concept.SetActive(true);
            concept.GetComponent<ObjectManipulator>().enabled = true;
        }

    }



    /// <summary>
    /// This method changes the phase of the application and manages the transition between phases
    /// </summary>
    /// <param name="nextPhase"></param>
    private void ChangePhase(Phase nextPhase)
    {
        conceptRotator.SetActive(false);


        if (nextPhase == Phase.MagnetDistribution)
        {

            Dialog dialog = InstantiateDialogBox("Magnet Distribution", "You are now in the magnet distribution phase, please pick up the magnet and place it near a chosen loci. Consider to distribuite the magnets around your room.");
            StartCoroutine(DismissDialog(dialog));

            if (phase == Phase.ConceptDistribution || phase == Phase.MEMORIZE)
            {

                Debug.Log("mydebug change phase to magnet distribution from start or concept distribution");

                if (Debugging.IsOnPC() == false)
                {
                    List<GameObject> concepts = objectManager.GetConceptsInScene();
                    spatialAnchorManager.EraseAndDestroyAnchorObjectListCurrentExperience(concepts);
                }
                else
                {
                    objectManager.GetConceptsInScene().ForEach(concept => concept.SetActive(false));

                }

                foreach (GameObject concept in objectManager.GetConceptsInScene())
                {
                    concept.SetActive(false);
                }

                objectManager.ResetConceptsInScene();

                phase = Phase.MagnetDistribution;
                RestoreMagnetData();
                SpawnMagnetIfPossible();

                availablePhase.Clear();
                availablePhase.Add(Phase.MagnetDistribution);
                if (AreAllMagnetsOutsideTableSpace() && leftMagnetToSpawn == 0)
                    availablePhase.Add(Phase.ConceptDistribution);

                foreach (GameObject magnet in objectManager.GetMagnetsInScene())
                {
                    magnet.SetActive(true);
                    magnet.GetComponent<ObjectManipulator>().enabled = true;
                }

            }

            else if (phase == Phase.MagnetDistribution)
            {

                Debug.Log("mydebug change phase to magnet distribution from magnet distribution");

                if (Debugging.IsOnPC() == false)
                {
                    List<GameObject> magnets = objectManager.GetMagnetsInScene();
                    spatialAnchorManager.EraseAndDestroyAnchorObjectListCurrentExperience(magnets);
                }
                else
                {
                    objectManager.GetMagnetsInScene().ForEach(magnet => Destroy(magnet));
                }

                objectManager.GetMagnetsInScene().Clear();
                phase = Phase.MagnetDistribution;
                RestoreMagnetData();
                SpawnMagnetIfPossible();

                availablePhase.Clear();
                availablePhase.Add(Phase.MagnetDistribution);


                foreach (GameObject magnet in objectManager.GetMagnetsInScene())
                {
                    magnet.SetActive(true);
                    magnet.GetComponent<ObjectManipulator>().enabled = true;
                }

            }


            else if (phase == Phase.PlayingMain || phase == Phase.PLayingFinal || phase == Phase.ENDED)
            {

                Debug.Log(
                    "mydebug change phase to magnet distribution from playing main or playing final or ended");

                playingCoroutine.ForEach(coroutine => StopCoroutine(coroutine));
                playingCoroutine.Clear();

                if (Debugging.IsOnPC() == false)
                {
                    foreach (GameObject magnet in magnetsData.Keys)
                    {
                        magnetsData[magnet].GetConceptAssociated().SetActive(false);
                        Destroy(magnet);
                    }

                    Destroy(table);

                    Debug.Log("mydebug objects disabled");

                    Debug.Log("mydebug anchors loaded code past");

                }
                else
                {
                    foreach (GameObject magnet in magnetsData.Keys)
                    {
                        magnetsData[magnet].GetConceptAssociated().transform.position = magnet.transform.position;
                    }
                }


                StartCoroutine(FromPlayingToMagnetDistribution());

            }


        }

        else if (nextPhase == Phase.ConceptDistribution)
        {
            Dialog dialog = InstantiateDialogBox("Concept Distribution", "You are now in the concept distribution phase, please pick up the concept and attach it to a magnet.");
            StartCoroutine(DismissDialog(dialog));

            if (phase == Phase.MagnetDistribution || phase == Phase.MEMORIZE)
            {

                Debug.Log("mydebug change phase to concept distribution from start or magnet distribution");

                phase = Phase.ConceptDistribution;
                RestoreMagnetData();
                SpawnConceptIfPossible();

                availablePhase.Clear();
                availablePhase.Add(Phase.MagnetDistribution);
                availablePhase.Add(Phase.ConceptDistribution);

                if (AreAllConceptsAssociated() && leftConceptToSpawn == 0)
                    if (availablePhase.Contains(Phase.PlayingMain) == false)
                        availablePhase.Add(Phase.PlayingMain);


                foreach (GameObject magnet in objectManager.GetMagnetsInScene())
                {
                    magnet.SetActive(true);
                    magnet.GetComponent<ObjectManipulator>().enabled = false;
                }


                foreach (GameObject concept in objectManager.GetConceptsInScene())
                {
                    concept.SetActive(true);
                    concept.GetComponent<ObjectManipulator>().enabled = true;
                }

            }


            else if (phase == Phase.ConceptDistribution)
            {

                Debug.Log("mydebug change phase to concept distribution from concept distribution");

                if (Debugging.IsOnPC() == false)
                {
                    List<GameObject> concepts = objectManager.GetConceptsInScene();
                    spatialAnchorManager.EraseAndDestroyAnchorObjectListCurrentExperience(concepts);

                }
                else
                {
                    objectManager.GetConceptsInScene().ForEach(concept => concept.SetActive(false));

                }

                objectManager.ResetConceptsInScene();

                phase = Phase.ConceptDistribution;
                RestoreMagnetData();
                SpawnConceptIfPossible();

                availablePhase.Clear();
                availablePhase.Add(Phase.MagnetDistribution);
                availablePhase.Add(Phase.ConceptDistribution);



                foreach (GameObject magnet in objectManager.GetMagnetsInScene())
                {
                    magnet.SetActive(true);
                    magnet.GetComponent<ObjectManipulator>().enabled = false;
                }

                foreach (GameObject concept in objectManager.GetConceptsInScene())
                {
                    concept.SetActive(true);
                    concept.GetComponent<ObjectManipulator>().enabled = true;
                }

            }

            else if (phase == Phase.PlayingMain || phase == Phase.PLayingFinal || phase == Phase.ENDED)
            {

                Debug.Log(
                    "mydebug change phase to concept distribution from playing main or playing final or ended");

                playingCoroutine.ForEach(coroutine => StopCoroutine(coroutine));
                playingCoroutine.Clear();

                if (Debugging.IsOnPC() == false)
                {
                    foreach (GameObject magnet in magnetsData.Keys)
                    {
                        magnetsData[magnet].GetConceptAssociated().SetActive(false);
                        Destroy(magnet);
                    }

                    Destroy(table);
                    objectManager.GetConceptsInScene().Clear();
                    objectManager.GetMagnetsInScene().Clear();
                    spatialAnchorManager.LoadAllAnchorsFromCurrentExperience(true);

                }
                else
                {
                    foreach (GameObject magnet in magnetsData.Keys)
                    {
                        magnetsData[magnet].GetConceptAssociated().transform.position = magnet.transform.position;
                    }
                }

                StartCoroutine(FromPlayingToConceptDistribution());

            }


        }



        else if (nextPhase == Phase.PLayingFinal)
        {
            phase = nextPhase;
            foreach (GameObject magnet in magnetsData.Keys)
            {
                magnetsData[magnet].SetAttachedConcept(null);
                MoveToRandomPositionOnFloor(magnetsData[magnet].GetConceptAssociated());
                PlayMagnetAudio(magnet, false, true);
                magnetsData[magnet].GetConceptAssociated().GetComponent<ObjectManipulator>().enabled = true;
                magnet.SetActive(true);
            }

            for (int i = 1; i < sortedMagnets.Count; i++)
            {
                sortedMagnets[i].SetActive(false);
            }

        }

        else if (nextPhase == Phase.PlayingMain &&
                 (phase == Phase.PLayingFinal || phase == Phase.PlayingMain || phase == Phase.ENDED))
        {

            playingCoroutine.ForEach(coroutine => StopCoroutine(coroutine));
            playingCoroutine.Clear();

            foreach (GameObject magnet in magnetsData.Keys)
            {
                magnetsData[magnet].GetConceptAssociated().transform.position = magnet.transform.position;
                magnetsData[magnet].GetConceptAssociated().GetComponent<ObjectManipulator>().enabled = false;
                magnetsData[magnet].SetTimeIsFree(0.0f);
                magnetsData[magnet].SetAssociatedConceptIsPickedPlayingMain(false);
                magnetsData[magnet].SetAttachedConcept(magnetsData[magnet].GetConceptAssociated());


            }

            phase = nextPhase;

            StartGame();

        }

        else if (nextPhase == Phase.PlayingMain)
        {

            phase = nextPhase;

            foreach (GameObject magnet in magnetsData.Keys)
            {

                if (Debugging.IsOnPC() == false && magnet.GetComponent<OVRSpatialAnchor>() != null)
                {
                    spatialAnchorManager.DestroyAnchor(magnet.GetComponent<OVRSpatialAnchor>());
                    spatialAnchorManager.DestroyAnchor(magnetsData[magnet].GetConceptAssociated()
                        .GetComponent<OVRSpatialAnchor>());

                }

                magnetsData[magnet].GetConceptAssociated().GetComponent<ObjectManipulator>().enabled = false;

            }

            if (Debugging.IsOnPC() == false && table.GetComponent<OVRSpatialAnchor>() != null)
                spatialAnchorManager.DestroyAnchor(table.GetComponent<OVRSpatialAnchor>());

            Debug.Log("mydebug phase changed to playing main from concept distribution");

            StartGame();


        }

        else if (nextPhase == Phase.MEMORIZE)
        {
            Dialog dialog = InstantiateDialogBox("Memorize", "You are now in the memorize phase, please memorize the concepts and their positions.");
            StartCoroutine(DismissDialog(dialog));

            phase = Phase.MEMORIZE;

            foreach (GameObject magnet in magnetsData.Keys)
            {
                magnet.SetActive(false);
                magnetsData[magnet].GetConceptAssociated().GetComponent<ObjectManipulator>().enabled = false;
            }
        }
    }



    /// <summary>
    /// This method count how many magnets are not associated to a concept yet
    /// </summary>
    /// <returns></returns>
    private int CountFreeMagnet()
    {

        int count = 0;
        foreach (MagnetData magnetData in magnetsData.Values)
        {
            if (magnetData.GetConceptAssociated() == null)
                count++;
        }

        Debug.Log("mydebug count free magnets " + count);
        return count;
    }

    /// <summary>
    /// Method that spawns a concept in the scene if possible and necessary
    /// </summary>
    private void SpawnConceptIfPossible()

    {
        Debug.Log("mydebug check if concept can be spawned");

        if (CountFreeMagnet() == leftConceptToSpawn && leftConceptToSpawn > 0)
        {

            GameObject concept = objectManager.SpawnNextConcept(table.transform.position + new Vector3(0.0f, 0.2f, 0.0f) + new Vector3(0.0f, GetTotalBounds(table).size.y, 0.0f), this);

            if (concept.TryGetComponent<ConceptController>(out ConceptController conceptController))
            {
                if (conceptController.GetConceptType() == ConceptType.OBJECT3D)
                {
                    conceptRotator.transform.rotation =
                        Quaternion.LookRotation(InspectorManager.instance.GetCam().transform.forward);
                    conceptRotator.SetActive(true);
                }
            }
            PlayTableAudio();

            concept.GetComponent<SpatialAnchorManipulation>().Init(objectManager, this, spatialAnchorManager);

            leftConceptToSpawn--;


            Debug.Log("mydebug concept spawned, left concepts: " + leftConceptToSpawn);

            concept.GetComponent<ObjectManipulator>().enabled = true;

            objectManager.AddConceptToScene(concept);




        }
    }



    /// <summary>
    /// This method returns the magnet associated to a given concept
    /// </summary>
    /// <param name="concept"></param>
    /// <returns></returns>
    private GameObject GetMagnetFromConcept(GameObject concept)
    {

        foreach (GameObject magnet in magnetsData.Keys)
        {
            if (magnetsData[magnet].GetConceptAssociated() == concept)
                return magnet;
        }

        Debug.Log("mydebug no magnet associated to the concept");
        return null;
    }



    /// <summary>
    /// This method checks if a given concept is inside a magnet space, updates the data of the magnets, and if yes moves the concept to the magnet
    /// </summary>
    /// <param name="concept"></param>
    public void ConceptMovedConceptDistribution(GameObject concept)
    {

        GameObject conceptMagnet = GetMagnetFromConcept(concept);

        if (conceptMagnet != null)
        {
            magnetsData[conceptMagnet].SetConceptAssociated(null);
        }

        GameObject nearestMagnet = FindNearestMagnet(concept);

        if (nearestMagnet == null)
        {

            if (Debugging.IsOnPC() == false)
            {
                spatialAnchorManager.AddAnchorToObject(concept);
            }

            Debug.Log("mydebug concept moved but no magnet near during concept distribution" + concept.tag);

        }

        else
        {

            Debug.Log("mydebug concept moved and magnet near during concept distribution" + concept.tag);

            GameObject conceptChanged = magnetsData[nearestMagnet].GetConceptAssociated();

            magnetsData[nearestMagnet].SetConceptAssociated(concept);

            if (conceptChanged != null)
            {
                Debug.Log("mydebug old concept to be moved to random position" + conceptChanged.tag);
                MoveToRandomPositionOnFloor(conceptChanged);
                PlayMagnetAudio(nearestMagnet, true, true);
                Debug.Log("mydebug old concept moved to random position" + conceptChanged.tag);
            }

            else
            {
                PlayMagnetAudio(nearestMagnet, true, false);
            }


            concept.transform.position = nearestMagnet.transform.position;
            concept.transform.rotation =
                Quaternion.LookRotation(InspectorManager.instance.GetCam().transform.forward);

            if (Debugging.IsOnPC() == false)
            {
                spatialAnchorManager.AddAnchorToObject(concept);
            }

            SpawnConceptIfPossible();


        }

        if (AreAllConceptsAssociated() && leftConceptToSpawn == 0)
        {
            if (availablePhase.Contains(Phase.PlayingMain) == false)
                availablePhase.Add(Phase.PlayingMain);

            Dialog dialog = InstantiateDialogBox("Playing Phase Available", "You can now go to the table and select Playing phase.");
            StartCoroutine(DismissDialog(dialog));
        }
        else
        {
            if (availablePhase.Contains(Phase.PlayingMain))
                availablePhase.Remove(Phase.PlayingMain);
        }

        foreach (Phase phase in availablePhase)
            Debug.Log("mydebug available phase concept distribution: " + phase.ToString());
    }



    /// <summary>
    /// This method checks if all concepts are associated to a magnet
    /// </summary>
    /// <returns></returns>
    private bool AreAllConceptsAssociated()
    {
        foreach (MagnetData magnetData in magnetsData.Values)
        {
            if (magnetData.GetConceptAssociated() == null)
                return false;
        }

        return true;
    }



    /// <summary>
    /// This method calculates the center of a list of points
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    private Vector3 GetCenter(List<Vector3> points)
    {
        Vector3 center = Vector3.zero;

        foreach (var point in points)
        {
            center += point;
        }

        center /= points.Count;

        return center;
    }



    /// <summary>
    /// This value influences the duration of the game considering the room area
    /// </summary>
    /// <returns></returns>
    private float GameTimeRoomFactor()
    {

        float reference = 10f;
        float baseValue = 1f;
        float sensitivity = 0.25f;
        float softnessFactor = 2f;
        float input = roomManager.GetRoomArea();

        float difference = (input - reference) / softnessFactor;
        baseValue += Mathf.Sign(difference) * Mathf.Log(1 + Mathf.Abs(difference)) * sensitivity;
        Debug.Log("mydebug game time room factor: " + baseValue);
        return baseValue;
    }



    /// <summary>
    /// This value influences the duration of the game considering the number of magnets in the scene
    /// </summary>
    /// <returns></returns>
    private float GameTimeNumberMagnetFactor()
    {

        float reference = 6f;
        float baseValue = 3f;
        float sensitivity = 0.3f;
        float softnessFactor = 2f;
        float input = magnetsData.Keys.Count;

        Debug.Log("mydebug game time magnet factor magnetsdata: " + magnetsData.Keys.Count);

        float difference = (input - reference) / softnessFactor;
        baseValue += Mathf.Sign(difference) * Mathf.Log(1 + Mathf.Abs(difference)) * sensitivity;
        Debug.Log("mydebug game time magnet factor: " + baseValue);
        return baseValue;

    }



    /// <summary>
    /// This is measured using the mean distance among the magnets
    /// </summary>
    /// <returns></returns>
    private float GameTimeMagnetsDistance()
    {

        List<Vector3> points = magnetsData.Keys.ToList().Select(magnet => magnet.transform.position).ToList();
        float sum = 0;
        float count = 0;
        float avg = 0;


        for (int i = 0; i < points.Count - 1; i++)
        {
            for (int j = i + 1; j < points.Count; j++)
            {
                sum = sum + Vector3.Distance(points[i], points[j]);
                count++;
            }
        }

        avg = sum / count;

        Debug.Log("mydebug game time distance: " + avg);

        float reference = 1.5f;
        float baseValue = 1f;
        float sensitivity = 0.3f;
        float softnessFactor = 2f;
        float input = avg;

        float difference = (input - reference) / softnessFactor;
        baseValue += Mathf.Sign(difference) * Mathf.Log(1 + Mathf.Abs(difference)) * sensitivity;
        Debug.Log("mydebug game time distance factor: " + baseValue);
        return baseValue;
    }



    /// <summary>
    /// This is the method where the game of loci is prepared and started
    /// </summary>
    private void StartGame()
    {

        //Two different methods for game time calculation

        //gameTime = GameTimeNumberMagnetFactor() + GameTimeRoomFactor();
        gameTime = GameTimeMagnetsDistance() + GameTimeNumberMagnetFactor();

        correctStreak = 0;
        indexToFree = 0;

        score = 0;

        magnetsToFreePlaying.Clear();

        Debug.Log("mydebug game started");

        List<Vector3> points = magnetsData.Keys.ToList().Select(magnet => magnet.transform.position).ToList();

        Vector3 center = GetCenter(points);

        //sortedMagnets = SortCounterClockWise(magnetsData.Keys.ToList(), center);

        //sortedMagnets = objectManager.ReorderMagnetsFromFirst(sortedMagnets, magnetsData);

        sortedMagnets = SortUploadOrder();

        GameObject concept = magnetsData[sortedMagnets[0]].GetConceptAssociated();

        foreach (GameObject magnet in magnetsData.Keys)
        {
            magnet.SetActive(false);

            magnetsData[magnet].SetAttachedConcept(magnetsData[magnet].GetConceptAssociated());
        }

        GameObject firstMagnet = GetMagnetFromConcept(concept);
        firstMagnet.SetActive(true);

        MoveToRandomPositionOnFloor(concept);

        PlayMagnetAudio(firstMagnet, false, true);

        concept.GetComponent<ObjectManipulator>().enabled = true;

        magnetsData[GetMagnetFromConcept(concept)].SetAttachedConcept(null);

        Coroutine temp;

        temp = StartCoroutine(ManageTimeFreeMagnets());

        playingCoroutine.Add(temp);

        temp = StartCoroutine(ToPlayingFinal());

        playingCoroutine.Add(temp);

        indexToFree++;
    }



    /// <summary>
    /// This method orders the magnets based on their upload order
    /// </summary>
    /// <returns></returns>
    private List<GameObject> SortUploadOrder()
    {

        List<GameObject> sortedMagnets = new List<GameObject>();
        List<GameObject> temp = new List<GameObject>();

        foreach (GameObject magnet in magnetsData.Keys)
        {
            temp.Add(magnet);
        }

        foreach (string id in objectManager.GetConceptsIdOrder())
        {
            foreach (GameObject magnet in temp)
            {
                if (magnetsData[magnet].GetConceptAssociated().GetComponent<ConceptController>().GetId() == id)
                {
                    sortedMagnets.Add(magnet);
                    temp.Remove(magnet);
                    break;
                }
            }
        }

        return sortedMagnets;
    }



    /// <summary>
    /// This method checks if a concept is near a magnet
    /// </summary>
    /// <param name="concept"></param>
    /// <returns></returns>
    private bool IsConceptNearMagnet(GameObject concept)
    {
        foreach (GameObject magnet in magnetsData.Keys)
        {
            if (ArePositionsFar(concept.transform.position, magnet.transform.position) == false)
                return true;
        }

        return false;
    }



    /// <summary>
    /// This method checks if a magnet is free or not during the playing phase and updates the time for how long it has been free,
    /// it also manages the rotation of concept associated to the magnet. When a concept is on the ground for long it starts rotating
    /// </summary>
    /// <returns></returns>
    private IEnumerator ManageTimeFreeMagnets()
    {

        Dictionary<GameObject, float> penaltyTimers = new Dictionary<GameObject, float>();
        foreach (GameObject magnet in magnetsData.Keys)
        {
            penaltyTimers.Add(magnet, 0.0f);
        }

        while (phase == Phase.PlayingMain)
        {

            foreach (GameObject magnet in magnetsData.Keys)
            {
                if (magnetsData[magnet].GetAttachedConcept() == null ||
                    magnetsData[magnet].GetAttachedConcept() != magnetsData[magnet].GetConceptAssociated())
                {

                    magnetsData[magnet].SetTimeIsFree(magnetsData[magnet].GetTimeIsFree() + Time.deltaTime);

                    if (magnetsData[magnet].GetTimeIsFree() >= gameTime * 2)
                    {
                        penaltyTimers[magnet] += Time.deltaTime;
                        if (penaltyTimers[magnet] > 3.0f)
                        {
                            penaltyTimers[magnet] = 0.0f;
                            score -= 1;
                            Debug.Log("mydebug score penalty applied : " + score +
                                      magnetsData[magnet].GetConceptAssociated().ToString());
                        }
                    }

                    if (magnetsData[magnet].GetTimeIsFree() >= gameTime * 2 &&
                        magnetsData[magnet].GetAssociatedConceptIsPickedPlayingMain() == false &&
                        IsConceptNearMagnet(magnetsData[magnet].GetConceptAssociated()) == false)
                    {
                        magnetsData[magnet].GetConceptAssociated().transform
                            .Rotate(new Vector3(0.0f, 1.0f, 0.0f), 1.0f);

                    }

                }

                else
                {
                    magnetsData[magnet].SetTimeIsFree(0.0f);
                }
            }

            yield return null;

        }
    }



    /// <summary>
    /// This is the method where after a certain time the final match is started
    /// </summary>
    /// <returns></returns>
    private IEnumerator ToPlayingFinal()
    {

        yield return new WaitForSeconds(80.0f);

        ChangePhase(Phase.PLayingFinal);

        Coroutine temp = StartCoroutine(PlayingFinalTimeEnded());
        playingCoroutine.Add(temp);

        Debug.Log("mydebug final match started");



    }



    /// <summary>
    /// This method starts the timer for final match
    /// </summary>
    /// <returns></returns>
    private IEnumerator PlayingFinalTimeEnded()
    {
        Debug.Log("mydebug final match timer started: " + gameTime * 60 / 5);

        yield return new WaitForSeconds(gameTime * 60 / 5);

        if (phase != Phase.ENDED)
        {
            Dialog dialog = InstantiateDialogBox("END",
                "What a pity! You couldn't finish final match. Your score is: " + score +
                ". Would you like to publish this in the standings?", DialogType.RequestStandings);
            StartCoroutine(DismissDialog(dialog));
            phase = Phase.ENDED;
        }
        else
        {
            Debug.Log("mydebug final match ended winning");
        }

        Debug.Log("mydebug final match ended");

    }



    /// <summary>
    /// Method used for placing the table in the room
    /// </summary>
    private void InstantiateTable()
    {

        // This is used for spawning objects near the user, if disabled they will spawn randomly in the room
        bool near = true;

        Debug.Log("mydebug table dimensions: " + GetTotalBounds(table).size.ToString());

        Bounds newPositionBounds = GetTotalBounds(table);


        bool isOk;
        int maxTry = 20;
        Vector3 newPosition;

        if (near == false)
        {

            while (maxTry > 0)
            {
                isOk = true;


                roomManager.GetCurrentRoom().GenerateRandomPositionOnSurface(MRUK.SurfaceType.FACING_UP, 0.1f, LabelFilter.Included(MRUKAnchor.SceneLabels.FLOOR), out newPosition, out var normal);

                newPosition += new Vector3(0.0f, 0.3f, 0.0f);

                newPositionBounds.center = newPosition + new Vector3(0.0f, GetTotalBounds(table).size.y / 2, 0.0f);

                /*foreach (GameObject magnet in magnetsData.Keys)
                {
                    if (ArePositionsFar(newPosition, magnet.transform.position) == false || ArePositionsFar(magnetsData[magnet].GetConceptAssociated().transform.position, newPosition) == false)
                    {
                        isOk = false;
                        Debug.Log("mydebug NO RANDOM POSITION FOUND because near magnet");
                        break;
                    }
                }*/

                /*if(isOk == true && (table.GetComponent<Renderer>().bounds.Contains(newPosition) || table.GetComponent<Renderer>().bounds.Intersects(newPositionBounds))){
                    isOk = false;
                    Debug.Log("mydebug NO RANDOM POSITION FOUND because near table");
                }*/

                if (isOk == true && roomManager.IsBoundInRoom(newPositionBounds) == false)
                {
                    isOk = false;
                    Debug.Log("mydebug NO RANDOM POSITION FOUND because out of room bounds");
                }

                if (isOk == true)
                {
                    foreach (MRUKAnchor anchor in roomManager.GetCurrentRoom().Anchors)
                    {
                        if (anchor.VolumeBounds.HasValue == true && IsBoundOutsideAnchor(newPositionBounds, anchor) == false)
                        {
                            isOk = false;
                            Debug.Log("mydebug NO RANDOM POSITION FOUND because near anchor " + anchor.Label.ToString() + "anchor bound " + anchor.VolumeBounds.Value.ToString() + " new position bound center" + anchor.transform.InverseTransformPoint(newPositionBounds.center).ToString() + "new position bounds" + newPositionBounds.ToString());
                            break;
                        }
                    }
                }

                if (isOk == true)
                {


                    newPosition.y = 0.0f;
                    newPositionBounds.center = newPosition + new Vector3(0.0f, GetTotalBounds(table).size.y / 2, 0.0f);
                    //DebugBounds(newPositionBounds);

                    table = Instantiate(table, newPosition, table.transform.rotation);
                    table.transform.parent = this.gameObject.transform;
                    Debug.Log("mydebug RANDOM POSITION FOUND, TABLE SPAWNED: " + newPositionBounds.ToString());
                    table.GetComponentInChildren<TableButton>().Init(this);
                    PrepareAudio(table);
                    return;
                }

                maxTry--;
            }
        }
        else
        {

            while (maxTry >= 10)
            {
                isOk = true;

                Vector3 randomPosition = GetRandomPointFrontSemiCircle();

                randomPosition += new Vector3(0.0f, 0.3f, 0.0f);

                newPositionBounds.center = randomPosition + new Vector3(0.0f, GetTotalBounds(table).size.y / 2, 0.0f);

                /*foreach (GameObject magnet in magnetsData.Keys)
                {
                    if (ArePositionsFar(newPosition, magnet.transform.position) == false || ArePositionsFar(magnetsData[magnet].GetConceptAssociated().transform.position, newPosition) == false)
                    {
                        isOk = false;
                        Debug.Log("mydebug NO RANDOM POSITION FOUND because near magnet");
                        break;
                    }
                }*/

                /*if(isOk == true && (table.GetComponent<Renderer>().bounds.Contains(newPosition) || table.GetComponent<Renderer>().bounds.Intersects(newPositionBounds))){
                    isOk = false;
                    Debug.Log("mydebug NO RANDOM POSITION FOUND because near table");
                }*/

                if (roomManager.GetCurrentRoom().IsPositionInRoom(randomPosition) == false)
                {
                    isOk = false;
                    Debug.Log("mydebug NO RANDOM POSITION FOUND because out of room bounds");
                }

                if (isOk == true && roomManager.IsBoundInRoom(newPositionBounds) == false)
                {
                    isOk = false;
                    Debug.Log("mydebug NO RANDOM POSITION FOUND because out of room bounds");
                }

                if (isOk == true)
                {
                    foreach (MRUKAnchor anchor in roomManager.GetCurrentRoom().Anchors)
                    {
                        if (anchor.VolumeBounds.HasValue == true && IsBoundOutsideAnchor(newPositionBounds, anchor) == false)
                        {
                            isOk = false;
                            Debug.Log("mydebug NO RANDOM POSITION FOUND because near anchor " + anchor.Label.ToString() + "anchor bound " + anchor.VolumeBounds.Value.ToString() + " new position bound center" + anchor.transform.InverseTransformPoint(newPositionBounds.center).ToString() + "new position bounds" + newPositionBounds.ToString());
                            break;
                        }
                    }
                }

                if (isOk == true)
                {


                    randomPosition.y = 0.0f;
                    newPositionBounds.center = randomPosition + new Vector3(0.0f, GetTotalBounds(table).size.y / 2, 0.0f);
                    //DebugBounds(newPositionBounds);

                    table = Instantiate(table, randomPosition, table.transform.rotation);
                    table.transform.parent = this.gameObject.transform;
                    Debug.Log("mydebug RANDOM POSITION FOUND, TABLE SPAWNED: " + newPositionBounds.ToString());
                    table.GetComponentInChildren<TableButton>().Init(this);
                    PrepareAudio(table);
                    return;
                }

                maxTry--;
            }

            while (maxTry > 0)
            {
                isOk = true;

                Vector3 randomPosition = GetRandomPointBackSemiCircle();

                randomPosition += new Vector3(0.0f, 0.3f, 0.0f);

                newPositionBounds.center = randomPosition + new Vector3(0.0f, GetTotalBounds(table).size.y / 2, 0.0f);

                /*foreach (GameObject magnet in magnetsData.Keys)
                {
                    if (ArePositionsFar(newPosition, magnet.transform.position) == false || ArePositionsFar(magnetsData[magnet].GetConceptAssociated().transform.position, newPosition) == false)
                    {
                        isOk = false;
                        Debug.Log("mydebug NO RANDOM POSITION FOUND because near magnet");
                        break;
                    }
                }*/

                /*if(isOk == true && (table.GetComponent<Renderer>().bounds.Contains(newPosition) || table.GetComponent<Renderer>().bounds.Intersects(newPositionBounds))){
                    isOk = false;
                    Debug.Log("mydebug NO RANDOM POSITION FOUND because near table");
                }*/

                if (roomManager.GetCurrentRoom().IsPositionInRoom(randomPosition) == false)
                {
                    isOk = false;
                    Debug.Log("mydebug NO RANDOM POSITION FOUND because out of room bounds");
                }

                if (isOk == true && roomManager.IsBoundInRoom(newPositionBounds) == false)
                {
                    isOk = false;
                    Debug.Log("mydebug NO RANDOM POSITION FOUND because out of room bounds");
                }

                if (isOk == true)
                {
                    foreach (MRUKAnchor anchor in roomManager.GetCurrentRoom().Anchors)
                    {
                        if (anchor.VolumeBounds.HasValue == true && IsBoundOutsideAnchor(newPositionBounds, anchor) == false)
                        {
                            isOk = false;
                            Debug.Log("mydebug NO RANDOM POSITION FOUND because near anchor " + anchor.Label.ToString() + "anchor bound " + anchor.VolumeBounds.Value.ToString() + " new position bound center" + anchor.transform.InverseTransformPoint(newPositionBounds.center).ToString() + "new position bounds" + newPositionBounds.ToString());
                            break;
                        }
                    }
                }

                if (isOk == true)
                {


                    randomPosition.y = 0.0f;
                    newPositionBounds.center = randomPosition + new Vector3(0.0f, GetTotalBounds(table).size.y / 2, 0.0f);
                    //DebugBounds(newPositionBounds);

                    table = Instantiate(table, randomPosition, table.transform.rotation);
                    table.transform.parent = this.gameObject.transform;
                    Debug.Log("mydebug RANDOM POSITION FOUND, TABLE SPAWNED: " + newPositionBounds.ToString());
                    table.GetComponentInChildren<TableButton>().Init(this);
                    PrepareAudio(table);
                    return;
                }

                maxTry--;
            }


        }

        Debug.LogError("mydebug NO RANDOM POSITION FOUND");
        table = Instantiate(table, InspectorManager.instance.GetCam().transform.position, table.transform.rotation);
        table.transform.parent = this.gameObject.transform;
        table.GetComponentInChildren<TableButton>().Init(this);
        PrepareAudio(table);
        Vector3 tablePosition = table.transform.position;
        tablePosition.y = 0.0f;
        table.transform.position = tablePosition;
    }



    /// <summary>
    /// This method moves a game object to a random position on the floor in an acceptable way
    /// </summary>
    /// <param name="gameObject"></param>
    private void MoveToRandomPositionOnFloor(GameObject gameObject)
    {

        // This is used for spawning objects near the user, if disabled they will spawn randomly in the room
        bool near = true;

        bool hadAnchor = false;

        if (gameObject.GetComponent<OVRSpatialAnchor>() != null && phase != Phase.PLayingFinal && phase != Phase.PlayingMain)
        {
            spatialAnchorManager.MovementStarted(gameObject.GetComponent<OVRSpatialAnchor>());
            hadAnchor = true;
            Debug.Log("mydebug object moved to random position with anchor: " + hadAnchor + gameObject.tag);
        }

        Bounds newPositionBounds = GetTotalBounds(gameObject);

        bool isOk;
        int maxTry = 20;
        Vector3 newPosition;

        if (near == false)
        {

            while (maxTry > 0)
            {
                isOk = true;


                roomManager.GetCurrentRoom().GenerateRandomPositionOnSurface(MRUK.SurfaceType.FACING_UP, 0.1f, LabelFilter.Included(MRUKAnchor.SceneLabels.FLOOR), out newPosition, out var normal);

                newPosition += new Vector3(0.0f, GetTotalBounds(gameObject).size.y / 2 + 0.1f, 0.0f);

                newPositionBounds.center = newPosition;

                foreach (GameObject magnet in magnetsData.Keys)
                {
                    if (ArePositionsFar(newPosition, magnet.transform.position) == false || (magnetsData[magnet].GetConceptAssociated() != null && ArePositionsFar(magnetsData[magnet].GetConceptAssociated().transform.position, newPosition) == false))
                    {
                        isOk = false;
                        Debug.Log("mydebug NO RANDOM POSITION FOUND because near magnet");
                        break;
                    }
                }

                if (isOk == true && (GetTotalBounds(table).Contains(newPosition) || GetTotalBounds(table).Intersects(newPositionBounds)))
                {
                    isOk = false;
                    Debug.Log("mydebug NO RANDOM POSITION FOUND because near table");
                }

                if (isOk == true && roomManager.IsBoundInRoom(newPositionBounds) == false)
                {
                    isOk = false;
                    Debug.Log("mydebug NO RANDOM POSITION FOUND because out of room bounds");
                }

                if (isOk == true)
                {
                    foreach (MRUKAnchor anchor in roomManager.GetCurrentRoom().Anchors)
                    {
                        if (anchor.VolumeBounds.HasValue == true && IsBoundOutsideAnchor(newPositionBounds, anchor) == false)
                        {
                            isOk = false;
                            Debug.Log("mydebug NO RANDOM POSITION FOUND because near anchor " + anchor.Label.ToString() + "anchor bound " + anchor.VolumeBounds.Value.ToString() + " new position bound center" + anchor.transform.InverseTransformPoint(newPositionBounds.center).ToString() + "new position bounds" + newPositionBounds.ToString());
                            break;
                        }
                    }
                }

                if (isOk == true)
                {
                    gameObject.transform.position = newPosition;
                    Debug.Log("mydebug RANDOM POSITION FOUND" + newPositionBounds.ToString());
                    if (hadAnchor == true)
                    {
                        StartCoroutine(ReattachAnchorAfterRandomPos(gameObject));
                        Debug.Log("mydebug object moved to random position with anchor, anchor recreated");
                    }
                    return;
                }

                maxTry--;
            }

        }
        else
        {

            while (maxTry >= 10)
            {
                isOk = true;

                Vector3 randomPosition = GetRandomPointFrontSemiCircle();
                randomPosition += new Vector3(0.0f, GetTotalBounds(gameObject).size.y / 2 + 0.1f, 0.0f);
                newPositionBounds.center = randomPosition;

                if (roomManager.GetCurrentRoom().IsPositionInRoom(randomPosition) == false)
                {
                    isOk = false;
                    Debug.Log("mydebug NO RANDOM POSITION FOUND because out of room bounds");
                }

                if (isOk == true)
                {
                    foreach (GameObject magnet in magnetsData.Keys)
                    {
                        if (ArePositionsFar(randomPosition, magnet.transform.position) == false || (magnetsData[magnet].GetConceptAssociated() != null && ArePositionsFar(magnetsData[magnet].GetConceptAssociated().transform.position, randomPosition) == false))
                        {
                            isOk = false;
                            Debug.Log("mydebug NO RANDOM POSITION FOUND because near magnet");
                            break;
                        }
                    }
                }

                if (isOk == true && (GetTotalBounds(table).Contains(randomPosition) || GetTotalBounds(table).Intersects(newPositionBounds)))
                {
                    isOk = false;
                    Debug.Log("mydebug NO RANDOM POSITION FOUND because near table");
                }

                if (isOk == true && roomManager.IsBoundInRoom(newPositionBounds) == false)
                {
                    isOk = false;
                    Debug.Log("mydebug NO RANDOM POSITION FOUND because out of room bounds");
                }

                if (isOk == true)
                {
                    foreach (MRUKAnchor anchor in roomManager.GetCurrentRoom().Anchors)
                    {
                        if (anchor.VolumeBounds.HasValue == true && IsBoundOutsideAnchor(newPositionBounds, anchor) == false)
                        {
                            isOk = false;
                            Debug.Log("mydebug NO RANDOM POSITION FOUND because near anchor " + anchor.Label.ToString() + "anchor bound " + anchor.VolumeBounds.Value.ToString() + " new position bound center" + anchor.transform.InverseTransformPoint(newPositionBounds.center).ToString() + "new position bounds" + newPositionBounds.ToString());
                            break;
                        }
                    }
                }

                if (isOk == true)
                {
                    gameObject.transform.position = randomPosition;
                    Debug.Log("mydebug RANDOM POSITION FOUND" + newPositionBounds.ToString());
                    if (hadAnchor == true)
                    {
                        StartCoroutine(ReattachAnchorAfterRandomPos(gameObject));
                        Debug.Log("mydebug object moved to random position with anchor, anchor recreated");
                    }
                    return;
                }

                maxTry--;
            }

            while (maxTry > 0)
            {
                isOk = true;

                Vector3 randomPosition = GetRandomPointBackSemiCircle();
                randomPosition += new Vector3(0.0f, GetTotalBounds(gameObject).size.y / 2 + 0.1f, 0.0f);
                newPositionBounds.center = randomPosition;

                if (roomManager.GetCurrentRoom().IsPositionInRoom(randomPosition) == false)
                {
                    isOk = false;
                    Debug.Log("mydebug NO RANDOM POSITION FOUND because out of room bounds");
                }

                if (isOk == true)
                {
                    foreach (GameObject magnet in magnetsData.Keys)
                    {
                        if (ArePositionsFar(randomPosition, magnet.transform.position) == false || (magnetsData[magnet].GetConceptAssociated() != null && ArePositionsFar(magnetsData[magnet].GetConceptAssociated().transform.position, randomPosition) == false))
                        {
                            isOk = false;
                            Debug.Log("mydebug NO RANDOM POSITION FOUND because near magnet");
                            break;
                        }
                    }
                }

                if (isOk == true && (GetTotalBounds(table).Contains(randomPosition) || GetTotalBounds(table).Intersects(newPositionBounds)))
                {
                    isOk = false;
                    Debug.Log("mydebug NO RANDOM POSITION FOUND because near table");
                }

                if (isOk == true && roomManager.IsBoundInRoom(newPositionBounds) == false)
                {
                    isOk = false;
                    Debug.Log("mydebug NO RANDOM POSITION FOUND because out of room bounds");
                }

                if (isOk == true)
                {
                    foreach (MRUKAnchor anchor in roomManager.GetCurrentRoom().Anchors)
                    {
                        if (anchor.VolumeBounds.HasValue == true && IsBoundOutsideAnchor(newPositionBounds, anchor) == false)
                        {
                            isOk = false;
                            Debug.Log("mydebug NO RANDOM POSITION FOUND because near anchor " + anchor.Label.ToString() + "anchor bound " + anchor.VolumeBounds.Value.ToString() + " new position bound center" + anchor.transform.InverseTransformPoint(newPositionBounds.center).ToString() + "new position bounds" + newPositionBounds.ToString());
                            break;
                        }
                    }
                }

                if (isOk == true)
                {
                    gameObject.transform.position = randomPosition;
                    Debug.Log("mydebug RANDOM POSITION FOUND" + newPositionBounds.ToString());
                    if (hadAnchor == true)
                    {
                        StartCoroutine(ReattachAnchorAfterRandomPos(gameObject));
                        Debug.Log("mydebug object moved to random position with anchor, anchor recreated");
                    }
                    return;
                }

                maxTry--;
            }

        }


        Debug.LogError("mydebug NO RANDOM POSITION FOUND");

        gameObject.transform.position = InspectorManager.instance.GetCam().transform.position;
        Vector3 temp = new Vector3(gameObject.transform.position.x, GetTotalBounds(gameObject).size.y / 2 + 0.1f, gameObject.transform.position.z);
        gameObject.transform.position = temp;

        if (hadAnchor == true && phase != Phase.PLayingFinal && phase != Phase.PlayingMain)
        {
            spatialAnchorManager.AddAnchorToObject(gameObject);
            Debug.Log("mydebug object moved to cam with anchor, anchor recreated" + gameObject.tag);
        }

    }



    /// <summary>
    /// This method returns a random point in front of the camera in a semi circle
    /// </summary>
    /// <returns></returns>
    public Vector3 GetRandomPointFrontSemiCircle()
    {
        Vector3 cameraPosition = InspectorManager.instance.GetCam().transform.position;

        Vector3 cameraForward = InspectorManager.instance.GetCam().transform.forward;
        Vector3 forwardXZ = new Vector3(cameraForward.x, 0f, cameraForward.z);


        if (forwardXZ == Vector3.zero)
        {
            forwardXZ = Vector3.forward;
            Debug.Log("mydebug front point forward zero");
        }
        else
        {
            forwardXZ.Normalize();
        }


        float randomAngle = UnityEngine.Random.Range(-90f, 90f);
        Quaternion rotation = Quaternion.Euler(0f, randomAngle, 0f);
        Vector3 randomDirection = rotation * forwardXZ;


        float radius = UnityEngine.Random.Range(0.3f, 1.0f);
        Vector3 offset = randomDirection * radius;

        Debug.Log("mydebug front point semi cicle found");

        return new Vector3(
            cameraPosition.x + offset.x,
            0f,
            cameraPosition.z + offset.z
        );
    }



    /// <summary>
    /// This method returns a random point in back of the camera in a semi circle
    /// </summary>
    /// <returns></returns>
    public Vector3 GetRandomPointBackSemiCircle()
    {

        Vector3 cameraPosition = InspectorManager.instance.GetCam().transform.position;

        Vector3 cameraForward = InspectorManager.instance.GetCam().transform.forward;
        Vector3 forwardXZ = new Vector3(cameraForward.x, 0f, cameraForward.z);

        if (forwardXZ == Vector3.zero)
        {
            forwardXZ = Vector3.forward;
            Debug.Log("mydebug back point forward zero");
        }
        else
        {
            forwardXZ.Normalize();
        }


        float randomAngle = 180f + UnityEngine.Random.Range(-90f, 90f);
        Quaternion rotation = Quaternion.Euler(0f, randomAngle, 0f);
        Vector3 randomDirection = rotation * forwardXZ;

        float radius = UnityEngine.Random.Range(0.3f, 1.0f);
        Vector3 offset = randomDirection * radius;

        Debug.Log("mydebug behind point semi cicle found");

        return new Vector3(
            cameraPosition.x + offset.x,
            0f,
            cameraPosition.z + offset.z
        );
    }



    /// <summary>
    /// This is used for reattaching an anchor to a game object after it has been moved to a random position, it's necessary to wait a bit
    /// </summary>
    /// <param name="gameObject"></param>
    /// <returns></returns>
    private IEnumerator ReattachAnchorAfterRandomPos(GameObject gameObject)
    {
        yield return new WaitForSeconds(0.5f);
        spatialAnchorManager.AddAnchorToObject(gameObject);
    }



    /// <summary>
    /// This method checks if a object is outside an anchor volume bounds
    /// </summary>
    /// <param name="bound"></param>
    /// <param name="anchor"></param>
    /// <returns></returns>
    private bool IsBoundOutsideAnchor(Bounds bound, MRUKAnchor anchor)
    {

        Vector3 min = bound.min;
        Vector3 max = bound.max;

        List<Vector3> points = new List<Vector3>
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, max.z)
            };

        foreach (Vector3 point in points)
        {
            if (anchor.IsPositionInVolume(point, true) == true)
                return false;
        }

        return true;


    }



    /// <summary>
    /// This method sorts the concepts in scene in counterclockwise order in the new list considering a center point
    /// </summary>
    /// <param name="concepts"></param>
    /// <param name="center"></param>
    /// <returns></returns>
    private List<GameObject> SortCounterClockWise(List<GameObject> concepts, Vector3 center)
    {
        return concepts.OrderBy(obj => GetAngleFromCenter(obj.transform.position, center)).ToList();
    }



    /// <summary>
    /// This method calculates the angle between the center point a given object
    /// </summary>
    /// <param name="objPosition"></param>
    /// <param name="center"></param>
    /// <returns></returns>
    private float GetAngleFromCenter(Vector3 objPosition, Vector3 center)
    {
        Vector3 direction = objPosition - center;
        float angle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
        return (angle + 360) % 360;
    }



    /// <summary>
    /// This method returns the phase of the game
    /// </summary>
    /// <returns></returns>
    public Phase GetPhase()
    {
        return phase;
    }



    /// <summary>
    /// This method returns the nearest available magnet to a given concept, if any
    /// </summary>
    /// <param name="concept"></param>
    /// <returns></returns>
    private GameObject FindNearestMagnet(GameObject concept)
    {

        GameObject nearestMagnet = null;
        float minDistance = float.MaxValue;

        if (phase == Phase.PlayingMain || phase == Phase.PLayingFinal)
        {

            foreach (GameObject magnet in magnetsData.Keys)
            {
                if (ArePositionsFar(concept.transform.position, magnet.transform.position) == false &&
                    magnetsData[magnet].GetAttachedConcept() == null && minDistance >
                    Vector3.Distance(concept.transform.position, magnet.transform.position))
                {
                    nearestMagnet = magnet;
                    minDistance = Vector3.Distance(concept.transform.position, magnet.transform.position);

                }
            }

        }

        else if (phase == Phase.ConceptDistribution)
        {

            foreach (GameObject magnet in magnetsData.Keys)
            {
                if (ArePositionsFar(concept.transform.position, magnet.transform.position) == false && minDistance >
                    Vector3.Distance(concept.transform.position, magnet.transform.position))
                {
                    nearestMagnet = magnet;
                    minDistance = Vector3.Distance(concept.transform.position, magnet.transform.position);

                }
            }

        }

        if (nearestMagnet == null)
        {
            Debug.Log("mydebug concept moved but no magnet near");
        }

        return nearestMagnet;

    }



    /// <summary>
    /// This method detaches a concept from a magnet in a sequential way considering the counterclockwise order of the magnets
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    private int ConceptMovedPlayingMainSequential(int i)
    {

        Debug.Log("mydebug magnets to be freed NO RANDOM");

        if (magnetsData[sortedMagnets[indexToFree]].GetAttachedConcept() != null)
        {

            Debug.Log("mydebug index to be freed num: " + indexToFree);

            GameObject conceptToMove = magnetsData[sortedMagnets[indexToFree]].GetConceptAssociated();

            GameObject magnetFreed = GetMagnetFromConcept(conceptToMove);

            magnetFreed.SetActive(true);

            PlayMagnetAudio(magnetFreed, false, true);


            MoveToRandomPositionOnFloor(conceptToMove);

            magnetsData[sortedMagnets[indexToFree]].SetAttachedConcept(null);

            conceptToMove.GetComponent<ObjectManipulator>().enabled = true;

            indexToFree++;

            i++;
        }

        else
        {
            indexToFree++;
        }

        if (indexToFree == sortedMagnets.Count)
        {
            indexToFree = 0;
        }

        return i;

    }



    /// <summary>
    /// This method detaches a concept from a magnet in a random way
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    private int ConceptMovedPlayingMainRandom(int i)
    {

        Debug.Log("mydebug magnets to be freed RANDOM");

        if (magnetsToFreePlaying.Count == 0)
        {
            foreach (GameObject magnet in magnetsData.Keys)
            {
                if (magnetsData[magnet].GetAttachedConcept() != null)
                    magnetsToFreePlaying.Add(magnet);
            }
        }

        int randomIndex = UnityEngine.Random.Range(0, magnetsToFreePlaying.Count);

        GameObject conceptToMove = magnetsData[magnetsToFreePlaying[randomIndex]].GetConceptAssociated();

        GameObject magnetFreed = GetMagnetFromConcept(conceptToMove);
        magnetFreed.SetActive(true);

        PlayMagnetAudio(magnetFreed, false, true);

        magnetsData[magnetsToFreePlaying[randomIndex]].SetAttachedConcept(null);

        magnetsToFreePlaying.Remove(magnetsToFreePlaying[randomIndex]);

        MoveToRandomPositionOnFloor(conceptToMove);

        conceptToMove.GetComponent<ObjectManipulator>().enabled = true;

        i++;

        return i;


    }



    /// <summary>
    /// This method considers the case when a concept is placed correctly during the playing main phase
    /// </summary>
    /// <param name="nearestMagnet"></param>
    private void ConceptMovedPlayingMainCorrect(GameObject nearestMagnet)
    {

        Debug.Log("mydebug concept placed correctly");

        magnetsData[nearestMagnet].GetConceptAssociated().GetComponent<ObjectManipulator>().enabled = false;

        int maxMagnets = (int)Math.Ceiling(magnetsData.Keys.Count / 2.0);

        correctStreak++;

        int adjustedStreak = (correctStreak / 4 >= maxMagnets) ? (correctStreak - maxMagnets * 4) : correctStreak;

        int magnetsToBeFree = Math.Min(adjustedStreak / 4 + 1, maxMagnets);


        int multiplayer = (correctStreak / 4 >= maxMagnets) ? 2 : 1;

        score = score + magnetsToBeFree * 5 * multiplayer;

        Debug.Log("mydebug score " + score);


        magnetsData[nearestMagnet].SetTimeIsFree(0f);


        rightParticleEffect.Play(nearestMagnet.transform.position);

        Debug.Log("mydebug magnets to be free " + magnetsToBeFree);

        Debug.Log("mydebug count free magnets during playing " + CountFreeMagnetDuringPlaying());


        for (int i = 0; i < magnetsToBeFree && CountFreeMagnetDuringPlaying() < magnetsToBeFree;)
        {

            Debug.Log("mydebug magnets to be freed inside for cycle");

            if (correctStreak / 4 < maxMagnets)
            {
                i = ConceptMovedPlayingMainSequential(i);
            }

            else
            {
                i = ConceptMovedPlayingMainRandom(i);
            }
        }

    }



    /// <summary>
    /// This method manages the main phase of the game, it simply checks if a concept is placed correctly and if not detaches it
    /// If it is placed correctly it also frees some magnets in a sequential or random way depending on some rules
    /// </summary>
    /// <param name="concept"></param>
    /// <param name="nearestMagnet"></param>
    private void ConceptMovedPlayingMain(GameObject concept, GameObject nearestMagnet)
    {

        magnetsData[nearestMagnet].SetAttachedConcept(concept);

        nearestMagnet.SetActive(false);

        Debug.Log("mydebug concept moved and magnet near");

        concept.transform.position = nearestMagnet.transform.position;
        concept.transform.rotation = Quaternion.LookRotation(InspectorManager.instance.GetCam().transform.forward);

        if (magnetsData[nearestMagnet].GetConceptAssociated() == concept)
        {
            ConceptMovedPlayingMainCorrect(nearestMagnet);
        }

        else
        {

            wrongParticleEffect.Play(nearestMagnet.transform.position);

            if (score > 0)
                score -= 5;
            Debug.Log("mydebug score wrong " + score);

            correctStreak -= 2;

            if (correctStreak < 0)
                correctStreak = 0;

            concept.GetComponent<ObjectManipulator>().enabled = false;

            StartCoroutine(DetachConcept(nearestMagnet, concept, 1.3f));

            Debug.Log("mydebug concept placed wrongly");
        }



    }



    /// <summary>
    /// This method checks if all magnets are associated correctly
    /// </summary>
    /// <returns></returns>
    private bool AreAllMagnetsOccupiedCorrectly()
    {
        foreach (GameObject magnet in magnetsData.Keys)
        {
            if (magnetsData[magnet].GetAttachedConcept() != magnetsData[magnet].GetConceptAssociated())
                return false;
        }

        Debug.Log("mydebug all magnets occupied correctly");
        return true;
    }



    /// <summary>
    /// This method manages the final phase of the game, it simply checks if a concept is placed correctly and if not detaches it
    /// </summary>
    /// <param name="concept"></param>
    /// <param name="nearestMagnet"></param>
    private void ConceptMovedPlayingFinal(GameObject concept, GameObject nearestMagnet)
    {
        magnetsData[nearestMagnet].SetAttachedConcept(concept);

        Debug.Log("mydebug concept moved and magnet near");

        concept.transform.position = nearestMagnet.transform.position;
        concept.transform.rotation = Quaternion.LookRotation(InspectorManager.instance.GetCam().transform.forward);

        if (magnetsData[nearestMagnet].GetConceptAssociated() == concept)
        {

            rightParticleEffect.Play(nearestMagnet.transform.position);

            Debug.Log("mydebug concept placed correctly");

            magnetsData[nearestMagnet].GetConceptAssociated().GetComponent<ObjectManipulator>().enabled = false;

            foreach (GameObject magnet in sortedMagnets)
            {
                if (magnet.activeSelf == false)
                {
                    magnet.SetActive(true);
                    break;
                }
            }

            if (AreAllMagnetsOccupiedCorrectly())
            {
                phase = Phase.ENDED;
                foreach (GameObject magnet in magnetsData.Keys)
                {
                    magnetsData[magnet].GetConceptAssociated().GetComponent<ObjectManipulator>().enabled = false;
                }

                score = score + score / 5;
                Debug.Log("mydebug score final " + score);
                Dialog dialog = InstantiateDialogBox("END",
                    "Congratulations! You have completed the game of loci. Your score is: " + score +
                    ". Would you like to publish this in the standings?", DialogType.RequestStandings);
                StartCoroutine(DismissDialog(dialog));
                return;
            }
        }

        else
        {

            wrongParticleEffect.Play(nearestMagnet.transform.position);

            concept.GetComponent<ObjectManipulator>().enabled = false;

            StartCoroutine(DetachConcept(nearestMagnet, concept, 1.3f));

            Debug.Log("mydebug concept placed wrongly");
        }

    }



    /// <summary>
    /// This method checks if a given concept is inside a magnet space, updates the data of the magnets, and then calls ConceptMovedPlayingMain() or ConceptMovedPlayingFinal() depending on the phase
    /// </summary>
    /// <param name="concept"></param>
    public void ConceptMovedPlaying(GameObject concept)
    {

        magnetsData[GetMagnetFromConcept(concept)].SetAttachedConcept(null);


        GameObject nearestMagnet = FindNearestMagnet(concept);

        if (nearestMagnet == null || magnetsData[nearestMagnet].GetAttachedConcept() != null ||
            (nearestMagnet.activeSelf == false && magnetsData[nearestMagnet].GetAttachedConcept() == null))
        {
            return;
        }


        else if (phase == Phase.PlayingMain)
        {
            ConceptMovedPlayingMain(concept, nearestMagnet);
            PlayMagnetAudio(nearestMagnet, true, false);

        }

        else if (phase == Phase.PLayingFinal)
        {

            ConceptMovedPlayingFinal(concept, nearestMagnet);
            PlayMagnetAudio(nearestMagnet, true, false);
        }
    }



    /// <summary>
    /// This method detaches a concept from a magnet after a certain time
    /// </summary>
    /// <param name="magnet"></param>
    /// <param name="concept"></param>
    /// <param name="time"></param>
    /// <returns></returns>
    private IEnumerator DetachConcept(GameObject magnet, GameObject concept, float time)
    {
        yield return new WaitForSeconds(time);

        magnet.SetActive(true);

        PlayMagnetAudio(magnet, false, true);

        MoveToRandomPositionOnFloor(concept);

        magnetsData[magnet].SetAttachedConcept(null);

        concept.GetComponent<ObjectManipulator>().enabled = true;
    }



    /// <summary>
    /// This method returns the number of free magnets during the playing phase
    /// </summary>
    /// <returns></returns>
    private int CountFreeMagnetDuringPlaying()
    {

        int count = 0;
        foreach (MagnetData magnetData in magnetsData.Values)
        {
            if (magnetData.GetAttachedConcept() == null)
                count++;
        }

        return count;
    }



    /// <summary>
    /// This method sets the concept associated to a magnet as picked up by hand or not during the playing main phase
    /// </summary>
    /// <param name="concept"></param>
    /// <param name="isMoving"></param>
    public void SetConceptPickedPlayingMain(GameObject concept, bool isMoving)
    {
        if (phase == Phase.PlayingMain)
            magnetsData[GetMagnetFromConcept(concept)].SetAssociatedConceptIsPickedPlayingMain(isMoving);
    }



    /// <summary>
    /// Create the particle systems for the right and wrong answers
    /// </summary>
    private void InitializeParticleSystems()
    {

        rightParticleEffect = new ParticleEffect(true, InspectorManager.instance.GetParticleMaterial(), this);

        wrongParticleEffect = new ParticleEffect(false, InspectorManager.instance.GetParticleMaterial(), this);
    }



    /// <summary>
    /// This method prepares and starts the background music
    /// </summary>
    private void PrepareAndPlayBackgroundMusic()
    {

        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.0f;
        audioSource.volume = 1.0f;
        audioSource.loop = true;
        audioSource.clip = InspectorManager.instance.GetBackgroundMusic();
        audioSource.Play();

        Debug.Log("mydebug background music started");

    }



    /// <summary>
    /// This method prepares the audio associated to a certaint object
    /// </summary>
    /// <param name="gameObject"></param>
    private void PrepareAudio(GameObject gameObject)
    {
        if (gameObject.GetComponent<AudioSource>() != null)
            Debug.LogError("mydebug audio source already exists");

        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f;
        audioSource.volume = 1.0f;
        audioSource.loop = false;

        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.maxDistance = 3.0f;
        audioSource.minDistance = 0.1f;


    }



    /// <summary>
    /// This method plays the audio of a magnet when it is attached or detached
    /// </summary>
    /// <param name="magnet"></param>
    /// <param name="attached"></param>
    /// <param name="detached"></param>
    private void PlayMagnetAudio(GameObject magnet, bool attached, bool detached)
    {


        AudioSource audioSource = magnet.GetComponent<AudioSource>();
        Debug.Log(gameObject.ToString() + audioSource.maxDistance.ToString());

        if (attached == true && detached == false)
        {
            audioSource.clip = InspectorManager.instance.GetMagnetAttachSound();
            audioSource.PlayOneShot(audioSource.clip);
            Debug.Log("mydebug play magnet audio attach");
        }
        else if (attached == false && detached == true)
        {
            audioSource.clip = InspectorManager.instance.GetMagnetDetachSound();
            audioSource.PlayOneShot(audioSource.clip);
            Debug.Log("mydebug play magnet audio detach");
        }
        else
        {
            audioSource.PlayOneShot(InspectorManager.instance.GetMagnetDetachSound());
            audioSource.PlayOneShot(InspectorManager.instance.GetMagnetAttachSound());

            Debug.Log("mydebug play magnet audio detach and attach");
        }
    }



    /// <summary>
    /// This method plays the audio of the table when a concept or a magnet is spawned
    /// </summary>
    private void PlayTableAudio()
    {

        AudioSource audioSource = table.GetComponent<AudioSource>();

        audioSource.clip = InspectorManager.instance.GetSpawnSound();
        audioSource.PlayOneShot(audioSource.clip);
        Debug.Log("mydebug play table audio");

    }



    /// <summary>
    /// This method return the concept detached for the most time during the playing main phase
    /// </summary>
    /// <returns></returns>
    private GameObject GetConceptDetachedForMost()
    {

        float maxTime = float.MinValue;
        GameObject chosenMagnet = null;

        foreach (GameObject magnet in magnetsData.Keys)
        {
            if (magnetsData[magnet].GetTimeIsFree() >= maxTime)
            {
                maxTime = magnetsData[magnet].GetTimeIsFree();
                chosenMagnet = magnet;
            }
        }

        return magnetsData[chosenMagnet].GetConceptAssociated();
    }

    /// <summary>
    /// This method is used for instantiating a dialog box
    /// </summary>
    /// <param name="title"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    private Dialog InstantiateDialogBox(string title, string message, DialogType dialogType = DialogType.Generic)
    {

        if (oldDialog != null)
        {
            oldDialog.DismissDialog();
        }

        if (dialogType == DialogType.Generic)
        {
            Dialog temp = Dialog.Open(InspectorManager.instance.GetDialogPrefab(), DialogButtonType.OK, title, message, true);
            temp.transform.parent = this.gameObject.transform;
            oldDialog = temp;
            return temp;
        }
        else if (dialogType == DialogType.RequestStandings)
        {
            Dialog temp = Dialog.Open(InspectorManager.instance.GetDialogPrefab(), DialogButtonType.Yes | DialogButtonType.No, title, message, true);
            temp.transform.parent = this.gameObject.transform;
            temp.OnClosed += PublishOnStandingsMessage;
            oldDialog = temp;
            return temp;
        }

        Debug.LogError("mydebug dialog type not recognized");
        return null;


    }



    /// <summary>
    /// Method used for saving the score in the standings
    /// </summary>
    /// <param name="result"></param>
    private void PublishOnStandingsMessage(DialogResult result)
    {
        if (result.Result == DialogButtonType.Yes)
        {
            standings.Add(controller.GetUserName(), score);
            controller.SaveStandings(standings);
            Debug.Log("mydebug publish on standings");

        }
        else
        {
            Debug.Log("mydebug do not publish on standings");
        }
    }



    /// <summary>
    /// This method is used for seeing the standings in more than one dialog box open one at a time
    /// </summary>
    private IEnumerator ManageStandingsMessage()
    {

        List<List<KeyValuePair<string, int>>> standingsList = StandingsPreparadeForPrint();

        foreach (List<KeyValuePair<string, int>> subList in standingsList)
        {
            string message = "";
            int maxKeyLength = subList.Max(entry => Math.Min(entry.Key.Length, 20));
            int maxValueDigits = subList.Max(entry => entry.Value.ToString().Length);

            foreach (KeyValuePair<string, int> entry in subList)
            {
                string truncatedKey = entry.Key.Substring(0, Math.Min(entry.Key.Length, 20));
                string formattedValue = entry.Value.ToString().PadLeft(maxValueDigits);
                message += $"{truncatedKey.PadRight(maxKeyLength)} {formattedValue}\n";
            }

            Dialog dialog = InstantiateDialogBox("STANDINGS", message);
            while (dialog != null)
            {
                yield return null;
            }

            if (interruptStandings)
                break;
        }


    }



    /// <summary>
    /// This method is used for preparing the standings dictionary in sub lists for printing in dialogs box
    /// </summary>
    /// <returns></returns>
    private List<List<KeyValuePair<string, int>>> StandingsPreparadeForPrint()
    {

        List<KeyValuePair<string, int>> temp = standings.OrderByDescending(entry => entry.Value).ToList();

        List<List<KeyValuePair<string, int>>> standingsList = new List<List<KeyValuePair<string, int>>>();

        for (int i = 0; i < temp.Count; i += 6)
        {
            standingsList.Add(temp.Skip(i).Take(6).ToList());
        }

        return standingsList;
    }



    /// <summary>
    /// This is used as Update method for the LociManager
    /// </summary>
    /// <returns></returns>
    private IEnumerator LociUpdate()
    {

        bool beforeInRoom = true;
        Dialog outOfRoomDialog = null;

        Dialog welcomeDialog = InstantiateDialogBox("WELCOME", "Welcome to the Loci application, please go to the table following the arrow and press the button located on the table to start the experience. ");
        StartCoroutine(DismissDialog(welcomeDialog));

        while (true)
        {

            if (!roomManager.GetCurrentRoom().IsPositionInRoom(InspectorManager.instance.GetCam().transform.position) && beforeInRoom == true && outOfRoomDialog == null)
            {
                interruptStandings = true;
                outOfRoomDialog = InstantiateDialogBox("OUT OF ROOM", "You are out of the room in which the experience is located, please go back to continue.");
                beforeInRoom = false;
            }

            if (roomManager.GetCurrentRoom().IsPositionInRoom(InspectorManager.instance.GetCam().transform.position))
            {
                beforeInRoom = true;
                interruptStandings = false;
                if (outOfRoomDialog != null)
                {
                    outOfRoomDialog.DismissDialog();
                    outOfRoomDialog = null;
                }
            }

            yield return null;
        }
    }



    /// <summary>
    /// This method is used for dismissing a dialog box after a certain time if the user has not done it
    /// </summary>
    /// <param name="dialog"></param>
    /// <returns></returns>
    private IEnumerator DismissDialog(Dialog dialog)
    {

        yield return new WaitForSeconds(10.0f);


        if (dialog != null)
        {
            dialog.DismissDialog();
            Debug.Log("mydebug dialog destroyed");
        }
        else
        {
            Debug.Log("mydebug dialog already destroyed");
        }

    }



    /// <summary>
    /// This is the action connected to the button of magnet phase
    /// </summary>
    private void MagnetButtonPressed()
    {

        SetMagnetsConceptsState(true, true);

        if (availablePhase.Contains(Phase.MagnetDistribution))
            ChangePhase(Phase.MagnetDistribution);
        Debug.Log("mydebug magnet button pressed");
        buttonsMenu.SetActive(false);
    }



    /// <summary>
    /// This is the action connected to the button of concept phase
    /// </summary>
    private void ConceptButtonPressed()
    {

        SetMagnetsConceptsState(true, true);

        if (availablePhase.Contains(Phase.ConceptDistribution))
            ChangePhase(Phase.ConceptDistribution);

        else if (AreAllMagnetsOutsideTableSpace() == false)
        {
            Dialog dialog = InstantiateDialogBox("WARNING", "One or more magnets are too close to the table, move them away a bit.");
            StartCoroutine(DismissDialog(dialog));
        }
        else
        {
            Dialog dialog = InstantiateDialogBox("WARNING", "You have not completed the magnet distribution phase yet, please complete it before continuing.");
            StartCoroutine(DismissDialog(dialog));
        }

        Debug.Log("mydebug concept button pressed");
        buttonsMenu.SetActive(false);
    }



    /// <summary>
    /// This is the action connected to the button of play phase
    /// </summary>
    private void PlayButtonPressed()
    {

        SetMagnetsConceptsState(true, true);

        if (availablePhase.Contains(Phase.PlayingMain))
            ChangePhase(Phase.PlayingMain);
        else if (!availablePhase.Contains(Phase.PlayingMain))
        {
            Dialog dialog = InstantiateDialogBox("WARNING",
                "You have not completed the concept distribution phase yet, please complete it before continuing.");
            StartCoroutine(DismissDialog(dialog));
        }

        Debug.Log("mydebug play button pressed");
        buttonsMenu.SetActive(false);
    }



    /// <summary>
    /// This button is used for seeing the standings
    /// </summary>
    private void StandingsButtonPressed()
    {

        SetMagnetsConceptsState(true, true);

        Debug.Log("mydebug standings button pressed");

        if (phase == Phase.PlayingMain || phase == Phase.PLayingFinal)
        {

            Dialog dialog = InstantiateDialogBox("WARNING",
                "You are in the playing phase, you cannot see the standings now.");
            StartCoroutine(DismissDialog(dialog));
        }
        else
        {
            StartCoroutine(ManageStandingsMessage());
        }


        buttonsMenu.SetActive(false);
    }



    /// <summary>
    /// This button is used for memorizing the concepts, it disables magnets renderings
    /// </summary>
    private void MemorizeButtonPressed()
    {

        SetMagnetsConceptsState(false, true);

        Debug.Log("mydebug memorize button pressed");

        if (availablePhase.Contains(Phase.PlayingMain) && phase == Phase.ConceptDistribution)
        {
            ChangePhase(Phase.MEMORIZE);

        }
        else if (availablePhase.Contains(Phase.PlayingMain) == false)
        {
            Dialog dialog = InstantiateDialogBox("WARNING",
                "You have not completed concept distribution yet, please please complete it before continuing.");
            StartCoroutine(DismissDialog(dialog));
            SetMagnetsConceptsState(true, true);
        }
        else if (phase == Phase.PlayingMain || phase == Phase.PLayingFinal || phase == Phase.ENDED)
        {
            Dialog dialog = InstantiateDialogBox("WARNING",
                "You are in the playing phase, you cannot memorize concepts now. Go back to concept distribution phase.");
            StartCoroutine(DismissDialog(dialog));
            SetMagnetsConceptsState(true, true);
        }

        buttonsMenu.SetActive(false);
    }



    /// <summary>
    /// This button is used for rotating the concept
    /// </summary>
    private void RightButtonPressed()
    {
        var concepts = objectManager.GetConceptsInScene();
        var obj = concepts[concepts.Count - 1];
        obj.transform.rotation = Quaternion.LookRotation(InspectorManager.instance.GetCam().transform.forward);

        obj.transform.GetChild(0).gameObject.transform.Rotate(0, 20, 0);

    }



    /// <summary>
    /// This button is used for rotating the concept
    /// </summary>
    private void LeftButtonPressed()
    {
        var concepts = objectManager.GetConceptsInScene();
        var obj = concepts[concepts.Count - 1];
        obj.transform.rotation = Quaternion.LookRotation(InspectorManager.instance.GetCam().transform.forward);

        obj.transform.GetChild(0).gameObject.transform.Rotate(0, -20, 0);
    }



    /// <summary>
    /// This button is used for rotating the concept
    /// </summary>
    private void UpButtonPressed()
    {
        var concepts = objectManager.GetConceptsInScene();
        var obj = concepts[concepts.Count - 1];
        obj.transform.rotation = Quaternion.LookRotation(InspectorManager.instance.GetCam().transform.forward);
        obj.transform.GetChild(0).gameObject.transform.Rotate(20, 0, 0);
    }



    /// <summary>
    /// This button is used for rotating the concept
    /// </summary>
    private void DownButtonPressed()
    {
        var concepts = objectManager.GetConceptsInScene();
        var obj = concepts[concepts.Count - 1];
        obj.transform.rotation = Quaternion.LookRotation(InspectorManager.instance.GetCam().transform.forward);
        obj.transform.GetChild(0).gameObject.transform.Rotate(-20, 0, 0);
    }


    /// <summary>
    /// This method is used for find the method connected to a button type
    /// </summary>
    /// <param name="buttonType"></param>
    private void ButtonMenuClicked(ButtonType buttonType)
    {

        Debug.Log("mydebug button clicked initialization " + buttonType.ToString());

        if (buttonType == ButtonType.MAGNETDISTRIBUTION)
        {
            buttonsOnMenu[buttonType].GetComponent<Interactable>().OnClick.AddListener(MagnetButtonPressed);
        }
        else if (buttonType == ButtonType.CONCEPTDISTRIBUTION)
        {
            buttonsOnMenu[buttonType].GetComponent<Interactable>().OnClick.AddListener(ConceptButtonPressed);
        }
        else if (buttonType == ButtonType.PLAYING)
        {
            buttonsOnMenu[buttonType].GetComponent<Interactable>().OnClick.AddListener(PlayButtonPressed);
        }
        else if (buttonType == ButtonType.STANDINGS)
        {
            buttonsOnMenu[buttonType].GetComponent<Interactable>().OnClick.AddListener(StandingsButtonPressed);
        }
        else if (buttonType == ButtonType.MEMORIZECONCEPTS)
        {
            buttonsOnMenu[buttonType].GetComponent<Interactable>().OnClick.AddListener(MemorizeButtonPressed);
        }
        else if (buttonType == ButtonType.RIGHT)
        {
            buttonsOnMenu[buttonType].GetComponent<Interactable>().OnClick.AddListener(RightButtonPressed);
        }
        else if (buttonType == ButtonType.LEFT)
        {
            buttonsOnMenu[buttonType].GetComponent<Interactable>().OnClick.AddListener(LeftButtonPressed);
        }
        else if (buttonType == ButtonType.UP)
        {
            buttonsOnMenu[buttonType].GetComponent<Interactable>().OnClick.AddListener(UpButtonPressed);
        }
        else if (buttonType == ButtonType.DOWN)
        {
            buttonsOnMenu[buttonType].GetComponent<Interactable>().OnClick.AddListener(DownButtonPressed);
        }
        else
        {
            Debug.LogError("mydebug button type not recognized");
        }
    }



    /// <summary>
    /// Action performed when the table button is pressed
    /// </summary>
    public void TableButtonPressed()
    {

        if (oldDialog != null)
        {
            return;
        }

        foreach (Phase phase in availablePhase)
        {
            Debug.Log("mydebug available phase " + phase.ToString());
        }

        Vector3 pos = table.transform.position + new Vector3(0, 0.6f, 0) + new Vector3(0.0f, GetTotalBounds(table).size.y, 0.0f);


        buttonsMenu.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(InspectorManager.instance.GetCam().transform.forward));
        buttonsMenu.SetActive(true);

        SetMagnetsConceptsState(false, false);

    }



    /// <summary>
    /// This method is used for enabling or disabling the magnets and concepts
    /// </summary>
    /// <param name="stateMagnets"></param>
    /// <param name="stateConcept"></param>
    private void SetMagnetsConceptsState(bool stateMagnets, bool stateConcept)
    {

        foreach (GameObject concept in objectManager.GetConceptsInScene())
        {
            concept.SetActive(stateConcept);
        }



        foreach (GameObject magnet in objectManager.GetMagnetsInScene())
        {
            magnet.SetActive(stateMagnets);
        }

    }



    /// <summary>
    /// Used for debugging, this method creates a sphere in each corner of a given bounds
    /// </summary>
    /// <param name="bounds"></param>
    private void DebugBounds(Bounds bounds)
    {
        Vector3[] corners = new Vector3[8];

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        corners[0] = new Vector3(min.x, min.y, min.z);
        corners[1] = new Vector3(min.x, min.y, max.z);
        corners[2] = new Vector3(min.x, max.y, min.z);
        corners[3] = new Vector3(min.x, max.y, max.z);
        corners[4] = new Vector3(max.x, min.y, min.z);
        corners[5] = new Vector3(max.x, min.y, max.z);
        corners[6] = new Vector3(max.x, max.y, min.z);
        corners[7] = new Vector3(max.x, max.y, max.z);

        int count = 0;

        foreach (Vector3 corner in corners)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = corner;
            sphere.transform.localScale = Vector3.one * 0.07f;
            count++;
        }

        Debug.Log("mydebug number of spheres bounds" + count);
    }



    /// <summary>
    /// This method is used for getting the total bounds of a game object
    /// </summary>
    /// <param name="gameObject"></param>
    /// <returns></returns>
    private Bounds GetTotalBounds(GameObject gameObject)
    {
        if (gameObject.CompareTag(ObjectType.LociTable.ToString()))
            return gameObject.GetComponent<Renderer>().bounds;

        Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();

        Debug.Log("mydebug number of renderers found " + renderers.Length);

        if (renderers.Length == 0)
        {
            Debug.LogError("mydebug no renderers found");
            return new Bounds(gameObject.transform.position, Vector3.zero);
        }

        Bounds totalBounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            totalBounds.Encapsulate(renderers[i].bounds);
        }

        return totalBounds;
    }



    /// <summary>
    /// This method plays the sound of the magnet when concept is picked up from a magnet
    /// </summary>
    /// <param name="concept"></param>
    public void PlaySoundBeginMovementConcept(GameObject concept)
    {

        foreach (GameObject magnet in magnetsData.Keys)
        {
            if (magnetsData[magnet].GetConceptAssociated() == concept)
            {
                PlayMagnetAudio(magnet, false, true);
                return;
            }
        }

    }


    /// <summary>
    /// This methods returns the controller of the game
    /// </summary>
    /// <returns></returns>
    public Controller GetController()
    {
        return controller;
    }

    /// <summary>
    /// This method return the menu for rotating concept
    /// </summary>
    /// <returns></returns>
    public GameObject GetRotatorConcept()
    {
        return conceptRotator;
    }


    /// <summary>
    /// This method sets the table of the game
    /// </summary>
    /// <param name="table"></param>
    public void SetTable(GameObject table)
    {
        this.table = table;
    }







}
