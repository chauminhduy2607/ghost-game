    using UnityEngine;

    /// <summary>
    /// ⭐ HỆ THỐNG 9 LINE THEO CHIỀU NGANG
    /// - Tự động tính toán 9 line dựa vào kích thước màn hình
    /// - Line 5 (giữa) là vị trí của Ghost
    /// - Các vật cản sẽ spawn theo line
    /// - Responsive với mọi kích thước màn hình
    /// </summary>
    public class LineSystem : MonoBehaviour
    {
        [Header("=== LINE SETTINGS ===")]
        [SerializeField] private int totalLines = 9;
        [SerializeField] private float marginPercent = 0.05f; // 5% margin mỗi bên
        
        [Header("=== DEBUG ===")]
        [SerializeField] private bool showDebugLines = true;
        [SerializeField] private Color lineColor = Color.yellow;
        
        // Private
        private Camera mainCamera;
        private float[] linePositionsX;
        private float screenWidth;
        private float lineSpacing;
        
        // Singleton
        public static LineSystem Instance { get; private set; }
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            mainCamera = Camera.main;
            CalculateLines();
        }
        
        void Start()
        {
            PrintLineInfo();
        }
        
        /// <summary>
        /// Tính toán vị trí các line dựa vào camera
        /// </summary>
        void CalculateLines()
        {
            if (mainCamera == null)
            {
                Debug.LogWarning("⚠️ LineSystem: Camera null, không thể tính toán!");
                return;
            }
            
            // Tính chiều rộng màn hình
            screenWidth = mainCamera.orthographicSize * 2f * mainCamera.aspect;
            
            // Tính margin
            float margin = screenWidth * marginPercent;
            float usableWidth = screenWidth - (margin * 2f);
            
            // Tính khoảng cách giữa các line
            lineSpacing = usableWidth / (totalLines - 1);
            
            // Khởi tạo mảng nếu chưa có hoặc size sai
            if (linePositionsX == null || linePositionsX.Length != totalLines)
            {
                linePositionsX = new float[totalLines];
            }
            
            // Tính vị trí mỗi line
            float startX = -screenWidth / 2f + margin;
            
            for (int i = 0; i < totalLines; i++)
            {
                linePositionsX[i] = startX + (i * lineSpacing);
            }
            
            Debug.Log($"✅ LineSystem: Đã tính toán {totalLines} lines (Width: {screenWidth:F2}, Spacing: {lineSpacing:F2})");
        }
        
        /// <summary>
        /// Lấy vị trí X của line (0-8)
        /// </summary>
        public float GetLineX(int lineIndex)
        {
            // ⭐ Safety check
            if (linePositionsX == null || linePositionsX.Length == 0)
            {
                Debug.LogWarning("⚠️ LineSystem: linePositionsX chưa được khởi tạo! Tính toán lại...");
                CalculateLines();
            }
            
            if (lineIndex < 0 || lineIndex >= totalLines)
            {
                Debug.LogWarning($"⚠️ Line index {lineIndex} ngoài phạm vi 0-{totalLines - 1}!");
                return 0f;
            }
            
            // Double check sau khi CalculateLines
            if (linePositionsX == null || lineIndex >= linePositionsX.Length)
            {
                Debug.LogError("❌ LineSystem: Không thể lấy line position!");
                return 0f;
            }
            
            return linePositionsX[lineIndex];
        }
        
        /// <summary>
        /// Lấy vị trí X của line giữa (Ghost)
        /// </summary>
        public float GetCenterLineX()
        {
            return GetLineX(totalLines / 2); // Line 4 (index 4)
        }
        
        /// <summary>
        /// Lấy vị trí X ngẫu nhiên từ các line cho phép
        /// </summary>
        public float GetRandomLineX(int[] allowedLines)
        {
            if (allowedLines == null || allowedLines.Length == 0)
            {
                Debug.LogWarning("⚠️ Không có line nào được phép!");
                return GetCenterLineX();
            }
            
            int randomIndex = allowedLines[Random.Range(0, allowedLines.Length)];
            return GetLineX(randomIndex);
        }
        
        /// <summary>
        /// Lấy line gần nhất với vị trí X cho trước
        /// </summary>
        public int GetNearestLineIndex(float x)
        {
            int nearestIndex = 0;
            float minDistance = Mathf.Abs(x - linePositionsX[0]);
            
            for (int i = 1; i < totalLines; i++)
            {
                float distance = Mathf.Abs(x - linePositionsX[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestIndex = i;
                }
            }
            
            return nearestIndex;
        }
        
        /// <summary>
        /// Snap vị trí X về line gần nhất
        /// </summary>
        public float SnapToNearestLine(float x)
        {
            int lineIndex = GetNearestLineIndex(x);
            return GetLineX(lineIndex);
        }
        
        /// <summary>
        /// Lấy tất cả vị trí line
        /// </summary>
        public float[] GetAllLinePositions()
        {
            return linePositionsX;
        }
        
        /// <summary>
        /// Lấy khoảng cách giữa các line
        /// </summary>
        public float GetLineSpacing()
        {
            return lineSpacing;
        }
        
        /// <summary>
        /// Lấy tổng số line
        /// </summary>
        public int GetTotalLines()
        {
            return totalLines;
        }
        
        void PrintLineInfo()
        {
            Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log("📍 LINE SYSTEM - THÔNG TIN");
            Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log($"📊 Tổng số line: {totalLines}");
            Debug.Log($"📏 Chiều rộng màn hình: {screenWidth:F2}");
            Debug.Log($"📏 Khoảng cách line: {lineSpacing:F2}");
            Debug.Log($"📍 Line giữa (Ghost): Line {totalLines / 2} (X = {GetCenterLineX():F2})");
            
            Debug.Log("\n📋 VỊ TRÍ CÁC LINE:");
            for (int i = 0; i < totalLines; i++)
            {
                string marker = (i == totalLines / 2) ? "👻 GHOST" : "";
                Debug.Log($"   Line {i}: X = {linePositionsX[i]:F2} {marker}");
            }
            Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }
        
        void OnDrawGizmos()
        {
            if (!showDebugLines) return;
            
            // ⭐ FIX: Kiểm tra linePositionsX trước khi dùng
            if (linePositionsX == null || linePositionsX.Length == 0) 
            {
                // Nếu chưa có data, tính toán ngay
                if (mainCamera == null)
                    mainCamera = Camera.main;
                
                if (mainCamera != null)
                    CalculateLines();
                
                // Nếu vẫn null thì return
                if (linePositionsX == null || linePositionsX.Length == 0)
                    return;
            }
            
            if (mainCamera == null)
                mainCamera = Camera.main;
            
            if (mainCamera == null) return;
            
            float screenHeight = mainCamera.orthographicSize * 2f;
            float bottomY = mainCamera.transform.position.y - screenHeight / 2f;
            float topY = mainCamera.transform.position.y + screenHeight / 2f;
            
            for (int i = 0; i < linePositionsX.Length && i < totalLines; i++)
            {
                // Màu đặc biệt cho line giữa
                if (i == totalLines / 2)
                {
                    Gizmos.color = Color.green;
                }
                else
                {
                    Gizmos.color = lineColor;
                }
                
                Vector3 bottom = new Vector3(linePositionsX[i], bottomY, 0f);
                Vector3 top = new Vector3(linePositionsX[i], topY, 0f);
                
                Gizmos.DrawLine(bottom, top);
                
                // Vẽ số line
    #if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    new Vector3(linePositionsX[i], topY - 0.5f, 0f),
                    $"L{i}",
                    new GUIStyle() { normal = new GUIStyleState() { textColor = Gizmos.color } }
                );
    #endif
            }
        }
        
        void OnValidate()
        {
            // Recalculate khi thay đổi settings trong Inspector
            if (Application.isPlaying && mainCamera != null)
            {
                CalculateLines();
            }
        }
    }