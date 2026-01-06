using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    [HideInInspector] public Transform target;
    
    private ParticleSystem trailParticles;

    private void Awake()
    {
        trailParticles = GetComponent<ParticleSystem>();
    }

    // Follows target each frame, when target is destroyed it stops and cleans up
    private void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position;
        }
        else
        {
            trailParticles.Stop();
            Destroy(gameObject, trailParticles.main.startLifetime.constantMax);
        }
    }
}