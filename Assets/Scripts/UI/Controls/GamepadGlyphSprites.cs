using System;
using UnityEngine;

/// <summary>
/// Draws the shapes used for gamepad button icons. The sprites are white and get their color from the Image that shows them.
/// </summary>
public static class GamepadGlyphSprites
{
    private const int SIZE = 128;
    private const float HALF_SIZE = SIZE / 2f;
    private const float STROKE_HALF_WIDTH = 7f;
    private const float SYMBOL_HALF_EXTENT = 27f;

    private static readonly Sprite[] _shapeSprites = new Sprite[2];
    private static readonly Sprite[] _symbolSprites = new Sprite[5];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Array.Clear(_shapeSprites, 0, _shapeSprites.Length);
        Array.Clear(_symbolSprites, 0, _symbolSprites.Length);
    }

    public static Sprite GetShape(GlyphShape shape)
    {
        int index = (int)shape;
        if (_shapeSprites[index] == null)
        {
            _shapeSprites[index] = shape == GlyphShape.Circle
                ? Build(p => p.magnitude - 60f)
                : Build(RoundedSquare);
        }

        return _shapeSprites[index];
    }

    /// <summary>
    /// Sprite for a symbol, or null for <see cref="GlyphSymbol.None"/>.
    /// </summary>
    public static Sprite GetSymbol(GlyphSymbol symbol)
    {
        if (symbol == GlyphSymbol.None)
        {
            return null;
        }

        int index = (int)symbol;
        if (_symbolSprites[index] == null)
        {
            _symbolSprites[index] = symbol switch
            {
                GlyphSymbol.Cross => Build(Cross),
                GlyphSymbol.Circle => Build(p => Mathf.Abs(p.magnitude - SYMBOL_HALF_EXTENT - 3f) - STROKE_HALF_WIDTH),
                GlyphSymbol.Square => Build(p => Mathf.Abs(Box(p, SYMBOL_HALF_EXTENT)) - STROKE_HALF_WIDTH),
                _ => Build(p => Mathf.Abs(Triangle(p + new Vector2(0f, 4f), SYMBOL_HALF_EXTENT + 6f)) - STROKE_HALF_WIDTH),
            };
        }

        return _symbolSprites[index];
    }

    // Distance functions return how far a point is from the shape's edge in pixels, negative inside it.
    private static float RoundedSquare(Vector2 p)
    {
        const float half = 56f;
        const float radius = 22f;

        Vector2 q = new(Mathf.Abs(p.x) - (half - radius), Mathf.Abs(p.y) - (half - radius));
        return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
    }

    private static float Box(Vector2 p, float half)
    {
        Vector2 q = new(Mathf.Abs(p.x) - half, Mathf.Abs(p.y) - half);
        return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f);
    }

    private static float Cross(Vector2 p)
    {
        float extent = SYMBOL_HALF_EXTENT;
        float distance = Mathf.Min(
            DistanceToSegment(p, new Vector2(-extent, -extent), new Vector2(extent, extent)),
            DistanceToSegment(p, new Vector2(-extent, extent), new Vector2(extent, -extent)));
        return distance - STROKE_HALF_WIDTH;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 pa = p - a;
        Vector2 ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude;
    }

    // Signed distance to an upward pointing equilateral triangle with the given half side length.
    private static float Triangle(Vector2 p, float r)
    {
        float k = Mathf.Sqrt(3f);
        p.x = Mathf.Abs(p.x) - r;
        p.y += r / k;
        if (p.x + k * p.y > 0f)
        {
            p = new Vector2(p.x - k * p.y, -k * p.x - p.y) / 2f;
        }

        p.x -= Mathf.Clamp(p.x, -2f * r, 0f);
        return -p.magnitude * Mathf.Sign(p.y);
    }

    private static Sprite Build(Func<Vector2, float> distance)
    {
        Texture2D texture = new(SIZE, SIZE, TextureFormat.RGBA32, mipChain: false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };

        Color32[] pixels = new Color32[SIZE * SIZE];
        for (int y = 0; y < SIZE; y++)
        {
            for (int x = 0; x < SIZE; x++)
            {
                // Sample at the pixel's center and fade over one pixel for a smooth edge.
                Vector2 point = new(x + 0.5f - HALF_SIZE, y + 0.5f - HALF_SIZE);
                byte alpha = (byte)(Mathf.Clamp01(0.5f - distance(point)) * 255f);
                pixels[y * SIZE + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, SIZE, SIZE), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
