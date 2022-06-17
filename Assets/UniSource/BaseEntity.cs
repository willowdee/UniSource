using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class BaseEntity : MonoBehaviour
{
    
    public string entityName;
    public int entityID;
    public int entityHealth;
    public bool isAlive;
    public bool removeAfterDeath;
    public float afterDeathRemoveDelay;
    public EntityType entityType;

    public virtual void TakeDamage(int damage)
    {
        entityHealth -= damage;
        if (entityHealth <= 0)
        {
            Destroy(gameObject);
            isAlive = false;
        }
    }
    
    void Awake()
    {
        entityID = GetInstanceID();

        if (entityName == "")
        {
            entityName = "Entity";
        }
        if (entityHealth == 0)
        {
            entityHealth = 1;
            isAlive = true;
        }
    }
    void Update()
    {
        if (entityHealth <= 0)
        {
            isAlive = false;
            if(removeAfterDeath)
            {
                Destroy(gameObject, afterDeathRemoveDelay);
            }
        }
    }

    public enum EntityType
    {
        Player,
        NPC,
        Item,
        Environment,
        Other
    }

    public Mesh GetModel(BaseEntity entity)
    {
        return GetComponent<MeshFilter>().mesh;
    }

    public enum RenderMode
    {
        Normal,
        Transparent,
        Glow,
        WorldSpaceGlow
    }

    

}
