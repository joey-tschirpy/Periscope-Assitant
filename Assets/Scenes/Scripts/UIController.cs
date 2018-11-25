using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class UIController : MonoBehaviour
{
    bool hidden = true;
    bool lowLightMode = false;
    bool nightMode = false;
    bool fogMode = false;

    Color normalColor = new Color(1f, 1f, 1f);
    Color lowLightColor = new Color(0.9f, 0f, 0f);

    [SerializeField]
    private GameObject infoDelPrefab;

    [SerializeField]
    GameObject infoPanel;
    [SerializeField]
    GameObject confirmPanel;

    GameObject panelToDel;
    List<GameObject> panelDelList = new List<GameObject>();

    [SerializeField]
    GameObject listPanel;
    [SerializeField]
    GameObject listPanelContent;
    [SerializeField]
    GameObject infoRowPrefab;

    [SerializeField]
    GameObject modelController;
    [SerializeField]
    Text vesselName;

    //[SerializeField]
    //Material matUI;

    enum Type { INFO_PANEL, SELECT_LIST };

    List<int> subHeadingBaseCount = new List<int>();
    Dictionary<string, Dictionary<string, string>> currentInfo;
    string currentTextEdit;
    string currentHeadEdit;

    // Use this for initialization
    void Start()
    {
        CountSubHeadings();
        RenderSettings.ambientLight = new Color(1f, 1f, 1f);
        RenderSettings.ambientMode = AmbientMode.Flat;
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Counts base amount of sub headings for each main heading (before any are added)
    /// </summary>
    private void CountSubHeadings()
    {
        InputField[] inputFields = infoPanel.GetComponentsInChildren<InputField>();
        int headingBaseIndex = -1;

        for (int i = 0; i < inputFields.Length; i++)
        {
            if (inputFields[i].name == "InputFieldHeading")
            {
                headingBaseIndex++;
                subHeadingBaseCount.Add(0);
            }
            else if (inputFields[i].name == "InputFieldSubHeading")
            {
                subHeadingBaseCount[headingBaseIndex]++;
            }
        }
    }

    /// <summary>
    /// Makes all input fields non interactable and stops blocking ray casting
    /// </summary>
    public void StopEditTexts()
    {
        InputField[] inputFields = infoPanel.GetComponentsInChildren<InputField>();
        for (int i = 0; i < inputFields.Length; i++)
        {
            inputFields[i].image.raycastTarget = false;
            inputFields[i].interactable = false;
        }
    }

    /// <summary>
    /// Allows editing of both input fields of given panel
    /// </summary>
    /// <param name="panel"> Contains input fields to be modified </param>
    public void EditPanel(GameObject panel)
    {
        InputField[] inputFields = panel.GetComponentsInChildren<InputField>();

        // Makes all fields non interactable if first input field in non interactable
        // In case of any other being interactable
        if (!inputFields[0].interactable)
        {
            StopEditTexts();
        }

        // Makes fields interactable and keeps record of current text
        for (int i = 0; i < inputFields.Length; i++)
        {
            inputFields[i].image.raycastTarget = !inputFields[i].interactable;
            inputFields[i].interactable = !inputFields[i].interactable;

            if (inputFields[i].name == "InputFieldSubHeading")
            {
                currentHeadEdit = inputFields[i].text;
            }
            else
            {
                currentTextEdit = inputFields[i].text;
            }
        }
    }

    /// <summary>
    /// Makes the given input field interactable, all others non interactable and keeps record of current text
    /// </summary>
    /// <param name="inputField"> Input field made to be interactable </param>
    public void EditText(InputField inputField)
    {
        if (!inputField.interactable)
        {
            StopEditTexts();
        }

        inputField.image.raycastTarget = !inputField.interactable;
        inputField.interactable = !inputField.interactable;

        InputField headingIF = inputField.transform.parent.parent.GetChild(0).GetComponentInChildren<InputField>();

        currentHeadEdit = headingIF.text;
        currentTextEdit = inputField.text;
    }

    /// <summary>
    /// Makes editable inputfields non interactable and compares text with the current text before edit.
    /// If comparison is different, information will be saved
    /// </summary>
    /// <param name="panel"> panel containing inputfields from which were finished being edited </param>
    public void finishEditText(GameObject panel)
    {
        foreach (InputField inputField in panel.GetComponentsInChildren<InputField>())
        {
            inputField.image.raycastTarget = false;
            inputField.interactable = false;

            string compStr = currentTextEdit;
            if (inputField.name == "InputFieldSubHeading")
            {
                compStr = currentHeadEdit;
            }

            if (inputField.text != compStr)
            {
                string heading = panel.transform.parent.GetChild(0).GetComponentInChildren<InputField>().text;
                saveInfo(inputField.text, inputField.name, heading);
            }
        }
    }

    /// <summary>
    /// Updates the currentInfo object
    /// Updates row in vessel select list if the heading exists as a column
    /// Overwrites or creates a text file with the text from the updated currentInfo object
    /// </summary>
    /// <param name="newStr"> New string upon finishing edit </param>
    /// <param name="inputName"> Name of the input field being edited </param>
    /// <param name="mainHeading"> Text of the main heading in which the inputfield being edited resides </param>
    private void saveInfo(string newStr, string inputName, string mainHeading)
    {
        if (inputName == "InputFieldSubHeading")
        {
            string value = currentInfo[mainHeading][currentHeadEdit];
            currentInfo[mainHeading].Remove(currentHeadEdit);
            currentInfo[mainHeading].Add(newStr, value);
        }
        else
        {
            currentInfo[mainHeading][currentHeadEdit] = newStr;

            // update row if column exists
            Text[] headings = listPanelContent.transform.GetChild(0).GetComponentsInChildren<Text>();
            int index = -1;
            for (int i = 0; i < headings.Length; i++)
            {
                if (headings[i].text == currentHeadEdit)
                {
                    index = i;
                }
            }

            for (int i = 1; i < listPanelContent.transform.childCount; i++)
            {
                string name = listPanelContent.transform.GetChild(i).GetChild(0).GetComponentInChildren<Text>().text;
                if (name == vesselName.text)
                {
                    listPanelContent.transform.GetChild(i).GetChild(index).GetComponentInChildren<Text>().text = newStr;
                }
            }
        }

        // Save file
        string path = Application.persistentDataPath + "/" + vesselName.text + ".txt";

        StreamReader reader = new StreamReader(path);

        List<string> originalStrList = new List<string>();
        while (!reader.EndOfStream)
        {
            originalStrList.Add(reader.ReadLine());
        }
        string[] originalStr = originalStrList.ToArray();

        //string[] originalStr = Regex.Split(reader.ReadToEnd(), "\r\n");

        reader.Close();



        StreamWriter writer = new StreamWriter(path);
        string[] newStrLines = Regex.Split(newStr, "\n");

        if (inputName == "InputFieldSubHeading")
        {
            for (int i = 0; i < originalStr.Length; i++)
            {
                if (originalStr[i] == currentHeadEdit)
                {
                    originalStr[i] = newStr;
                }
            }
        }
        else
        {
            int indent = 0;
            bool correctHead = false;
            bool finished = false;

            for (int i = 0; i < originalStr.Length; i++)
            {
                switch (originalStr[i])
                {
                    case "{":
                        indent++;
                        break;
                    case "}":
                        indent--;
                        break;
                    default:
                        if (indent == 1 && originalStr[i] == currentHeadEdit)
                        {
                            correctHead = true;
                        }
                        else if (correctHead && indent == 2)
                        {
                            // Add new content
                            for (int j = 0; j < newStrLines.Length; j++)
                            {
                                writer.WriteLine(newStrLines[j]);
                            }

                            // Add rest of original content
                            bool adding = false;
                            for (int j = i; j < originalStr.Length; j++)
                            {
                                if (!adding && originalStr[j] == "}")
                                {
                                    adding = true;
                                }

                                if (adding)
                                {
                                    if (j == originalStr.Length - 1)
                                    {
                                        writer.Write(originalStr[j]);
                                    }
                                    else
                                    {
                                        writer.WriteLine(originalStr[j]);
                                    }
                                }
                            }

                            finished = true;
                        }
                        break;
                }

                if (finished)
                {
                    break;
                }

                if (i == originalStr.Length - 1)
                {
                    writer.Write(originalStr[i]);
                }
                else
                {
                    writer.WriteLine(originalStr[i]);
                }
            }
        }

        writer.Close();
    }

    /// <summary>
    /// Changes position the info panel area to be in camera view, to be visible by user
    /// </summary>
    /// <param name="infoButton"> Button being pressed </param>
    public void MoveInfo(Button infoButton)
    {
        hidden = !hidden;

        RectTransform rtCanvas = (RectTransform)transform;

        if (hidden)
        {
            infoButton.image.sprite = Resources.Load<Sprite>("Sprites/panelButtonOpen");
            infoPanel.transform.localPosition += Vector3.right * infoPanel.GetComponent<RectTransform>().sizeDelta.x;
        }
        else
        {
            infoButton.image.sprite = Resources.Load<Sprite>("Sprites/panelButtonClose");
            infoPanel.transform.localPosition += Vector3.left * infoPanel.GetComponent<RectTransform>().sizeDelta.x;
        }
    }

    /// <summary>
    /// Records reference to panel to be delete
    /// Asks confirmation for custom panel to be deleted
    /// </summary>
    /// <param name="panel"> Panel to be deleted </param>
    public void DelPanel(GameObject panel)
    {
        panelToDel = panel;

        confirmPanel.SetActive(true);
    }

    /// <summary>
    /// Deletes panel recorded in panelToDel reference
    /// </summary>
    public void DelPanel()
    {
        panelDelList.Remove(panelToDel);
        Destroy(panelToDel);
        panelToDel = null;
    }

    /// <summary>
    /// Creates panel of input fields (heading and text) with edit and delete buttons
    /// Adds panel under the main heading in which add button was pressed (under parent object)
    /// </summary>
    /// <param name="parent"> The object to add new panel to </param>
    public void addInfoDel(GameObject parent)
    {
        GameObject newPanel = Instantiate(infoDelPrefab, parent.transform);
        PanelInformation thisInformation = newPanel.GetComponent<PanelInformation>();

        thisInformation.editButton.onClick.AddListener(delegate { EditPanel(newPanel); });
        thisInformation.delButton.onClick.AddListener(delegate { DelPanel(newPanel); });

        thisInformation.headingInput.onEndEdit.AddListener(delegate { finishEditText(newPanel); });
        thisInformation.editInput.onEndEdit.AddListener(delegate { finishEditText(newPanel); });

        panelDelList.Add(newPanel);
    }

    /// <summary>
    /// Changes the ambient colour of the scene between red and white
    /// </summary>
    /// <param name="lowLightButton"> Button pressed </param>
    public void lowLightModeChange(Button lowLightButton)
    {
        lowLightMode = !lowLightMode;

        if (lowLightMode)
        {
            lowLightButton.image.sprite = Resources.Load<Sprite>("Sprites/lowlightpressed");
            RenderSettings.ambientLight = lowLightColor;
        }
        else
        {
            lowLightButton.image.sprite = Resources.Load<Sprite>("Sprites/lowlight");
            RenderSettings.ambientLight = normalColor;
        }
    }

    /// <summary>
    /// Collapses/uncollapses panel by hiding/showing sub panels respectively
    /// </summary>
    /// <param name="panel"> panel to collapse/uncollapse </param>
    public void collapsePanel(GameObject panel)
    {
        bool collapsed = false;

        for (int i = 1; i < panel.transform.childCount; i++)
        {
            GameObject subPanel = panel.transform.GetChild(i).gameObject;
            subPanel.SetActive(!subPanel.activeSelf);

            if (i == 1) collapsed = !subPanel.activeSelf;
        }

        Button[] btns = panel.GetComponentsInChildren<Button>();
        if (collapsed)
        {
            btns[0].image.sprite = Resources.Load<Sprite>("Sprites/addbutton");
            btns[1].interactable = false;
        }
        else
        {
            btns[0].image.sprite = Resources.Load<Sprite>("Sprites/minusbutton");
            btns[1].interactable = true;
        }
    }

    /// <summary>
    /// Removes all content in info panel and updates with new information based on selected vessel
    /// </summary>
    /// <param name="name"> name of file of selected vessel (without extension) </param>
    public void updateInfo(string name)
    {
        vesselName.text = name;

        // Uncollapse all panels
        Transform content = infoPanel.transform.GetChild(1).GetChild(0).GetChild(0);
        for (int i = 0; i < content.childCount; i++)
        {
            if (!content.GetChild(i).GetChild(1).gameObject.activeSelf)
            {
                collapsePanel(content.GetChild(i).gameObject);
            }
        }

        // Remove custom panels
        while (panelDelList.Count > 0)
        {
            Destroy(panelDelList[0].gameObject);
            panelDelList.RemoveAt(0);

        }

        StartCoroutine(updateInfoDelay(name));
    }

    IEnumerator updateInfoDelay(string name)
    {
        yield return new WaitForEndOfFrame();

        InputField[] inputFields = infoPanel.GetComponentsInChildren<InputField>();

        // Clear information
        for (int i = 0; i < inputFields.Length; i++)
        {
            if (inputFields[i] == null) continue;

            if (inputFields[i].IsInteractable())
            {
                EditText(inputFields[i]);
            }

            if (inputFields[i].name == "InputField")
            {
                inputFields[i].text = "";
            }
        }

        // If no file exists in persistant data path, creates file
        // Otherwise overwrites existing text file

        string[] infoStr = null;
        if (File.Exists(Application.persistentDataPath + "/" + name + ".txt"))
        {
            infoStr = File.ReadAllLines(Application.persistentDataPath + "/" + name + ".txt");
        }
        else
        {
            StreamWriter sw = File.CreateText(Application.persistentDataPath + "/" + name + ".txt");
            TextAsset ta = Resources.Load<TextAsset>("Package/TextFiles/" + name);

            if (ta == null)
            {
                ta = Resources.Load<TextAsset>("Package/TextFiles/empty");
            }

            infoStr = Regex.Split(ta.text, "\r\n");
            for (int i = 0; i < infoStr.Length; i++)
            {
                if (i < infoStr.Length - 1)
                {
                    sw.WriteLine(infoStr[i]);
                }
                else
                {
                    sw.Write(infoStr[i]);
                }
            }

            sw.Close();
        }

        currentInfo = readText(infoStr, Type.INFO_PANEL);

        // Update array of input fields to include custom headings if any
        inputFields = infoPanel.GetComponentsInChildren<InputField>();
        string currentHeading = "";
        string currentSubHeading = "";

        // Update input fields
        for (int i = 0; i < inputFields.Length; i++)
        {
            switch (inputFields[i].name)
            {
                case "InputFieldHeading":
                    currentHeading = inputFields[i].text;
                    break;
                case "InputFieldSubHeading":
                    currentSubHeading = inputFields[i].text;
                    break;
                default:
                    inputFields[i].text = currentInfo[currentHeading][currentSubHeading];
                    break;
            }
        }
    }

    /// <summary>
    /// Adds relevant information from array of strings to a dictionary object
    /// </summary>
    /// <param name="lines"> string array to add to dictionary object </param>
    /// <param name="type"> What the text reading is for </param>
    /// <returns> Dictionary object updated with  </returns>
    private Dictionary<string, Dictionary<string, string>> readText(string[] lines, Type type)
    {
        Dictionary<string, Dictionary<string, string>> dictInfo = new Dictionary<string, Dictionary<string, string>>();

        if (lines != null)
        {
            string currentHeading = "";
            string currentSubHeading = "";
            int subHeadingCount = 0;
            int headingIndex = -1;
            int indent = 0;

            for (int i = 0; i < lines.Length - 1; i++)
            {
                switch (lines[i])
                {
                    case "{":
                        indent++;
                        break;
                    case "}":
                        indent--;
                        break;
                    default:
                        switch (indent)
                        {
                            case 0:
                                headingIndex++;
                                subHeadingCount = 0;

                                dictInfo.Add(lines[i], new Dictionary<string, string>());
                                currentHeading = lines[i];
                                break;
                            case 1:
                                subHeadingCount++;
                                currentSubHeading = lines[i];

                                if (type.Equals(Type.INFO_PANEL) || subHeadingCount <= subHeadingBaseCount[headingIndex])
                                {
                                    dictInfo[currentHeading].Add(lines[i], "");
                                }

                                if (subHeadingCount > subHeadingBaseCount[headingIndex])
                                {
                                    addInfoDel(infoPanel.GetComponentsInChildren<InputField>()[headingIndex].
                                        transform.parent.parent.gameObject);
                                    GameObject newPanel = panelDelList[panelDelList.Count - 1];
                                    newPanel.GetComponentInChildren<InputField>().text = currentSubHeading;

                                    newPanel.SetActive(newPanel.transform.parent.GetChild(0).gameObject.activeSelf);
                                }
                                break;
                            case 2:
                                if (type.Equals(Type.INFO_PANEL) || subHeadingCount <= subHeadingBaseCount[headingIndex])
                                {
                                    dictInfo[currentHeading][currentSubHeading] +=
                                    dictInfo[currentHeading][currentSubHeading] == "" ?
                                    lines[i] : "\n" + lines[i];
                                }
                                break;
                        }
                        break;
                }
            }
        }

        return dictInfo;
    }

    /// <summary>
    /// Adds a row for each model in the list of models to the content in the scroll view of the vessel select panel
    /// </summary>
    /// <param name="models"> List of models to create rows for </param>
    public void addVesselSelectRow(List<GameObject> models)
    {
        foreach (GameObject model in models)
        {
            addVesselSelectRow(model);
        }
    }

    /// <summary>
    /// Adds a row for the model to the content in the scroll view of the vessel select panel
    /// </summary>
    /// <param name="model"> Model to create a row for </param>
    private void addVesselSelectRow(GameObject model)
    {
        GameObject infoRow = Instantiate(infoRowPrefab, listPanelContent.transform);

        infoRow.GetComponent<Button>().onClick.AddListener(delegate
        {
            vesselName.text = model.name;
            setRowSelect(infoRow);
        });

        Text[] texts = infoRow.GetComponentsInChildren<Text>();
        Text[] headings = listPanelContent.transform.GetChild(0).GetComponentsInChildren<Text>();

        // If no file exists in persistant data path, loads data from Text files folder within resource folder,
        // If no asset exists, then loades empty text asset

        string[] infoStr = null;
        if (File.Exists(Application.persistentDataPath + "/" + model.name + ".txt"))
        {
            infoStr = File.ReadAllLines(Application.persistentDataPath + "/" + model.name + ".txt");
        }
        else
        {
            TextAsset ta = Resources.Load<TextAsset>("Package/TextFiles/" + model.name);
            string str = "";
            if (ta != null)
            {
                str = ta.text;
            }
            else
            {
                str = Resources.Load<TextAsset>("Package/TextFiles/empty").text;
            }
            infoStr = Regex.Split(str, "\r\n");
        }

        Dictionary<string, Dictionary<string, string>> dictInfo = readText(infoStr, Type.SELECT_LIST);

        texts[0].text = model.name;
        for (int i = 1; i < headings.Length; i++)
        {
            foreach (KeyValuePair<string, Dictionary<string, string>> entry in dictInfo)
            {
                if (entry.Value.ContainsKey(headings[i].text))
                {
                    texts[i].text = entry.Value[headings[i].text];
                }
            }
        }
    }

    /// <summary>
    /// Highlights the row that was selected in the vessel select panel
    /// </summary>
    /// <param name="row"> Row that was selected </param>
    private void setRowSelect(GameObject row)
    {
        for (int i = 0; i < listPanelContent.transform.childCount; i++)
        {
            listPanelContent.transform.GetChild(i).GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f);
        }
        row.GetComponent<Image>().color = new Color(0.75f, 0.8f, 0.8f);
    }

    /// <summary>
    /// Calls select vessel from model controller
    /// </summary>
    public void selectVessel()
    {
        modelController.GetComponent<ModelController>().selectVessel(vesselName.text);
    }

    /*
    public void loadFiles(Text name)
    {
        loadFiles(name.text);
    }

    private void loadFiles(string name)
    {
        string filename = Application.dataPath + "/VesselFiles/" + name;
        string savePath = Application.persistentDataPath + "/" + name;
        Debug.Log("filename: " + filename);

        string objStr = "";
        string matStr = "";
        List<Texture2D> textures = new List<Texture2D>();

        // Load object file to string
        if (File.Exists(filename + ".obj"))
        {
            objStr = File.ReadAllText(filename + ".obj");
            Debug.Log("obj string loaded");

            File.WriteAllText(savePath + "_obj.txt", objStr);
        }
        else
        {
            // File doesn't exist
            Debug.Log("File doesn't exist");
            return;
        }

        // Load material file to string
        if (File.Exists(filename + ".mtl"))
        {
            matStr = File.ReadAllText(filename + ".mtl");
            Debug.Log("mat string loaded");

            File.WriteAllText(savePath + "_mtl.txt", matStr);
        }

        // Load textures to texture list (if any)
        int texNum = 1;
        while (File.Exists(filename + texNum + ".jpg"))
        {
            byte[] fileData = File.ReadAllBytes(filename + texNum + ".jpg");
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);
            textures.Add(tex);

            File.WriteAllBytes(savePath + "_tex" + texNum + ".png", tex.EncodeToPNG());

            Debug.Log("texture " + texNum + " loaded");
            texNum++;
        }

        GameObject go = ObjImporter.Import(objStr, matStr, textures.ToArray());
        go.name = name;
        go.transform.SetParent(modelController.transform, false);
        go.SetActive(false);

        // TODO: add info to vessel select row
        string infoStr = "";
        // Load object file to string
        if (File.Exists(filename + ".txt"))
        {
            infoStr = File.ReadAllText(filename + ".txt");
            Debug.Log("info string loaded");

            File.WriteAllText(savePath + "_info.txt", infoStr);
        }
    }
    */
}
