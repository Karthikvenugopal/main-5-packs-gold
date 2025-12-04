using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Helper that builds a compact exit portal with a tight trigger and consistent visuals.
/// </summary>
public static class ExitPortalFactory
{
    private const float PortalWidthCells = 2f;
    private const float PortalHeightCells = 1f;
    private static Sprite s_whiteSprite;

    /// <summary>
    /// Creates a two-tile wide exit portal at the requested world position.
    /// </summary>
    /// <param name="parent">Transform that should own the portal.</param>
    /// <param name="worldCenter">World-space center for the portal.</param>
    /// <param name="cellSize">Size of a single grid cell (used for sizing).</param>
    /// <param name="configureExitZone">Callback that wires the ExitZone to the correct manager.</param>
    /// <returns>The root GameObject of the created portal.</returns>
    public static GameObject CreateExitPortal(
        Transform parent,
        Vector3 worldCenter,
        float cellSize,
        Action<ExitZone> configureExitZone)
    {
        if (parent == null) throw new ArgumentNullException(nameof(parent));
        if (configureExitZone == null) throw new ArgumentNullException(nameof(configureExitZone));

        GameObject root = new GameObject("ExitPortal");
        root.transform.SetParent(parent);
        root.transform.position = worldCenter;

        BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(cellSize * PortalWidthCells - cellSize * 0.08f, cellSize * 0.85f);
        trigger.offset = Vector2.zero;

        ExitZone exitZone = root.AddComponent<ExitZone>();
        configureExitZone(exitZone);

        CreateBoundaryBox(root.transform, cellSize);
        CreateLabel(root.transform, cellSize);

        return root;
    }

    private static void CreateBoundaryBox(Transform parent, float cellSize)
    {
        float width = cellSize * PortalWidthCells;
        float height = cellSize * PortalHeightCells;
        float thickness = Mathf.Max(0.04f, cellSize * 0.08f);
        Color outlineColor = new Color(0.9f, 0.75f, 0.35f, 1f);

        // Top edge
        CreateOutlineEdge(
            parent,
            "TopEdge",
            new Vector3(0f, height * 0.5f, 0f),
            new Vector2(width, thickness),
            outlineColor
        );

        // Bottom edge
        CreateOutlineEdge(
            parent,
            "BottomEdge",
            new Vector3(0f, -height * 0.5f, 0f),
            new Vector2(width, thickness),
            outlineColor
        );

        // Left edge
        CreateOutlineEdge(
            parent,
            "LeftEdge",
            new Vector3(-width * 0.5f, 0f, 0f),
            new Vector2(thickness, height),
            outlineColor
        );

        // Right edge
        CreateOutlineEdge(
            parent,
            "RightEdge",
            new Vector3(width * 0.5f, 0f, 0f),
            new Vector2(thickness, height),
            outlineColor
        );
    }

    private static void CreateOutlineEdge(Transform parent, string name, Vector3 localPosition, Vector2 size, Color color)
    {
        GameObject edge = new GameObject(name);
        edge.transform.SetParent(parent, false);
        edge.transform.localPosition = localPosition;

        SpriteRenderer renderer = edge.AddComponent<SpriteRenderer>();
        renderer.sprite = GetWhiteSprite();
        renderer.color = color;
        renderer.sortingOrder = 6;

        edge.transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private static void CreateLabel(Transform parent, float cellSize)
    {
        GameObject label = new GameObject("Label");
        label.transform.SetParent(parent, false);
        label.transform.localPosition = Vector3.zero;

        TextMeshPro text = label.AddComponent<TextMeshPro>();
        text.text = "EXIT";
        text.fontSize = Mathf.Clamp(cellSize * 5.5f, 4f, 9f);
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.enableWordWrapping = false;
        text.color = new Color(0.99f, 0.95f, 0.78f, 1f);

        if (text.TryGetComponent(out MeshRenderer meshRenderer))
        {
            meshRenderer.sortingOrder = 7;
        }
    }

    private static Sprite GetWhiteSprite()
    {
        if (s_whiteSprite != null)
        {
            return s_whiteSprite;
        }

        Texture2D texture = Texture2D.whiteTexture;
        s_whiteSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            texture.width
        );
        s_whiteSprite.name = "ExitPortal_WhiteSprite";
        return s_whiteSprite;
    }

}
