using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float damage;

    // Set the damage value for this projectile
    public void SetDamage(float damageValue)
    {
        damage = damageValue;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        //if (collision.collider.tag == "enemy")
        //{
        //    EnemyAssignments enemy = collision.gameObject.GetComponent<EnemyAssignments>();
        //    if (enemy != null)
        //    {
        //        enemy.TakeDamage(damage);
        //        Destroy(gameObject);
        //    }
        //}
        Debug.Log("hit");
        Destroy(gameObject);
    }
}
