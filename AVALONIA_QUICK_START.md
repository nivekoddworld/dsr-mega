# Avalonia Smooth Animations — 5-Minute Quick Start

## What You Need to Know

Avalonia provides **automatic property animations** called **Transitions**. When you change a property, it automatically animates over a duration you specify.

---

## The 30-Second Version

To slide a panel from the right in 0.4 seconds:

### Step 1: XAML (Define initial state + transitions)
```xml
<Panel Name="SlidePanel" Opacity="0">
  <Panel.RenderTransform>
    <TranslateTransform X="300" />
  </Panel.RenderTransform>
  <Panel.Transitions>
    <Transitions>
      <DoubleTransition Property="Opacity" Duration="0:0:0.4" Easing="CubicEaseOut" />
      <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.4" Easing="CubicEaseOut" />
    </Transitions>
  </Panel.Transitions>
  <!-- Your content -->
</Panel>
```

### Step 2: C# (Change properties to trigger animation)
```csharp
var panel = this.FindControl<Panel>("SlidePanel");

// Show
panel.Opacity = 1;
panel.RenderTransform = new TranslateTransform(0, 0);

// Hide
panel.Opacity = 0;
panel.RenderTransform = new TranslateTransform(300, 0);
```

**Done!** Both animations run smoothly in parallel.

---

## The Secret Formula

```
Initial State (XAML)  +  Property Change (C#)  +  Transition (XAML)  =  Smooth Animation
     Opacity="0"         panel.Opacity = 1;         <DoubleTransition>
     X="300"             panel.RenderTransform      <TransformOperationsTransition>
```

---

## Animation Vocabulary

| Term | Meaning | Example |
|------|---------|---------|
| **Transition** | Auto-animate when property changes | `<DoubleTransition Property="Opacity" />` |
| **Duration** | How long animation lasts | `0:0:0.4` = 400 milliseconds |
| **Easing** | Curve controlling animation speed | `CubicEaseOut` = smooth deceleration |
| **RenderTransform** | Layout-free position/scale/rotate | `<TranslateTransform X="300" />` |
| **Initial State** | Starting values for animation | `Opacity="0"` in XAML |

---

## Duration Quick Reference

```
0:0:0.2   = 200ms  (fast)     ← Button click
0:0:0.3   = 300ms  (quick)    ← Fade
0:0:0.4   = 400ms  (standard) ← Panel slide ⭐ BEST
0:0:0.5   = 500ms  (slow)     ← Smooth transition
```

**Format:** `hours:minutes:seconds.milliseconds` → `0:0:0.3` = 300ms

---

## Easing Functions at a Glance

```
CubicEaseOut    ← Natural deceleration (RECOMMENDED 90% of time)
                  ╱────────────────────
                ╱
Linear          ─────────────────────  (robotic - rarely use)
BackEaseOut     ╱─────────────────╲    (slight bounce - emphasis)
                ╱                ╲│
```

**Recommendation:** Always use `CubicEaseOut` unless you want a special effect.

---

## The 5 Most Common Animations

### 1. Fade In/Out
```xml
<DoubleTransition Property="Opacity" Duration="0:0:0.3" Easing="CubicEaseOut" />
```
Change: `Opacity` from 0 → 1 (fade in) or 1 → 0 (fade out)

### 2. Slide from Right
```xml
<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.4" />
<!-- Initial: X="300" -->
<!-- Change to: new TranslateTransform(0, 0) -->
```

### 3. Slide from Top
```xml
<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.4" />
<!-- Initial: Y="-100" -->
<!-- Change to: new TranslateTransform(0, 0) -->
```

### 4. Scale Pop (Emphasis)
```xml
<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.25" Easing="BackEaseOut" />
<!-- Initial: ScaleX="0.8" ScaleY="0.8" -->
<!-- Change to: new ScaleTransform(1, 1) -->
```

### 5. Fade + Slide Combined
```xml
<DoubleTransition Property="Opacity" Duration="0:0:0.4" Easing="CubicEaseOut" />
<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.4" Easing="CubicEaseOut" />
```
Both run simultaneously!

---

## RenderTransform Reference

### TranslateTransform (Slide)
```csharp
new TranslateTransform(X, Y)  // X=horizontal, Y=vertical
new TranslateTransform(300, 0)  // Slide 300px right
new TranslateTransform(0, -100) // Slide 100px up
```

### ScaleTransform (Zoom)
```csharp
new ScaleTransform(scaleX, scaleY)
new ScaleTransform(1, 1)      // Normal size
new ScaleTransform(0.8, 0.8)  // 80% size
new ScaleTransform(1.2, 1.2)  // 120% size
```

### RotateTransform (Spin)
```csharp
new RotateTransform(angle)
new RotateTransform(0)        // No rotation
new RotateTransform(360)      // Full spin
new RotateTransform(45)       // 45° rotated
```

---

## Transition Types Reference

| Property Type | Use This Transition | Example |
|---------------|-------------------|---------|
| Opacity, Angle, Rotation | `DoubleTransition` | `<DoubleTransition Property="Opacity" />` |
| Margin, Padding, BorderThickness | `ThicknessTransition` | `<ThicknessTransition Property="Margin" />` |
| Foreground, Background (solid colors) | `ColorTransition` | `<ColorTransition Property="Foreground" />` |
| RenderTransform (Translate/Scale/Rotate) | `TransformOperationsTransition` | `<TransformOperationsTransition Property="RenderTransform" />` |

---

## Mistakes to Avoid

### ❌ Wrong: Animating IsVisible
```csharp
panel.IsVisible = true;  // No animation
```

### ✅ Right: Animate Opacity
```csharp
panel.IsVisible = true;
panel.Opacity = 1;  // Triggers animation
```

---

### ❌ Wrong: Animating Margin
```xml
<ThicknessTransition Property="Margin" Duration="0:0:0.4" />
```
Causes layout recalculation, stutters on complex UIs.

### ✅ Right: Use RenderTransform
```xml
<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.4" />
```
No layout impact, smooth 60fps.

---

### ❌ Wrong: Using Linear Easing
```xml
<DoubleTransition Easing="Linear" />
```

### ✅ Right: Use CubicEaseOut
```xml
<DoubleTransition Easing="CubicEaseOut" />
```

---

## Data-Binding Animations

If you want animations driven by ViewModel properties:

**ViewModel:**
```csharp
public class MyViewModel : INotifyPropertyChanged
{
    private double _panelOpacity;
    public double PanelOpacity
    {
        get => _panelOpacity;
        set { if (_panelOpacity != value) { _panelOpacity = value; OnPropertyChanged(); } }
    }
    
    public void ShowPanel() => PanelOpacity = 1;
    public void HidePanel() => PanelOpacity = 0;
}
```

**XAML:**
```xml
<Panel Opacity="{Binding PanelOpacity}">
  <Panel.Transitions>
    <Transitions>
      <DoubleTransition Property="Opacity" Duration="0:0:0.3" />
    </Transitions>
  </Panel.Transitions>
</Panel>
```

Changing the ViewModel property automatically triggers the animation!

---

## Complete Working Example

**File: MyWindow.xaml**
```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Slide Animation">
  <DockPanel>
    <Button DockPanel.Dock="Top" Name="ToggleButton" Content="Show Panel" Click="OnToggle" />
    
    <Border Name="SidePanel" Width="300" Background="Gray" Opacity="0" DockPanel.Dock="Right">
      <Border.RenderTransform>
        <TranslateTransform X="300" />
      </Border.RenderTransform>
      <Border.Transitions>
        <Transitions>
          <DoubleTransition Property="Opacity" Duration="0:0:0.4" Easing="CubicEaseOut" />
          <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.4" Easing="CubicEaseOut" />
        </Transitions>
      </Border.Transitions>
    </Border>
  </DockPanel>
</Window>
```

**File: MyWindow.xaml.cs**
```csharp
using Avalonia.Controls;
using Avalonia.Media.Transformation;

public partial class MyWindow : Window
{
    private bool isOpen = false;

    public MyWindow() => InitializeComponent();

    private void OnToggle(object? s, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var panel = this.FindControl<Border>("SidePanel");
        
        if (!isOpen)
        {
            panel.Opacity = 1;
            panel.RenderTransform = new TranslateTransform(0, 0);
            isOpen = true;
        }
        else
        {
            panel.Opacity = 0;
            panel.RenderTransform = new TranslateTransform(300, 0);
            isOpen = false;
        }
    }
}
```

**Result:** Panel smoothly slides in and out from the right with fade effect!

---

## Checklist: Before You Code

- [ ] Is initial state set in XAML? (Opacity="0", X="300", etc.)
- [ ] Is Transition element defined with correct Property?
- [ ] Is Duration in correct format? (0:0:0.3 = 300ms)
- [ ] Are you using CubicEaseOut easing?
- [ ] Are you changing the property in C# to trigger animation?
- [ ] Using RenderTransform instead of Margin/Width/Height?

---

## Next Steps

1. **Start here:** Read `AVALONIA_ANIMATION_CHEATSHEET.md` for copy-paste examples
2. **Deep dive:** Read `AVALONIA_ANIMATIONS_GUIDE.md` for detailed explanations
3. **Visual learner:** Read `AVALONIA_ANIMATION_VISUAL_GUIDE.md` for diagrams
4. **Working code:** Copy examples from `AVALONIA_ANIMATION_EXAMPLES.cs`

---

## TL;DR Summary

**To slide a panel from right in 0.4s:**

1. Set XAML: `Opacity="0"`, `RenderTransform X="300"`
2. Define transitions: `<DoubleTransition>` + `<TransformOperationsTransition>`
3. In C#: `panel.Opacity = 1; panel.RenderTransform = new TranslateTransform(0, 0);`
4. Watch it animate smoothly! ✨

---

That's it! You're ready to create smooth animations in Avalonia. 🚀
