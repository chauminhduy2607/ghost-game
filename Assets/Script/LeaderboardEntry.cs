using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardEntry : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;
    public Image backgroundImage;
    
    [Header("Colors")]
    public Color firstPlaceColor = new Color(1f, 0.84f, 0f); // Vàng
    public Color secondPlaceColor = new Color(0.75f, 0.75f, 0.75f); // Bạc
    public Color thirdPlaceColor = new Color(0.8f, 0.5f, 0.2f); // Đồng
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    // Thêm vào LeaderboardEntry.cs
public void ApplyGradient()
{
    // Tạo gradient từ trên xuống dưới
    backgroundImage.color = Color.Lerp(
        new Color(0.1f, 0.3f, 0.5f),
        new Color(0.3f, 0.1f, 0.5f),
        0.5f
    );
}
    
    public void SetEntry(int rank, string playerName, float score)
{
    string rankIcon = "";
    if (rank == 1) rankIcon = "🥇 ";
    else if (rank == 2) rankIcon = "🥈 ";
    else if (rank == 3) rankIcon = "🥉 ";
    
    rankText.text = rankIcon + "#" + rank;
    nameText.text = playerName;
    scoreText.text = score.ToString("F2");
    
    // Đổi màu cho top 3
    if (rank == 1)
        backgroundImage.color = firstPlaceColor;
    else if (rank == 2)
        backgroundImage.color = secondPlaceColor;
    else if (rank == 3)
        backgroundImage.color = thirdPlaceColor;
    else
        backgroundImage.color = normalColor;
}
}