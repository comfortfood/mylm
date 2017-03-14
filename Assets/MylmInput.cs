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
	// Mic selected
	private bool micSelected = false;

	// config
	private int notches = 5;
	private int inputTypes = 10;
	private int holdForToScore = 30;
	private float closeEnough = 0.2f;
	private int rainbowRepeats = 4;
	private int rainbowRepeatsPitch = 4;
	private int rainbowRepeatsNote = 1;
	// Millisecons needed to detect tone
	public int pitchTimeInterval = 100;
	// RMS value for 0 dB
	private float refValue = 0.1f;
	// Min volume in bd needed to start detection
	public float minVolumeDB = -17f;
	// every 8 notes should produce the same 'note' input
	public float wrapEvery = 8f;

	// state
	private float note = 0;
	private int score = 0;
	private int target = -1;
	private int chosenInput = -1;
	private int heldFor = 0;
	private float[] rawInputs;
	// Sound samples data
	float[] ssData;

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
			
//		selectedDevice = Microphone.devices [0].ToString ();
//		micSelected = true;
//		GetMicCaps ();
//
//		listening = true;
//		setUpMic ();
//
//		// Estimates bufer len, based on pitchTimeInterval value
//
//		int bufferLen = (int)Mathf.Round (AudioSettings.outputSampleRate * pitchTimeInterval / 1000f);
//		Debug.Log ("Buffer len: " + bufferLen);
//		ssData = new float[bufferLen];
//		}
	}

	float[] getRawInputs (float[] rawInputs)
	{
//		if (Input.touchCount > 0 && Input.GetTouch (0).phase == TouchPhase.Began) {
//		}

		Quaternion q1 = Input.gyro.attitude;

		GameObject g = new GameObject ();
		Transform t = g.transform;
		t.localRotation = new Quaternion (q1 [2], q1 [0], q1 [3], q1 [1]);

		float yaw = t.localEulerAngles [1];
		t.Rotate (0f, yaw, 0f);

		float pitch = t.localEulerAngles [0];
		t.Rotate (pitch, 0f, 0f);

		float roll = t.localEulerAngles [2];
		t.Rotate (0f, 0f, roll * -1);

		Destroy (g);

		if (pitch > 180) {
			pitch -= 360;
		}

		yaw /= 57.2958f * Mathf.PI * 2;
		pitch = (pitch / -57.2958f + Mathf.PI / 2) / Mathf.PI;
		roll /= 57.2958f * Mathf.PI * 2;

		if (listening) {

			GetComponent<AudioSource> ().GetOutputData (ssData, 0);

			float sum = 0f;

			for (int i = 0; i < ssData.Length; i++) {
				sum += ssData [i] * ssData [i];
			}

			float rmsValue = Mathf.Sqrt (sum / ssData.Length);
			float dbValue = 20f * Mathf.Log10 (rmsValue / refValue);

			if (dbValue >= minVolumeDB) {

				pitchDetector.DetectPitch (ssData);

				float midi = pitchDetector.lastMidiNotePrecise ();

				if (midi > 0) {

					/// EXAMPLE ///
					// (50.5 - 8 * Fti(50.5 / 8)) / 8 =
					// (50.5 - 8 * Fti(6.3125)) / 8 =
					// (50.5 - 8 * 6) / 8 =
					// (50.5 - 48) / 8 =
					// 2.5 / 8

					note = (midi - wrapEvery * Mathf.FloorToInt (midi / wrapEvery)) / wrapEvery;
				}
			}
		}

		float pitchPlus;
		float pitchMinus;
		float rollPlus;
		float rollMinus;
		float yawPlus;
		float yawMinus;

		if (rawInputs == null) {

			pitchPlus = pitch;
			pitchMinus = pitch;
			rollPlus = roll;
			rollMinus = roll;
			yawPlus = yaw;
			yawMinus = yaw;

		} else {

			// pitch change
			float pc = Mathf.Abs (pitch - rawInputs [0]);

			if (pc > 0.003 && pc < 0.997) {
				
				if ((pc < 0.5 && pitch > rawInputs [0]) ||
				    (pc >= 0.5 && pitch <= rawInputs [0])) {
					
					pitchPlus = (rawInputs [4] + pc) % 1.0f;
					pitchMinus = rawInputs [5];

				} else {
					
					pitchPlus = rawInputs [4];
					pitchMinus = (rawInputs [5] + 1 - pc) % 1.0f;
				}
			} else {
				
				pitchPlus = rawInputs [4];
				pitchMinus = rawInputs [5];
			}

			// roll change
			float rc = Mathf.Abs (roll - rawInputs [1]);

			if (rc > 0.003 && rc < 0.997) {
				
				if ((rc < 0.5 && roll > rawInputs [1]) ||
				    (rc >= 0.5 && roll <= rawInputs [1])) {

					rollPlus = (rawInputs [6] + rc) % 1.0f;
					rollMinus = rawInputs [7];

				} else {
					
					rollPlus = rawInputs [6];
					rollMinus = (rawInputs [7] + 1 - rc) % 1.0f;
				}
			} else {
				
				rollPlus = rawInputs [6];
				rollMinus = rawInputs [7];
			}

			// yaw change
			float yc = Mathf.Abs (yaw - rawInputs [2]);

			if (yc > 0.003 && yc < 0.997) {
				
				if ((yc < 0.5 && yaw > rawInputs [2]) ||
				    (yc >= 0.5 && yaw <= rawInputs [2])) {

					yawPlus = (rawInputs [8] + yc) % 1.0f;
					yawMinus = rawInputs [9];

				} else {
					
					yawPlus = rawInputs [8];
					yawMinus = (rawInputs [9] + 1 - yc) % 1.0f;
				}
			} else {
				
				yawPlus = rawInputs [8];
				yawMinus = rawInputs [9];
			}
		}

		return new float[] { pitch, roll, yaw, note, 
			pitchPlus, pitchMinus, 
			rollPlus, rollMinus, 
			yawPlus, yawMinus
		};
	}

	void Update ()
	{
		rawInputs = getRawInputs (rawInputs);

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
				
			val = rawInputs [chosenInput] * tmpRr;
			if (val >= tmpRr) {
				val = 0;
			}
			val = val % 1f;
			val = val * notches;

			GameObject cBg0 = GameObject.Find ("/cell-bg-0/TextMeshPro");
			TextMeshPro cBgTmp0 = cBg0.GetComponent (typeof(TextMeshPro)) as TextMeshPro;

			GameObject cCore0 = GameObject.Find ("/cell-core-0/TextMeshPro");
			TextMeshPro cCoreTmp0 = cCore0.GetComponent (typeof(TextMeshPro)) as TextMeshPro;

			GameObject cBg1 = GameObject.Find ("/cell-bg-1/TextMeshPro");
			TextMeshPro cBgTmp1 = cBg1.GetComponent (typeof(TextMeshPro)) as TextMeshPro;

			GameObject cCore1 = GameObject.Find ("/cell-core-1/TextMeshPro");
			TextMeshPro cCoreTmp1 = cCore1.GetComponent (typeof(TextMeshPro)) as TextMeshPro;

			GameObject cBg2 = GameObject.Find ("/cell-bg-2/TextMeshPro");
			TextMeshPro cBgTmp2 = cBg2.GetComponent (typeof(TextMeshPro)) as TextMeshPro;

			GameObject cCore2 = GameObject.Find ("/cell-core-2/TextMeshPro");
			TextMeshPro cCoreTmp2 = cCore2.GetComponent (typeof(TextMeshPro)) as TextMeshPro;

			if (Mathf.Abs (val - target) <= closeEnough ||
			    (target == 0 && Mathf.Abs (val - notches) <= closeEnough)) {
				
				heldFor++;

				cBgTmp0.text = "Same";
				cCoreTmp0.text = "Same";
				cBgTmp1.text = "Same";
				cCoreTmp1.text = "Same";
				cBgTmp2.text = "Same";
				cCoreTmp2.text = "Same";

			} else {
				heldFor = 0;

				cBgTmp0.text = "Yours";
				cCoreTmp0.text = "Mine";
				cBgTmp1.text = "Yours";
				cCoreTmp1.text = "Mine";
				cBgTmp2.text = "Yours";
				cCoreTmp2.text = "Mine";
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

			GameObject cellCore0 = GameObject.Find ("/cell-core-0");
			cellCore0.GetComponent<Renderer> ().material.color = Color.HSVToRGB ((float)target / notches, 1f, 1f);

			GameObject cellCore1 = GameObject.Find ("/cell-core-1");
			cellCore1.GetComponent<Renderer> ().material.color = Color.HSVToRGB ((float)target / notches, 1f, 1f);

			GameObject cellCore2 = GameObject.Find ("/cell-core-2");
			cellCore2.GetComponent<Renderer> ().material.color = Color.HSVToRGB ((float)target / notches, 1f, 1f);

			chosenInput = Mathf.FloorToInt (Random.value * inputTypes);

			//NO NOTES FOR NOW
			while (chosenInput == 3) {
				chosenInput = Mathf.FloorToInt (Random.value * inputTypes);
			}

			if (chosenInput == inputTypes) {
				chosenInput = 0;
			}
		}

		GameObject cellBg0 = GameObject.Find ("/cell-bg-0");
		cellBg0.GetComponent<Renderer> ().material.color = Color.HSVToRGB (val / notches, 1f, 1f);

		GameObject cellBg1 = GameObject.Find ("/cell-bg-1");
		cellBg1.GetComponent<Renderer> ().material.color = Color.HSVToRGB (val / notches, 1f, 1f);

		GameObject cellBg2 = GameObject.Find ("/cell-bg-2");
		cellBg2.GetComponent<Renderer> ().material.color = Color.HSVToRGB (val / notches, 1f, 1f);

		outputTmp.text = 
			"";
//		"p:" + rawInputs [0].ToString ("F3") + "\n" +
//		"r:" + rawInputs [1].ToString ("F3") + "\n" +
//		"y:" + rawInputs [2].ToString ("F3") + "\n" +
//		"m:" + rawInputs [3].ToString ("F3") + "\n" +
//		"v:" + val.ToString ("F3") + "\n" +
//		"t:" + target + "\n" +
//		"ci:" + chosenInput + "\n" +
//		"s:" + score;
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