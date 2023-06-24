using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using String = System.String;

public class SpriteBox : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public bool UseCollider { get; private set; }
    private GameObject colliderObject;
    private new BoxCollider2D collider;
    public MouseCollider Mouse { get; private set; }
    void Awake()
    {
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        colliderObject = General.AddChild(gameObject, "Collider");
        collider = colliderObject.AddComponent<BoxCollider2D>();
        Mouse = gameObject.AddComponent<MouseCollider>();
        Mouse.Initialize(collider);
    }
    public void Initialize(Sprite sprite, string sortingLayerName, int sortingOrder, Vector3 position, string layer = "Default", bool useCollider = false)
    {
        gameObject.layer = LayerMask.NameToLayer(layer);
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = sortingOrder;
        transform.localPosition = position;
        UseCollider = useCollider;
        if (UseCollider)
        {
            collider.size = sprite.bounds.size;
            colliderObject.transform.localPosition = new Vector3(sprite.bounds.size.x * .5f - sprite.pivot.x / sprite.pixelsPerUnit,
                sprite.bounds.size.y * .5f - sprite.pivot.y / sprite.pixelsPerUnit, 0);
        }
        else
        {
            Destroy(colliderObject);
            Destroy(Mouse);
        }
    }
}
