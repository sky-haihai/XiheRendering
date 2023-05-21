using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PaletteData", menuName = "PalettePixelation/PaletteData", order = 1)]
public class PaletteData : ScriptableObject
{
    public List<Color> paletteColors = new List<Color>();
}
