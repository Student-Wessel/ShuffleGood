using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private UIState _UIState = UIState.MAINMENU;
    
    [SerializeField]
    private UIJoystick _joystick;

    [SerializeField]
    private GameObject _mainMenuPanel,_searchForGamePanel,_waitForOpponentPanel,_winPanel,_losePanel,_joystickPanel;

    public UIJoystick joystick => _joystick;
    
    public void HideAllPanels()
    {
        _mainMenuPanel.gameObject.SetActive(false);
        _searchForGamePanel.gameObject.SetActive(false);
        _waitForOpponentPanel.gameObject.SetActive(false);
        _winPanel.gameObject.SetActive(false);
        _losePanel.gameObject.SetActive(false);
        _joystickPanel.gameObject.SetActive(false);
    }

    public void SwitchToState(UIState pUIState)
    {
        HideAllPanels();
        
        switch (pUIState)
        {
            case UIState.MAINMENU:
                _mainMenuPanel.gameObject.SetActive(true);

                break;
            case UIState.SEARCHFORGAME:
                _searchForGamePanel.gameObject.SetActive(true);

                break;
            case UIState.WAITFOROPPONENT:
                _waitForOpponentPanel.gameObject.SetActive(true);

                break;
            case UIState.WINPANEL:
                _winPanel.gameObject.SetActive(true);

                break;
            case UIState.LOSEPANEL:
                _losePanel.gameObject.SetActive(true);

                break;
            case UIState.JOYSTICK:
                _joystickPanel.gameObject.SetActive(true);

                break;
        }
    }
}

[Serializable]
public enum UIState
{
    MAINMENU,
    SEARCHFORGAME,
    WAITFOROPPONENT,
    WINPANEL,
    LOSEPANEL,
    JOYSTICK
}


