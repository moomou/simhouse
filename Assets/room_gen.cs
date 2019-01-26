using System.Collections;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Text;

public class room_gen : MonoBehaviour
{

    public int resWidth = 640;
    public int resHeight = 480;
    public const string OUTPUT_DIR = "/Users/moomou/Downloads/interior";
    public enum ScreenshotPosition
    {
        Top,
        Middle,
        Left,
        Right
    }

    Camera m_MainCamera;
    string config;

    public static string CreateMD5(string input)
    {
        // Use input string to calculate MD5 hash
        using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
        {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }

    void DebugArray(IEnumerable arrs)
    {
        foreach (var a in arrs)
        {
            Debug.Log(a);
        }
    }

    string OutputLocation(string cameraPosition, string config, bool isGood)
    {
        string dir = System.IO.Path.Combine(OUTPUT_DIR, isGood ? "good" : "bad");
        string filename = cameraPosition + "_" + config + ".jpg";
        return System.IO.Path.Combine(dir, filename);
    }

    System.Collections.Generic.IEnumerable<string> GetAllFurnitures()
    {
        var allAssetPaths = AssetDatabase.GetAllAssetPaths()
             .Where(s => s.EndsWith("fbx", System.StringComparison.Ordinal));
        // DebugArray(allAssetGuids);
        return allAssetPaths;
    }

    void CaptureScreenShot(string outputName)
    {
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
        Camera cam = GetComponent<Camera>();
        cam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        cam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        cam.targetTexture = null;
        RenderTexture.active = null; // JC: added to avoid errors
        Destroy(rt);
        byte[] bytes = screenShot.EncodeToPNG();

        System.IO.File.WriteAllBytes(outputName, bytes);
        Debug.Log(string.Format("Took screenshot to: {0}", outputName));
    }

    void PlaceItem(string assetPath)
    {
        Debug.Log("HERE");
        Debug.Log(assetPath);
        Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(assetPath), new Vector3(0, 0, 0), Quaternion.identity);
    }

    // Use this for initialization
    void Start()
    {
        m_MainCamera = Camera.main;


        var furns = GetAllFurnitures();
        PlaceItem(furns.OrderBy(x => System.Guid.NewGuid()).FirstOrDefault());
    }

    // Update is called once per frameforeach(string g in allAssetGuids.
    void Update()
    {

        // jKey => good
        bool jKey = Input.GetKey("j");
        // kKey => bad
        bool kKey = Input.GetKey("j");


        if (jKey || kKey)
        {


            foreach (ScreenshotPosition pos in System.Enum.GetValues(typeof(ScreenshotPosition)))
            {
                string outputFilename = OutputLocation(pos.ToString(), CreateMD5(config), jKey == true);
                CaptureScreenShot(outputFilename)
            }

        }
    }
}