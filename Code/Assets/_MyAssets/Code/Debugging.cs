using UnityEngine;

enum SceneDebugging
{
    MAIN,
    SAMPLE
}

public class Debugging : MonoBehaviour
{
    /// <summary>
    /// Boolean used for setting some parameters, useful for debugging on pc
    /// </summary>
#if UNITY_EDITOR
    private static readonly bool onPC = true;
#else
    private static readonly bool onPC = false;
#endif

    /// <summary>
    /// This is the scene debugging, it's useful for saying if we are on main scene (the true application of loci)
    /// or on sample scene (the scene used for testing anchors).
    /// </summary>
    private static readonly SceneDebugging sceneDebugging = SceneDebugging.MAIN;

    /// <summary>
    /// This is the boolean used for allowing the delete of the memory on files for loci scene
    /// </summary>
    private static readonly bool allowDeleteMemoryLoci = true;



    /// <summary>
    /// This method returns if we are on pc
    /// </summary>
    /// <returns></returns>
    public static bool IsOnPC()
    {
        return onPC;
    }



    /// <summary>
    /// Says if we are on the main scene
    /// </summary>
    /// <returns></returns>
    public static bool IsMainScene()
    {

        if (sceneDebugging == SceneDebugging.MAIN)
        {
            return true;
        }
        else
        {
            return false;
        }
    }



    /// <summary>
    /// Says if we can delete the memory of loci application
    /// </summary>
    /// <returns></returns>
    public static bool AllowDeleteMemoryLoci()
    {
        return allowDeleteMemoryLoci;
    }
}

