namespace Lyt.PhotoPostPro.Workflow.Documentation;

using global::Avalonia.Controls.Presenters;

public partial class DocumentationView : View
{
    private int pdfPageInView = -1;

    public DocumentationView() : base()
        => this.PagesScrollViewer.ScrollChanged += this.OnScrollViewerScrollChanged;

    protected override void OnDataContextChanged(object? sender, EventArgs e) { }

    private void OnScrollViewerScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        void PublishPageNumberIfChanged(DocPageViewModel docPageViewModel)
        {
            int page = docPageViewModel.PageNumber;
            if (page != this.pdfPageInView)
            {
                new PdfPageInViewMessage(page).Publish();
                this.pdfPageInView = page;
            }
        }

        var controls = this.PagesItemControl.GetRealizedContainers().ToList();
        if (controls.Count == 0)
        {
            return;
        }

        if (controls.Count == 1)
        {
            if ((controls[0] is ContentPresenter presenter && presenter.Content is DocPageViewModel docPageViewModel))
            {
                PublishPageNumberIfChanged(docPageViewModel);
            }

            return;
        }

        double bestVisibleRatio = double.MinValue;
        DocPageViewModel? bestDocPageViewModel = null;
        foreach (var control in controls)
        {
            if ((control is not ContentPresenter presenter) ||
                (presenter.Content is not DocPageViewModel docPageViewModel))
            {
                continue;
            }

            var transform = control.TransformToVisual(this.PagesScrollViewer);
            if (transform is null)
            {
                continue;
            }

            var itemRect = new Rect(control.Bounds.Size).TransformToAABB(transform.Value);
            var viewport = this.PagesScrollViewer.Viewport;
            var viewportRect = new Rect(0, 0, viewport.Width, viewport.Height);
            Rect intersection = itemRect.Intersect(viewportRect);
            double visibleRatio =
                (intersection.Width * intersection.Height) / (itemRect.Width * itemRect.Height);
            if (visibleRatio > bestVisibleRatio)
            {
                bestVisibleRatio = visibleRatio;
                bestDocPageViewModel = docPageViewModel;
            }
        }

        if (bestDocPageViewModel is not null)
        {
            PublishPageNumberIfChanged(bestDocPageViewModel);
        }
    }
}