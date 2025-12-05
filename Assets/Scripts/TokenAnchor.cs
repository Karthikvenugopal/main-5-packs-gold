using UnityEngine;

/// <summary>
/// Editor-visible marker that indicates where a collectible token should spawn.
/// Keeps level editing simple while still allowing the maze to be generated at runtime.
/// </summary>
public class TokenAnchor : MonoBehaviour
{
    [SerializeField] private TokenSpriteConfigurator.TokenType tokenType = TokenSpriteConfigurator.TokenType.Fire;

    public TokenSpriteConfigurator.TokenType TokenType => tokenType;

    /// <summary>
    /// Sets the token type for runtime-created anchors.
    /// </summary>
    public void SetTokenType(TokenSpriteConfigurator.TokenType type)
    {
        tokenType = type;
    }

    private static readonly Color FireColor = new Color(1f, 0.55f, 0.15f, 0.9f);
    private static readonly Color WaterColor = new Color(0.3f, 0.6f, 1f, 0.9f);

    private void OnDrawGizmos()
    {
        Gizmos.color = tokenType == TokenSpriteConfigurator.TokenType.Fire ? FireColor : WaterColor;
        Vector3 position = transform.position;
        float size = 0.35f;

        if (tokenType == TokenSpriteConfigurator.TokenType.Fire)
        {
            // Draw a simple triangle outline.
            Vector3 top = position + Vector3.up * size;
            Vector3 left = position + Quaternion.Euler(0f, 0f, 140f) * Vector3.up * size;
            Vector3 right = position + Quaternion.Euler(0f, 0f, -140f) * Vector3.up * size;

            Gizmos.DrawLine(top, left);
            Gizmos.DrawLine(left, right);
            Gizmos.DrawLine(right, top);
        }
        else
        {
            // Draw a droplet-ish gizmo by combining a circle and a point.
            Gizmos.DrawSphere(position + Vector3.up * size * 0.25f, size * 0.7f);
            Gizmos.DrawSphere(position + Vector3.down * size * 0.7f, size * 0.35f);
        }
    }
}
