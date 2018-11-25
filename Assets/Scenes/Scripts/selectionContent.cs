using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class selectionContent : MonoBehaviour {

    bool IsAscending = true;
    int lastCol = -1;

    // Use this for initialization
	void Start () {
        
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    // sorts list of vessels by selected heading (column)

    /// <summary>
    /// Sorts the rows of the vessel list based on the column of the heading pressed
    /// </summary>
    /// <param name="head"> The heading panel of the column to be sorted </param>
    public void sort(GameObject head)
    {
        int col = head.transform.GetSiblingIndex();

        if (col == lastCol)
        {
            IsAscending = !IsAscending;
        }
        else
        {
            IsAscending = true;
        }
        lastCol = col;

        for (int i = 1; i < transform.childCount; i++)
        {
            for (int j = 1; j < i; j++)
            {
                GameObject row1 = transform.GetChild(i).gameObject;
                string str1 = row1.transform.GetChild(col).GetComponentInChildren<Text>().text.ToLower();

                GameObject row2 = transform.GetChild(j).gameObject;
                string str2 = row2.transform.GetChild(col).GetComponentInChildren<Text>().text.ToLower();

                if (IsAscending)
                {
                    if (str1.CompareTo(str2) < 0)
                    {
                        row1.transform.SetSiblingIndex(j);
                    }
                }
                else
                {
                    if (str1.CompareTo(str2) > 0)
                    {
                        row1.transform.SetSiblingIndex(j);
                    }
                }
            }
        }
    }
}
