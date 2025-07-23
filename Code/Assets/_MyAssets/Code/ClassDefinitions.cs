using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;


public class ClassDefinitions : MonoBehaviour { }

public enum DialogType
{
    Generic,
    RequestStandings,
    Standings,
}

/// <summary>
/// This enum contains different kind of objects that can be spawned in the scene
/// </summary>
public enum ObjectType
{
    LociSphere,
    LociCube,
    LociConcept,
    LociMagnet,
    LociTable
}

public enum ConceptType
{
    IMAGE,
    VIDEO,
    AUDIO,
    OBJECT3D
}

/// <summary>
/// This class is used because objectType is not sufficient to identify concepts, so it is necessary to add the id.
/// In order to use a single structure in anchorDictionary, this solution was chosen.
/// </summary>
public class AnchorType
{

    /// <summary>
    /// Enum for identifying the type of object associated with the anchor
    /// </summary>
    [JsonProperty]
    private ObjectType objectType;

    /// <summary>
    /// This is used if the the object associated with the anchor is a concept, in order to identify the concept
    /// </summary>
    [JsonProperty]
    private string id;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="id"></param>
    public AnchorType(ObjectType objectType, string id = null)
    {
        this.objectType = objectType;
        this.id = id;
    }



    /// <summary>
    /// Getter for the object type
    /// </summary>
    /// <returns></returns>
    public ObjectType GetObjectType()
    {
        return objectType;
    }



    /// <summary>
    /// Getter for the id
    /// </summary>
    /// <returns></returns>
    public string GetId()
    {
        return id;
    }
}



/// <summary>
/// This enum identifies the possible phase in which the application can be
/// </summary>
public enum Phase
{
    MagnetDistribution,
    ConceptDistribution,
    PlayingMain,
    PLayingFinal,
    ENDED,
    MEMORIZE,
}



public enum ButtonType
{
    MAGNETDISTRIBUTION,
    CONCEPTDISTRIBUTION,
    PLAYING,
    MEMORIZECONCEPTS,
    STANDINGS,
    RIGHT,
    UP,
    LEFT,
    DOWN
}



/// <summary>
/// Class that contains the data associated to a magnet
/// </summary>
public class MagnetData
{

    /// <summary>
    /// This variable is true if the magnet is outside the table space, used for knowing if spawning a new magnet is possible
    /// </summary>
    private bool isOutSideTableSpace;

    /// <summary>
    /// This object represents a concept of Loci technique associated to the magnet
    /// </summary>
    private GameObject conceptAssociated;

    /// <summary>
    /// This float indicates for how long the magnet is free during the playing phase, when it becomes occupied then it's set to 0.0f.
    /// The free state is reffered to its associated concept.
    /// </summary>
    private float timeIsFree;

    /// <summary>
    /// This boolean is true if the concept associated to the magnet is picked by hand
    /// </summary>
    private bool associatedConceptIsPicked;

    /// <summary>
    /// This represents the concepts attached to the magnet at a certain time
    /// </summary>
    private GameObject attachedConcept;



    /// <summary>
    /// Constructor of the class
    /// </summary>
    public MagnetData()
    {
        this.conceptAssociated = null;
        this.isOutSideTableSpace = false;
        this.timeIsFree = 0.0f;
        this.associatedConceptIsPicked = false;
        this.attachedConcept = null;
    }



    /// <summary>
    /// Set if the magnet is outside the table space
    /// </summary>
    /// <param name="isOutSideTableSpace"></param>
    public void SetIsOutSideTableSpace(bool isOutSideTableSpace)
    {
        this.isOutSideTableSpace = isOutSideTableSpace;
    }



    /// <summary>
    /// Get if the magnet is outside the table space
    /// </summary>
    /// <returns></returns>
    public bool GetIsOutSideTableSpace()
    {
        return isOutSideTableSpace;
    }



    /// <summary>
    /// Get the concept associated to the magnet
    /// </summary>
    /// <returns></returns>
    public GameObject GetConceptAssociated()
    {
        return conceptAssociated;
    }



    /// <summary>
    /// Set the concept associated to the magnet
    /// </summary>
    /// <param name="concept"></param>
    public void SetConceptAssociated(GameObject concept)
    {
        conceptAssociated = concept;
    }


    /// <summary>
    /// Set for how long the magnet is free during the playing phase
    /// </summary>
    /// <param name="timeIsFree"></param>
    public void SetTimeIsFree(float timeIsFree)
    {
        this.timeIsFree = timeIsFree;
    }



    /// <summary>
    /// Get for how long the magnet is free during the playing phase
    /// </summary>
    /// <returns></returns>
    public float GetTimeIsFree()
    {
        return timeIsFree;
    }



    /// <summary>
    /// Get if the concept associated to the magnet is picked by hand during the playing main phase
    /// </summary>
    /// <param name="associatedConceptIsPicked"></param>
    public void SetAssociatedConceptIsPickedPlayingMain(bool associatedConceptIsPicked)
    {
        this.associatedConceptIsPicked = associatedConceptIsPicked;
    }



    /// <summary>
    /// Set if the concept associated to the magnet is picked by hand during the playing main phase
    /// </summary>
    /// <returns></returns>
    public bool GetAssociatedConceptIsPickedPlayingMain()
    {
        return associatedConceptIsPicked;
    }




    /// <summary>
    /// This sets the object attached to the magnet at a given time
    /// </summary>
    /// <param name="attachedConcept"></param>
    public void SetAttachedConcept(GameObject attachedConcept)
    {
        this.attachedConcept = attachedConcept;
    }


    /// <summary>
    /// Get the object attached to the magnet at a given time
    /// </summary>
    /// <returns></returns>
    public GameObject GetAttachedConcept()
    {
        return attachedConcept;
    }



}



/// <summary>
/// This class contains the data of the experience which need to be saved cross sessions
/// </summary>
public class ExperienceData
{

    /// <summary>
    /// The dictionary containing anchor information
    /// </summary>
    [JsonProperty]
    private Dictionary<System.Guid, AnchorType> anchorData;

    [JsonProperty]
    private Dictionary<string, List<float>> concepts3DRotations;

    /// <summary>
    /// Constructor of the class
    /// </summary>
    /// <param name="anchorData"></param>
    public ExperienceData(Dictionary<System.Guid, AnchorType> anchorData, Dictionary<string, List<float>> concepts3DRotations)
    {
        this.anchorData = anchorData;
        this.concepts3DRotations = concepts3DRotations;
    }


    /// <summary>
    /// Get the dictionary containing anchor information
    /// </summary>
    /// <returns></returns>
    public Dictionary<System.Guid, AnchorType> GetAnchorData()
    {
        return anchorData;
    }


    /// <summary>
    /// Set the dictionary containing anchor information
    /// </summary>
    /// <param name="anchorType"></param>
    public void SetAnchorData(Dictionary<System.Guid, AnchorType> anchorType)
    {
        this.anchorData = anchorType;
    }


    /// <summary>
    /// Getter for the 3D rotation of the concepts
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, List<float>> GetRotationData()
    {
        return concepts3DRotations;
    }


    /// <summary>
    /// Setter for the 3D rotation of the concepts
    /// </summary>
    /// <param name="rotationData"></param>
    public void SetRotationData(string id, List<float> rotationData)
    {

        if (concepts3DRotations.ContainsKey(id))
        {
            concepts3DRotations[id] = rotationData;
            Debug.Log("mydebug rotation data updated");
        }
        else
        {
            concepts3DRotations.Add(id, rotationData);
            Debug.Log("mydebug rotation data added");
        }

    }


}



/// <summary>
/// Class used in LociManager for managing particle systems
/// </summary>
public class ParticleEffect
{

    /// <summary>
    /// Object to which the particle system is attached
    /// </summary>
    private GameObject particleSystemObject;

    /// <summary>
    /// The particle system
    /// </summary>
    private ParticleSystem particleSystem;

    /// <summary>
    /// Constructor of the class, the boolean is used to determine if the particle system is positive(correct "answer") or negative(wrong "answer")
    /// </summary>
    /// <param name="isPositive"></param>
    /// <param name="material"></param>
    public ParticleEffect(Boolean isPositive, Material material, LociManager lociManager)
    {

        this.particleSystemObject = new GameObject();

        if (isPositive)
            this.particleSystemObject.name = "PositiveParticleSystem";
        else
            this.particleSystemObject.name = "NegativeParticleSystem";

        this.particleSystemObject.transform.parent = lociManager.gameObject.transform;
        this.particleSystemObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        this.particleSystemObject.transform.Rotate(new Vector3(-90.0f, 0.0f, 0.0f));
        this.particleSystem = particleSystemObject.AddComponent<ParticleSystem>();
        this.particleSystemObject.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

        this.particleSystem.Stop();
        var main = particleSystem.main;
        main.duration = 0f;
        main.startLifetime = 1f;
        main.startSpeed = 5f;
        main.startSize = 0.4f;
        main.playOnAwake = false;
        main.loop = false;

        var renderer = particleSystem.GetComponent<ParticleSystemRenderer>().material = material;

        var emission = particleSystem.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(
            new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0.0f, 1700),
            });

        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.donutRadius = 0.2f;


        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        if (isPositive)
        {
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.cyan, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }
        else
        {
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.red, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var forceOverLifetime = particleSystem.forceOverLifetime;
        forceOverLifetime.enabled = true;
        float zForce = isPositive ? 10f : -10f;
        forceOverLifetime.z = new ParticleSystem.MinMaxCurve(zForce);
    }




    /// <summary>
    /// Get the particle system object
    /// </summary>
    /// <returns></returns>
    public GameObject GetParticleSystemObject()
    {
        return particleSystemObject;
    }




    /// <summary>
    /// Get the particle system
    /// </summary>
    /// <returns></returns>
    public ParticleSystem GetParticleSystem()
    {
        return particleSystem;
    }



    /// <summary>
    /// Play the particle system at the given position
    /// </summary>
    /// <param name="position"></param>
    public void Play(Vector3 position)
    {
        if (!particleSystem.isStopped)
            particleSystem.Stop();

        particleSystemObject.transform.position = position;

        particleSystem.Play();

        Debug.Log("Particle system played");
    }

}