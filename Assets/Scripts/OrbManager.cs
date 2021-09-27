using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbManager : MonoBehaviour
{
    private AudioOrb activeOrb;

    public void TriggerOrb( AudioOrb orb )
    {
        // Only trigger the orb if it is not active.
        if ( orb != activeOrb )
        {
            // Sync this current id with next id on the new orb.

            
            activeOrb = orb;
		}

	}

    public bool OrbIsActive( AudioOrb orb )
    {
        return orb == activeOrb;
	}


}
