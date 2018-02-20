﻿/// <summary>
/// CodeArtist.mx 2015
/// This is the main class of the project, its in charge of raycasting to a model and place brush prefabs infront of the canvas camera.
/// If you are interested in saving the painted texture you can use the method at the end and should save it to a file.
/// </summary>


using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class ViveTexturePainter : MonoBehaviour {
    public const float MAX_DISTANCE = 1f;

    public SteamVR_TrackedObject rController;

	public GameObject brushContainer; //The cursor that overlaps the model and our container for the brushes painted
	public Camera canvasCam;  //The camera that looks at the model, and the camera that looks at the canvas.
	public Sprite cursorPaint; // Cursor for the differen functions 
	public RenderTexture canvasTexture; // Render Texture that looks at our Base Texture and the painted brushes
	public Material baseMaterial; // The material of our base texture (Were we will save the painted texture)

	Painter_BrushMode mode; //Our painter mode (Paint brushes or decals)
	float brushSize; //The size of our brush calculate based on distance, brushScale, brushDropoff
    public float brushScale = 1; //The scale of our brush
    public float brushDropoff; //The dropoff of our brush
    Color brushColor; //The selected color
	int brushCounter=0,MAX_BRUSH_COUNT=2000; //To avoid having millions of brushes
	bool saving=false; //Flag to check if we are saving the texture
    private IEnumerator coroutine;
    public ParticleSystem ps;
    public string fileName = "CanvasTextureXXX.png";
    //public BrushSize size;
    Texture2D tex;
    private Rect photoRect;

    void Update () {
        //Debug.Log("void Update started");
        brushColor = ColorManager.Instance.color; //Updates our painted color with the selected color
        //Debug.Log("brushColor updated");
        //var main = ps.main;
        //main.startColor = brushColor;
        var device = SteamVR_Controller.Input((int)rController.index);
        //Debug.Log("check controller input");
        if (device.GetTouch(SteamVR_Controller.ButtonMask.Trigger)) {
            ps.Play();
            //Debug.Log("PS PLAY");
            DoAction();
        }
        else {
            ps.Stop();
            //Debug.Log("ps stopped");
        }

        if (device.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad)) {
            Debug.Log("                                        TOUCHPAD");
            //Invoke("SaveTexture", 0.001f);
            SaveTexture();
            StartCoroutine(SaveTextureToFile(tex));
            //StartCoroutine(TakeScreenshot());
        }

        //UpdateBrushCursor ();
    }

	//The main action, instantiates a brush or decal entity at the clicked position on the UV map
	void DoAction(){
        //Debug.Log("DoAction started");
        if (saving)
			return;
		Vector3 uvWorldPosition=Vector3.zero;		
		if(HitTestUVPosition(ref uvWorldPosition)){
			GameObject brushObj;

            //brushColor.a = 1f / Mathf.Exp(brushSize * brushDropoff); // Brushes have alpha to have a merging effect when painted over.
            brushColor.a = 1f / Mathf.Exp(brushSize * brushDropoff); // Brushes have alpha to have a merging effect when painted over.
            Debug.Log("just updated brushColor.a=" + brushColor.a + "  based on:  brushSize=" + brushSize + "  brushDropoff=" + brushDropoff);
   
            brushObj =(GameObject)Instantiate(Resources.Load("TexturePainter-Instances/BrushEntity")); //Paint a brush
			brushObj.GetComponent<SpriteRenderer>().color=brushColor; //Set the brush color
					
			brushObj.transform.parent=brushContainer.transform; //Add the brush to our container to be wiped later
			brushObj.transform.localPosition=uvWorldPosition; //The position of the brush (in the UVMap)
			brushObj.transform.localScale=Vector3.one*brushSize; //The size of the brush
		}
		brushCounter++; //Add to the max brushes
        Debug.Log("brushCounter increased");
        if (brushCounter >= MAX_BRUSH_COUNT) { //If we reach the max brushes available, flatten the texture and clear the brushes
			//saving=true;
			//Invoke("SaveTexture",0.1f);
            SaveTexture();			
		}
	}

	//Returns the position on the texuremap according to a hit in the mesh collider
	bool HitTestUVPosition(ref Vector3 uvWorldPosition){
		RaycastHit hit;
        //Vector3 cursorPos = new Vector3 (Input.mousePosition.x, Input.mousePosition.y, 0.0f);
        //Ray cursorRay=sceneCamera.ScreenPointToRay (cursorPos);

        Ray cursorRay = new Ray(rController.transform.position, -rController.transform.up);
        if (Physics.Raycast(cursorRay,out hit, MAX_DISTANCE)){
            brushSize = ((hit.distance / MAX_DISTANCE) * brushScale) +.003f;
            Debug.Log("just updated brushSize=" + brushSize + "  based on hit.distance=" + hit.distance + "  MAX_DISTANCE=" + MAX_DISTANCE + "  brushScale=" + brushScale);
            MeshCollider meshCollider = hit.collider as MeshCollider;
			if (meshCollider == null || meshCollider.sharedMesh == null)
				return false;			
			Vector2 pixelUV  = new Vector2(hit.textureCoord.x,hit.textureCoord.y);
			uvWorldPosition.x=pixelUV.x-canvasCam.orthographicSize;//To center the UV on X
			uvWorldPosition.y=pixelUV.y-canvasCam.orthographicSize;//To center the UV on Y
			uvWorldPosition.z=0.0f;
			return true;
		}
		else{		
			return false;
		}
	}

    //Sets the base material with a our canvas texture, then removes all our brushes
    void SaveTexture(){
        Debug.Log("                                   void SaveTexture (not to file) called.");
        brushCounter =0;
		System.DateTime date = System.DateTime.Now;
		RenderTexture.active = canvasTexture;
		//Texture2D tex = new Texture2D(canvasTexture.width, canvasTexture.height, TextureFormat.RGB24, false);
        tex = new Texture2D(canvasTexture.width, canvasTexture.height, TextureFormat.RGB24, false);
        tex.ReadPixels (new Rect (0, 0, canvasTexture.width, canvasTexture.height), 0, 0);
		tex.Apply ();
        foreach (Transform child in brushContainer.transform)
        {//Clear brushes
            Destroy(child.gameObject);
            Debug.Log("One brush cleared");
        }
        RenderTexture.active = null;
		baseMaterial.mainTexture =tex;	//Put the painted texture as the base
		
		//StartCoroutine ("SaveTextureToFile"); //Do you want to save the texture? This is your method!
        
        Invoke ("ShowCursor", 0.1f);
	}
	//Show again the user cursor (To avoid saving it to the texture)
	void ShowCursor(){	
		saving = false;
	}

	////////////////// PUBLIC METHODS //////////////////

	public void SetBrushSize(float newBrushSize){ //Sets the size of the cursor brush or decal
		brushSize = newBrushSize;
		//brushCursor.transform.localScale = Vector3.one * brushSize;
	}

    ////////////////// OPTIONAL METHODS //////////////////

//#if !UNITY_WEBPLAYER
        //IEnumerator SaveTextureToFile(Texture2D savedTexture){
        IEnumerator SaveTextureToFile(Texture2D savedTexture){
            Debug.Log("                                               SaveTextureToFile called.");		
            brushCounter = 0;
			string fullPath=System.IO.Directory.GetCurrentDirectory()+"\\UserCanvas\\";
			System.DateTime date = System.DateTime.Now;
			//string fileName = "CanvasTexturexxx.png";
			if (!System.IO.Directory.Exists(fullPath))		
				System.IO.Directory.CreateDirectory(fullPath);
			var bytes = savedTexture.EncodeToPNG();
			System.IO.File.WriteAllBytes(fullPath+fileName, bytes);
			Debug.Log ("<color=orange>Saved Successfully!</color>"+fullPath+fileName);
			yield return null;
		}
    //#endif

    ////////////// SAVE SNAPSHOT -  ////////////////////////////

    private IEnumerator TakeScreenshot()
    {
        Camera cam = canvasCam; //Camera cam = Camera.main;
        Texture2D image = new Texture2D(2048, 2048);  //make public?

        RenderTexture currentRT = RenderTexture.active;

        RenderTexture.active = cam.targetTexture;
        cam.Render();

        yield return new WaitForEndOfFrame();

        photoRect = new Rect(0, 0, 264, 592);
        image.ReadPixels(photoRect, 0, 0);

        //Resize the image. Useful if you don't need a 1:1 screenshot.
        //4 is just used as an example. You could use 10 to resize it
        //to a tenth of the original scale or whatever floats your boat.
        //if (resizePhotos)
        //    TextureScale.Bilinear(image, image.width / 4, image.height / 4);

        image.Apply();
        RenderTexture.active = currentRT;

        //target.renderer.material.mainTexture = image;

        //Save it as PNG, but it could easily be changed to JPG
        byte[] bytes = image.EncodeToPNG();

        string filename = "MyScreenshot.png";    //make public?

        System.IO.File.WriteAllBytes(filename, bytes);
        Debug.Log(string.Format("Took screenshot to: {0}", filename));
    }


}
