using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Game;

public class ParticleCollisionInstance : MonoBehaviour
{
    public GameObject[] EffectsOnCollision;
    public float DestroyTimeDelay = 5;
    public bool UseWorldSpacePosition;
    public float Offset = 0;
    public Vector3 rotationOffset = new Vector3(0, 0, 0);
    public bool useOnlyRotationOffset = true;
    public bool UseFirePointRotation;
    public bool DestoyMainEffect = true;
    private ParticleSystem part;
    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    [SerializeField]
    private Spell spell;
    void Start()
    {
        part = GetComponent<ParticleSystem>();
        if (spell == null) spell = GetComponent<Spell>();
        if(spell == null) spell = GetComponentInParent<Spell>();
    }
    void OnParticleCollision(GameObject other) {
        if (spell.GetCaster() == other.gameObject)
            return;
        if (other.layer == 6 || other.layer == 7 || other.layer == 3 || (spell.GetCaster().gameObject.GetComponent<PlayerClasses>().isMonster && other.layer == 9)) {
            int numCollisionEvents = part.GetCollisionEvents(other, collisionEvents);
            AchieveDestroy(numCollisionEvents);
        }
    }

    void AchieveDestroy(int numCollisionEvents) {
#if !UNITY_SERVER
        for (int i = 0 ; i < numCollisionEvents ; i++) {
            foreach (var effect in EffectsOnCollision) {
                var instance = Instantiate(effect, collisionEvents[i].intersection + collisionEvents[i].normal * Offset, new Quaternion()) as GameObject;
                    if (!UseWorldSpacePosition)
                        instance.transform.parent = transform;
                    if (UseFirePointRotation) { instance.transform.LookAt(transform.position); }
                    else if (rotationOffset != Vector3.zero && useOnlyRotationOffset) { instance.transform.rotation = Quaternion.Euler(rotationOffset); }
                    else {
                        instance.transform.LookAt(collisionEvents[i].intersection + collisionEvents[i].normal);
                        instance.transform.rotation *= Quaternion.Euler(rotationOffset);
                    }
                    Destroy(instance, DestroyTimeDelay + 0.5f);
                }
        }
#endif
        if (DestoyMainEffect == true) {
                Destroy(gameObject);
        }
    }
}