using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using ZXing;
using System.IO;

/// <summary>
/// Lynn.C
/// 2017-1-4
/// QR code reader with Z xing.
/// </summary>
public class QRCodeReaderWithZXing : MonoBehaviour
{

	public Canvas canvas;
	public RawImage cameraTexture;
	public Image rectImg;
	public Camera uiCamera;
	private float timer = 0;
	public bool isScan = false;

	private WebCamTexture webCameraTexture;
	private BarcodeReader barcodeReader;
	//框选区域的取色数组
	private Color32[] data;
	private Texture2D tex2D;

	private int width, height;
	private RenderTexture rt;
	private Camera qrCamera;

	IEnumerator Start ()
	{
		width = Screen.width;
		height = Screen.height;
		yield return Application.RequestUserAuthorization (UserAuthorization.WebCam | UserAuthorization.Microphone);
		if (Application.platform == RuntimePlatform.WindowsPlayer ||
		    Application.platform == RuntimePlatform.WindowsEditor) {
			cameraTexture.transform.localEulerAngles = new Vector3 (0, 180f, 0);
		} else if (Application.platform == RuntimePlatform.IPhonePlayer) {
			cameraTexture.transform.localEulerAngles = new Vector3 (0, 180f, 270f);
			cameraTexture.rectTransform.sizeDelta = new Vector2 (height, width);
		} else if (Application.platform == RuntimePlatform.Android) {
			cameraTexture.transform.localEulerAngles = new Vector3 (0, 0, 270f);
			cameraTexture.rectTransform.sizeDelta = new Vector2 (height, width);
		}
		barcodeReader = new BarcodeReader ();
		barcodeReader.AutoRotate = true;

		WebCamDevice[] devices = WebCamTexture.devices;
		string devicename = devices [0].name;
		webCameraTexture = new WebCamTexture (devicename, width, height);
		cameraTexture.texture = webCameraTexture;
		webCameraTexture.Play ();

		tex2D = new Texture2D (rectImg.mainTexture.width, rectImg.mainTexture.height, TextureFormat.RGB24, true);
		rt = new RenderTexture (width, height, 0);
		if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
			qrCamera = uiCamera;
		else
			qrCamera = Camera.main;
	}

	void Update ()
	{
		if (webCameraTexture != null) {
			cameraTexture.rectTransform.sizeDelta = GetSizeDelta (cameraTexture.rectTransform.sizeDelta.y, (float)webCameraTexture.width / webCameraTexture.height);
		}
		if (isScan) {
			timer += Time.deltaTime;
			if (timer > 0.5f) {
				StartCoroutine (ScanQRCode ());
				timer = 0;
			}
		}
	}

	/// <summary>
	/// Scans the QR code.
	/// 开始扫描
	/// </summary>
	/// <returns>The QR code.</returns>
	IEnumerator ScanQRCode()
	{
		qrCamera.targetTexture = rt;
		qrCamera.Render ();
		RenderTexture.active = rt;

		Vector3 screenPoint;
		if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
			screenPoint = uiCamera.WorldToScreenPoint (rectImg.rectTransform.position);
		else
			screenPoint = rectImg.rectTransform.position;
		tex2D.ReadPixels (new Rect (screenPoint.x - rectImg.mainTexture.width * canvas.scaleFactor / 2, 
			screenPoint.y - rectImg.mainTexture.height * canvas.scaleFactor / 2,
			rectImg.mainTexture.width * canvas.scaleFactor, 
			rectImg.mainTexture.height * canvas.scaleFactor), 0, 0);
		tex2D.Apply ();
		data = tex2D.GetPixels32 ();
		DecodeQR (tex2D.width, tex2D.height);

		WriteTexture (tex2D);

		qrCamera.targetTexture = null;
		RenderTexture.active = null;

		yield return new WaitForEndOfFrame ();
	}

	void WriteTexture(Texture2D tex2D)
	{
		byte[] bytes = tex2D.EncodeToPNG ();
		//获取系统时间
		System.DateTime now = new System.DateTime();
		now = System.DateTime.Now;
		string filename = string.Format("image{0}{1}{2}{3}.png", now.Day, now.Hour, now.Minute, now.Second);
		Debug.Log (Application.dataPath + "/../" + filename);
		File.WriteAllBytes (Application.dataPath + "/../" + filename, bytes);
	}

	void OnGUI()
	{
//		Vector3 screenPoint = uiCamera.WorldToScreenPoint (rectImg.rectTransform.position);
//		GUI.Box (new Rect (screenPoint.x - rectImg.mainTexture.width * canvas.scaleFactor / 2, 
//			Screen.height - screenPoint.y - rectImg.mainTexture.height * canvas.scaleFactor / 2,
//			rectImg.mainTexture.width * canvas.scaleFactor, 
//			rectImg.mainTexture.height * canvas.scaleFactor), "");
	}

	void DecodeQR(int width, int height)
	{
		var br = barcodeReader.Decode (data, width, height);
		if (br != null) {
			Debug.Log (br.Text);
			isScan = false;
		}
	}

	/// <summary>
	/// Gets the size delta.
	/// 缩放后的sizedelta
	/// </summary>
	/// <returns>The size delta.</returns>
	/// <param name="matchValue">Match value.</param>
	/// <param name="proportion">Proportion.</param>
	Vector2 GetSizeDelta(float matchValue, float proportion)
	{
		Vector2 sizeDelta;
		sizeDelta = new Vector2 (matchValue * proportion, matchValue);
		return sizeDelta;
	}

}
