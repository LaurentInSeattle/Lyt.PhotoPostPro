namespace Lyt.PhotoPostPro.Workflow.Book.Messages;

using Lyt.PhotoPostPro.Model.PhotoBook.Typography;

using Books = Model.PhotoBook;
using Pages = Model.PhotoBook.Pages;
using Slots = Model.PhotoBook.Slots;

public sealed record class BookGridLinesMessage(bool ShowGridLines);

public sealed record class BookTextSlotSelectedMessage(SlotViewModel? TextSlotViewModel);

public sealed record class BookApplyStyleMessage(SlotViewModel TextSlotViewModel, SlotTextStyle TextStyle);
