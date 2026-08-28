using Microsoft.Maui.Graphics;

namespace JumpRopeApp;

public class TimerRingDrawable : IDrawable
{
    public double Progress { get; set; } = 1.0; // 0.0 to 1.0
    public bool IsRestMode { get; set; } = false;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.Antialias = true;

        float width = dirtyRect.Width;
        float height = dirtyRect.Height;
        float strokeWidth = 14;

        float radius = (Math.Min(width, height) - strokeWidth) / 2;
        float centerX = width / 2;
        float centerY = height / 2;

        // Draw Track Background
        canvas.StrokeColor = Color.FromArgb("#1E293B");
        canvas.StrokeSize = strokeWidth;
        canvas.DrawCircle(centerX, centerY, radius);

        // Draw Active Ring Arc
        if (Progress > 0)
        {
            canvas.StrokeColor = IsRestMode ? Color.FromArgb("#F59E0B") : Color.FromArgb("#6366F1");
            canvas.StrokeSize = strokeWidth;
            canvas.StrokeLineCap = LineCap.Round;

            float endAngle = (float)(90 - (360 * Progress));
            canvas.DrawArc(
                centerX - radius,
                centerY - radius,
                radius * 2,
                radius * 2,
                90,
                endAngle,
                false,
                false
            );
        }
    }
}