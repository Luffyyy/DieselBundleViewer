﻿using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DieselBundleViewer.Views
{
    // Taken from https://github.com/digimezzo/WPFControls MIT License.
    public enum WrapPanelAlignment
    {
        Left,
        Right,
        Center
    }

    public class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
    {
        private TranslateTransform trans = new TranslateTransform();
        private ScrollViewer owner;
        private bool canHScroll = false;
        private bool canVScroll = false;
        private Size extent = new Size(0, 0);
        private Size viewport = new Size(0, 0);
        private Point offset;
        //private DoubleAnimation transAnimation;
        //private IEasingFunction easingFunction;

        public VirtualizingWrapPanel()
        {
            // For use in the IScrollInfo implementation
            this.RenderTransform = this.trans;

            //Who the fuck likes smooth scrolling
            /*this.easingFunction = new SineEase() { EasingMode = EasingMode.EaseOut };
            this.transAnimation = new DoubleAnimation()
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(0)),
                EasingFunction = this.easingFunction,
                FillBehavior = FillBehavior.Stop
            };*/
        }

        public int ScrollOffset
        {
            get { return Convert.ToInt32(GetValue(ScrollOffsetProperty)); }
            set { SetValue(ScrollOffsetProperty, value); }
        }

        public static readonly DependencyProperty ScrollOffsetProperty =
            DependencyProperty.RegisterAttached(nameof(ScrollOffset), typeof(int), typeof(VirtualizingWrapPanel), new PropertyMetadata(0));

        public double ChildWidth
        {
            get { return Convert.ToDouble(GetValue(ChildWidthProperty)); }
            set { SetValue(ChildWidthProperty, value); }
        }

        public static readonly DependencyProperty ChildWidthProperty =
            DependencyProperty.RegisterAttached(nameof(ChildWidth), typeof(double), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(200.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public double ChildHeight
        {
            get { return Convert.ToDouble(GetValue(ChildHeightProperty)); }
            set { SetValue(ChildHeightProperty, value); }
        }

        public static readonly DependencyProperty ChildHeightProperty =
            DependencyProperty.RegisterAttached(nameof(ChildHeight), typeof(double), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(200.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public WrapPanelAlignment HorizontalContentAlignment
        {
            get { return (WrapPanelAlignment)GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }

        public static readonly DependencyProperty HorizontalContentAlignmentProperty =
            DependencyProperty.RegisterAttached(nameof(HorizontalContentAlignment), typeof(WrapPanelAlignment), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(WrapPanelAlignment.Left, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// Measure the children
        /// </summary>
        /// <param name="availableSize">Size available</param>
        /// <returns>Size desired</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            try
            {
                this.UpdateScrollInfo(availableSize);

                // Figure out range that's visible based on layout algorithm
                int firstVisibleItemIndex = 0;
                int lastVisibleItemIndex = 0;
                GetVisibleRange(ref firstVisibleItemIndex, ref lastVisibleItemIndex);

                // We need to access InternalChildren before the generator to work around a bug
                UIElementCollection children = this.InternalChildren;
                IItemContainerGenerator generator = this.ItemContainerGenerator;

                // Get the generator position of the first visible data item
                GeneratorPosition startPos = generator.GeneratorPositionFromIndex(firstVisibleItemIndex);

                // Get index where we'd insert the child for this position. If the item is realized
                // (position.Offset == 0), it's just position.Index, otherwise we have to add one to
                // insert after the corresponding child
                int childIndex = (startPos.Offset == 0) ? startPos.Index : startPos.Index + 1;

                using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
                {
                    int itemIndex = firstVisibleItemIndex;
                    while (itemIndex <= lastVisibleItemIndex)
                    {
                        bool newlyRealized = false;

                        // Get or create the child
                        UIElement child = generator.GenerateNext(out newlyRealized) as UIElement;
                        if (newlyRealized)
                        {
                            // Figure out if we need to insert the child at the end or somewhere in the middle
                            if (childIndex >= children.Count)
                            {
                                base.AddInternalChild(child);
                            }
                            else
                            {
                                base.InsertInternalChild(childIndex, child);
                            }
                            generator.PrepareItemContainer(child);
                        }
                        else
                        {
                            // The child has already been created, let's be sure it's in the right spot
                            Debug.Assert(child.Equals(children[childIndex]), "Wrong child was generated");
                        }

                        // Measurements will depend on layout algorithm
                        child.Measure(GetChildSize());
                        itemIndex += 1;
                        childIndex += 1;
                    }
                }

                // Note: this could be deferred to idle time for efficiency
                CleanUpItems(firstVisibleItemIndex, lastVisibleItemIndex);
            }
            catch (ArgumentOutOfRangeException)
            {
                // No idea if we can ignore this
            }
            catch (NullReferenceException)
            {
                // No idea if we can ignore this
            }

            // Guard against possible infinity if exiting measure early
            return new Size(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width, double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);
        }

        /// <summary>
        /// Arrange the children
        /// </summary>
        /// <param name="finalSize">Size available</param>
        /// <returns>Size used</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            IItemContainerGenerator generator = this.ItemContainerGenerator;

            UpdateScrollInfo(finalSize);

            for (int i = 0; i <= this.Children.Count - 1; i++)
            {
                UIElement child = this.Children[i];

                // Map the child offset to an item offset
                int itemIndex = generator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));

                ArrangeChild(itemIndex, child, finalSize);
            }

            return finalSize;
        }

        /// <summary>
        /// When items are removed, remove the corresponding UI if necessary
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                    break;
                case NotifyCollectionChangedAction.Move:
                    RemoveInternalChildRange(args.OldPosition.Index, args.ItemUICount);
                    break;
            }
        }

        /// <summary>
        /// Makes sure the Vertical scroll Offset is updated when the size changes.
        /// </summary>
        /// <param name="sizeInfo"></param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            this.SetVerticalOffset(this.VerticalOffset);
        }

        /// <summary>
        /// Makes sure that the Vertical scroll Offset is reset to 0 when all children are removed.
        /// </summary>
        protected override void OnClearChildren()
        {
            base.OnClearChildren();
            this.SetVerticalOffset(0);
        }

        protected override void BringIndexIntoView(int index)
        {
            if (index < 0 || index >= Children.Count)
            {
                return;
            }

            int childrenPerRow = CalculateChildrenPerRow(RenderSize);
            int row = index / childrenPerRow;
            SetVerticalOffset(row * ChildHeight);
        }

        /// <summary>
        /// Revirtualize items that are no longer visible
        /// </summary>
        /// <param name="minDesiredGenerated">first item index that should be visible</param>
        /// <param name="maxDesiredGenerated">last item index that should be visible</param>
        private void CleanUpItems(int minDesiredGenerated, int maxDesiredGenerated)
        {
            UIElementCollection children = this.InternalChildren;
            IItemContainerGenerator generator = this.ItemContainerGenerator;

            int itemCount = GetItemCount();
            int childrenPerRow = CalculateChildrenPerRow(this.extent);
            if (minDesiredGenerated - 2 * childrenPerRow > 0)
                minDesiredGenerated -= 2 * childrenPerRow;
            if (maxDesiredGenerated + 2 * childrenPerRow < itemCount)
                maxDesiredGenerated += 2 * childrenPerRow;

            for (int i = children.Count - 1; i >= 0; i += -1)
            {
                GeneratorPosition childGeneratorPos = new GeneratorPosition(i, 0);
                int itemIndex = generator.IndexFromGeneratorPosition(childGeneratorPos);
                if ((itemIndex > 2 * childrenPerRow - 1 && itemIndex < minDesiredGenerated) ||
                    (itemIndex < itemCount - 2 * childrenPerRow - 1 && itemIndex > maxDesiredGenerated))
                {
                    generator.Remove(childGeneratorPos, 1);
                    RemoveInternalChildRange(i, 1);
                }
            }
        }

        // I've isolated the layout specific code to this region. If you want to do something other than tiling, this is
        // where you'll make your changes

        /// <summary>
        /// Calculate the extent of the view based on the available size
        /// </summary>
        /// <param name="availableSize">available size</param>
        /// <param name="itemCount">number of data items</param>
        /// <returns></returns>
        private Size CalculateExtent(Size availableSize, int itemCount)
        {
            int childrenPerRow = CalculateChildrenPerRow(availableSize);

            // See how big we are
            return new Size(childrenPerRow * this.ChildWidth, this.ChildHeight * Math.Ceiling(Convert.ToDouble(itemCount) / childrenPerRow));
        }

        /// <summary>
        /// Get the range of children that are visible
        /// </summary>
        /// <param name="firstVisibleItemIndex">The item index of the first visible item</param>
        /// <param name="lastVisibleItemIndex">The item index of the last visible item</param>
        private void GetVisibleRange(ref int firstVisibleItemIndex, ref int lastVisibleItemIndex)
        {
            int childrenPerRow = CalculateChildrenPerRow(this.extent);

            try
            {
                firstVisibleItemIndex = Convert.ToInt32(Math.Floor(this.offset.Y / this.ChildHeight)) * childrenPerRow;
                lastVisibleItemIndex = Convert.ToInt32(Math.Ceiling((this.offset.Y + this.viewport.Height) / this.ChildHeight)) * childrenPerRow - 1;

                int itemCount = GetItemCount();
                if (lastVisibleItemIndex >= itemCount)
                {
                    lastVisibleItemIndex = itemCount - 1;
                }
            }
            catch (OverflowException)
            {
                // No idea if we can ignore this
            }
        }

        /// <summary>
        /// Get the size of the children. We assume they are all the same
        /// </summary>
        /// <returns>The size</returns>
        private Size GetChildSize()
        {
            return new Size(this.ChildWidth, this.ChildHeight);
        }

        /// <summary>
        /// Position a child
        /// </summary>
        /// <param name="itemIndex">The data item index of the child</param>
        /// <param name="child">The element to position</param>
        /// <param name="finalSize">The size of the panel</param>
        private void ArrangeChild(int itemIndex, UIElement child, Size finalSize)
        {
            int childrenPerRow = CalculateChildrenPerRow(finalSize);

            int row = itemIndex / childrenPerRow;
            int column = itemIndex % childrenPerRow;

            double xCoordForItem = 0;
            if (HorizontalContentAlignment == WrapPanelAlignment.Left)
            {
                xCoordForItem = column * this.ChildWidth;
            }
            else // alignment is WrapPanelAlignment.Center or WrapPanelAlignment.Right
            {
                if (childrenPerRow > this.Children.Count)
                {
                    childrenPerRow = this.Children.Count;
                }
                double widthOfRow = childrenPerRow * this.ChildWidth;
                double startXForRow = finalSize.Width - widthOfRow;
                if (HorizontalContentAlignment == WrapPanelAlignment.Center)
                {
                    startXForRow /= 2;
                }
                xCoordForItem = startXForRow + (column * this.ChildWidth);
            }

            child.Arrange(new Rect(xCoordForItem, row * this.ChildHeight, this.ChildWidth, this.ChildHeight));
        }

        /// <summary>
        /// Helper function for tiling layout
        /// </summary>
        /// <param name="availableSize">Size available</param>
        /// <returns></returns>
        private int CalculateChildrenPerRow(Size availableSize)
        {
            // Figure out how many children fit on each row
            int childrenPerRow = 0;

            if (availableSize.Width == double.PositiveInfinity)
            {
                childrenPerRow = this.Children.Count;
            }
            else
            {
                try
                {
                    childrenPerRow = Math.Max(1, Convert.ToInt32(Math.Floor(availableSize.Width / this.ChildWidth)));
                }
                catch (OverflowException)
                {
                    // No idea if we can ignore this
                }
            }
            return childrenPerRow;
        }

        private int GetItemCount()
        {
            // See how many items there are
            ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);
            int itemCount = itemsControl.HasItems ? itemsControl.Items.Count : 0;

            return itemCount;
        }

        // See Ben Constable's series of posts at http://blogs.msdn.com/bencon/
        private void UpdateScrollInfo(Size availableSize)
        {
            int itemCount = GetItemCount();

            Size extent = CalculateExtent(availableSize, itemCount);
            // Update extent
            if (extent != this.extent)
            {
                this.extent = extent;
                if (this.owner != null)
                {
                    this.owner.InvalidateScrollInfo();
                }
            }

            // Update viewport
            if (availableSize != this.viewport)
            {
                this.viewport = availableSize;
                if (this.owner != null)
                {
                    this.owner.InvalidateScrollInfo();
                }
            }
        }

        public ScrollViewer ScrollOwner
        {
            get { return this.owner; }
            set { this.owner = value; }
        }

        public bool CanHorizontallyScroll
        {
            get { return this.canHScroll; }
            set { this.canHScroll = value; }
        }

        public bool CanVerticallyScroll
        {
            get { return this.canVScroll; }
            set { this.canVScroll = value; }
        }

        public double HorizontalOffset
        {
            get { return this.offset.X; }
        }

        public double VerticalOffset
        {
            get { return this.offset.Y; }
        }

        public double ExtentHeight
        {
            get { return this.extent.Height; }
        }

        public double ExtentWidth
        {
            get { return this.extent.Width; }
        }

        public double ViewportHeight
        {
            get { return this.viewport.Height; }
        }

        public double ViewportWidth
        {
            get { return this.viewport.Width; }
        }

        public void LineUp()
        {
            this.SetVerticalOffset(this.VerticalOffset - (this.ScrollOffset > 0 ? this.ScrollOffset : this.ChildHeight));
        }

        public void LineDown()
        {
            this.SetVerticalOffset(this.VerticalOffset + (this.ScrollOffset > 0 ? this.ScrollOffset : this.ChildHeight));
        }

        public void PageUp()
        {
            this.SetVerticalOffset(this.VerticalOffset - this.viewport.Height);
        }

        public void PageDown()
        {
            this.SetVerticalOffset(this.VerticalOffset + this.viewport.Height);
        }

        public void MouseWheelUp()
        {
            this.EasingSetVerticalOffset(this.VerticalOffset - (this.ScrollOffset > 0 ? this.ScrollOffset : this.ChildHeight));
        }

        public void MouseWheelDown()
        {
            this.EasingSetVerticalOffset(this.VerticalOffset + (this.ScrollOffset > 0 ? this.ScrollOffset : this.ChildHeight));
        }

        public void LineLeft()
        {
            throw new InvalidOperationException();
        }

        public void LineRight()
        {
            throw new InvalidOperationException();
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return new Rect();
        }

        public void MouseWheelLeft()
        {
            throw new InvalidOperationException();
        }

        public void MouseWheelRight()
        {
            throw new InvalidOperationException();
        }

        public void PageLeft()
        {
            throw new InvalidOperationException();
        }

        public void PageRight()
        {
            throw new InvalidOperationException();
        }

        public void SetHorizontalOffset(double offset)
        {
            throw new InvalidOperationException();
        }

        public void EasingSetVerticalOffset(double offset)
        {
            AdjustVerticalOffset(ref offset);

            //transAnimation.From = trans.Y;
            //transAnimation.To = -offset;
            //this.trans.BeginAnimation(TranslateTransform.YProperty, transAnimation);
            this.trans.Y = -offset;

            // Force us to realize the correct children
            this.InvalidateMeasure();
        }

        public void SetVerticalOffset(double offset)
        {
            AdjustVerticalOffset(ref offset);

            this.trans.Y = -offset;

            // Force us to realize the correct children
            this.InvalidateMeasure();
        }

        private void AdjustVerticalOffset(ref double offset)
        {
            if (offset < 0 || this.viewport.Height >= this.extent.Height)
            {
                offset = 0;
            }
            else
            {
                if (offset + this.viewport.Height >= this.extent.Height)
                {
                    offset = this.extent.Height - this.viewport.Height;
                }
            }

            this.offset.Y = offset;

            if (this.owner != null)
            {
                this.owner.InvalidateScrollInfo();
            }
        }
    }
}