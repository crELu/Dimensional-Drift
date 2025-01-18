using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class Hitbox : MonoBehaviour
{
    public GameObject self;
    public Collider collider3d;
    private Vector3 defaultColliderDims;
    private Light sun;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        self = gameObject;    // gameobject this hitbox is attached to
        if (collider3d is BoxCollider box)
        {
            defaultColliderDims = box.size;
        }
        sun = GameObject.FindWithTag("Sun").GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Make2dCollider(GameObject obj, BoxCollider box)
    {
        box.size = new Vector3(box.size.x, 100, box.size.z);
        collider3d = box;
    }

    public void toggleColliderDimension(bool is2D)
    {
        if (is2D)
        {
            if (!collider3d)
                return;

            if (collider3d is BoxCollider box)
                Make2dCollider(self, box);
            
            sun.shadows = LightShadows.None;
        }
        else
        {
            if (collider3d is BoxCollider box)
                box.size = defaultColliderDims;
            
            sun.shadows = LightShadows.Hard;
        }
        
    }



    private T GetOrCreateComponent<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        if (component == null)
        {
            component = obj.AddComponent<T>();
        }
        return component;
    }
}
