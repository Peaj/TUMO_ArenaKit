using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public GameObject Explosion;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("BOOOM!");
        Instantiate(this.Explosion, this.transform.position, Quaternion.identity);
        Destroy(this.gameObject);
    }
}
