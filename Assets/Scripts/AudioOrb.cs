using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioOrb : MonoBehaviour
{

	public bool Active => orbManager != null ? orbManager.OrbIsActive(this) : false;

	public bool playOnStart = false;	// this should only be true on a single orb.
    public Color colour = new Color(0, 0, 1f, 0.25f);

	public float maxFadeDistance = 10;
	public float minAlpha = 0.333f;
	public float maxAlpha = 0.666f;


    public float  paulseLength = 1.5f;
	private float HalfPaulseLength => paulseLength / 2f;
	private float paulseTime   = 0f;

    public float paulseMinSize = 0.75f;
    public float paulseMaxSize = 1.25f;

	public OrbManager orbManager;

	const int samplesPerSecond  = 44100;
	const int loopOverlapLength = 3375;	    // samples
	const float minSyncTime     = 1.5f;	    // seconds

	public AudioClip[] clips;
	private AudioSource[] audioSources; // for simplisity we just use 1 AudioSource per clip.
	public AudioSource currentAudioSource => Active ? audioSources[currentID] : null;
	private int currentID = 0;
	public int loopingID = 0;

	private Material mat;

	private Transform player;

	public void Start()
	{

		// set to the start of the paulse.
		transform.localScale = new Vector3(paulseMinSize, paulseMinSize, paulseMinSize);

		player = GameObject.FindGameObjectWithTag("Player").transform;

		UpdateColour();

		// Spwan the Audio Sources for each audio clip

		audioSources = new AudioSource[clips.Length];

		for ( int i = 0; i < clips.Length; i++ )
		{

			AudioSource newSource = gameObject.AddComponent<AudioSource>();
			newSource.clip = clips[i];
			newSource.loop = false;
			newSource.playOnAwake = false;

			audioSources[i] = newSource;

		}

		if (playOnStart)
			orbManager.TriggerOrb( this );

	}

	public void Update()
	{

		// if 0 <= paulseTime < paulseLength / 2 -> get bigger
		// else -> get smaller

		float newSize = 0;

		if (paulseTime < HalfPaulseLength)
			newSize = Mathf.Lerp(paulseMinSize, paulseMaxSize, paulseTime);
		else
			newSize = Mathf.Lerp(paulseMaxSize, paulseMinSize, paulseTime - HalfPaulseLength);

		transform.localScale = new Vector3( newSize, newSize, newSize );

		UpdateColour();
		UpdateAudio();

		// update time at the end to make sure its correct on the first tick.
		paulseTime += Time.deltaTime;

		if (paulseTime >= paulseLength)
			paulseTime -= paulseLength;

	}

	public void ActivateOrb( AudioOrb currentOrb ) //int startClipID, AudioSource currentAudioSource )
	{

		// if no current audio source is passed in, no source is currently playing
		// therefor we can just start playing the clip.
		if (currentOrb == null)
		{
			audioSources[currentID].Play();
			return;
		}

		AudioSource orbAS = currentOrb.currentAudioSource;
		int orbCID        = currentOrb.currentID;

		//TODO: we need to prevent the posiblity of a clip starting if there is less than the min sync time remaining.

		currentID = orbCID+1 < clips.Length ? orbCID + 1 : loopingID;

		int currentSample       = orbAS.timeSamples;
		double remainingTimeSec = (double)(orbAS.clip.samples - currentSample) / (double)samplesPerSecond;

		double dspStartTime = AudioSettings.dspTime + remainingTimeSec;

		// cue the audio to start playing 
		audioSources[currentID].PlayScheduled( dspStartTime );

		StartCoroutine( TransitionOrbIn( dspStartTime ) );

	}

	private IEnumerator TransitionOrbIn( double dspStart )
	{
		while (dspStart < AudioSettings.dspTime)
			yield return new WaitForEndOfFrame();

		orbManager.ForceLastOrbStop();

	}

	public void TransitionOrbOut( )
	{
		// Trasision any audio clips out so its not so jaring when the genra changes.

		StartCoroutine( TransitionOut() );

	}

	private IEnumerator TransitionOut( )
	{

		AudioSource transitionSource = currentAudioSource;

		float remainingTime = transitionSource.clip.length - transitionSource.time;
		float transitionLength = transitionSource.clip.length * 0.05f;

		// wait for the transition start position.
		while (remainingTime > transitionLength)
		{
			yield return new WaitForEndOfFrame();

			remainingTime = transitionSource.clip.length - transitionSource.time;
			transitionLength = transitionSource.clip.length * 0.05f;

		}

		// do transition
		while (transitionSource.isPlaying )
		{

			yield return new WaitForEndOfFrame();

			remainingTime = transitionSource.clip.length - transitionSource.time;
			transitionLength = transitionSource.clip.length * 0.05f;

			transitionSource.pitch = remainingTime / transitionLength;
			transitionSource.volume = remainingTime / transitionLength;

			print($"{remainingTime} / {transitionLength} = {remainingTime / transitionLength}");

		}

		// now the audio has stop we must put the pitch and vol back to the default value.
		transitionSource.pitch = 1f;
		transitionSource.volume = 1f;

	}

	private void UpdateAudio()
	{
		
		if (!Active) return;

		int currentSample = audioSources[ currentID ].timeSamples;
		//start the next loop aprox 0.05 seconds erly
		//to help prevent any slight glitches between tracks.
		double remainingTimeSec = (double)(clips[currentID].samples - currentSample - loopOverlapLength) / (double)samplesPerSecond;	

		if ( remainingTimeSec < minSyncTime )
		{
			
			// queue the next audio source.
			currentID++;

			if ( currentID >= clips.Length )
				currentID = loopingID;

			audioSources[ currentID ].PlayScheduled( AudioSettings.dspTime + remainingTimeSec);

		}

	}

	private void UpdateColour()
	{
		float currentDistance = Vector3.Distance(transform.position, player.position);

		if ( !Active )
			colour.a = Mathf.Lerp(maxAlpha, minAlpha, currentDistance / maxFadeDistance);
		else
			colour.a = Mathf.Lerp(maxAlpha / 2f, minAlpha / 2f, currentDistance / maxFadeDistance);

		// update the orbs colour.
		mat = GetComponent<Renderer>().material;
		mat.SetColor("_Color", colour);
	}

	public void ForceStop()
	{
		// force stop all audio sources.
		foreach ( AudioSource source in audioSources )
		{
			source.Stop();

			source.time = 0;
			source.pitch = 1f;
			source.volume = 1f;
		}
	}

	public void OnTriggerEnter(Collider other)
	{

		// if the player enters the orb we need to activate the nex audio source, 
		// as long as it is not currently active.

		if ( !other.CompareTag("Player") || Active ) return;
		
		orbManager.TriggerOrb( this );

	}
}
