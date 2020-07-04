using Shadowsocks.LipP2P;
using Shadowsocks.Properties;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace LimitlessUI
{
    public class Button_WOC : Control
    {
        private Color _borderColor = Color.Silver;
        private Color _onHoverBorderColor = Color.Gray;
        private Color _buttonColor = Color.Red;
        private Color _onHoverButtonColor = Color.Yellow;
        private Color _textColor = Color.Black;
        private Color _onHoverTextColor = Color.Gray;

        private bool _isHovering;
        private int _borderThickness = 10;
       // private FlatButton_WOC flatButton_WOC1;
        private int _borderThicknessByTwo = 5;


        public enum StyleEnum
        {
            Style1,
            Style2,
            Style3,
            Style4,
            None
        }

        private StyleEnum _style = StyleEnum.Style3;
        private Font _font1, _font2, _font3;
        private int _angle = 360;
        private Point _offset = new Point(0, 0);
        private bool _ignoreHeight = true;

        private float _progress = 50;
        private float _line1Thinkness = 5;
        private float _line2Thinkness = 9;

        private Color _color1 = Color.Silver;
        private Color _color2 = Color.FromArgb(86, 224, 208);
        private Color _text1Color = DefaultForeColor;
        private Color _text2Color = DefaultForeColor;
        private Color _text3Color = DefaultForeColor;

        private bool _enabled = true;
        private SynchronizationContext _syncContext = null;
        private Thread progressThread = null;

        public Button_WOC()
        {
            DoubleBuffered = true;
            MouseEnter += (sender, e) =>
            {
                _isHovering = true;
                Invalidate();
            };
            MouseLeave += (sender, e) =>
            {
                _isHovering = false;
                Invalidate();
            };

            _font1 = Font;
            _font2 = Font;
            _font3 = Font;
            _syncContext = SynchronizationContext.Current;
            progressThread = new Thread(() =>
            {
                while (_enabled)
                {
                    if (P2pLib.GetInstance().connectStarted || P2pLib.GetInstance().disConnectStarted)
                    {
                        _progress += 2;
                        if (_progress >= 360)
                        {
                            _progress = 0;
                        }

                        _syncContext.Post(ThreadRefresh, null);
                    }
                    Thread.Sleep(10);
                }
            });
            progressThread.IsBackground = true;
            progressThread.Start();
        }

        public void ThreadRefresh(object obj)
        {
            Refresh();
        }

        ~Button_WOC()
        {
            _enabled = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (P2pLib.GetInstance().connectSuccess)
            {
                _borderColor = Color.FromArgb(0, 184, 170);
                _onHoverBorderColor = Color.FromArgb(86, 224, 208);
                _buttonColor = Color.FromArgb(3, 134, 120);
                _onHoverButtonColor = Color.FromArgb(0, 184, 170);
                _textColor = System.Drawing.SystemColors.ButtonHighlight;
                _onHoverTextColor = System.Drawing.SystemColors.ButtonHighlight;
            }
            else
            {
                _borderColor = System.Drawing.Color.WhiteSmoke;
                _onHoverBorderColor = System.Drawing.Color.Gainsboro;
                _buttonColor = System.Drawing.Color.LightGray;
                _onHoverButtonColor = System.Drawing.Color.Silver;
                _textColor = System.Drawing.SystemColors.ButtonHighlight;
                _onHoverTextColor = System.Drawing.SystemColors.ButtonHighlight;
            }
            if (P2pLib.GetInstance().connectStarted || P2pLib.GetInstance().disConnectStarted)
            {
                _isHovering = false;
            }

            base.OnPaint(e);
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Brush brush = new SolidBrush(_isHovering ? _onHoverBorderColor : _borderColor);

                //Border
                //g.FillEllipse(brush, 0, 0, Height - 2, Height - 2);
                g.FillEllipse(brush, Width - Height + 1, 1, Height - 2, Height - 2);
                //g.FillRectangle(brush, Height / 2, 0, Width - Height, Height);

                brush.Dispose();
                brush = new SolidBrush(_isHovering ? _onHoverButtonColor : _buttonColor);
                                
                //Inner part. Button itself
                g.FillEllipse(brush, _borderThicknessByTwo, _borderThicknessByTwo, Height - _borderThickness,
                    Height - _borderThickness);
                g.FillEllipse(brush, (Width - Height) + _borderThicknessByTwo, _borderThicknessByTwo,
                    Height - _borderThickness, Height - _borderThickness);
                g.FillRectangle(brush, Height / 2 + _borderThicknessByTwo, _borderThicknessByTwo,
                    Width - Height - _borderThickness, Height - _borderThickness);

                brush.Dispose();
                brush = new SolidBrush(_isHovering ? _onHoverTextColor : _textColor);
                //Button Text
                RectangleF rect = new RectangleF(Width / 2 - 35, Height / 2 - 55, 70, 70);
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                if (P2pLib.GetInstance().connectSuccess)
                {
                    Text = "Connected";
                    SizeF stringSize = g.MeasureString(Text, _font1);
                    g.DrawString(Text, _font1, brush, (Width - stringSize.Width) / 2, 48 + (Height - stringSize.Height) / 2);
                    e.Graphics.DrawImage(Resources.connected, rect);
                }
                else
                {
                    Text = "Connect";
                    SizeF stringSize = g.MeasureString(Text, _font1);
                    g.DrawString(Text, _font1, brush, (Width - stringSize.Width) / 2, 48 + (Height - stringSize.Height) / 2);
                    e.Graphics.DrawImage(Resources.connect, rect);
                }
            }
            
            if (P2pLib.GetInstance().connectStarted || P2pLib.GetInstance().disConnectStarted)
            {
                var pen1 = new Pen(_color1, _line1Thinkness);
                var pen2 = new Pen(Color.FromArgb(0, 114, 108), _line1Thinkness);

                var circleRadius = _ignoreHeight
                    ? (Width - ProgressLineThikness)
                    : (Width > Height ? (Height - ProgressLineThikness) : (Width - ProgressLineThikness));
                var radiusByTwo = circleRadius / 2;
                var progressEndAngle = (_angle) / 100F;

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TranslateTransform(_line2Thinkness / 2F + radiusByTwo, _line2Thinkness / 2F + radiusByTwo);
                e.Graphics.RotateTransform((360 - _angle) / 2 + 90);

                if(P2pLib.GetInstance().disConnectStarted)
                {
                    e.Graphics.DrawArc(pen2, -radiusByTwo, -radiusByTwo, circleRadius, circleRadius, _progress, 30);

                }
                else
                {
                    e.Graphics.DrawArc(pen1, -radiusByTwo, -radiusByTwo, circleRadius, circleRadius, _progress, 30);
                }
                pen1.Dispose();
                pen2.Dispose();
            }
        }

        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                Invalidate();
            }
        }

        public Color OnHoverBorderColor
        {
            get => _onHoverBorderColor;
            set
            {
                _onHoverBorderColor = value;
                Invalidate();
            }
        }

        public Color ButtonColor
        {
            get => _buttonColor;
            set
            {
                _buttonColor = value;
                Invalidate();
            }
        }

        public Color OnHoverButtonColor
        {
            get => _onHoverButtonColor;
            set
            {
                _onHoverButtonColor = value;
                Invalidate();
            }
        }

        public Color TextColor
        {
            get => _textColor;
            set
            {
                _textColor = value;
                Invalidate();
            }
        }

        public Color OnHoverTextColor
        {
            get => _onHoverTextColor;
            set
            {
                _onHoverTextColor = value;
                Invalidate();
            }
        }


        private void InitializeComponent()
        {
            // this.flatButton_WOC1 = new LimitlessUI.FlatButton_WOC();
            this.SuspendLayout();
            // 
            // flatButton_WOC1
            // 
            // Button_WOC
            // 
            this.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ResumeLayout(false);

        }

        #region Getters and Setters

        public bool IgnoreHeight
        {
            get => _ignoreHeight;
            set
            {
                _ignoreHeight = value;
                Invalidate();
            }
        }

        public Point Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                Invalidate();
            }
        }

        public StyleEnum Style
        {
            get => _style;
            set
            {
                _style = value;
                Invalidate();
            }
        }

        public Font Font1
        {
            get => _font1;
            set
            {
                _font1 = value;
                Invalidate();
            }
        }

        public Font Font2
        {
            get => _font2;
            set
            {
                _font2 = value;
                Invalidate();
            }
        }

        public Font Font3
        {
            get => _font3;
            set
            {
                _font3 = value;
                Invalidate();
            }
        }

        public int Angle
        {
            get => _angle;
            set
            {
                _angle = value;
                Invalidate();
            }
        }

        public float Value
        {
            get => _progress;
            set
            {
                if (value != _progress)
                {
                    _progress = value;
                    Invalidate();
                }
            }
        }

        public float BackLineThikness
        {
            get => _line1Thinkness;
            set
            {
                _line1Thinkness = value;
                Invalidate();
            }
        }

        public float ProgressLineThikness
        {
            get => _line2Thinkness;
            set
            {
                _line2Thinkness = value;
                Invalidate();
            }
        }

        public Color ProgressBackColor
        {
            get => _color1;
            set
            {
                _color1 = value;
                Invalidate();
            }
        }

        public Color ProgressColor
        {
            get => _color2;
            set
            {
                _color2 = value;
                Invalidate();
            }
        }

        public Color Text1Color
        {
            get => _text1Color;
            set
            {
                _text1Color = value;
                Invalidate();
            }
        }

        public Color Text2Color
        {
            get => _text2Color;
            set
            {
                _text2Color = value;
                Invalidate();
            }
        }

        public Color Text3Color
        {
            get => _text3Color;
            set
            {
                _text3Color = value;
                Invalidate();
            }
        }

        #endregion
    }
}