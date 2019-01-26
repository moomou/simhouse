using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class RoomGen : MonoBehaviour
{
    public const string OUTPUT_DIR = "/Users/moomou/Downloads/interior";

    public string RoomType = "bedroom";
    public int ResWidth = 640;
    public int ResHeight = 480;
    public int MaxItemCount = 12;

    public enum ScreenshotPosition
    {
        Top,
        Middle,
        Left,
        Right
    };

    Camera m_MainCamera;
    string generatorConfig;
    IEnumerable<string> allFurns;
    List<GameObject> objs;
    HashSet<Vector2> grid;

    string OutputLocation(string cameraPosition, string config, bool isGood)
    {
        string dir = System.IO.Path.Combine(OUTPUT_DIR, isGood ? "good" : "bad");
        string filename = cameraPosition + "_" + config + ".jpg";
        return System.IO.Path.Combine(dir, filename);
    }

    IEnumerable<string> GetAllFurnitures()
    {
        var allAssetPaths = AssetDatabase.GetAllAssetPaths()
             .Where(s => s.EndsWith("fbx", System.StringComparison.Ordinal));
        // RoomGenUtils.DebugArray(allAssetGuids);
        return allAssetPaths; items.OrderBy(x => Random.value).First();
    }


    void CaptureScreenShot(string outputName)
    {
        RenderTexture rt = new RenderTexture(ResWidth, ResHeight, 24);
        Camera cam = GetComponent<Camera>();
        cam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(ResWidth, ResHeight, TextureFormat.RGB24, false);
        cam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, ResWidth, ResHeight), 0, 0);
        cam.targetTexture = null;
        RenderTexture.active = null; // JC: added to avoid errors
        Destroy(rt);
        byte[] bytes = screenShot.EncodeToPNG();

        System.IO.File.WriteAllBytes(outputName, bytes);
        Debug.Log(string.Format("Took screenshot to: {0}", outputName));
    }

    string GetFurnAssetPathByCategory(string category, int randomSeed = 42)
    {
        var items = RoomGenConfig.ItemsByCategory[category];
        var item = items.OrderBy(x => Random.value).First();
        return allFurns.Where(f => f.EndsWith(string.Format("{0}.fbx", item), System.StringComparison.Ordinal)).First();
    }

    GameObject PlaceItem(string assetPath, Vector3 position)
    {
        return PlaceItem(assetPath, position, Vector3.up, 0);
    }

    GameObject PlaceItem(string assetPath, Vector3 position, Vector3 rotationAxis, float angle)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        var size = asset.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        var centroidPosition = new Vector3(position.x - size.x / 2, position.y, position.z - size.z / 2);

        var obj = Instantiate(asset, centroidPosition, Quaternion.identity);
        //obj.transform.RotateAround(centroidPosition, rotationAxis, rotation);
        obj.transform.RotateAround(obj.transform.position + new Vector3(
            size.x / 2f, size.y / 2f, 0f), Vector3.up, angle);

        objs.Add(obj);
        return obj;
    }
    GameObject PlaceFloor()
    {
        var floor = GetFurnAssetPathByCategory("floor");
        var obj = PlaceItem(floor, Vector3.zero);
        return obj;
    }
    List<GameObject> PlaceWall(GameObject floor)
    {
        var size = floor.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        var wallPath = GetFurnAssetPathByCategory("wall");

        // place item around the floor
        PlaceItem(wallPath, new Vector3(0, 0, size.z / 2));
        PlaceItem(wallPath, new Vector3(size.x / 2, 0, 0), Vector3.up, 90);
        PlaceItem(wallPath, new Vector3(-size.x / 2, 0, 0), Vector3.up, -90);
        return new List<GameObject> { };
    }
    void PlaceStair()
    {

    }
    void PlaceDoor()
    {

    }

    Vector2 NextRoomGridCoord(Vector2 roomSize)
    {

    }
    void Generate(string roomType)
    {
        // Clear everything
        foreach (var obj in objs)
        {
            Destroy(obj);
        }
        // Clear the array
        objs.Clear();

        // build up our generator_config
        generatorConfig = roomType;

        // step 1: floor
        var floor = PlaceFloor();
        // step 2: wall
        PlaceWall(floor);
        // step 3: door
        // PlaceDoor();
        // step 4: room specific 
        if (roomType == "bedroom")
        {
            var roomSize = floor.GetComponent<MeshFilter>().sharedMesh.bounds.size;
            var categories = RoomGenConfig.Room2Category[roomType];
            var itemCount = (int)(MaxItemCount / 2 + Random.Range(0, MaxItemCount / 2));

            for (int i = 0; i < itemCount; i++)
            {
                var category = categories.OrderBy(x => Random.value).First();
                var itemPath = RoomGenConfig.ItemsByCategory[category].OrderBy(x => Random.value).First();
                
                PlaceItem(itemPath, )
            }
        }
        else
        {
            throw new System.NotImplementedException(string.Format("roomType {0} not implemented", roomType));
        }
    }

    // Use this for initialization
    void Start()
    {
        m_MainCamera = Camera.main;
        allFurns = GetAllFurnitures();
        objs = new List<GameObject>();
        Generate(RoomType);
    }

    // Update is called once per frameforeach(string g in allAssetGuids.
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("MOUSE. Regenerate here.");
            Generate(RoomType);
            return;
        }

        // jKey => good
        bool jKey = Input.GetKey("j");
        // kKey => bad
        bool kKey = Input.GetKey("k");

        if (jKey || kKey)
        {
            foreach (ScreenshotPosition pos in System.Enum.GetValues(typeof(ScreenshotPosition)))
            {
                string outputFilename = OutputLocation(pos.ToString(), RoomGenUtils.CreateMD5(generatorConfig), jKey == true);
                CaptureScreenShot(outputFilename);
            }

            // regenerate after screenshot because we don't want repeats
        }
    }
}