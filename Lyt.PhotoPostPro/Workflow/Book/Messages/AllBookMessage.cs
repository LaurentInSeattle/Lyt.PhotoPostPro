namespace Lyt.PhotoPostPro.Workflow.Book.Messages;

using Lyt.PhotoPostPro.Model.PhotoBook.Typography;

public sealed record class BookGridLinesMessage(bool ShowGridLines);

public sealed record class BookBindingAreaMessage(bool ShowBindingArea);

public sealed record class BookTextSlotSelectedMessage(SlotViewModel? TextSlotViewModel);

public sealed record class BookApplyStyleMessage(SlotViewModel TextSlotViewModel, SlotTextStyle TextStyle);
