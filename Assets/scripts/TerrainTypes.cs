using UnityEngine;

[CreateAssetMenu(fileName = "TerrainTypes", menuName = "Scriptable Objects/TerrainTypes")]
public class TerrainTypes : ScriptableObject
{
    public Terrain[] rules;

    // optional: penalties stored here too
    public int lightPenalty = 5;
    public int heavyPenalty = 15;
    public int untraversablePenalty = 999999;
}
