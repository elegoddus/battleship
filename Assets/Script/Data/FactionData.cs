using UnityEngine;

[CreateAssetMenu(fileName = "NewFaction", menuName = "Game Data/Faction Data")]
public class FactionData : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    public string factionKey;
    public Color mainColor = Color.white;
    public Color[] subColors;
    public Sprite flagIcon;
    public Sprite victoryBackground;

    [Header("Hình ảnh Tàu (MVP)")]
    [Tooltip("Index 0: Tàu 5, Index 1: Tàu 4, Index 2: Tàu 3, Index 3: Tàu 2")] 
    [Header("Hình ảnh Tàu (Thủy chiến)")]
    public Sprite shipSize5;
    public Sprite shipSize4;
    public Sprite shipSize3; // Sẽ dùng chung cho cả 2 chiếc 3 ô
    public Sprite shipSize2;

    [Header("Hình ảnh Máy bay (Tương lai)")]
    public Sprite planeBomber;  // Tấn công 3x3
    public Sprite planeRecon;   // Trinh sát đi 2 ô
    public Sprite planeStealth; // Tàng hình
    public Sprite planeScout;   // Do thám
}