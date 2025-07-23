using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

public class SpatialAnchorManipulation : MonoBehaviour
{
    /// <summary>
    /// Boolean used for checking if the object is a concept
    /// </summary>
    private bool IsConcept = false;

    /// <summary>
    /// Boolean used for checking if the object has an anchor
    /// </summary>
    private bool hasAnchor = false;

    /// <summary>
    /// This is the reference to the loci manager
    /// </summary>
    private LociManager lociManager;

    /// <summary>
    /// This is the reference to the spatial anchor manager
    /// </summary>
    private SpatialAnchorManager spatialAnchorManager;

    /// <summary>
    /// This is the boolean used for checking if the object has been moved for the first time
    /// </summary>
    private bool isFirstTimeMoved;



    /// <summary>
    /// Method used for initializing the spatial anchor manipulation
    /// </summary>
    public void Init(ObjectManager objectManager, LociManager lociManager,
        SpatialAnchorManager spatialAnchorManager)
    {
        if (objectManager.IsConcept(gameObject))
        {
            IsConcept = true;
        }

        this.lociManager = lociManager;
        this.spatialAnchorManager = spatialAnchorManager;
        isFirstTimeMoved = true;
    }



    /// <summary>
    /// Anchors can't be moved, so when an object is touched, its anchor should be deleted
    /// </summary>
    public void ManipulationStarted(ManipulationEventData eventData)
    {

        lociManager.GetRotatorConcept().SetActive(false);

        if (IsConcept)
        {

            Debug.Log("mydebug concept picked, calling set concept picked");
            lociManager.SetConceptPickedPlayingMain(gameObject, true);
            Debug.Log("mydebug concept picked, called set concept picked");

            if (lociManager.GetPhase() == Phase.ConceptDistribution)
            {
                lociManager.PlaySoundBeginMovementConcept(gameObject);
            }

        }

        if (gameObject.GetComponent<OVRSpatialAnchor>() == null)
        {
            hasAnchor = false;
            Debug.Log("mydebug object picked no anchor");
            return;
        }

        else
        {
            hasAnchor = true;
            Debug.Log("mydebug object picked anchor");
            spatialAnchorManager.MovementStarted(gameObject.GetComponent<OVRSpatialAnchor>());

        }
    }



    /// <summary>
    /// After leaving the object, the anchor should be recreated
    /// </summary>
    public void ManipulationEnded(ManipulationEventData eventData)
    {

        if (IsConcept == true)
        {
            lociManager.SetConceptPickedPlayingMain(gameObject, false);

            if (lociManager.GetPhase() == Phase.ConceptDistribution)
            {
                Debug.Log("mydebug concept moved in concept distribution phase");
                lociManager.ConceptMovedConceptDistribution(gameObject);
            }

            else if (lociManager.GetPhase() == Phase.PlayingMain || lociManager.GetPhase() == Phase.PLayingFinal)
            {
                Debug.Log("mydebug concept moved in playing phase or final match");
                lociManager.ConceptMovedPlaying(gameObject);
            }
        }

        else if (gameObject.CompareTag(ObjectType.LociMagnet.ToString()) == true)
        {
            Debug.Log("mydebug magnet moved");
            lociManager.MagnetMovedMagnetDistribution(gameObject);
        }


        if (isFirstTimeMoved == true && IsConcept == true)
        {
            gameObject.GetComponent<ConceptController>().ConceptMovedFirstTime(lociManager.GetController());
            Debug.Log("mydebug concept left first time moved");

        }

        else if (hasAnchor == false)
        {
            Debug.Log("mydebug object left no anchor");
        }

        else if (IsConcept == false)
        {
            Debug.Log("mydebug object left anchor");
            spatialAnchorManager.MovementEnded(gameObject.AddComponent<OVRSpatialAnchor>(), gameObject.tag);
        }

        isFirstTimeMoved = false;

    }


}
