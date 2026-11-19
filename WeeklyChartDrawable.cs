using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JumpRopeApp;

public class WeeklyChartDrawable : IDrawable
{
    public List<string> DayLabels { get; set; } = new();
    public List<int> DailyTotals { get; set; } = new();

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (DailyTotals == null || DailyTotals.Count == 0) return;

        canvas.SaveState();
        canvas.Antialias = true;

        float width = dirtyRect.Width;
        float height = dirtyRect.Height;
        float padding = 24f;
        float chartBottom = height - padding;
        float chartHeight = height - (padding * 2);

        int maxVal = DailyTotals.Max();
        if (maxVal == 0) maxVal = 100;

        float stepX = (width - (padding * 2)) / Math.Max(1, DailyTotals.Count - 1);

        // Draw background grid lines
        canvas.StrokeColor = Color.FromArgb("#334155");
        canvas.StrokeSize = 1;
        canvas.DrawLine(padding, chartBottom, width - padding, chartBottom);

        // Plot bar points
        for (int i = 0; i < DailyTotals.Count; i++)
        {
            float x = padding + (i * stepX);
            float barHeight = (float)DailyTotals[i] / maxVal * chartHeight;
            float y = chartBottom - barHeight;

            // Bar background
            canvas.FillColor = Color.FromArgb("#6366F1");
            canvas.FillRoundedRectangle(x - 8, y, 16, barHeight, 4);

            // Day label
            if (i < DayLabels.Count)
            {
                canvas.FontColor = Color.FromArgb("#94A3B8");
                canvas.FontSize = 10;
                canvas.DrawString(DayLabels[i], x - 15, chartBottom + 4, 30, 16, HorizontalAlignment.Center, VerticalAlignment.Top);
            }
        }

        canvas.RestoreState();
    }
}