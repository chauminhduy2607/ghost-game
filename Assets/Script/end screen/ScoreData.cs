using System;

[Serializable]
public class ScoreData
{
    public string nickname;
    public int score;
    public string time;  // Format: "HH:mm:ss" hoặc giây
    public int day;
    public long timestamp; // Để sort theo thời gian mới nhất
    
    public ScoreData()
    {
    }
    
    public ScoreData(string nickname, int score, string time, int day)
    {
        this.nickname = nickname;
        this.score = score;
        this.time = time;
        this.day = day;
        this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}