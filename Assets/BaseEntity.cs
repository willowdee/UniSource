using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseEntity : MonoBehaviour
{
    public string entityName;
    public int entityID;
    public int entityHealth;
    public bool isAlive;

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
            entityHealth = 100;
            isAlive = true;
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

    public static Mesh GetModel(BaseEntity entity)
    {
        return entity.GetComponent<MeshFilter>().mesh;
    }

}
