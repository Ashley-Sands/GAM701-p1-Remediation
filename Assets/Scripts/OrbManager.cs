using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbManager : MonoBehaviour
{
    private AudioOrb activeOrb;
    private AudioOrb privOrb;

    public void TriggerOrb( AudioOrb orb )
    {
        // Only trigger the orb if it is not active.
        if ( orb != activeOrb )
        {
            orb.ActivateOrb(activeOrb);
            if ( activeOrb != null )
                activeOrb.TransitionOrbOut();

            privOrb = activeOrb;
            activeOrb = orb;
		}

	}

    public void ForceLastOrbStop()
    {
        if ( privOrb != null )
            privOrb.ForceStop();
	}

    public bool OrbIsActive( AudioOrb orb )
    {
        return orb == activeOrb;
	}


}
