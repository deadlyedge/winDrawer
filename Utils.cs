namespace winDrawer
{
  public class TransparentScrollBar : VScrollBar
  {
    protected override void OnPaint(PaintEventArgs pe)
    {
      base.OnPaint(pe);
      // 全部透明
      pe.Graphics.FillRectangle(new SolidBrush(Color.Transparent), this.ClientRectangle);
      // 滑块自定义
      Rectangle thumbRect = new Rectangle(
          0, this.Value, this.Width, 30 /* 高度可自定义 */
      );
      using (SolidBrush brush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
      {
        pe.Graphics.FillRectangle(brush, thumbRect);
      }
    }
  }

}