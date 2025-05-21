using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public GameObject Explosion;
    public float Damage = 10f;
    public float Force = 100f;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Instantiate(this.Explosion, this.transform.position, Quaternion.identity);
        Destroy(this.gameObject);

        Character character = other.GetComponent<Character>();
        if (character != null)
        {
            Debug.Log("BOOOM!");
            character.TakeDamage(this.Damage);
        }
        
        Rigidbody2D rigidbody2D = other.GetComponent<Rigidbody2D>();
        if (rigidbody2D != null)
        {
            Vector2 direction = other.transform.position - this.transform.position;
            rigidbody2D.AddForce(direction * this.Force);
        }
    }
}
