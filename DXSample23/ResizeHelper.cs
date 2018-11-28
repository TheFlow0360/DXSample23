using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.LayoutControl;

namespace DXSample23
{
    public static class ResizeHelper
    {
        public static readonly DependencyProperty PreventOversizingProperty = DependencyProperty.RegisterAttached(
            "PreventOversizing",
            typeof(Boolean),
            typeof(ResizeHelper),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsParentMeasure, PreventOversizingPropertyChangedCallback));

        public static Boolean GetPreventOversizing(LayoutGroup layoutGroup)
        {
            return (Boolean)layoutGroup.GetValue(PreventOversizingProperty);
        }

        public static void SetPreventOversizing(LayoutGroup layoutGroup, Boolean value)
        {
            layoutGroup.SetValue(PreventOversizingProperty, value);
        }

        private static void PreventOversizingPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is LayoutGroup layoutGroup && layoutGroup.Parent is LayoutControl))
            {
                Console.WriteLine("PreventOversizing is not supported for " + d.GetType() + " or it's parent.");
                return;
            }

            if (e.NewValue is Boolean active && active)
            {
                layoutGroup.SizeChanged += LayoutGroupOnSizeChanged;
            }
            else
            {
                layoutGroup.SizeChanged -= LayoutGroupOnSizeChanged;
            }
        }

        private static void LayoutGroupOnSizeChanged(Object sender, SizeChangedEventArgs e)
        {
            if (!(sender is LayoutGroup layoutGroup && layoutGroup.Parent is LayoutControl layoutControl))
            {
                return;
            }


            var isHorizontal = layoutControl.Orientation == Orientation.Horizontal;
            var parentSize = isHorizontal ? layoutControl.ActualWidth : layoutControl.ActualHeight;
            var groupSize = isHorizontal
                ? layoutGroup.ActualWidth
                : layoutGroup.ActualHeight;
            var groupMargin = isHorizontal
                ? layoutGroup.Margin.Left + layoutGroup.Margin.Right
                : layoutGroup.Margin.Top + layoutGroup.Margin.Bottom;
            if (groupMargin <= 0)
            {
                // Helper funktioniert nicht ohne Margin, Splitter wird sonst unbedienbar 
                return;
            }

            // magisches Element! -1 nicht entfernen!
            // das ist notwendig, damit die Maximalgröße immer kleiner als das Element plus Margin ist
            // ansonsten wird der Splitter unbedienbar
            groupSize += groupMargin - 1;
            
            var otherCount = 0;
            var otherSize = 0.0;
            foreach (var child in layoutControl.Children)
            {
                if (!(child is LayoutGroup childGroup && childGroup != layoutGroup))
                {
                    continue;
                }

                otherCount++;
                otherSize += isHorizontal
                    ? childGroup.ActualWidth + childGroup.Margin.Left + childGroup.Margin.Right
                    : childGroup.ActualHeight + childGroup.Margin.Top + childGroup.Margin.Bottom;
            }

            if (otherCount != 1)
            {
                // kein Support für mehr als zwei Gruppen (nicht trivial), keinen Nutzen für eine einzelne Gruppe
                return;
            }

            var spacingSize = (otherCount) * layoutControl.ItemSpace +
                              (isHorizontal
                                   ? layoutControl.Padding.Left + layoutControl.Padding.Right
                                   : layoutControl.Padding.Top + layoutControl.Padding.Bottom);
            
            var maxGroupSize = Math.Max(parentSize - otherSize - spacingSize, isHorizontal ? layoutGroup.MinWidth : layoutGroup.MinHeight);
            if (isHorizontal)
            {
                layoutGroup.MaxWidth = maxGroupSize;
            }
            else
            {
                layoutGroup.MaxHeight = maxGroupSize;
            }

            var maxOtherSize = Math.Max(0, parentSize - groupSize - spacingSize);

            foreach (var child in layoutControl.Children)
            {
                if (!(child is LayoutGroup childGroup && childGroup != layoutGroup))
                {
                    continue;
                }

                if (isHorizontal)
                {
                    childGroup.MaxWidth = maxOtherSize;
                }
                else
                {
                    childGroup.MaxHeight = maxOtherSize;
                }
            }
        }
    }
}