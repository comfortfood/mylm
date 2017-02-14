using UnityEngine;
using System.Collections;
using TMPro;
using PitchDetector;

public class MylmInput : MonoBehaviour
{
	private TextMeshPro outputTmp;

	private Detector pitchDetector;

	private int minFreq, maxFreq;
	// Max and min frequencies window
	public string selectedDevice { get; private set; }

	private bool micSelected = false;
	// Mic selected

	float[] data;
	// Sound samples data
	public int pitchTimeInterval = 100;
	// Millisecons needed to detect tone
	private float refValue = 0.1f;
	// RMS value for 0 dB
	public float minVolumeDB = -17f;
	// Min volume in bd needed to start detection
	public float wrapEvery = 8f;
	// every 8 notes should produce the same 'note' input

	// config
	private int notches = 5;
	private int inputTypes = 4;
	private int holdForToScore = 30;
	private float closeEnough = 0.2f;
	private int rainbowRepeats = 4;
	private int rainbowRepeatsPitch = 4;
	private int rainbowRepeatsNote = 1;

	// state
	private float note = 0;
	private int score = 0;
	private int target = -1;
	private int chosenInput = -1;
	private int heldFor = 0;
	private int inputOrder = 6;

	private bool listening = false;
	// Flag for listening

	void Awake ()
	{
		pitchDetector = new Detector ();
		pitchDetector.setSampleRate (AudioSettings.outputSampleRate);
	}

	IEnumerator Start ()
	{
		GameObject output = GameObject.Find ("/output-bg/output-tmp");
		outputTmp = output.GetComponent (typeof(TextMeshPro)) as TextMeshPro;

		yield return Application.RequestUserAuthorization (UserAuthorization.Microphone);

//		if (Application.HasUserAuthorization(UserAuthorization.Microphone)) {
			
		selectedDevice = Microphone.devices [0].ToString ();
		micSelected = true;
		GetMicCaps ();

		listening = true;
		setUpMic ();

		// Estimates bufer len, based on pitchTimeInterval value

		int bufferLen = (int)Mathf.Round (AudioSettings.outputSampleRate * pitchTimeInterval / 1000f);
		Debug.Log ("Buffer len: " + bufferLen);
		data = new float[bufferLen];
//		}
	}

	float[] getRawInputs ()
	{
		Quaternion q1 = Input.gyro.attitude;

		Quaternion q;

		switch (inputOrder) {
		case 0:
			q = new Quaternion (q1 [0], q1 [1], q1 [2], q1 [3]);
			break;
		case 1:
			q = new Quaternion (q1 [0], q1 [1], q1 [3], q1 [2]);
			break;
		case 2:
			q = new Quaternion (q1 [0], q1 [2], q1 [1], q1 [3]);
			break;
		case 3:
			q = new Quaternion (q1 [0], q1 [2], q1 [3], q1 [1]);
			break;
		case 4:
			q = new Quaternion (q1 [0], q1 [3], q1 [1], q1 [2]);
			break;
		case 5:
			q = new Quaternion (q1 [0], q1 [3], q1 [2], q1 [1]);
			break;
		case 6:
			q = new Quaternion (q1 [1], q1 [0], q1 [2], q1 [3]);
			break;
		case 7:
			q = new Quaternion (q1 [1], q1 [0], q1 [3], q1 [2]);
			break;
		case 8:
			q = new Quaternion (q1 [1], q1 [2], q1 [0], q1 [3]);
			break;
		case 9:
			q = new Quaternion (q1 [1], q1 [2], q1 [3], q1 [0]);
			break;
		case 10:
			q = new Quaternion (q1 [1], q1 [3], q1 [0], q1 [2]);
			break;
		case 11:
			q = new Quaternion (q1 [1], q1 [3], q1 [2], q1 [0]);
			break;
		case 12:
			q = new Quaternion (q1 [2], q1 [0], q1 [1], q1 [3]);
			break;
		case 13:
			q = new Quaternion (q1 [2], q1 [0], q1 [3], q1 [1]);
			break;
		case 14:
			q = new Quaternion (q1 [2], q1 [1], q1 [0], q1 [3]);
			break;
		case 15:
			q = new Quaternion (q1 [2], q1 [1], q1 [3], q1 [0]);
			break;
		case 16:
			q = new Quaternion (q1 [2], q1 [3], q1 [0], q1 [1]);
			break;
		case 17:
			q = new Quaternion (q1 [2], q1 [3], q1 [1], q1 [0]);
			break;
		case 18:
			q = new Quaternion (q1 [3], q1 [0], q1 [1], q1 [2]);
			break;
		case 19:
			q = new Quaternion (q1 [3], q1 [0], q1 [2], q1 [1]);
			break;
		case 20:
			q = new Quaternion (q1 [3], q1 [1], q1 [0], q1 [2]);
			break;
		case 21:
			q = new Quaternion (q1 [3], q1 [1], q1 [2], q1 [0]);
			break;
		case 22:
			q = new Quaternion (q1 [3], q1 [2], q1 [0], q1 [1]);
			break;
		default:
			q = new Quaternion (q1 [3], q1 [2], q1 [1], q1 [0]);
			break;
		}

		float yaw = (float)Mathf.Atan2 (2f * (q [2] * q [3] + q [0] * q [1]), 
			q [0] * q [0] - q [1] * q [1] - q [2] * q [2] + q [3] * q [3]);
		
		float pitch = (float)Mathf.Asin (-2f * (q [1] * q [3] - q [0] * q [2]));

		float roll = (float)Mathf.Atan2 (2f * (q [1] * q [2] + q [0] * q [3]),
			q [0] * q [0] + q [1] * q [1] - q [2] * q [2] - q [3] * q [3]);

		pitch = (pitch + Mathf.PI / 2f) / Mathf.PI;
		roll = (roll + Mathf.PI) / (2f * Mathf.PI);
		yaw = (yaw + Mathf.PI) / (2f * Mathf.PI);

		if (listening) {

			GetComponent<AudioSource> ().GetOutputData (data, 0);

			float sum = 0f;

			for (int i = 0; i < data.Length; i++) {
				sum += data [i] * data [i];
			}

			float rmsValue = Mathf.Sqrt (sum / data.Length);
			float dbValue = 20f * Mathf.Log10 (rmsValue / refValue);

			if (dbValue >= minVolumeDB) {

				pitchDetector.DetectPitch (data);

				float midiFlt = pitchDetector.lastMidiNotePrecise ();

				if (midiFlt > 0) {

					/// EXAMPLE ///
					// (50.5 - 8 * Fti(50.5 / 8)) / 8 =
					// (50.5 - 8 * Fti(6.3125)) / 8 =
					// (50.5 - 8 * 6) / 8 =
					// (50.5 - 48) / 8 =
					// 2.5 / 8

					note = (midiFlt - wrapEvery * Mathf.FloorToInt (midiFlt / wrapEvery)) / wrapEvery;
				}
			}
		}	

		return new float[]{ pitch, roll, yaw, note };
	}

	void Update ()
	{
		float[] ri = getRawInputs ();
		float val = -1;

		if (chosenInput != -1) {

			float tmpRr;
			if (chosenInput == 0) {
				tmpRr = rainbowRepeatsPitch;

			} else if (chosenInput == 3) {
				tmpRr = rainbowRepeatsNote;

			} else {
				tmpRr = rainbowRepeats;
			}
				
			val = ri [chosenInput] * tmpRr;
			if (val >= tmpRr) {
				val = 0;
			}
			val = val % 1f;
			val = val * notches;

			GameObject cBg = GameObject.Find ("/cell-bg/TextMeshPro");
			TextMeshPro cBgTmp = cBg.GetComponent (typeof(TextMeshPro)) as TextMeshPro;

			GameObject cCore = GameObject.Find ("/cell-core/TextMeshPro");
			TextMeshPro cCoreTmp = cCore.GetComponent (typeof(TextMeshPro)) as TextMeshPro;

			if (Mathf.Abs (val - target) <= closeEnough ||
			    (target == 0 && Mathf.Abs (val - notches) <= closeEnough)) {
				
				heldFor++;

				cBgTmp.text = "Same";
				cCoreTmp.text = "Same";

			} else {
				heldFor = 0;

				cBgTmp.text = "Yours";
				cCoreTmp.text = "Mine";
			}
		}

		if (target == -1 || heldFor >= holdForToScore) {

			if (heldFor >= holdForToScore) {
				score++;
			}

			heldFor = 0;

			target = Mathf.FloorToInt (Random.value * notches);
			if (target == notches) {
				target = 0;
			}

			GameObject cellCore = GameObject.Find ("/cell-core");
			cellCore.GetComponent<Renderer> ().material.color = Color.HSVToRGB ((float)target / notches, 1f, 1f);

			chosenInput = Mathf.FloorToInt (Random.value * inputTypes);

			//NO NOTES FOR NOW
			while (chosenInput == 3) {
				chosenInput = Mathf.FloorToInt (Random.value * inputTypes);
			}

			if (chosenInput == inputTypes) {
				chosenInput = 0;
			}
		}
			
		GameObject cellBg = GameObject.Find ("/cell-bg");
		cellBg.GetComponent<Renderer> ().material.color = Color.HSVToRGB (val / notches, 1f, 1f);

		outputTmp.text = 
			"p:" + ri [0].ToString ("F3") + "\n" +
		"r:" + ri [1].ToString ("F3") + "\n" +
		"y:" + ri [2].ToString ("F3") + "\n" +
		"m:" + ri [3].ToString ("F3") + "\n" +
		"v:" + val.ToString ("F3") + "\n" +
		"hf:" + heldFor + "\n" +
		"t:" + target + "\n" +
		"ci:" + chosenInput + "\n" +
		"io:" + inputOrder + "\n" +
		"s:" + score;
	}

	void setUpMic ()
	{
		// GetComponent<AudioSource>().volume = 0f;

		GetComponent<AudioSource> ().clip = null;
		GetComponent<AudioSource> ().loop = true; // Set the AudioClip to loop
		GetComponent<AudioSource> ().mute = false; // Mute the sound, we don't want the player to hear it

		StartMicrophone ();
	}

	public void GetMicCaps ()
	{
		Microphone.GetDeviceCaps (selectedDevice, out minFreq, out maxFreq); // Gets the frequency of the device

		if ((minFreq + maxFreq) == 0) {
			maxFreq = 44100;
		}
	}

	public void StartMicrophone ()
	{
		Debug.Log ("Setting up mic");

		GetComponent<AudioSource> ().clip = Microphone.Start (selectedDevice, true, 10, maxFreq); // Starts recording

		while (!(Microphone.GetPosition (selectedDevice) > 0)) {
		} // Wait until the recording has started

		GetComponent<AudioSource> ().Play (); // Play the audio source!

		Debug.Log ("Setted");
	}

	public void StopMicrophone ()
	{
		GetComponent<AudioSource> ().Stop (); // Stops the audio
		Microphone.End (selectedDevice); // Stops the recording of the device	
	}
}