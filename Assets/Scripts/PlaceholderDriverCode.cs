using UnityEngine;

public class PlaceholderDriverCode : MonoBehaviour
{
    // References to the main camera and default camera positions
    public Camera mainCamera;
    public Vector3 defaultCameraPosition;
    public Quaternion defaultCameraRotation;

    public Vector3 topDownCameraPosition = new Vector3(0, 20, 0); // Adjust as needed
    public Quaternion topDownCameraRotation = Quaternion.Euler(90, 0, 0);

    private bool is2D = false; // Tracks whether we're in 2D mode

    public GameObject placeholder;

    void Start()
    {
        // Initialize camera if not assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Save default camera position and rotation
        defaultCameraPosition = mainCamera.transform.position;
        defaultCameraRotation = mainCamera.transform.rotation;
    }

    void Update()
    {
        // Toggle 2D/3D mode when spacebar is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleMode();
        }
    }

    public void ToggleMode()
    {
        is2D = !is2D;

        if (is2D)
        {
            SwitchTo2D();
        }
        else
        {
            SwitchTo3D();
        }
    }

    private void SwitchTo2D()
    {
        // Set camera to orthographic top-down view
        mainCamera.orthographic = true;
        mainCamera.transform.position = topDownCameraPosition;
        mainCamera.transform.rotation = topDownCameraRotation;

        placeholder.GetComponent<Hitbox>().toggleColliderDimension(true);
    }

    private void SwitchTo3D()
    {
        // Reset camera to perspective view
        mainCamera.orthographic = false;
        mainCamera.transform.position = defaultCameraPosition;
        mainCamera.transform.rotation = defaultCameraRotation;

        placeholder.GetComponent<Hitbox>().toggleColliderDimension(false);
    }

    //public void Generate2DHitboxes()
    //{
    //    foreach (var obj in FindObjectsOfType<GameObject>())
    //    {
    //        Collider collider = obj.GetComponent<Collider>();
    //        if (collider == null)
    //            continue;

    //        if (collider is SphereCollider sphere)
    //        {
    //            CreateCircleCollider2D(obj, sphere);
    //        }
    //        else if (collider is BoxCollider box)
    //        {
    //            CreateBoxCollider2D(obj, box);
    //        }
    //        else if (collider is CapsuleCollider capsule)
    //        {
    //            CreateCapsuleCollider2D(obj, capsule);
    //        }
    //        else if (collider is MeshCollider mesh)
    //        {
    //            CreatePolygonCollider2D(obj, mesh);
    //        }
    //    }
    //}

    //private void CreateCircleCollider2D(GameObject obj, SphereCollider sphere)
    //{
    //    float radius = sphere.radius * Mathf.Max(obj.transform.lossyScale.x, obj.transform.lossyScale.z);
    //    var circleCollider = GetOrCreateComponent<CircleCollider2D>(obj);
    //    circleCollider.radius = radius;
    //}

    //private void CreateBoxCollider2D(GameObject obj, BoxCollider box)
    //{
    //    Vector3 size = box.size;
    //    Vector3 scale = obj.transform.lossyScale;

    //    float width = size.x * scale.x;
    //    float height = size.z * scale.z;

    //    var boxCollider = GetOrCreateComponent<BoxCollider2D>(obj);
    //    boxCollider.size = new Vector2(width, height);
    //}

    //private void CreateCapsuleCollider2D(GameObject obj, CapsuleCollider capsule)
    //{
    //    float radius = capsule.radius * Mathf.Max(obj.transform.lossyScale.x, obj.transform.lossyScale.z);
    //    float height = capsule.height * Mathf.Max(obj.transform.lossyScale.y, obj.transform.lossyScale.z);

    //    var boxCollider = GetOrCreateComponent<BoxCollider2D>(obj);
    //    boxCollider.size = new Vector2(radius * 2, height);
    //}

    //private void CreatePolygonCollider2D(GameObject obj, MeshCollider mesh)
    //{
    //    var polygonCollider = GetOrCreateComponent<PolygonCollider2D>(obj);
    //    var meshVertices = mesh.sharedMesh.vertices;

    //    Vector2[] projectedVertices = new Vector2[meshVertices.Length];
    //    for (int i = 0; i < meshVertices.Length; i++)
    //    {
    //        Vector3 worldPoint = obj.transform.TransformPoint(meshVertices[i]);
    //        projectedVertices[i] = new Vector2(worldPoint.x, worldPoint.z);
    //    }

    //    polygonCollider.SetPath(0, projectedVertices);
    //}

    //private void Disable3DColliders()
    //{
    //    foreach (var collider in FindObjectsOfType<Collider>())
    //    {
    //        collider.enabled = false;
    //    }
    //}

    //private void Enable3DColliders()
    //{
    //    foreach (var collider in FindObjectsOfType<Collider>())
    //    {
    //        collider.enabled = true;
    //    }
    //}

    //private void Remove2DColliders()
    //{
    //    foreach (var obj in FindObjectsOfType<GameObject>())
    //    {
    //        var circle = obj.GetComponent<CircleCollider2D>();
    //        if (circle)
    //            Destroy(circle);

    //        var box = obj.GetComponent<BoxCollider2D>();
    //        if (box)
    //            Destroy(box);

    //        var polygon = obj.GetComponent<PolygonCollider2D>();
    //        if (polygon)
    //            Destroy(polygon);
    //    }
    //}

    //private T GetOrCreateComponent<T>(GameObject obj) where T : Component
    //{
    //    T component = obj.GetComponent<T>();
    //    if (component == null)
    //    {
    //        component = obj.AddComponent<T>();
    //    }
    //    return component;
    //}
}
