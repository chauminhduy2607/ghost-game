using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class FirebaseInitializer : MonoBehaviour
{
    public static FirebaseInitializer Instance { get; private set; }
    
    private DatabaseReference databaseRef;
    public DatabaseReference DatabaseRef => databaseRef;
    
    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        Debug.Log("🔥 Initializing Firebase...");
        
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            DependencyStatus status = task.Result;
            
            if (status == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                databaseRef = FirebaseDatabase.DefaultInstance.RootReference;
                
                IsInitialized = true;
                
                Debug.Log("✅ Firebase ready!");
                Debug.Log($"📍 Database URL: {FirebaseDatabase.DefaultInstance.App.Options.DatabaseUrl}");
                
                TestConnection();
            }
            else
            {
                Debug.LogError($"❌ Firebase error: {status}");
                IsInitialized = false;
            }
        });
    }
    
    private void TestConnection()
    {
        Debug.Log("🧪 Testing Firebase write...");
        
        databaseRef.Child("test").Child("message").SetValueAsync("Hello from Unity!")
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("✅ Write test SUCCESS!");
                }
                else
                {
                    Debug.LogError("❌ Write test FAILED!");
                }
            });
    }
}