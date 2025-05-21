using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject Projectile;
    public float ProjectileSpeed = 10f;
    
    private AudioSource audioSource;

    private void Awake()
    {
        this.audioSource = GetComponent<AudioSource>();
    }

    public void Fire()
    {
        Debug.Log("Fire");
        float direction = Mathf.Sign(this.transform.lossyScale.x);
        
        Quaternion rotation = direction < 0f ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;
        
        GameObject projectileInstance = Instantiate(this.Projectile, this.transform.position, rotation);
        Rigidbody2D projectileRigidbody = projectileInstance.GetComponent<Rigidbody2D>();
        projectileRigidbody.linearVelocity = Vector2.right * this.ProjectileSpeed * direction;

        this.audioSource.Play();
    }
}
