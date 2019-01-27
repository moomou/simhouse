using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

public class RoomGen : MonoBehaviour
{
    public const string OUTPUT_DIR = "/Users/moomou/Downloads/interior";

    public string RoomType = "bedroom";
    public int ResWidth = 640;
    public int ResHeight = 480;
    public int MaxItemCount = 15;
    public int FloorPlan = 2;
    public int GridN = 20;

    Camera m_MainCamera;
    string generatorConfig;
    IEnumerable<string> allFurns;
    List<GameObject> objs;
    HashSet<Vector2> grid;
    bool film;

    string OutputLocation(string dir, string prefix, string config)
    {

        string filename = prefix + "_" + config + ".jpg";
        return System.IO.Path.Combine(dir, filename);
    }

    IEnumerable<string> GetAllFurnitures()
    {
        var allAssetPaths = AssetDatabase.GetAllAssetPaths()
             .Where(s => s.EndsWith("fbx", System.StringComparison.Ordinal));

        return allAssetPaths;
    }


    void CaptureScreenShot(string outputName)
    {
        RenderTexture rt = new RenderTexture(ResWidth, ResHeight, 24);
        Camera cam = m_MainCamera;
        cam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(ResWidth, ResHeight, TextureFormat.RGB24, false);
        cam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, ResWidth, ResHeight), 0, 0);
        cam.targetTexture = null;
        RenderTexture.active = null; // JC: added to avoid errors
        Destroy(rt);
        byte[] bytes = screenShot.EncodeToJPG();

        System.IO.File.WriteAllBytes(outputName, bytes);
        Debug.Log(string.Format("Took screenshot to: {0}", outputName));
    }

    string GetFurnAssetPath(string category, string item)
    {
        var items = RoomGenConfig.ItemsByCategory[category];
        return allFurns.First(f => f.EndsWith(string.Format("{0}.fbx", item), System.StringComparison.Ordinal));
    }
    string GetRandomFurnAssetPathByCategory(string category, int randomSeed = 42)
    {
        var items = RoomGenConfig.ItemsByCategory[category];
        var item = items.OrderBy(x => Random.value).First();
        return allFurns.First(f => f.EndsWith(string.Format("{0}.fbx", item), System.StringComparison.Ordinal));
    }
    Vector3 GetMeshSize(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var size = asset.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        return size;
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
        var floorPath = GetRandomFurnAssetPathByCategory("floor");
        var floors = new List<GameObject>();
        var fSize = GetMeshSize(floorPath);
        Debug.Log(string.Format("Single Floor Size {0},{1}", fSize.x, fSize.z));

        // place floor boards
        for (int i = 0; i < FloorPlan; i++)
        {
            var offsetX = (-fSize.x * FloorPlan / 2 + fSize.x / 2) + i * fSize.x;
            for (int j = 0; j < FloorPlan; j++)
            {
                var offsetZ = (-fSize.z * FloorPlan / 2 + fSize.z / 2) + j * fSize.z;
                floors.Add(
                    PlaceItem(floorPath, new Vector3(offsetX, 0, offsetZ))
                );
            }
        }

        CombineInstance[] combine = new CombineInstance[floors.Count()];
        for (int i = 0; i < floors.Count(); i++)
        {
            var m = floors[i].GetComponent<MeshFilter>();
            combine[i].mesh = m.sharedMesh;
            combine[i].transform = m.transform.localToWorldMatrix;
            floors[i].gameObject.SetActive(false);
        }

        var combinedFloor = new GameObject();
        combinedFloor.transform.position = Vector3.zero;

        combinedFloor.AddComponent<MeshFilter>();
        combinedFloor.AddComponent<MeshRenderer>();

        // a hack really...
        combinedFloor.GetComponent<MeshRenderer>().material = new Material(floors[0].GetComponent<MeshRenderer>().material);
        combinedFloor.GetComponent<MeshFilter>().mesh = new Mesh();
        combinedFloor.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
        combinedFloor.gameObject.SetActive(true);

        objs.Add(combinedFloor);
        return combinedFloor;
    }
    List<GameObject> PlaceWall(GameObject floor)
    {
        var floorSize = floor.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        var wallPath = GetRandomFurnAssetPathByCategory("wall");

        generatorConfig += wallPath;

        var wallSize = GetMeshSize(wallPath);
        var walls = new List<GameObject>();

        // 3 sections of wall to place
        // back
        for (int i = 0; i < (int)(floorSize.x / wallSize.x); i++)
        {
            var x = (-floorSize.x / 2 + wallSize.x / 2) + (i) * wallSize.x;
            walls.Add(PlaceItem(wallPath, new Vector3(x, 0, floorSize.z / 2)));
        }

        // left & right
        for (int i = 0; i < (int)(floorSize.z / wallSize.x); i++)
        {
            var z = (-floorSize.z / 2 + wallSize.x / 2) + (i) * wallSize.x;
            walls.Add(PlaceItem(wallPath, new Vector3(floorSize.x / 2, 0, z), Vector3.up, 90));
            walls.Add(PlaceItem(wallPath, new Vector3(-floorSize.x / 2, 0, z), Vector3.up, -90));
        }

        return walls;
    }
    void PlaceStair()
    {

    }
    void PlaceDoor()
    {

    }

    Vector2 NextRoomGridCoord()
    {
        for (int i = 0; i < 3; i++)
        {
            int x = (int)(Random.value * GridN);
            int z = (int)(Random.value * GridN);

            Vector2 pos = new Vector2(x, z);
            if (grid.Count() >= GridN * GridN)
            {
                throw new System.Exception("BAD BAD");
            }
            if (!grid.Contains(pos))
            {
                grid.Add(pos);
                return pos;
            }
        }

        throw new System.Exception("BAD");
    }
    Vector3 Grid2WorldCoord(Vector3 roomSize, Vector2 gridCoord)
    {
        var x = roomSize.x * (gridCoord.x / (1f * GridN)) - roomSize.x / 2;
        // NOTE - gridCoord.y is is intentional
        var z = roomSize.z * (gridCoord.y / (1f * GridN)) - roomSize.z / 2;
        return new Vector3(x, 0, z);
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
        // clear grid
        grid.Clear();

        // build up our generator_config
        generatorConfig = roomType;

        // step 1: floor
        var floor = PlaceFloor();
        // step 2: wall
        PlaceWall(floor);
        // step 3: door
        // PlaceDoor();
        // step 4: room specific 
        var roomSize = floor.GetComponent<MeshFilter>().sharedMesh.bounds.size;

        var categories = RoomGenConfig.Room2Category[roomType];
        var itemCount = (int)(MaxItemCount / 2 + Random.Range(0, MaxItemCount / 2));

        for (int i = 0; i < itemCount; i++)
        {
            var category = categories.OrderBy(x => Random.value).First();
            var item = RoomGenConfig.ItemsByCategory[category].OrderBy(x => Random.value).First();
            var itemPath = GetFurnAssetPath(category, item);
            var gridPos = NextRoomGridCoord();
            generatorConfig += category + item + gridPos.ToString();

            PlaceItem(itemPath, Grid2WorldCoord(roomSize, gridPos));
        }

    }

    // Use this for initialization
    void Start()
    {
        m_MainCamera = Camera.main;
        allFurns = GetAllFurnitures();
        objs = new List<GameObject>();
        grid = new HashSet<Vector2>();
        film = false;
        StartCoroutine(CaptureCoroutine());

        Generate(RoomType);
    }

    IEnumerator CaptureCoroutine()
    {
        string dir = System.IO.Path.Combine(OUTPUT_DIR, RoomType);

        while (true)
        {
            if (film)
            {
                var unixTimestamp = System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1)).TotalMilliseconds;
                for (int i = 0; i < 1000; i++)
                {
                    string outputFilename = OutputLocation(dir, unixTimestamp.ToString(), RoomGenUtils.CreateMD5(generatorConfig));
                    CaptureScreenShot(outputFilename);
                    Generate(RoomType);
                    yield return new WaitForSeconds(.5f);
                }
                film = false;
            }
            yield return new WaitForSeconds(1);
        }
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
        // gKey => generate 10K images
        bool gKey = Input.GetKey("g");

        if (jKey || kKey)
        {
            string dir = System.IO.Path.Combine(OUTPUT_DIR, jKey ? "good" : "bad");
            string outputFilename = OutputLocation(dir, "default", RoomGenUtils.CreateMD5(generatorConfig));
            CaptureScreenShot(outputFilename);

            Generate(RoomType);
        }
        else if (gKey && !film)
        {
            film = true;
        }
    }
}