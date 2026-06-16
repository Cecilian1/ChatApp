using System.Security.Cryptography;
using System.Text;

namespace ChatApp.Shared.Utils;

/// <summary>
/// 统一头像 SVG 生成器，Web 与桌面端共用同一算法。
/// </summary>
public static class AvatarGenerator
{
    public static string GenerateSvg(string? seed, bool isGroup = false) =>
        isGroup ? GenerateGroupAvatar(seed) : GenerateFaceAvatar(seed);

    private static string GenerateFaceAvatar(string? seed)
    {
        seed = NormalizeSeed(seed);
        var h = HashBytes(seed);

        var bgHue = h[0] % 360;
        var hairHue = h[1] % 360;
        var skinLight = 72 + h[2] % 18;
        var eyeY = 46 + h[3] % 5;
        var eyeOffset = 11 + h[4] % 4;
        var hasGlasses = h[5] % 4 == 0;
        var hasSmile = h[6] % 3 != 0;
        var hairStyle = h[7] % 3;

        var bg = $"hsl({bgHue},35%,88%)";
        var hair = $"hsl({hairHue},55%,38%)";
        var skin = $"hsl(28,45%,{skinLight}%)";
        var shirt = $"hsl({(bgHue + 40) % 360},45%,55%)";

        var sb = new StringBuilder();
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 100 100\">");
        sb.Append($"<rect width=\"100\" height=\"100\" fill=\"{bg}\"/>");
        sb.Append($"<rect x=\"0\" y=\"72\" width=\"100\" height=\"28\" fill=\"{shirt}\"/>");

        AppendHair(sb, hairStyle, hair);
        sb.Append($"<circle cx=\"50\" cy=\"52\" r=\"26\" fill=\"{skin}\"/>");
        sb.Append($"<circle cx=\"{50 - eyeOffset}\" cy=\"{eyeY}\" r=\"3.2\" fill=\"#2d3436\"/>");
        sb.Append($"<circle cx=\"{50 + eyeOffset}\" cy=\"{eyeY}\" r=\"3.2\" fill=\"#2d3436\"/>");
        sb.Append($"<circle cx=\"{50 - eyeOffset + 1}\" cy=\"{eyeY - 1}\" r=\"1\" fill=\"#fff\" opacity=\"0.8\"/>");
        sb.Append($"<circle cx=\"{50 + eyeOffset + 1}\" cy=\"{eyeY - 1}\" r=\"1\" fill=\"#fff\" opacity=\"0.8\"/>");

        if (hasGlasses)
        {
            sb.Append($"<circle cx=\"{50 - eyeOffset}\" cy=\"{eyeY}\" r=\"7\" fill=\"none\" stroke=\"#2d3436\" stroke-width=\"1.5\"/>");
            sb.Append($"<circle cx=\"{50 + eyeOffset}\" cy=\"{eyeY}\" r=\"7\" fill=\"none\" stroke=\"#2d3436\" stroke-width=\"1.5\"/>");
            sb.Append($"<line x1=\"{50 - eyeOffset + 7}\" y1=\"{eyeY}\" x2=\"{50 + eyeOffset - 7}\" y2=\"{eyeY}\" stroke=\"#2d3436\" stroke-width=\"1.5\"/>");
        }

        if (hasSmile)
            sb.Append("<path d=\"M40 60 Q50 68 60 60\" fill=\"none\" stroke=\"#c0392b\" stroke-width=\"2\" stroke-linecap=\"round\"/>");
        else
            sb.Append("<line x1=\"42\" y1=\"63\" x2=\"58\" y2=\"63\" stroke=\"#c0392b\" stroke-width=\"2\" stroke-linecap=\"round\"/>");

        if (h[8] % 5 == 0)
            sb.Append($"<ellipse cx=\"50\" cy=\"58\" rx=\"4\" ry=\"3\" fill=\"hsl(0,40%,{68 + h[9] % 10}%)\" opacity=\"0.35\"/>");

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static void AppendHair(StringBuilder sb, int style, string hair)
    {
        switch (style)
        {
            case 0:
                sb.Append($"<ellipse cx=\"50\" cy=\"30\" rx=\"34\" ry=\"24\" fill=\"{hair}\"/>");
                break;
            case 1:
                sb.Append($"<path d=\"M16 48 Q18 8 50 6 Q82 8 84 48 Q72 22 50 18 Q28 22 16 48 Z\" fill=\"{hair}\"/>");
                break;
            default:
                sb.Append($"<ellipse cx=\"50\" cy=\"28\" rx=\"30\" ry=\"20\" fill=\"{hair}\"/>");
                sb.Append($"<rect x=\"18\" y=\"24\" width=\"64\" height=\"18\" fill=\"{hair}\"/>");
                break;
        }
    }

    private static string GenerateGroupAvatar(string? seed)
    {
        seed = NormalizeSeed(seed);
        var h = HashBytes(seed);
        var hue = h[0] % 360;
        var bg = $"hsl({hue},30%,90%)";
        var c1 = $"hsl({hue},55%,50%)";
        var c2 = $"hsl({(hue + 40) % 360},55%,45%)";
        var c3 = $"hsl({(hue + 80) % 360},55%,40%)";

        var sb = new StringBuilder();
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 100 100\">");
        sb.Append($"<rect width=\"100\" height=\"100\" fill=\"{bg}\"/>");
        sb.Append($"<circle cx=\"35\" cy=\"40\" r=\"16\" fill=\"{c1}\"/>");
        sb.Append($"<circle cx=\"65\" cy=\"40\" r=\"16\" fill=\"{c2}\"/>");
        sb.Append($"<circle cx=\"50\" cy=\"62\" r=\"18\" fill=\"{c3}\"/>");
        sb.Append("<circle cx=\"35\" cy=\"37\" r=\"2.5\" fill=\"#fff\" opacity=\"0.7\"/>");
        sb.Append("<circle cx=\"65\" cy=\"37\" r=\"2.5\" fill=\"#fff\" opacity=\"0.7\"/>");
        sb.Append("<circle cx=\"50\" cy=\"59\" r=\"2.5\" fill=\"#fff\" opacity=\"0.7\"/>");
        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string NormalizeSeed(string? seed) =>
        string.IsNullOrWhiteSpace(seed) ? "default" : seed.Trim();

    private static byte[] HashBytes(string seed) => SHA256.HashData(Encoding.UTF8.GetBytes(seed));
}
