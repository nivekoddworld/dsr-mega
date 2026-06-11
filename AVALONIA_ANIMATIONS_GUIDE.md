# Avalonia Smooth Sliding & Transition Animations Guide

## Overview

Avalonia provides two primary mechanisms for creating smooth animations:

1. **Transitions** (Property-based) - CSS-inspired, automatic animations when properties change
2. **Keyframe Animations** (Timeline-based) - Explicit animations with multiple keyframes
3. **RenderTransform** (Performance-optimized) - Transform animations that don't trigger layout recalculation

## 1. Best Practices for Animating Visibility/Appearance

### Core Principle: Opacity vs IsVisible

**Don't:**
```csharp
panel.IsVisible = false;  // Immediate - no animation
```

**Do:**
```csharp
// Method 1: Fade out then hide
panel.Opacity = 0;  // Animate to transparent
await Task.Delay(300);  // Wait for animation
panel.IsVisible = false;  // Then hide

// Method 2: Show then fade in
panel.IsVisible = true;  // Show immediately
panel.Opacity = 1;  // Animate opacity (triggers Transition)
```

### Key Insight
- **Transitions don't animate IsVisible changes** - you must animate a separate property (Opacity)
- IsVisible affects layout; Opacity doesn't - use Opacity for smooth transitions
- Always set the initial state in XAML, then change it via code to trigger the animation

---

## 2. RenderTransform Animations (Translate, Scale, Rotate)

### Basic Pattern: Slide Panel from Right

**XAML - Initial State:**
```xml
<Panel Name="SlidePanel" Opacity="0">
  <Panel.RenderTransform>
    <TranslateTransform X="300" Y="0" />
  </Panel.RenderTransform>
  <Panel.Transitions>
    <Transitions>
      <DoubleTransition 
        Property="Opacity" 
        Duration="0:0:0.4" 
        Easing="CubicEaseOut" />
      <TransformOperationsTransition 
        Property="RenderTransform" 
        Duration="0:0:0.4" 
        Easing="CubicEaseOut" />
    </Transitions>
  </Panel.Transitions>
  <!-- Panel content here -->
</Panel>
```

**C# - Trigger Animation:**
```csharp
public void ShowSlidePanel()
{
    var panel = this.FindControl<Panel>("SlidePanel");
    
    // Change properties to trigger transitions
    panel.Opacity = 1;  // Fade in
    panel.RenderTransform = new TranslateTransform(0, 0);  // Slide to final position
}

public async void HideSlidePanel()
{
    var panel = this.FindControl<Panel>("SlidePanel");
    
    panel.Opacity = 0;
    panel.RenderTransform = new TranslateTransform(300, 0);  // Slide back right
    
    await Task.Delay(400);  // Wait for animation
    panel.IsVisible = false;  // Optional: hide after animation
}
```

### Transform Types

**TranslateTransform** - Position changes (Slide):
```xml
<TranslateTransform X="300" Y="0" />
```

**ScaleTransform** - Size changes (Zoom):
```xml
<ScaleTransform ScaleX="1" ScaleY="1" />
```

**RotateTransform** - Rotation:
```xml
<RotateTransform Angle="0" />
```

**Combined Transforms:**
```xml
<TransformGroup>
  <TranslateTransform X="100" />
  <ScaleTransform ScaleX="1" ScaleY="1" />
  <RotateTransform Angle="0" />
</TransformGroup>
```

---

## 3. Property-Based Transitions vs Keyframe Animations

### Transitions (Recommended for Most Cases)

**Pros:**
- Simpler syntax
- CSS-like, familiar to web developers
- Automatic - just change the property
- Better performance for simple animations

**When to use:**
- Opacity fades
- Position/scale changes via RenderTransform
- Margin, padding, color changes
- Hover states, visibility toggles

**Example:**
```xml
<Button Content="Hover Me">
  <Button.Transitions>
    <Transitions>
      <DoubleTransition Property="Opacity" Duration="0:0:0.3" />
      <ThicknessTransition Property="Margin" Duration="0:0:0.3" />
      <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.3" />
    </Transitions>
  </Button.Transitions>
</Button>
```

**Triggering:**
```csharp
button.Opacity = 0.5;  // Automatically animates
button.Margin = new Thickness(10);  // Automatically animates
```

### Keyframe Animations (For Complex Sequences)

**Pros:**
- Multiple keyframes with specific timings
- Can define intermediate values (0%, 50%, 100%)
- More control over animation timeline

**When to use:**
- Multi-step animations
- Complex sequences that can't be expressed in single property changes
- Reusable animation templates

**Example:**
```xml
<Window.Resources>
  <Animation x:Key="SlideInAnimation" Duration="0:0:0.5">
    <KeyFrame Cue="0%">
      <Setter Property="Opacity" Value="0" />
      <Setter Property="RenderTransform">
        <TranslateTransform X="300" Y="0" />
      </Setter>
    </KeyFrame>
    <KeyFrame Cue="50%">
      <!-- Intermediate state (optional) -->
      <Setter Property="Opacity" Value="0.5" />
    </KeyFrame>
    <KeyFrame Cue="100%">
      <Setter Property="Opacity" Value="1" />
      <Setter Property="RenderTransform">
        <TranslateTransform X="0" Y="0" />
      </Setter>
    </KeyFrame>
  </Animation>
</Window.Resources>
```

**Triggering in C#:**
```csharp
var animation = this.Resources["SlideInAnimation"] as Animation;
await animation.RunAsync(slidePanel);
```

---

## 4. Triggering Animations Based on Data Binding Changes

### Pattern 1: Using Attached Behaviors

**XAML with Binding:**
```xml
<Panel Opacity="{Binding PanelOpacity}" IsVisible="{Binding IsVisible}">
  <Panel.Transitions>
    <Transitions>
      <DoubleTransition Property="Opacity" Duration="0:0:0.3" />
    </Transitions>
  </Panel.Transitions>
</Panel>
```

**ViewModel:**
```csharp
public class MyViewModel : INotifyPropertyChanged
{
    private double _panelOpacity;
    public double PanelOpacity
    {
        get => _panelOpacity;
        set
        {
            if (_panelOpacity != value)
            {
                _panelOpacity = value;
                OnPropertyChanged(nameof(PanelOpacity));
                // Transition happens automatically!
            }
        }
    }

    public void ShowPanel()
    {
        PanelOpacity = 1.0;  // Trigger fade-in animation
    }

    public void HidePanel()
    {
        PanelOpacity = 0.0;  // Trigger fade-out animation
    }

    // INotifyPropertyChanged implementation...
}
```

### Pattern 2: RenderTransform with Binding

```xml
<Panel Name="SlidingPanel">
  <Panel.RenderTransform>
    <TranslateTransform 
      X="{Binding SlideX}" 
      Y="{Binding SlideY}" />
  </Panel.RenderTransform>
  <Panel.Transitions>
    <Transitions>
      <TransformOperationsTransition 
        Property="RenderTransform" 
        Duration="0:0:0.4" 
        Easing="CubicEaseOut" />
    </Transitions>
  </Panel.Transitions>
</Panel>
```

**ViewModel:**
```csharp
private double _slideX = 300;
public double SlideX
{
    get => _slideX;
    set { if (_slideX != value) { _slideX = value; OnPropertyChanged(nameof(SlideX)); } }
}

public void AnimateSlideIn()
{
    SlideX = 0;  // Property change triggers transition
}
```

### Pattern 3: State Enum Triggers Multiple Animations

```csharp
public enum PanelState { Hidden, Visible, Loading }

private PanelState _state;
public PanelState State
{
    get => _state;
    set
    {
        if (_state != value)
        {
            _state = value;
            ApplyStateAnimation();
            OnPropertyChanged(nameof(State));
        }
    }
}

private void ApplyStateAnimation()
{
    switch (State)
    {
        case PanelState.Visible:
            PanelOpacity = 1.0;
            SlideX = 0;
            break;
        case PanelState.Hidden:
            PanelOpacity = 0.0;
            SlideX = 300;
            break;
        case PanelState.Loading:
            // Show spinner, different animation
            break;
    }
}
```

---

## 5. Built-In Avalonia Helpers & Transition Types

### Transition Types

```xml
<!-- For double values (Opacity, Angle, etc.) -->
<DoubleTransition Property="Opacity" Duration="0:0:0.3" />

<!-- For thickness values (Margin, Padding, BorderThickness) -->
<ThicknessTransition Property="Margin" Duration="0:0:0.3" />

<!-- For color values -->
<ColorTransition Property="Background" Duration="0:0:0.3" />

<!-- For transform operations (RenderTransform) -->
<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.3" />

<!-- For corner radius -->
<CornerRadiusTransition Property="CornerRadius" Duration="0:0:0.3" />

<!-- For brush values -->
<BrushTransition Property="Background" Duration="0:0:0.3" />
```

### Easing Functions

All easing functions support three variants: `EaseIn`, `EaseOut`, `EaseInOut`

**Common Easing Functions:**
- `Linear` - constant speed (default)
- `CubicEaseOut` - quick start, gentle deceleration (recommended for UI)
- `CubicEaseInOut` - smooth both ways
- `BackEaseOut` - slight overshoot at end
- `ElasticEaseOut` - bouncy effect
- `BounceEaseOut` - bounce effect
- `ExponentialEaseOut` - dramatic deceleration

**Example:**
```xml
<DoubleTransition 
  Property="Opacity" 
  Duration="0:0:0.3" 
  Easing="CubicEaseOut" />
```

### Duration Format

Duration uses TimeSpan format: `HH:MM:SS.ms`

```xml
<!-- 300 milliseconds -->
<DoubleTransition Duration="0:0:0.3" />

<!-- 500 milliseconds -->
<DoubleTransition Duration="0:0:0.5" />

<!-- 1 second -->
<DoubleTransition Duration="0:0:1" />

<!-- 2.5 seconds -->
<DoubleTransition Duration="0:0:2.5" />
```

---

## Complete Practical Example: Slide-In Panel

### Full XAML

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="MyApp.MainWindow"
        Title="Slide Animation Demo"
        Width="800" Height="600">
  
  <Grid RowDefinitions="Auto,*">
    <!-- Toggle Button -->
    <Button 
      Grid.Row="0" 
      Content="Toggle Panel"
      Click="OnToggleClick"
      Padding="10" 
      Margin="10" />
    
    <!-- Slide-in Panel from Right -->
    <Border 
      Grid.Row="1"
      Name="SidePanel"
      Background="#FF2A2A2A"
      Opacity="0">
      
      <Border.RenderTransform>
        <TranslateTransform X="300" Y="0" />
      </Border.RenderTransform>
      
      <Border.Transitions>
        <Transitions>
          <!-- Fade in/out -->
          <DoubleTransition 
            Property="Opacity" 
            Duration="0:0:0.4" 
            Easing="CubicEaseOut" />
          
          <!-- Slide from right -->
          <TransformOperationsTransition 
            Property="RenderTransform" 
            Duration="0:0:0.4" 
            Easing="CubicEaseOut" />
        </Transitions>
      </Border.Transitions>
      
      <!-- Panel Content -->
      <StackPanel Padding="20">
        <TextBlock 
          Text="Side Panel" 
          FontSize="24" 
          Foreground="White" 
          Margin="0,0,0,10" />
        <TextBlock 
          Text="This panel slides in from the right with a smooth fade." 
          Foreground="#FFCCCCCC"
          TextWrapping="Wrap" />
      </StackPanel>
    </Border>
  </Grid>
</Window>
```

### Code-Behind

```csharp
using Avalonia.Controls;
using Avalonia.Media.Transformation;

public partial class MainWindow : Window
{
    private bool _isPanelVisible = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnToggleClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _isPanelVisible = !_isPanelVisible;
        
        if (_isPanelVisible)
        {
            ShowSidePanel();
        }
        else
        {
            HideSidePanel();
        }
    }

    private void ShowSidePanel()
    {
        var panel = this.FindControl<Border>("SidePanel");
        panel.Opacity = 1;  // Fade in
        panel.RenderTransform = new TranslateTransform(0, 0);  // Slide to position
    }

    private async void HideSidePanel()
    {
        var panel = this.FindControl<Border>("SidePanel");
        panel.Opacity = 0;  // Fade out
        panel.RenderTransform = new TranslateTransform(300, 0);  // Slide back to right
        
        // Panel stays invisible but still takes space
        // Optional: hide completely after animation
        // await Task.Delay(400);
        // panel.IsVisible = false;
    }
}
```

---

## Performance Tips

### Optimal Durations

- **Fast feedback** - 0.15s to 0.2s (button clicks, hovers)
- **Standard transition** - 0.3s to 0.4s (panel slides, fades)
- **Slow transition** - 0.5s to 0.7s (major layout changes)
- **Avoid** - Anything over 1s (feels sluggish)

### Layout vs Render Optimization

| Property | Triggers Layout | Performance |
|----------|-----------------|-------------|
| Opacity | ❌ No | ⭐⭐⭐⭐⭐ |
| RenderTransform (Translate) | ❌ No | ⭐⭐⭐⭐⭐ |
| RenderTransform (Scale) | ❌ No | ⭐⭐⭐⭐⭐ |
| Margin | ✅ Yes | ⭐⭐⭐ |
| Width/Height | ✅ Yes | ⭐⭐ |
| Padding | ✅ Yes | ⭐⭐ |

**Best Practice:** Use Opacity + RenderTransform for smooth, performant animations.

---

## Common Patterns

### 1. Fade In/Out
```xml
<DoubleTransition Property="Opacity" Duration="0:0:0.3" Easing="CubicEaseOut" />
```

### 2. Slide Up
```xml
<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.4" />
<!-- Initial: TranslateTransform Y="50" -->
<!-- Final: TranslateTransform Y="0" -->
```

### 3. Scale Pop
```xml
<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.2" Easing="BackEaseOut" />
<!-- Initial: ScaleTransform ScaleX="0" ScaleY="0" -->
<!-- Final: ScaleTransform ScaleX="1" ScaleY="1" -->
```

### 4. Spin
```xml
<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.5" />
<!-- Initial: RotateTransform Angle="0" -->
<!-- Final: RotateTransform Angle="360" -->
```

### 5. Combined Slide + Fade
```xml
<DoubleTransition Property="Opacity" Duration="0:0:0.4" Easing="CubicEaseOut" />
<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.4" Easing="CubicEaseOut" />
```

---

## Summary

| Approach | Best For | Performance | Complexity |
|----------|----------|-------------|-----------|
| **Transitions** | Most UI animations | High | Low |
| **RenderTransform** | Smooth motion, slides, scales | Very High | Low |
| **Keyframe Animation** | Complex sequences | Good | Medium |
| **Binding-driven** | Data-driven animations | High | Medium |

**For your use case (panel sliding from right):** Use Transitions with RenderTransform. Duration 0.3-0.5s, CubicEaseOut easing, TranslateTransform X property. Set initial state in XAML, change in code-behind to trigger animation.
