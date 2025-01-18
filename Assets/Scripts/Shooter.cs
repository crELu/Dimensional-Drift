using UnityEngine;

public class Shooter : MonoBehaviour
{
    // attributes
    public int radius = 10;
    public int damage = 10;
    public GameObject currentEnemy;

    public GameObject projectile;
    public float launchVelocity = 10f;

    public SphereCollider hitRangeCollider;

    public float hitInterval = 1f;
    private float timeElapsed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // initialize collider radius and center the collider just to be safe
        hitRangeCollider.radius = radius;
        hitRangeCollider.center = new Vector3(0, 0, 0);    // in its gameobjects local space
        timeElapsed = 0f;

    }

    // Update is called once per frame
    void Update()
    {
        // if locked onto an enemy, update
        timeElapsed += Time.deltaTime;
        if (timeElapsed >= hitInterval)
        {
            if (currentEnemy)
            {
                LaunchProjectile(currentEnemy, projectile);
            }
            timeElapsed = 0f;
        }
    }

    // something enters defender radius
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.tag == "enemy")
        {
            if (!currentEnemy)    // lock onto a new enemy
            {
                currentEnemy = collision.gameObject;
                timeElapsed = 0f;    // reset time
            }
        }
    }

    // something exits defender radius
    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.tag == "enemy")
        {
            if (currentEnemy)
            {
                currentEnemy = null;
                timeElapsed = 0f;    // reset time
            }
        }
    }

    // instantiate projectile with damage and launch projectile at target
    private void LaunchProjectile(GameObject target, GameObject projectile)
    {
        GameObject p = Instantiate(projectile, transform.position, transform.rotation);
        Projectile projectileScript = p.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.SetDamage(damage);
        }

        Vector3 dir = target.transform.position - transform.position;
        p.GetComponent<Rigidbody>().AddRelativeForce(dir * launchVelocity);
    }
}
