namespace DS1Mod.Core.ImGui;

[Flags]
public enum ImGuiWindowFlags : int
{
    None                   = 0,
    NoTitleBar             = 1 << 0,
    NoResize               = 1 << 1,
    NoMove                 = 1 << 2,
    NoScrollbar            = 1 << 3,
    NoScrollWithMouse      = 1 << 4,
    NoCollapse             = 1 << 5,
    AlwaysAutoResize       = 1 << 6,
    NoBackground           = 1 << 7,
    NoSavedSettings        = 1 << 8,
    NoMouseInputs          = 1 << 9,
    MenuBar                = 1 << 10,
    HorizontalScrollbar    = 1 << 11,
    NoFocusOnAppearing     = 1 << 12,
    NoBringToDisplayOnFocus = 1 << 13,
    AlwaysVerticalScrollbar = 1 << 14,
    AlwaysHorizontalScrollbar = 1 << 15,
    NoNavInputs            = 1 << 16,
    NoNavFocus             = 1 << 17,
    UnsavedDocument        = 1 << 20,
    NoNav                  = NoNavInputs | NoNavFocus,
    NoDecoration           = NoTitleBar | NoResize | NoScrollbar | NoCollapse,
    NoInputs               = NoMouseInputs | NoNavInputs | NoNavFocus,
}

public enum ImGuiCond : int
{
    None         = 0,
    Always       = 1 << 0,
    Once         = 1 << 1,
    FirstUseEver = 1 << 2,
    Appearing    = 1 << 3,
}

public enum ImGuiCol : int
{
    Text                   = 0,
    TextDisabled           = 1,
    WindowBg               = 2,
    ChildBg                = 3,
    PopupBg                = 4,
    Border                 = 5,
    BorderShadow           = 6,
    FrameBg                = 7,
    FrameBgHovered         = 8,
    FrameBgActive          = 9,
    TitleBg                = 10,
    TitleBgActive          = 11,
    TitleBgCollapsed       = 12,
    MenuBarBg              = 13,
    ScrollbarBg            = 14,
    ScrollbarGrab          = 15,
    ScrollbarGrabHovered   = 16,
    ScrollbarGrabActive    = 17,
    CheckMark              = 18,
    SliderGrab             = 19,
    SliderGrabActive       = 20,
    Button                 = 21,
    ButtonHovered          = 22,
    ButtonActive           = 23,
    Header                 = 24,
    HeaderHovered          = 25,
    HeaderActive           = 26,
    Separator              = 27,
    SeparatorHovered       = 28,
    SeparatorActive        = 29,
    ResizeGrip             = 30,
    ResizeGripHovered      = 31,
    ResizeGripActive       = 32,
    TabHovered             = 33,
    Tab                    = 34,
    TabSelected            = 35,
    TabSelectedOverline    = 36,
    TabDimmed              = 37,
    TabDimmedSelected      = 38,
    TabDimmedSelectedOverline = 39,
    PlotLines              = 40,
    PlotLinesHovered       = 41,
    PlotHistogram          = 42,
    PlotHistogramHovered   = 43,
    TableHeaderBg          = 44,
    TableBorderStrong      = 45,
    TableBorderLight       = 46,
    TableRowBg             = 47,
    TableRowBgAlt          = 48,
    TextLink               = 49,
    TextSelectedBg         = 50,
    DragDropTarget         = 51,
    NavCursor              = 52,
    NavWindowingHighlight  = 53,
    NavWindowingDimBg      = 54,
    ModalWindowDimBg       = 55,
}

[Flags]
public enum ImGuiTreeNodeFlags : int
{
    None             = 0,
    Selected         = 1 << 0,
    Framed           = 1 << 1,
    AllowOverlap     = 1 << 2,
    NoTreePushOnOpen = 1 << 3,
    NoAutoOpenOnLog  = 1 << 4,
    DefaultOpen      = 1 << 5,
    OpenOnDoubleClick = 1 << 6,
    OpenOnArrow      = 1 << 7,
    Leaf             = 1 << 8,
    Bullet           = 1 << 9,
    CollapsingHeader = Framed | NoTreePushOnOpen | NoAutoOpenOnLog,
}

[Flags]
public enum ImGuiTabBarFlags : int
{
    None                        = 0,
    Reorderable                 = 1 << 0,
    AutoSelectNewTabs           = 1 << 1,
    TabListPopupButton          = 1 << 2,
    NoCloseWithMiddleMouseButton = 1 << 3,
    NoTabListScrollingButtons   = 1 << 4,
    NoTooltip                   = 1 << 5,
    FittingPolicyResizeDown     = 1 << 6,
    FittingPolicyScroll         = 1 << 7,
}

[Flags]
public enum ImGuiTabItemFlags : int
{
    None                          = 0,
    UnsavedDocument               = 1 << 0,
    SetSelected                   = 1 << 1,
    NoCloseWithMiddleMouseButton  = 1 << 2,
    NoPushId                      = 1 << 3,
    NoTooltip                     = 1 << 4,
    NoReorder                     = 1 << 5,
    Leading                       = 1 << 6,
    Trailing                      = 1 << 7,
}

[Flags]
public enum ImGuiComboFlags : int
{
    None            = 0,
    PopupAlignLeft  = 1 << 0,
    HeightSmall     = 1 << 1,
    HeightRegular   = 1 << 2,
    HeightLarge     = 1 << 3,
    HeightLargest   = 1 << 4,
    NoArrowButton   = 1 << 5,
    NoPreview       = 1 << 6,
}

[Flags]
public enum ImGuiSelectableFlags : int
{
    None             = 0,
    DontClosePopups  = 1 << 0,
    SpanAllColumns   = 1 << 1,
    AllowDoubleClick = 1 << 2,
    Disabled         = 1 << 3,
    AllowOverlap     = 1 << 4,
}
