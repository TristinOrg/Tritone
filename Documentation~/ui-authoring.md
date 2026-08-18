# UI authoring and composition

This guide builds one generated view, reusable item, panel, and window
from prefab authoring through module ownership.

## Prerequisites

Create a `UIRoot` in the bootstrap scene and assign the layer transforms that
the project uses. Register assets, pools, and UI in that order:

```csharp
protected override void Configure(GameApplicationBuilder builder)
{
    builder.UseAssets();
    builder.UsePools();
    builder.UseUI(mUIRoot);
    builder.AddModule(new InventoryModule());
}
```

Items and panels require both assets and pools. A window without composition
can use `UseUI` without `UsePools`.

## Generate a typed view

1. Add `UIPrefabRef` to the root of each window, panel, and item prefab.
2. Set its namespace, class name, and output directory.
3. Drag the required child GameObjects or Components into `References` and
   select the exact component exposed by each generated field.
4. Select **Generate UIView Script**.
5. Wait for Unity compilation, then select **Bind Generated UIView**.
6. Select **Preprocess Sorting Hierarchy** whenever a Canvas, Renderer, or
   authored sorting order changes.

A generated view contains serialized references only. Keep behavior in the
matching `UIWindow`, `UIPanel`, or `UIItem` class.

```csharp
public sealed class UIInventoryView : UIView
{
    public Button        BtnClose;
    public RectTransform Content;
    public RectTransform PanelRoot;
}
```

## Implement items and panels

Item and panel behavior uses the same enable-lifetime binding stages as a
window:

```csharp
public sealed class InventoryItem : UIItem<UIInventoryItemView>
{
    protected override void OnBindEvents()
    {
        BindButton(mView.BtnSelect, OnSelect);
    }

    private void OnSelect()
    {
    }
}

public sealed class ItemDetailPanel : UIPanel<UIItemDetailPanelView>
{
    protected override void OnBindEvents()
    {
        BindButton(mView.BtnClose, Close);
    }
}
```

`OnBindEvents` runs each time the object opens. Tritone releases its Unity and
Tritone event bindings when the object closes, so do not retain duplicate
listeners elsewhere.

## Compose the window

Register composition once in `OnInitialize`. Prefabs load lazily on first use:

```csharp
public sealed class InventoryWindow : UIWindow<UIInventoryView>
{
    private InventoryItem mSelectedItem;

    protected override void OnInitialize()
    {
        AddItemTemplate<InventoryItem>("UI/Inventory/InventoryItem");
        AddPanel<ItemDetailPanel>("UI/Inventory/ItemDetailPanel",
                                  mView.PanelRoot);
    }

    protected override void OnBindEvents()
    {
        BindButton(mView.BtnClose, Close);
    }

    protected override void OnOpen()
    {
        mSelectedItem = CreateItem<InventoryItem>(mView.Content);
    }

    protected override void OnClose()
    {
        ReleaseItem(ref mSelectedItem);
    }

    private ItemDetailPanel ShowDetails()
    {
        return OpenPanel<ItemDetailPanel>();
    }
}
```

Panels can be nested below any stable transform owned by the window view. The
window remains the lifetime owner, regardless of the visual hierarchy.

## Register module ownership

```csharp
public sealed class InventoryModule : ModuleBase
{
    protected override void OnConfigure(IServiceRegistry services)
    {
        AddWindow<InventoryWindow>("UI/Inventory/InventoryWindow",
                                   EUILayer.Normal);
    }

    private void ShowInventory()
    {
        OpenWindow<InventoryWindow>();
    }

    private async Task ShowInventoryAsync()
    {
        await OpenWindowAsync<InventoryWindow>();
    }
}
```

Concurrent asynchronous opens share one load. If loading fails, a later call
can retry. If the final module owner stops during a load, the result is released
and the pending open fails instead of creating an unowned window.

## Lifetime rules

- Closing a window returns its active items and panels to shared prefab pools.
- Reopening the same window reuses retained prefabs and pooled instances.
- Closing a panel keeps its single instance available for the current window
  activity.
- Releasing the final module owner destroys the window instance and releases
  all retained prefab assets.
- A composition prefab missing its requested component fails immediately and
  its asset reference is rolled back.
- Dynamic child views participate in their window's preprocessed sorting-order
  sequence.

## Validation checklist

- Every prefab root contains its generated `UIView` and matching behavior.
- Generated references are assigned and the sorting hierarchy is current.
- The bootstrap registers assets, pools, and UI.
- Each module-owned window is registered before it is opened.
- Repeated open and close cycles leave no duplicate listeners or active items.
- Async failure and owner-release paths are handled by the caller when needed.
