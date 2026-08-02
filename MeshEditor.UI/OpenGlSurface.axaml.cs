using Avalonia.Controls;
using Avalonia.Input;

namespace MeshEditor.UI;

public partial class OpenGlSurface : UserControl
{
    public OpenGlSurface()
    {
        InitializeComponent();
        SizeChanged += (_, _) => UpdateViewportSize();

        PointerPressed += Viewport_PointerPressed;
        PointerMoved += Viewport_PointerMoved;
        PointerReleased += Viewport_PointerReleased;
        PointerWheelChanged += Viewport_PointerWheelChanged;
    }
}
