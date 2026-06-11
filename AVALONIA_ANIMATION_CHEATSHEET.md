# Avalonia Animations — Quick Reference Cheat Sheet

## TL;DR: Slide Panel from Right (0.4s)

**XAML:**
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
</Panel>
```

**C#:**
```csharp
// Show
slidePanel.Opacity = 1;
slidePanel.RenderTransform = new TranslateTransform(0, 0);

// Hide
slidePanel.Opacity = 0;
slidePanel.RenderTransform = new TranslateTransform(300, 0);
```

---

## Duration Format

```
0:0:0.3       = 300ms
0:0:0.4       = 400ms
0:0:0.5       = 500ms
0:0:1         = 1000ms (1s)
0:0:2.5       = 2500ms (2.5s)
```

**Recommended:** 0.3–0.5s for UI animations

---

## Transition Types

| Type | Property | Example |
|------|----------|---------|
| **DoubleTransition** | Opacity, Angle, Width, Height | `<DoubleTransition Property="Opacity" />` |
| **ThicknessTransition** | Margin, Padding, BorderThickness | `<ThicknessTransition Property="Margin" />` |
| **ColorTransition** | Foreground, Background (colors) | `<ColorTransition Property="Foreground" />` |
| **TransformOperationsTransition** | RenderTransform | `<TransformOperationsTransition Property="RenderTransform" />` |
| **CornerRadiusTransition** | CornerRadius | `<CornerRadiusTransition Property="CornerRadius" />` |
| **BrushTransition** | Background, Foreground (brushes) | `<BrushTransition Property="Background" />` |

---

## RenderTransform Types

### TranslateTransform (Slide)
```xml
<!-- Slide from right (300px) -->
<TranslateTransform X="300" Y="0" />

<!-- Slide from bottom (100px) -->
<TranslateTransform X="0" Y="100" />

<!-- Diagonal slide -->
<TranslateTransform X="200" Y="200" />
```

### ScaleTransform (Zoom)
```xml
<!-- Start at 80% size -->
<ScaleTransform ScaleX="0.8" ScaleY="0.8" />

<!-- Start at 0 (invisible) -->
<ScaleTransform ScaleX="0" ScaleY="0" />

<!-- Non-uniform scale -->
<ScaleTransform ScaleX="1.2" ScaleY="0.8" />
```

### RotateTransform (Spin)
```xml
<!-- Start at 0 degrees -->
<RotateTransform Angle="0" />

<!-- Pre-rotated 45 degrees -->
<RotateTransform Angle="45" />
```

### Combined (TransformGroup)
```xml
<TransformGroup>
  <TranslateTransform X="300" Y="0" />
  <ScaleTransform ScaleX="1" ScaleY="1" />
  <RotateTransform Angle="0" />
</TransformGroup>
```

---

## Easing Functions

### Quick Selection Guide

| Easing | Feel | Use When |
|--------|------|----------|
| **Linear** | Robotic | Rarely, for special effects |
| **CubicEaseOut** ⭐ | Natural deceleration | 90% of UI animations (RECOMMENDED) |
| **CubicEaseInOut** | Smooth both ways | Transitions in/out simultaneously |
| **BackEaseOut** | Slight overshoot | Emphasis, pop-in effects |
| **ElasticEaseOut** | Springy bounce | Loading indicators, playful UI |
| **BounceEaseOut** | Multiple bounces | Alerts, attention-grabbing (sparingly) |
| **ExponentialEaseOut** | Quick deceleration | Fast transitions |

### Easing Variants
```
CubicEaseOut      <- Accelerates out (most natural)
CubicEaseIn       <- Decelerates into (slow start)
CubicEaseInOut    <- Both ways (smooth)
```

---

## Animation Patterns by Scenario

### 1. Fade In/Out
```xml
<DoubleTransition Property="Opacity" Duration="0:0:0.3" Easing="CubicEaseOut" />
```
Change: `Opacity` from 0 → 1 (fade in) or 1 → 0 (fade out)

### 2. Slide from Right
```xml
<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.4" Easing="CubicEaseOut" />
```
Initial: `<TranslateTransform X="300" />`  
Change to: `new TranslateTransform(0, 0)`

### 3. Slide from Top
```xml
<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.4" />
```
Initial: `<TranslateTransform Y="-100" />`  
Change to: `new TranslateTransform(0, 0)`

### 4. Scale Pop (Emphasis)
```xml
<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.2" Easing="BackEaseOut" />
```
Initial: `<ScaleTransform ScaleX="0.8" ScaleY="0.8" />`  
Change to: `new ScaleTransform(1, 1)`

### 5. Spin (360°)
```xml
<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.5" />
```
Initial: `<RotateTransform Angle="0" />`  
Change to: `new RotateTransform(360)`

### 6. Fade + Slide (Combined)
```xml
<DoubleTransition Property="Opacity" Duration="0:0:0.4" Easing="CubicEaseOut" />
<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.4" Easing="CubicEaseOut" />
```
Both run simultaneously when either property changes.

### 7. Button Hover
```xml
<DoubleTransition Property="Opacity" Duration="0:0:0.2" />
<ThicknessTransition Property="Margin" Duration="0:0:0.2" />
```

### 8. Color Change
```xml
<BrushTransition Property="Background" Duration="0:0:0.3" />
```

---

## Common Mistakes & Fixes

### ❌ Animation Doesn't Trigger
**Problem:** Changed `IsVisible` but animation didn't run  
**Fix:** Animate `Opacity` instead; then toggle `IsVisible` after delay
```csharp
// Wrong:
panel.IsVisible = true;  // No animation

// Right:
panel.IsVisible = true;
panel.Opacity = 1;  // Triggers DoubleTransition
```

### ❌ Animation Stutters
**Problem:** Animating `Width`, `Height`, or `Margin`  
**Fix:** Use `RenderTransform` instead (doesn't trigger layout)
```xml
<!-- Wrong: -->
<ThicknessTransition Property="Margin" Duration="0:0:0.3" />

<!-- Right: -->
<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.3" />
```

### ❌ Slide Doesn't Work
**Problem:** Changed `RenderTransform` but no animation  
**Fix:** Ensure `TransformOperationsTransition` is defined and initial state is set
```xml
<!-- Always set initial state in XAML: -->
<Panel.RenderTransform>
  <TranslateTransform X="300" />  <!-- Initial position -->
</Panel.RenderTransform>

<!-- Then define transition: -->
<TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.4" />
```

### ❌ Scale Animation Looks Off
**Problem:** Scaling from wrong origin point  
**Fix:** Use `RenderTransformOrigin` to adjust pivot
```xml
<Border RenderTransformOrigin="0.5,0.5">  <!-- Center pivot -->
  <Border.RenderTransform>
    <ScaleTransform ScaleX="1" ScaleY="1" />
  </Border.RenderTransform>
</Border>
```

### ❌ Animation Too Slow/Fast
**Problem:** Duration is 1+ second or less than 0.15s  
**Fix:** Use 0.3–0.5s for standard transitions
```xml
<DoubleTransition Duration="0:0:0.3" />   <!-- ✅ Good -->
<DoubleTransition Duration="0:0:1.5" />   <!-- ❌ Too slow -->
<DoubleTransition Duration="0:0:0.05" />  <!-- ❌ Too fast -->
```

---

## Data Binding Animations

### Property Binding with Auto-Animation

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

**ViewModel:**
```csharp
private double _panelOpacity;
public double PanelOpacity
{
    get => _panelOpacity;
    set { if (_panelOpacity != value) { _panelOpacity = value; OnPropertyChanged(); } }
}

public void Show() => PanelOpacity = 1;
public void Hide() => PanelOpacity = 0;
```

Changing `PanelOpacity` automatically triggers the transition!

---

## Performance Checklist

- ✅ Use `Opacity` instead of `IsVisible` for visibility transitions
- ✅ Use `RenderTransform` instead of `Margin`/`Width`/`Height`
- ✅ Prefer 0.3–0.5s duration for responsive UI
- ✅ Use `CubicEaseOut` for natural motion
- ✅ Avoid animating >5 properties simultaneously
- ✅ Test on lower-end machines
- ❌ Don't animate `Width`, `Height`, `Margin` (triggers layout)
- ❌ Don't exceed 1s duration (feels sluggish)
- ❌ Don't use `Linear` easing (feels robotic)
- ❌ Don't animate on every frame (use Transitions, not custom loops)

---

## Real-World Timing Examples

| Scenario | Duration | Easing |
|----------|----------|--------|
| Button hover color change | 0:0:0.15 | Linear |
| Button click feedback | 0:0:0.2 | CubicEaseOut |
| Panel slide-in | 0:0:0.4 | CubicEaseOut |
| Modal fade-in | 0:0:0.3 | CubicEaseOut |
| Notification pop-up | 0:0:0.25 | BackEaseOut |
| Spinner rotation | 0:0:2 | Linear |
| Page transition | 0:0:0.5 | CubicEaseOut |

---

## Full Template

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Smooth Animation">
  
  <Panel Name="AnimatedPanel" Opacity="0">
    <!-- Initial RenderTransform state -->
    <Panel.RenderTransform>
      <TranslateTransform X="300" Y="0" />
    </Panel.RenderTransform>

    <!-- Define all transitions -->
    <Panel.Transitions>
      <Transitions>
        <!-- Opacity fade -->
        <DoubleTransition Property="Opacity" Duration="0:0:0.4" Easing="CubicEaseOut" />
        
        <!-- Transform slide -->
        <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.4" Easing="CubicEaseOut" />
      </Transitions>
    </Panel.Transitions>

    <!-- Content -->
  </Panel>
</Window>
```

**In C# Code-Behind:**
```csharp
var panel = this.FindControl<Panel>("AnimatedPanel");

// Trigger animation
panel.Opacity = 1;
panel.RenderTransform = new TranslateTransform(0, 0);

// Reverse animation
panel.Opacity = 0;
panel.RenderTransform = new TranslateTransform(300, 0);
```

---

## Links & Resources

- **Avalonia Docs:** https://docs.avaloniaui.net/docs/animations
- **Easing Functions:** https://easings.net/ (visualize timing)
- **TimeSpan Format:** `HH:MM:SS.fff` (hours:minutes:seconds.milliseconds)

---

## Summary

**For 90% of cases:**
1. Use **Transitions** (not Keyframes)
2. Animate **Opacity** + **RenderTransform** (not Margin/Width/Height)
3. Use **CubicEaseOut** easing
4. Set duration to **0.3–0.5s**
5. Define initial state in XAML, change in C#

That's it! 🎯
