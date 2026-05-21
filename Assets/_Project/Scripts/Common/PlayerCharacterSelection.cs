using UnityEngine;

public static class PlayerCharacterSelection
{
    private const string PrefsKey = "PlayerCharacterIndex";

    private static int _selectedIndex = -1;

    public static int SelectedIndex
    {
        get
        {
            if (_selectedIndex < 0)
                _selectedIndex = PlayerPrefs.GetInt(PrefsKey, 0);
            return _selectedIndex;
        }
        set
        {
            _selectedIndex = value;
            PlayerPrefs.SetInt(PrefsKey, value);
            PlayerPrefs.Save();
        }
    }
}
