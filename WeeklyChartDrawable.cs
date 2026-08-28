using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;

namespace JumpRopeApp;

public class WeeklyChartDrawable : IDrawable
{
    public List<(string DayName, int Jumps)> Data { get; set; } = new();

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.Antialias = true;
        if (Data == null || Data.Count == 0) return;

        float paddingLeft = 10;
        float paddingRight = 10;
        float paddingTop = 20;
        float paddingBottom = 25;

        float width = dirtyRect.Width - paddingLeft - paddingRight;
        float height = dirtyRect.Height - paddingTop - paddingBottom;

        int maxJumps = 1;
        foreach (var item in Data)
        {
            if (item.Jumps > maxJumps) maxJumps = item.Jumps;
        }

        float barWidth = (width / Data.Count) - 12;
        float x = paddingLeft + 6;

        for (int i = 0; i < Data.Count; i++)
        {
            var item = Data[i];
            float barHeight = (float)item.Jumps / maxJumps * height;
            float y = dirtyRect.Height - paddingBottom - barHeight;

            // Draw Bar Background Container
            canvas.FillColor = Color.FromArgb("#1E293B");
            canvas.FillRoundedRectangle(x, paddingTop, barWidth, height, 6);

            // Draw Active Bar Value
            if (barHeight > 0)
            {
                canvas.FillColor = Color.FromArgb("#6366F1");
                canvas.FillRoundedRectangle(x, y, barWidth, barHeight, 6);
            }

            // Draw Day Text Label
            canvas.FontColor = Color.FromArgb("#94A3B8");
            canvas.FontSize = 11;
            canvas.DrawString(item.DayName, x + (barWidth / 2), dirtyRect.Height - 5, HorizontalAlignment.Center);

            x += barWidth + 12;
        }
    }
}