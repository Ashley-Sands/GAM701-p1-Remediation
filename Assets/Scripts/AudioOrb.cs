using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioOrb : MonoBehaviour
{

	public bool Active => orbManager != null ? orbManager.OrbIsActive(this) : false;

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

	const int samplesPerSecond = 44100;
	const float minSyncTime = 1.5f;	// seconds

	public AudioClip[] clips;
	private AudioSource[] audioSources; // for simplisity we just use 1 AudioSource per clip.
	public AudioSource currentAudioSource => Active ? audioSources[currentID] : null;
	private int currentID = 0;

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

	public void ActivateOrb( int startClipID, AudioSource currentAudioSource )
	{
		currentID = startClipID;

		int currentSample = currentAudioSource.timeSamples;
		double remainingTimeSec = (double)(currentAudioSource.clip.samples - currentSample) / (double)samplesPerSecond;

		// cue the audio to start playing 
		audioSources[currentID].PlayScheduled(AudioSettings.dspTime + remainingTimeSec);

	}

	private void UpdateAudio()
	{
		
		if (!Active) return;

		int currentSample = audioSources[ currentID ].timeSamples;
		double remainingTimeSec = (double)(clips[currentID].samples - currentSample) / (double)samplesPerSecond;

		if ( remainingTimeSec < minSyncTime )
		{
			
			// queue the next audio source.
			currentID++;

			if ( currentID >= clips.Length )
				currentID = 0;

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

	public void OnTriggerEnter(Collider other)
	{

		// if the player enters the orb we need to activate the nex audio source, 
		// as long as it is not currently active.

		if ( !other.CompareTag("Player") || Active ) return;
		
		orbManager.TriggerOrb( this );

	}
}
