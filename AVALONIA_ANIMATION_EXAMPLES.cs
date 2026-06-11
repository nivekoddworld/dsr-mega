/// <summary>
/// Practical Avalonia Animation Code Examples
/// Use these as templates for smooth sliding and transition animations in your UI
/// </summary>

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Transformation;
using System;
using System.Threading.Tasks;

namespace AvaloniaAnimationExamples
{
    // ============================================================================
    // EXAMPLE 1: Basic Slide-In Panel (0.4s)
    // ============================================================================

    public class BasicSlideInExample
    {
        /// <summary>
        /// XAML Setup (in your Window/UserControl):
        /// <Panel Name="SlidePanel" Opacity="0">
        ///   <Panel.RenderTransform>
        ///     <TranslateTransform X="300" />
        ///   </Panel.RenderTransform>
        ///   <Panel.Transitions>
        ///     <Transitions>
        ///       <DoubleTransition Property="Opacity" Duration="0:0:0.4" Easing="CubicEaseOut" />
        ///       <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.4" Easing="CubicEaseOut" />
        ///     </Transitions>
        ///   </Panel.Transitions>
        /// </Panel>
        /// </summary>

        private Panel slidePanel;

        public void InitializePanel(Window window)
        {
            slidePanel = window.FindControl<Panel>("SlidePanel");
        }

        public void ShowPanel()
        {
            // Trigger both animations simultaneously
            slidePanel.Opacity = 1;  // Fade in
            slidePanel.RenderTransform = new TranslateTransform(0, 0);  // Slide to position
        }

        public async void HidePanel()
        {
            slidePanel.Opacity = 0;  // Fade out
            slidePanel.RenderTransform = new TranslateTransform(300, 0);  // Slide back right

            // Optional: wait for animation to complete
            await Task.Delay(400);
            slidePanel.IsVisible = false;
        }
    }

    // ============================================================================
    // EXAMPLE 2: Vertical Slide-In (From Top)
    // ============================================================================

    public class VerticalSlideInExample
    {
        /// <summary>
        /// XAML (slides in from top):
        /// <Border Name="TopPanel" Opacity="0">
        ///   <Border.RenderTransform>
        ///     <TranslateTransform X="0" Y="-100" />
        ///   </Border.RenderTransform>
        ///   <Border.Transitions>
        ///     <Transitions>
        ///       <DoubleTransition Property="Opacity" Duration="0:0:0.3" />
        ///       <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.3" Easing="CubicEaseOut" />
        ///     </Transitions>
        ///   </Border.Transitions>
        /// </Border>
        /// </summary>

        private Border topPanel;

        public void InitializePanel(Window window)
        {
            topPanel = window.FindControl<Border>("TopPanel");
        }

        public void SlideInFromTop()
        {
            topPanel.Opacity = 1;
            topPanel.RenderTransform = new TranslateTransform(0, 0);  // Y: -100 -> 0
        }

        public void SlideOutToTop()
        {
            topPanel.Opacity = 0;
            topPanel.RenderTransform = new TranslateTransform(0, -100);
        }
    }

    // ============================================================================
    // EXAMPLE 3: Fade + Scale Pop (Emphasis Effect)
    // ============================================================================

    public class FadeAndScaleExample
    {
        /// <summary>
        /// XAML (scales from 0.8 while fading in):
        /// <Border Name="PopupPanel" Opacity="0">
        ///   <Border.RenderTransform>
        ///     <ScaleTransform ScaleX="0.8" ScaleY="0.8" />
        ///   </Border.RenderTransform>
        ///   <Border.Transitions>
        ///     <Transitions>
        ///       <DoubleTransition Property="Opacity" Duration="0:0:0.3" Easing="CubicEaseOut" />
        ///       <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.3" Easing="BackEaseOut" />
        ///     </Transitions>
        ///   </Border.Transitions>
        /// </Border>
        /// </summary>

        private Border popupPanel;

        public void InitializePanel(Window window)
        {
            popupPanel = window.FindControl<Border>("PopupPanel");
        }

        public void ShowWithPop()
        {
            popupPanel.Opacity = 1;
            popupPanel.RenderTransform = new ScaleTransform(1, 1);  // 0.8 -> 1.0
        }

        public void HideWithShrink()
        {
            popupPanel.Opacity = 0;
            popupPanel.RenderTransform = new ScaleTransform(0.8, 0.8);
        }
    }

    // ============================================================================
    // EXAMPLE 4: Multi-Property Animation (Slide + Rotate)
    // ============================================================================

    public class SlideAndRotateExample
    {
        /// <summary>
        /// XAML (slides while rotating):
        /// <Border Name="RotatingPanel" Opacity="0">
        ///   <Border.RenderTransform>
        ///     <TransformGroup>
        ///       <TranslateTransform X="300" Y="0" />
        ///       <RotateTransform Angle="0" />
        ///     </TransformGroup>
        ///   </Border.RenderTransform>
        ///   <Border.Transitions>
        ///     <Transitions>
        ///       <DoubleTransition Property="Opacity" Duration="0:0:0.4" />
        ///       <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.4" Easing="CubicEaseOut" />
        ///     </Transitions>
        ///   </Border.Transitions>
        /// </Border>
        /// </summary>

        private Border rotatingPanel;

        public void InitializePanel(Window window)
        {
            rotatingPanel = window.FindControl<Border>("RotatingPanel");
        }

        public void ShowWithRotation()
        {
            var group = new TransformGroup();
            group.Children.Add(new TranslateTransform(0, 0));
            group.Children.Add(new RotateTransform(360));  // Rotate 360 degrees

            rotatingPanel.Opacity = 1;
            rotatingPanel.RenderTransform = group;
        }

        public void ResetRotation()
        {
            var group = new TransformGroup();
            group.Children.Add(new TranslateTransform(300, 0));
            group.Children.Add(new RotateTransform(0));

            rotatingPanel.Opacity = 0;
            rotatingPanel.RenderTransform = group;
        }
    }

    // ============================================================================
    // EXAMPLE 5: Data-Binding Driven Animation
    // ============================================================================

    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class AnimatedViewModel : INotifyPropertyChanged
    {
        private double _panelOpacity;
        private double _slideX;
        private bool _isPanelVisible;

        public double PanelOpacity
        {
            get => _panelOpacity;
            set { if (_panelOpacity != value) { _panelOpacity = value; OnPropertyChanged(); } }
        }

        public double SlideX
        {
            get => _slideX;
            set { if (_slideX != value) { _slideX = value; OnPropertyChanged(); } }
        }

        public bool IsPanelVisible
        {
            get => _isPanelVisible;
            set { if (_isPanelVisible != value) { _isPanelVisible = value; OnPropertyChanged(); } }
        }

        public void ShowPanel()
        {
            IsPanelVisible = true;
            PanelOpacity = 1;
            SlideX = 0;  // Property changes trigger transitions
        }

        public async void HidePanel()
        {
            PanelOpacity = 0;
            SlideX = 300;

            await Task.Delay(400);
            IsPanelVisible = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // ============================================================================
    // EXAMPLE 6: Staggered Animation (Multiple Panels)
    // ============================================================================

    public class StaggeredAnimationExample
    {
        private Border[] panels;

        public void InitializePanels(Window window)
        {
            panels = new[]
            {
                window.FindControl<Border>("Panel1"),
                window.FindControl<Border>("Panel2"),
                window.FindControl<Border>("Panel3")
            };
        }

        public async void ShowAllWithStagger()
        {
            for (int i = 0; i < panels.Length; i++)
            {
                await Task.Delay(100);  // 100ms stagger between each

                panels[i].Opacity = 1;
                panels[i].RenderTransform = new TranslateTransform(0, 0);
            }
        }

        public async void HideAllWithStagger()
        {
            for (int i = panels.Length - 1; i >= 0; i--)
            {
                panels[i].Opacity = 0;
                panels[i].RenderTransform = new TranslateTransform(300, 0);

                await Task.Delay(100);
            }
        }
    }

    // ============================================================================
    // EXAMPLE 7: Controlled Animation with Duration Variables
    // ============================================================================

    public class CustomDurationExample
    {
        /// <summary>
        /// XAML (with parameterizable duration):
        /// <Border Name="CustomPanel" Opacity="0">
        ///   <Border.RenderTransform>
        ///     <TranslateTransform X="300" />
        ///   </Border.RenderTransform>
        ///   <Border.Transitions>
        ///     <Transitions>
        ///       <DoubleTransition
        ///         Property="Opacity"
        ///         Duration="{Binding AnimationDuration}"
        ///         Easing="CubicEaseOut" />
        ///       <TransformOperationsTransition
        ///         Property="RenderTransform"
        ///         Duration="{Binding AnimationDuration}"
        ///         Easing="CubicEaseOut" />
        ///     </Transitions>
        ///   </Border.Transitions>
        /// </Border>
        /// </summary>

        private Border customPanel;
        private TimeSpan _animationDuration = TimeSpan.FromMilliseconds(300);

        public TimeSpan AnimationDuration
        {
            get => _animationDuration;
            set => _animationDuration = value;
        }

        public void InitializePanel(Window window)
        {
            customPanel = window.FindControl<Border>("CustomPanel");
        }

        public void ShowWithCustomDuration(int millisecondsDelay)
        {
            AnimationDuration = TimeSpan.FromMilliseconds(millisecondsDelay);

            customPanel.Opacity = 1;
            customPanel.RenderTransform = new TranslateTransform(0, 0);
        }
    }

    // ============================================================================
    // EXAMPLE 8: Animation with Completion Callback
    // ============================================================================

    public class AnimationWithCallbackExample
    {
        private Border panel;

        public void InitializePanel(Window window)
        {
            panel = window.FindControl<Border>("CallbackPanel");
        }

        public async Task ShowPanelAsync()
        {
            panel.Opacity = 1;
            panel.RenderTransform = new TranslateTransform(0, 0);

            // Wait for animation (400ms)
            await Task.Delay(400);

            // Animation complete - do something
            OnAnimationComplete();
        }

        public async Task HidePanelAsync()
        {
            panel.Opacity = 0;
            panel.RenderTransform = new TranslateTransform(300, 0);

            await Task.Delay(400);

            panel.IsVisible = false;
            OnAnimationComplete();
        }

        private void OnAnimationComplete()
        {
            // Handle post-animation logic
            // Enable buttons, update state, etc.
        }
    }

    // ============================================================================
    // EXAMPLE 9: Easing Function Comparison
    // ============================================================================

    public class EasingComparisonExample
    {
        /// <summary>
        /// XAML variations with different easing:
        ///
        /// <!-- Linear (constant speed) -->
        /// <DoubleTransition Property="Opacity" Duration="0:0:0.3" Easing="Linear" />
        ///
        /// <!-- CubicEaseOut (smooth deceleration - RECOMMENDED) -->
        /// <DoubleTransition Property="Opacity" Duration="0:0:0.3" Easing="CubicEaseOut" />
        ///
        /// <!-- BackEaseOut (slight overshoot) -->
        /// <DoubleTransition Property="Opacity" Duration="0:0:0.3" Easing="BackEaseOut" />
        ///
        /// <!-- ElasticEaseOut (spring effect) -->
        /// <DoubleTransition Property="Opacity" Duration="0:0:0.3" Easing="ElasticEaseOut" />
        ///
        /// <!-- BounceEaseOut (bounce) -->
        /// <DoubleTransition Property="Opacity" Duration="0:0:0.3" Easing="BounceEaseOut" />
        /// </summary>

        public void ShowEasingComparison()
        {
            // Linear: 300ms, no easing
            // Result: Constant, robotic motion

            // CubicEaseOut: 300ms, smooth deceleration
            // Result: Feels natural, recommended for UI

            // BackEaseOut: 300ms, slight overshoot
            // Result: Dynamic, playful, good for emphasis

            // ElasticEaseOut: 300ms, spring bounce
            // Result: Energetic, springy

            // BounceEaseOut: 300ms, multiple bounces
            // Result: Very playful, use sparingly
        }
    }

    // ============================================================================
    // EXAMPLE 10: Complete Real-World Implementation
    // ============================================================================

    public class RealWorldSidebarExample
    {
        private Border sidebar;
        private Button toggleButton;
        private bool isSidebarOpen = false;

        public void Initialize(Window window)
        {
            sidebar = window.FindControl<Border>("Sidebar");
            toggleButton = window.FindControl<Button>("ToggleButton");

            toggleButton.Click += (s, e) => ToggleSidebar();
        }

        private void ToggleSidebar()
        {
            if (isSidebarOpen)
                CloseSidebar();
            else
                OpenSidebar();
        }

        private void OpenSidebar()
        {
            isSidebarOpen = true;

            // Sidebar XAML initial state: Opacity="0", X="300"
            // Change properties to trigger 0.4s transition with CubicEaseOut
            sidebar.Opacity = 1;
            sidebar.RenderTransform = new TranslateTransform(0, 0);

            // Update button text/state
            toggleButton.Content = "Close";
        }

        private async void CloseSidebar()
        {
            isSidebarOpen = false;

            // Animate out
            sidebar.Opacity = 0;
            sidebar.RenderTransform = new TranslateTransform(300, 0);

            // Wait for animation
            await Task.Delay(400);

            toggleButton.Content = "Open";
            // sidebar.IsVisible = false;  // Optional: hide from layout
        }
    }

    // ============================================================================
    // EXAMPLE XAML TEMPLATE (Copy and adapt)
    // ============================================================================

    /*
    <Window xmlns="https://github.com/avaloniaui"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            x:Class="MyApp.MainWindow"
            Title="Smooth Animations"
            Width="800" Height="600">

      <DockPanel>
        <!-- Control Button -->
        <StackPanel DockPanel.Dock="Top" Padding="10" Spacing="10">
          <Button Name="ToggleButton" Content="Show Panel" Click="OnToggleClick" />
        </StackPanel>

        <!-- Animated Sidebar Panel -->
        <Border
          Name="SidePanel"
          Width="300"
          Background="#2A2A2A"
          Opacity="0"
          DockPanel.Dock="Right">

          <!-- RenderTransform for smooth sliding -->
          <Border.RenderTransform>
            <TranslateTransform X="300" Y="0" />
          </Border.RenderTransform>

          <!-- Define transitions -->
          <Border.Transitions>
            <Transitions>
              <!-- Fade in/out over 0.4 seconds -->
              <DoubleTransition
                Property="Opacity"
                Duration="0:0:0.4"
                Easing="CubicEaseOut" />

              <!-- Slide horizontally over 0.4 seconds -->
              <TransformOperationsTransition
                Property="RenderTransform"
                Duration="0:0:0.4"
                Easing="CubicEaseOut" />
            </Transitions>
          </Border.Transitions>

          <!-- Panel Content -->
          <StackPanel Padding="20" Spacing="15">
            <TextBlock
              Text="Settings"
              FontSize="20"
              Foreground="White" />
            <Button Content="Option 1" />
            <Button Content="Option 2" />
            <Button Content="Option 3" />
          </StackPanel>
        </Border>

        <!-- Main Content -->
        <TextBlock
          DockPanel.Dock="Left"
          Foreground="Black"
          Text="Main content area"
          VerticalAlignment="Center"
          HorizontalAlignment="Center" />
      </DockPanel>
    </Window>
    */
}
