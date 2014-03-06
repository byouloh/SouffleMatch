﻿using UnityEngine;

public class MapButton : MonoBehaviour
{
    public GameObject pauseButton;

    public Window levelList;

    public void OnPress()
    {
        PanelManager.Show(levelList);
        pauseButton.SendMessage("OnClick", SendMessageOptions.DontRequireReceiver);
    }
}