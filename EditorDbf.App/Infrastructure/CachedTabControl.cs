using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace EditorDbf.App.Infrastructure;

/// <summary>
/// Un TabControl que mantiene vivos los visuales de las pestañas para evitar la recreación constante.
/// </summary>
public class CachedTabControl : TabControl
{
    private Panel? _itemsHolder;

    public CachedTabControl()
    {
        // Aseguramos que se use nuestra plantilla personalizada que incluye el Panel contenedor
        DefaultStyleKey = typeof(CachedTabControl);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _itemsHolder = GetTemplateChild("PART_ItemsHolder") as Panel;
        UpdateData();
    }

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        UpdateData();
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);
        UpdateData();
    }

    private void UpdateData()
    {
        if (_itemsHolder == null) return;

        // 1. Aseguramos que cada item tenga un contenedor visual en el panel
        foreach (object item in Items)
        {
            ContentPresenter? cp = FindChildContentPresenter(item);
            if (cp == null)
            {
                cp = CreateChildContentPresenter(item);
                _itemsHolder.Children.Add(cp);
            }
        }

        // 2. Eliminamos contenedores de items que ya no están en la colección
        for (int i = _itemsHolder.Children.Count - 1; i >= 0; i--)
        {
            ContentPresenter cp = (ContentPresenter)_itemsHolder.Children[i];
            if (!Items.Contains(cp.Content))
            {
                _itemsHolder.Children.Remove(cp);
            }
        }

        // 3. Mostramos solo el seleccionado y ocultamos el resto
        foreach (ContentPresenter cp in _itemsHolder.Children)
        {
            cp.Visibility = (cp.Content == SelectedItem) ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private ContentPresenter? FindChildContentPresenter(object data)
    {
        if (_itemsHolder == null) return null;
        foreach (ContentPresenter cp in _itemsHolder.Children)
        {
            if (cp.Content == data) return cp;
        }
        return null;
    }

    private ContentPresenter CreateChildContentPresenter(object item)
    {
        ContentPresenter cp = new ContentPresenter
        {
            Content = item,
            ContentTemplate = ContentTemplate,
            ContentTemplateSelector = ContentTemplateSelector,
            ContentStringFormat = ContentStringFormat,
            Visibility = Visibility.Collapsed
        };
        return cp;
    }
}
