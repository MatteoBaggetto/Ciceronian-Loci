using UnityEngine;

public class TableButton : MonoBehaviour
{
    /// <summary>
    /// This is the reference to the loci manager
    /// </summary>
    LociManager lociManager;

    /// <summary>
    /// Initialize the button script
    /// </summary>
    /// <param name="lociManager"></param>
    public void Init(LociManager lociManager)
    {
        this.lociManager = lociManager;
    }


    public void ButtonClicked()
    {
        lociManager.TableButtonPressed();
    }
}
