using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ModelController : MonoBehaviour
{
    const float EARTH_RADIUS = 6371001f; // ave radius in metres
    const float FT_TO_M = 0.3048f; // for converting feet to meters
    const float M_TO_YD = 1.09361f; // for converting meters to yards

    float periscopeHeight;
    float distAtHorizon;
    float maxHeightAboveSea;
    float subHorizonDistScale;

    float xPosMouse;
    float yRotObj;
    float yPosDistOffSet;
    float yPosHeightOffSet;
    bool isRotating;

    Vector3 modelLP;
    Vector3 ImageWaterLP;

    List<GameObject> modelList = new List<GameObject>();

    [SerializeField]
    Text heightText;
    [SerializeField]
    Text distanceText;
    [SerializeField]
    Text rotationText;

    [SerializeField]
    Image ImageWaterFG;
    [SerializeField]
    Canvas canvasUI;
    [SerializeField]
    GameObject listPanel;

    [SerializeField]
    Slider heightSlider;
    [SerializeField]
    Slider distSlider;

    [SerializeField]
    GameObject buttonPrefab;

    // Use this for initialization
    void Start ()
    {
        RectTransform canvasRT = canvasUI.GetComponent<RectTransform>();

        // Expand distance slider to fill rest of the panel
        RectTransform distSliderRT = distSlider.GetComponent<RectTransform>();
        float width = (distSlider.transform.position.y - distanceText.transform.position.y) / canvasRT.localScale.y;
        distSliderRT.sizeDelta = new Vector2(width, distSliderRT.sizeDelta.y);

        // Bounds of the model
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            bounds.Encapsulate(renderer.bounds);
        }

        // Setting slider values
        maxHeightAboveSea = (bounds.size.y / 2f + bounds.center.y - transform.position.y);
        heightSlider.maxValue = 0;
        heightSlider.minValue = -0.1f * maxHeightAboveSea;
        heightSlider.value = 0;
        distSlider.maxValue = maxHeightAboveSea;
        distSlider.minValue = -0.1f * canvasRT.rect.height * canvasRT.localScale.y;
        distSlider.value = 0;

        // To scale when distance of vessel is less than distance from horizon
        float maxDistHorToPer = 3000f;
        float maxDistAtMin = 2 * Mathf.PI * EARTH_RADIUS *
            Mathf.Acos(EARTH_RADIUS / (EARTH_RADIUS - distSlider.minValue / transform.localScale.y * FT_TO_M)) *
            Mathf.Rad2Deg / 360;
        subHorizonDistScale = maxDistHorToPer / maxDistAtMin;

        transform.position = new Vector3(
            50f * canvasRT.localScale.x, -0.2f * canvasRT.rect.height * canvasRT.localScale.y, canvasRT.position.z + 1000f * canvasRT.localScale.z);

        isRotating = false;
        updateRotText(transform.eulerAngles.y);

        modelLP = transform.localPosition;
        yPosDistOffSet = 0f;
        yPosHeightOffSet = 0f;
        ImageWaterLP = ImageWaterFG.transform.localPosition;

        periscopeHeight = 1f;
        distAtHorizon = 2 * Mathf.PI * EARTH_RADIUS *
            Mathf.Acos(EARTH_RADIUS / (EARTH_RADIUS + periscopeHeight)) *
            Mathf.Rad2Deg / 360;

        distanceText.text = ((int)(distAtHorizon * M_TO_YD + 0.5f)).ToString() + " yds";

        //initialiseModels();
        initialiseModelsInternal();
        initialiseSelectionList();

        float heightAboveSeaLevel = (int)(maxHeightAboveSea / transform.localScale.y * 10f + 0.5f) / 10f; // In feet
        heightText.text = heightAboveSeaLevel.ToString() + " ft";
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Original coordinates of click/touch
            xPosMouse = Input.mousePosition.x;
            yRotObj = transform.eulerAngles.y;

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Camera.main.farClipPlane))
            {
                if (hit.transform.tag == "display")
                {
                    xPosMouse = Input.mousePosition.x;
                    yRotObj = transform.eulerAngles.y;
                    isRotating = true;
                }
            }
        }

        // Checks if left click or touch is held down on display area
        if (Input.GetMouseButton(0) && isRotating)
        {
            transform.eulerAngles = new Vector3(
                transform.eulerAngles.x,
                (xPosMouse - Input.mousePosition.x) / 4 + yRotObj,
                transform.eulerAngles.z);

            updateRotText(transform.eulerAngles.y);
        }
        else
        {
            isRotating = false;
        }
    }

    /// <summary>
    /// Loads the models from the resource folder and creates gameobjects for each model
    /// </summary>
    private void initialiseModelsInternal()
    {
        GameObject[] models = Resources.LoadAll<GameObject>("Package/Models");
        Texture2D[] textures = Resources.LoadAll<Texture2D>("Package/ModelTextures");

        for (int i = 0; i < models.Length; i++)
        {
            GameObject model = Instantiate(models[i], transform);
            model.name = models[i].name;

            for (int j = 0; textures != null && j < textures.Length; j++)
            {
                if (model.name == textures[j].name)
                {
                    MeshRenderer[] meshRenderers = model.GetComponentsInChildren<MeshRenderer>();
                    for (int k = 0; k < meshRenderers.Length; k++)
                    {
                        meshRenderers[k].material.mainTexture = textures[j];
                    }
                }
            }

            modelList.Add(model);
            model.SetActive(false);
        }

        if (modelList.Count > 0)
        {
            selectVessel(modelList[0].name);
        }
    }

    /// <summary>
    /// Initialises models read from OBJ files in the persistent data path
    /// </summary>
    /*
    private void initialiseModels()
    {
        // Read object file through url (WWW object)
        {
            string[] objFilenames = Directory.GetFiles(Application.persistentDataPath, "*.obj");

            for (int i = 0; i < objFilenames.Length; i++)
            {
                WWW www = WWW.LoadFromCacheOrDownload("file:///" + objFilenames[i], 1);

                while (!www.isDone)
                {
                    // TODO: use coroutine for loading screen
                }

                AssetBundle bundle = www.assetBundle;
                AssetBundleRequest request = bundle.LoadAssetAsync<GameObject>("bugatti");

                GameObject go = request.asset as GameObject;
                Instantiate(go);
                go.transform.SetParent(transform);
            }
        }

        //    StreamReader objSR = File.OpenText(objFilenames[i]);

        //    string objStr = objSR.ReadLine();
        //    Debug.Log("first line: " + objStr);
        //    while (!objSR.EndOfStream)
        //    {
        //        string str = objSR.ReadLine();
        //        Debug.Log("line: <" + str + ">");
        //        objStr += '\n' + str;

        //        if (objSR.EndOfStream)
        //        {
        //            Debug.Log("end of stream");
        //        }
        //    }

        //    objSR.Close();
        //    Debug.Log(objStr);

        //    StreamReader matSR = File.OpenText(objFilenames[i].Replace(".obj", ".mtl"));
        //    string matStr = matSR.ReadToEnd();
        //    matSR.Close();

        //    //string objStr = File.ReadAllText(objFilenames[i]);
        //    //string matStr = File.ReadAllText(objFilenames[i].Replace(".obj", ".mtl"));
        //    //Debug.Log(objStr);

        //    int index = objFilenames[i].LastIndexOf('/') > objFilenames[i].LastIndexOf('\\') ? objFilenames[i].LastIndexOf('/') : objFilenames[i].LastIndexOf('\\');
        //    int length = objFilenames[i].Substring(index).Length;
        //    string name = objFilenames[i].Substring(index + 1, length - 5);
        //    //Debug.Log("name: " + name);

        //    List<Texture2D> textures = new List<Texture2D>();
        //    string[] texFilenames = Directory.GetFiles(Application.persistentDataPath, name + "_*.png");

        //    for (int j = 0; j < texFilenames.Length; j++)
        //    {
        //        byte[] fileData = File.ReadAllBytes(texFilenames[j]);
        //        Texture2D tex = new Texture2D(2, 2);
        //        tex.LoadImage(fileData);
        //        textures.Add(tex);
        //    }

        //    GameObject model = ObjImporter.Import(objStr, matStr, textures.ToArray());
        //    model.name = name;
        //    model.transform.SetParent(transform, false);
        //    model.SetActive(false);

        //    modelList.Add(model);
        //}

        //if (modelList.Count > 0)
        //{
        //    selectVessel(modelList[0].name);
        //}
    }
    */

    /// <summary>
    /// Initialises models read from OBJ files in the persistent data path
    /// </summary>
    private void initialiseModels()
    {
        string[] objFilenames = Directory.GetFiles(Application.persistentDataPath, "*_obj.*");
        for (int i = 0; i < objFilenames.Length; i++)
        {
            string objStr = File.ReadAllText(objFilenames[i]);
            string matStr = File.ReadAllText(objFilenames[i].Replace("_obj.", "_mtl."));

            int index = objFilenames[i].LastIndexOf('/') > objFilenames[i].LastIndexOf('\\') ? objFilenames[i].LastIndexOf('/') : objFilenames[i].LastIndexOf('\\');
            int length = objFilenames[i].Substring(index).Length;
            string name = objFilenames[i].Substring(index + 1, length - 9);

            List<Texture2D> textures = new List<Texture2D>();
            string[] texFilenames = Directory.GetFiles(Application.persistentDataPath, name + "_tex*.png");

            for (int j = 0; j < texFilenames.Length; j++)
            {
                byte[] fileData = File.ReadAllBytes(texFilenames[j]);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(fileData);
                textures.Add(tex);
            }

            GameObject model = ObjImporter.Import(objStr, matStr, textures.ToArray());
            model.name = name;
            model.transform.SetParent(transform, false);
            model.SetActive(false);

            modelList.Add(model);
        }

        if (modelList.Count > 0)
        {
            selectVessel(modelList[0].name);
        }
    }

    /// <summary>
    /// Adds a row for each model loaded to the vessel select list
    /// </summary>
    private void initialiseSelectionList()
    {
        canvasUI.GetComponent<UIController>().addVesselSelectRow(modelList);
    }

    /// <summary>
    /// Changes the model displayed to the selected vessel.
    /// Updates information associated to the selected vessel.
    /// Scales the model of the selected vessel to fit display area
    /// </summary>
    /// <param name="vesselName"> Name of the vessel being selected </param>
    public void selectVessel(string vesselName)
    {
        for(int i = 0; i < modelList.Count; i++)
        {
            if (vesselName == modelList[i].name)
            {
                modelList[i].SetActive(true);
            }
            else
            {
                modelList[i].SetActive(false);
            }
        }

        scaleModel();
        canvasUI.GetComponent<UIController>().updateInfo(vesselName);
    }

    /// <summary>
    /// Rotates the model clockwise looking down on the y axis
    /// </summary>
    public void rotateRight()
    {
        transform.eulerAngles = new Vector3(
                transform.eulerAngles.x,
                transform.eulerAngles.y + 1.0f,
                transform.eulerAngles.z);

        updateRotText(transform.eulerAngles.y);
    }

    /// <summary>
    /// Rotates the model anti-clockwise looking down on the y axis
    /// </summary>
    public void rotateLeft()
    {
        transform.eulerAngles = new Vector3(
                transform.eulerAngles.x,
                transform.eulerAngles.y - 1.0f,
                transform.eulerAngles.z);

        updateRotText(transform.eulerAngles.y);
    }

    /// <summary>
    /// Updates the rotation text based on the given angle
    /// </summary>
    /// <param name="angle"> Angle to update text to </param>
    private void updateRotText(float angle)
    {
        int rot = (int)(angle + 0.5f);

        if (rot == 0 || rot == 360)
        {
            rotationText.text = "RIGHT ASTERN";
            rotationText.color = new Color(0, 0, 0);
        }
        else if (rot == 180)
        {
            rotationText.text = "RIGHT AHEAD";
            rotationText.color = new Color(0, 0, 0);
        }
        else if (rot < 180)
        {
            rotationText.text = (180 - rot).ToString() + " STARBOARD";
            rotationText.color = new Color(0, 128, 0);
        }
        else
        {
            rotationText.text = (rot - 180).ToString() + " PORT";
            rotationText.color = new Color(128, 0, 0);
        }
    }

    /// <summary>
    /// Adjusts draught of the vessel (height above the water) based on the slider value (heightSlider)
    /// </summary>
    public void adjustHeight()
    {
        transform.localPosition = new Vector3(
        modelLP.x,
        modelLP.y + heightSlider.value + yPosDistOffSet,
        modelLP.z);

        yPosHeightOffSet = heightSlider.value;


        float heightAboveSeaLevel = (int)((maxHeightAboveSea + heightSlider.value) /
            transform.localScale.y * 10f + 0.5f) / 10f; // In feet
        heightText.text = heightAboveSeaLevel.ToString() + " ft";
    }

    /// <summary>
    /// Adjusts the vertical model position and displays the estimated distance based on the horizon and slider value (distSlider)
    /// </summary>
    public void adjustDistance()
    {
        // Behind horizon
        if (distSlider.value > 0)
        {
            ImageWaterFG.transform.localPosition = ImageWaterLP;

            transform.localPosition = new Vector3(
            modelLP.x,
            modelLP.y - distSlider.value + yPosHeightOffSet,
            modelLP.z);

            yPosDistOffSet = -distSlider.value;

            float heightUnderHorizon = distSlider.value / transform.localScale.y * FT_TO_M; // Converted to meters
            int dist = (int) ((distAtHorizon + 2f * Mathf.PI * EARTH_RADIUS *
                Mathf.Acos(EARTH_RADIUS / (EARTH_RADIUS + heightUnderHorizon)) * Mathf.Rad2Deg / 360) *
                M_TO_YD + 0.5f);
            distanceText.text = dist.ToString() + " yds";
        }
        // In front of horizon
        else
        {
            transform.localPosition = new Vector3(
            modelLP.x,
            modelLP.y + distSlider.value + yPosHeightOffSet,
            modelLP.z);

            ImageWaterFG.transform.localPosition = new Vector3(
                ImageWaterLP.x,
                ImageWaterLP.y + distSlider.value / canvasUI.transform.localScale.y,
                ImageWaterLP.z);

            yPosDistOffSet = distSlider.value;

            float heightUnderHorizon = distSlider.value / transform.localScale.y * FT_TO_M; // Converted to meters
            float distFromHorizon = 2f * Mathf.PI * EARTH_RADIUS *
                Mathf.Acos((EARTH_RADIUS + heightUnderHorizon) / EARTH_RADIUS) * Mathf.Rad2Deg / 360;

            int dist = (int)((distAtHorizon - distFromHorizon * subHorizonDistScale) * M_TO_YD + 0.5f);
            distanceText.text = dist.ToString() + " yds";
        }
    }

    /// <summary>
    /// Scales the model to fit the display area
    /// </summary>
    private void scaleModel()
    {
        transform.localScale = new Vector3(1, 1, 1);
        transform.eulerAngles = new Vector3(0, 90, 0);

        // Bounds of the model
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer.gameObject.activeSelf)
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        RectTransform canvasRT = canvasUI.GetComponent<RectTransform>();

        // Scaling model to fit display area
        float maxSize = bounds.size.x > bounds.size.z ? bounds.size.x : bounds.size.z;
        transform.localScale *= ((canvasRT.rect.width - 300f) / maxSize) * canvasRT.localScale.x;

        // Setting slider values
        maxHeightAboveSea = (bounds.size.y / 2f + bounds.center.y - transform.position.y) * transform.localScale.y;

        heightSlider.minValue = -0.1f * maxHeightAboveSea;
        distSlider.maxValue = maxHeightAboveSea;

        // To scale when distance of vessel is less than distance from horizon
        float maxDistHorToPer = 3000f;
        float maxDistAtMin = 2 * Mathf.PI * EARTH_RADIUS *
            Mathf.Acos(EARTH_RADIUS / (EARTH_RADIUS - distSlider.minValue / transform.localScale.y * FT_TO_M)) *
            Mathf.Rad2Deg / 360;
        subHorizonDistScale = maxDistHorToPer / maxDistAtMin;
    }
}
