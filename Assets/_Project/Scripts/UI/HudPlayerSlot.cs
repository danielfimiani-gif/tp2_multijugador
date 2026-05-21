using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class HudPlayerSlot
{
    [FormerlySerializedAs("root")] public GameObject Root;
    [FormerlySerializedAs("nameLabel")] public TMP_Text NameLabel;
    [FormerlySerializedAs("damageLabel")] public TMP_Text DamageLabel;
    [FormerlySerializedAs("livesLabel")] public TMP_Text LivesLabel;
    [FormerlySerializedAs("kosLabel")] public TMP_Text KosLabel;
}
