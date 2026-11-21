namespace Lyt.PhotoPostPro.Model.Messaging;

public sealed record class BookListChangedMessage;

public sealed record class BookPagesChangedMessage(int PageToShow);

public sealed record class BookPageEditedMessage(Page Page);
